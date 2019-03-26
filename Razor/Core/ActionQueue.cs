using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assistant
{
    public delegate void DropDoneCallback(Serial iser, Serial dser, Point3D newPos);

    public class DragDropManager
    {
        public enum ProcStatus
        {
            Nothing,
            Success,
            KeepWaiting,
            ReQueue
        }

        public static bool Debug = false;

        private static Serial m_Holding;
        private static bool m_ClientLiftReq;
        private static DateTime m_Lifted = DateTime.MinValue;

        private static readonly Dictionary<Serial, Queue<DropReq>> m_DropReqs = new Dictionary<Serial, Queue<DropReq>>();

        private static readonly LiftReq[] m_LiftReqs = new LiftReq[256];
        private static byte m_Front, m_Back;

        public static Item Holding { get; private set; }
        public static Serial Pending { get; private set; }

        public static int LastIDLifted { get; private set; }

        public static bool Empty => m_Back == m_Front;
        public static bool Full => (byte) (m_Back + 1) == m_Front;

        private static void Log(string str, params object[] args)
        {
            if (Debug)
            {
                try
                {
                    using (StreamWriter w = new StreamWriter("DragDrop.log", true))
                    {
                        w.Write(Engine.MistedDateTime.ToString("HH:mm:ss.fff"));
                        w.Write(":: ");
                        w.WriteLine(str, args);
                        w.Flush();
                    }
                }
                catch
                {
                }
            }
        }

        public static void Initialize()
        {
            try
            {
                File.Delete("DragDrop.log");
            }
            catch
            {
            }

            HotKey.Add(HKCategory.Misc, LocString.DropCur, DropCurrent);
        }

        private static void DropCurrent()
        {
            Log("Drop current requested on {0}", m_Holding);

            if (m_Holding.IsItem)
            {
                if (World.Player.Backpack != null)
                    ClientCommunication.SendToServer(new DropRequest(m_Holding, Point3D.MinusOne, World.Player.Backpack.Serial));
                else
                    ClientCommunication.SendToServer(new DropRequest(m_Holding, World.Player.Position, Serial.Zero));
            }
            else
                World.Player.SendMessage(MsgLevel.Force, LocString.NoHold);

            Clear();
        }

        public static void Clear()
        {
            Log("Clearing....");

            m_DropReqs.Clear();

            for (int i = 0; i < 256; i++)
                m_LiftReqs[i] = null;
            m_Front = m_Back = 0;
            m_Holding = Pending = Serial.Zero;
            Holding = null;
            m_Lifted = DateTime.MinValue;
        }

        public static void DragDrop(Item i, Serial to)
        {
            Drag(i, i.Amount);
            Drop(i, to, Point3D.MinusOne);
        }

        public static void DragDrop(Item i, Item to)
        {
            Drag(i, i.Amount);
            Drop(i, to.Serial, Point3D.MinusOne);
        }

        public static void DragDrop(Item i, Point3D dest)
        {
            Drag(i, i.Amount);
            Drop(i, Serial.MinusOne, dest);
        }

        public static void DragDrop(Item i, int amount, Item to)
        {
            Drag(i, amount);
            Drop(i, to.Serial, Point3D.MinusOne);
        }

        public static void DragDrop(Item i, Mobile to, Layer layer, bool doLast)
        {
            Drag(i, i.Amount, false, doLast);
            Drop(i, to, layer);
        }

        public static void DragDrop(Item i, Mobile to, Layer layer)
        {
            Drag(i, i.Amount, false);
            Drop(i, to, layer);
        }

        public static int Drag(Item i, int amount, bool fromClient)
        {
            return Drag(i, amount, fromClient, false);
        }

        public static int Drag(Item i, int amount)
        {
            return Drag(i, amount, false, false);
        }

        public static int Drag(Item i, int amount, bool fromClient, bool doLast)
        {
            LiftReq lr = new LiftReq(i.Serial, amount, fromClient, doLast);
            LiftReq prev = null;

            if (Full)
            {
                World.Player.SendMessage(MsgLevel.Error, LocString.DragDropQueueFull);

                if (fromClient)
                    ClientCommunication.SendToClient(new LiftRej());

                return 0;
            }

            Log("Queuing Drag request {0}", lr);

            if (m_Back >= m_LiftReqs.Length)
                m_Back = 0;

            if (m_Back <= 0)
                prev = m_LiftReqs[m_LiftReqs.Length - 1];
            else if (m_Back <= m_LiftReqs.Length)
                prev = m_LiftReqs[m_Back - 1];

            // if the current last req must stay last, then insert this one in its place
            if (prev != null && prev.DoLast)
            {
                Log("Back-Queuing {0}", prev);

                if (m_Back <= 0)
                    m_LiftReqs[m_LiftReqs.Length - 1] = lr;
                else if (m_Back <= m_LiftReqs.Length)
                    m_LiftReqs[m_Back - 1] = lr;

                // and then re-insert it at the end
                lr = prev;
            }

            m_LiftReqs[m_Back++] = lr;

            ActionQueue.SignalLift(!fromClient);

            return lr.Id;
        }

        public static bool Drop(Item i, Mobile to, Layer layer)
        {
            if (Pending == i.Serial)
            {
                Log("Equipping {0} to {1} (@{2})", i, to.Serial, layer);
                ClientCommunication.SendToServer(new EquipRequest(i.Serial, to, layer));
                Pending = Serial.Zero;
                m_Lifted = DateTime.MinValue;

                return true;
            }

            bool add = false;

            for (byte j = m_Front; j != m_Back && !add; j++)
            {
                if (m_LiftReqs[j] != null && m_LiftReqs[j].Serial == i.Serial)
                {
                    add = true;

                    break;
                }
            }

            if (add)
            {
                Log("Queuing Equip {0} to {1} (@{2})", i, to.Serial, layer);

                if (!m_DropReqs.TryGetValue(i.Serial, out var q) || q == null)
                    m_DropReqs[i.Serial] = q = new Queue<DropReq>();

                q.Enqueue(new DropReq(to == null ? Serial.Zero : to.Serial, layer));

                return true;
            }

            Log("Drop/Equip for {0} (to {1} (@{2})) not found, skipped", i, to == null ? Serial.Zero : to.Serial, layer);

            return false;
        }

        public static bool Drop(Item i, Serial dest, Point3D pt)
        {
            if (Pending == i.Serial)
            {
                Log("Dropping {0} to {1} (@{2})", i, dest, pt);

                ClientCommunication.SendToServer(new DropRequest(i.Serial, pt, dest));
                Pending = Serial.Zero;
                m_Lifted = DateTime.MinValue;

                return true;
            }

            bool add = false;

            for (byte j = m_Front; j != m_Back && !add; j++)
            {
                if (m_LiftReqs[j] != null && m_LiftReqs[j].Serial == i.Serial)
                {
                    add = true;

                    break;
                }
            }

            if (add)
            {
                Log("Queuing Drop {0} (to {1} (@{2}))", i, dest, pt);

                if (!m_DropReqs.TryGetValue(i.Serial, out var q) || q == null)
                    m_DropReqs[i.Serial] = q = new Queue<DropReq>();

                q.Enqueue(new DropReq(dest, pt));

                return true;
            }

            Log("Drop for {0} (to {1} (@{2})) not found, skipped", i, dest, pt);

            return false;
        }

        public static bool Drop(Item i, Item to, Point3D pt)
        {
            return Drop(i, to == null ? Serial.MinusOne : to.Serial, pt);
        }

        public static bool Drop(Item i, Item to)
        {
            return Drop(i, to.Serial, Point3D.MinusOne);
        }

        public static bool LiftReject()
        {
            Log("Server rejected lift for item {0}", m_Holding);

            if (m_Holding == Serial.Zero)
                return true;

            m_Holding = Pending = Serial.Zero;
            Holding = null;
            m_Lifted = DateTime.MinValue;

            return m_ClientLiftReq;
        }

        public static bool HasDragFor(Serial s)
        {
            for (byte j = m_Front; j != m_Back; j++)
            {
                if (m_LiftReqs[j] != null && m_LiftReqs[j].Serial == s)
                    return true;
            }

            return false;
        }

        public static bool CancelDragFor(Serial s)
        {
            if (Empty)
                return false;

            int skip = 0;

            for (byte j = m_Front; j != m_Back; j++)
            {
                if (skip == 0 && m_LiftReqs[j] != null && m_LiftReqs[j].Serial == s)
                {
                    m_LiftReqs[j] = null;
                    skip++;

                    if (j == m_Front)
                    {
                        m_Front++;

                        break;
                    }

                    m_Back--;
                }

                if (skip > 0)
                    m_LiftReqs[j] = m_LiftReqs[(byte) (j + skip)];
            }

            if (skip > 0)
            {
                m_LiftReqs[m_Back] = null;

                return true;
            }

            return false;
        }

        public static bool EndHolding(Serial s)
        {
            //if ( m_Pending == s )
            //	return false;

            if (m_Holding == s)
            {
                m_Holding = Serial.Zero;
                Holding = null;
            }

            return true;
        }

        private static DropReq DequeueDropFor(Serial s)
        {
            DropReq dr = null;

            if (m_DropReqs.TryGetValue(s, out var q) && q != null)
            {
                if (q.Count > 0)
                    dr = q.Dequeue();

                if (q.Count <= 0)
                    m_DropReqs.Remove(s);
            }

            return dr;
        }

        public static void GracefulStop()
        {
            m_Front = m_Back = 0;

            if (Pending.IsValid)
            {
                m_DropReqs.TryGetValue(Pending, out var q);
                m_DropReqs.Clear();
                m_DropReqs[Pending] = q;
            }
        }

        public static ProcStatus ProcessNext(int numPending)
        {
            if (Pending != Serial.Zero)
            {
                if (m_Lifted + TimeSpan.FromMinutes(2) < DateTime.UtcNow)
                {
                    Item i = World.FindItem(Pending);

                    Log("Lift timeout, forced drop to pack for {0}", Pending);

                    if (World.Player != null)
                    {
                        World.Player.SendMessage(MsgLevel.Force, LocString.ForceEndHolding);

                        if (World.Player.Backpack != null)
                            ClientCommunication.SendToServer(new DropRequest(Pending, Point3D.MinusOne, World.Player.Backpack.Serial));
                        else
                            ClientCommunication.SendToServer(new DropRequest(Pending, World.Player.Position, Serial.Zero));
                    }

                    m_Holding = Pending = Serial.Zero;
                    Holding = null;
                    m_Lifted = DateTime.MinValue;
                }
                else
                    return ProcStatus.KeepWaiting;
            }

            if (m_Front == m_Back)
            {
                m_Front = m_Back = 0;

                return ProcStatus.Nothing;
            }

            LiftReq lr = m_LiftReqs[m_Front];

            if (numPending > 0 && lr != null && lr.DoLast)
                return ProcStatus.ReQueue;

            m_LiftReqs[m_Front] = null;
            m_Front++;

            if (lr != null)
            {
                Log("Lifting {0}", lr);

                Item item = World.FindItem(lr.Serial);

                if (item != null && item.Container == null)
                {
                    // if the item is on the ground and out of range then dont grab it
                    if (Utility.Distance(item.GetWorldPosition(), World.Player.Position) > 3)
                    {
                        Log("Item is too far away... uncaching.");
                        ScavengerAgent.Instance.Uncache(item.Serial);

                        return ProcStatus.Nothing;
                    }
                }

                ClientCommunication.SendToServer(new LiftRequest(lr.Serial, lr.Amount));

                LastIDLifted = lr.Id;
                m_Holding = lr.Serial;
                Holding = World.FindItem(lr.Serial);
                m_ClientLiftReq = lr.FromClient;

                DropReq dr = DequeueDropFor(lr.Serial);

                if (dr != null)
                {
                    Pending = Serial.Zero;
                    m_Lifted = DateTime.MinValue;

                    Log("Dropping {0} to {1}", lr, dr.Serial);

                    if (dr.Serial.IsMobile && dr.Layer > Layer.Invalid && dr.Layer <= Layer.LastUserValid)
                        ClientCommunication.SendToServer(new EquipRequest(lr.Serial, dr.Serial, dr.Layer));
                    else
                        ClientCommunication.SendToServer(new DropRequest(lr.Serial, dr.Point, dr.Serial));
                }
                else
                {
                    Pending = lr.Serial;
                    m_Lifted = DateTime.UtcNow;
                }

                return ProcStatus.Success;
            }

            Log("No lift to be done?!");

            return ProcStatus.Nothing;
        }

        private class LiftReq
        {
            private static int NextID = 1;
            public readonly int Amount;
            public readonly bool DoLast;
            public readonly bool FromClient;
            public readonly int Id;

            public readonly Serial Serial;

            public LiftReq(Serial s, int a, bool cli, bool last)
            {
                Serial = s;
                Amount = a;
                FromClient = cli;
                DoLast = last;
                Id = NextID++;
            }

            public override string ToString()
            {
                return string.Format("{2}({0},{1},{3},{4})", Serial, Amount, Id, FromClient, DoLast);
            }
        }

        private class DropReq
        {
            public readonly Layer Layer;
            public readonly Point3D Point;

            public readonly Serial Serial;

            public DropReq(Serial s, Point3D pt)
            {
                Serial = s;
                Point = pt;
            }

            public DropReq(Serial s, Layer layer)
            {
                Serial = s;
                Layer = layer;
            }
        }
    }

    public class ActionQueue
    {
        private static Serial m_Last = Serial.Zero;
        private static readonly Queue m_Queue = new Queue();
        private static readonly ProcTimer m_Timer = new ProcTimer();
        private static int m_Total;

        public static bool Empty => m_Queue.Count <= 0 && !m_Timer.Running;

        public static string TimeLeft
        {
            get
            {
                if (m_Timer.Running)
                {
                    //Config.GetBool("ObjectDelayEnabled")
                    //double time = Config.GetInt( "ObjectDelay" ) / 1000.0;

                    double time = Config.GetInt("ObjectDelay") / 1000.0;

                    if (!Config.GetBool("ObjectDelayEnabled")) time = 0;

                    double init = 0;

                    if (m_Timer.LastTick != DateTime.MinValue)
                        init = time - (DateTime.UtcNow - m_Timer.LastTick).TotalSeconds;
                    time = init + time * m_Queue.Count;

                    if (time < 0)
                        time = 0;

                    return string.Format("{0:F1} seconds", time);
                }

                return "0.0 seconds";
            }
        }

        public static void DoubleClick(bool silent, Serial s)
        {
            if (s != Serial.Zero)
            {
                if (m_Last != s)
                {
                    m_Queue.Enqueue(s);
                    m_Last = s;
                    m_Total++;

                    if (m_Queue.Count == 1 && !m_Timer.Running)
                        m_Timer.StartMe();
                    else if (!silent && m_Total > 1)
                        World.Player.SendMessage(LocString.ActQueued, m_Queue.Count, TimeLeft);
                }
                else if (!silent) World.Player.SendMessage(LocString.QueueIgnore);
            }
        }

        public static void SignalLift(bool silent)
        {
            m_Queue.Enqueue(Serial.Zero);
            m_Total++;

            if ( /*m_Queue.Count == 1 &&*/ !m_Timer.Running)
                m_Timer.StartMe();
            else if (!silent && m_Total > 1)
                World.Player.SendMessage(LocString.LiftQueued, m_Queue.Count, TimeLeft);
        }

        public static void Stop()
        {
            if (m_Timer != null && m_Timer.Running)
                m_Timer.Stop();
            m_Queue.Clear();
            DragDropManager.Clear();
        }

        private class ProcTimer : Timer
        {
            private DateTime m_StartTime;

            public ProcTimer() : base(TimeSpan.Zero, TimeSpan.Zero)
            {
            }

            public DateTime LastTick { get; private set; }

            public void StartMe()
            {
                LastTick = DateTime.UtcNow;
                m_StartTime = DateTime.UtcNow;

                OnTick();

                Delay = Interval;

                Start();
            }

            protected override void OnTick()
            {
                ArrayList requeue = null;

                LastTick = DateTime.UtcNow;

                if (m_Queue != null && m_Queue.Count > 0)
                {
                    Interval = TimeSpan.FromMilliseconds(Config.GetBool("ObjectDelayEnabled") ? Config.GetInt("ObjectDelay") : 0);

                    //this.Interval = TimeSpan.FromMilliseconds( Config.GetInt( "ObjectDelay" ) );

                    while (m_Queue.Count > 0)
                    {
                        Serial s = (Serial) m_Queue.Peek();

                        if (s == Serial.Zero) // dragdrop action
                        {
                            DragDropManager.ProcStatus status = DragDropManager.ProcessNext(m_Queue.Count - 1);

                            if (status != DragDropManager.ProcStatus.KeepWaiting)
                            {
                                m_Queue.Dequeue(); // if not waiting then dequeue it

                                if (status == DragDropManager.ProcStatus.ReQueue)
                                    m_Queue.Enqueue(s);
                            }

                            if (status == DragDropManager.ProcStatus.KeepWaiting || status == DragDropManager.ProcStatus.Success)
                                break; // don't process more if we're waiting or we just processed something
                        }
                        else
                        {
                            m_Queue.Dequeue();
                            ClientCommunication.SendToServer(new DoubleClick(s));

                            break;
                        }
                    }

                    if (requeue != null)
                    {
                        for (int i = 0; i < requeue.Count; i++)
                            m_Queue.Enqueue(requeue[i]);
                    }
                }
                else
                {
                    Stop();

                    if (m_Total > 1 && World.Player != null)
                        World.Player.SendMessage(LocString.QueueFinished, m_Total, (DateTime.UtcNow - m_StartTime - Interval).TotalSeconds);

                    m_Last = Serial.Zero;
                    m_Total = 0;
                }
            }
        }
    }
}