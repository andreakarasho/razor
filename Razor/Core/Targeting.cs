using System;
using System.Collections.Generic;

using Assistant.Macros;

namespace Assistant
{
    public class TargetInfo
    {
        public byte Flags;
        public ushort Gfx;
        public Serial Serial;
        public uint TargID;
        public byte Type;
        public int X, Y;
        public int Z;
    }

    public class Targeting
    {
        public delegate void CancelTargetCallback();

        public delegate void TargetResponseCallback(bool location, Serial serial, Point3D p, ushort gfxid);

        public const uint LocalTargID = 0x7FFFFFFF; // uid for target sent from razor

        private static CancelTargetCallback m_OnCancel;
        private static TargetResponseCallback m_OnTarget;

        private static bool m_Intercept;
        private static bool m_ClientTarget;
        private static TargetInfo m_LastTarget;
        private static TargetInfo m_LastGroundTarg;
        private static TargetInfo m_LastBeneTarg;
        private static TargetInfo m_LastHarmTarg;

        private static bool m_AllowGround;
        private static uint m_CurrentID;
        private static byte m_CurFlags;

        private static uint m_PreviousID;
        private static bool m_PreviousGround;
        private static byte m_PrevFlags;

        private static Serial m_LastCombatant;
        private static readonly QueueTarget TargetSelfAction = DoTargetSelf;
        private static readonly QueueTarget LastTargetAction = DoLastTarget;
        private static QueueTarget m_QueueTarget;


        private static readonly List<uint> m_FilterCancel = new List<uint>();

        private static List<ushort> m_MonsterIds = new List<ushort>
        {
            0x1, 0x2, 0x3, 0x4, 0x7, 0x8, 0x9, 0xC, 0xD, 0xE, 0xF,
            0x10, 0x11, 0x12, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C,
            0x1E, 0x1F, 0x21, 0x23, 0x24, 0x25, 0x27, 0x29, 0x2A, 0x2C,
            0x2D, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x3B, 0x3C, 0x3D, 0x42, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4B, 0x4F, 0x50, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x59, 0x5A,
            0x5B, 0x5C, 0x5D, 0x5E, 0x60, 0x61, 0x62, 0x69, 0x6A, 0x6B, 0x6C,
            0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x87, 0x88, 0x89,
            0x8A, 0x8B, 0x8C, 0x8E, 0x8F, 0x91, 0x93, 0x96, 0x99, 0x9B, 0x9E,
            0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xB4, 0x4C, 0x4D, 0x3D
        };

        private static Serial m_OldLT = Serial.Zero;

        private static readonly TimerCallbackState m_OneTimeRespCallback = OneTimeResponse;

        private static int m_NextPrevTargIdx;

        private static int m_NextPrevTargHumanoidIdx;

        private static int m_NextPrevTargEnemyHumanoidIdx;
        public static uint SpellTargetID { get; set; }

        public static bool HasTarget { get; private set; }

        public static bool LTWasSet { get; private set; }

        public static void Initialize()
        {
            PacketHandler.RegisterClientToServerViewer(0x6C, TargetResponse);
            PacketHandler.RegisterServerToClientViewer(0x6C, NewTarget);
            PacketHandler.RegisterServerToClientViewer(0xAA, CombatantChange);

            HotKey.Add(HKCategory.Targets, LocString.LastTarget, LastTarget);
            HotKey.Add(HKCategory.Targets, LocString.TargetSelf, TargetSelf);
            HotKey.Add(HKCategory.Targets, LocString.ClearTargQueue, OnClearQueue);
            HotKey.Add(HKCategory.Targets, LocString.SetLT, TargetSetLastTarget);
            HotKey.Add(HKCategory.Targets, LocString.TargRandRed, TargetRandRed);
            HotKey.Add(HKCategory.Targets, LocString.TargRandNFriend, TargetRandNonFriendly);
            HotKey.Add(HKCategory.Targets, LocString.TargRandFriend, TargetRandFriendly);
            HotKey.Add(HKCategory.Targets, LocString.TargRandBlue, TargetRandInnocent);
            HotKey.Add(HKCategory.Targets, LocString.TargRandGrey, TargetRandGrey);
            HotKey.Add(HKCategory.Targets, LocString.TargRandGreyMonster, TargetRandGreyMonster);
            HotKey.Add(HKCategory.Targets, LocString.TargRandEnemy, TargetRandEnemy);
            HotKey.Add(HKCategory.Targets, LocString.TargRandEnemyMonster, TargetRandEnemyMonster);
            HotKey.Add(HKCategory.Targets, LocString.TargRandCriminal, TargetRandCriminal);

            HotKey.Add(HKCategory.Targets, LocString.TargRandEnemyHuman, TargetRandEnemyHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.TargRandGreyHuman, TargetRandGreyHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.TargRandInnocentHuman, TargetRandInnocentHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.TargRandCriminalHuman, TargetRandCriminalHumanoid);

            HotKey.Add(HKCategory.Targets, LocString.AttackLastComb, AttackLastComb);
            HotKey.Add(HKCategory.Targets, LocString.AttackLastTarg, AttackLastTarg);
            HotKey.Add(HKCategory.Targets, LocString.CancelTarget, CancelTarget);

            HotKey.Add(HKCategory.Targets, LocString.NextTarget, NextTarget);
            HotKey.Add(HKCategory.Targets, LocString.NextTargetEnemyHumanoid, NextTargetEnemyHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.NextTargetHumanoid, NextTargetHumanoid);

            HotKey.Add(HKCategory.Targets, LocString.PrevTarget, PrevTarget);
            HotKey.Add(HKCategory.Targets, LocString.PrevTargetEnemyHumanoid, PrevTargetEnemyHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.PrevTargetHumanoid, PrevTargetHumanoid);

            HotKey.Add(HKCategory.Targets, LocString.TargCloseRed, TargetCloseRed);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseNFriend, TargetCloseNonFriendly);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseFriend, TargetCloseFriendly);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseBlue, TargetCloseInnocent);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseGrey, TargetCloseGrey);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseGreyMonster, TargetCloseGreyMonster);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseEnemy, TargetCloseEnemy);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseCriminal, TargetCloseCriminal);

            HotKey.Add(HKCategory.Targets, LocString.TargCloseEnemyHuman, TargetCloseEnemyHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseEnemyMonster, TargetCloseEnemyMonster);

            HotKey.Add(HKCategory.Targets, LocString.TargCloseGreyHuman, TargetCloseGreyHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseInnocentHuman, TargetCloseInnocentHumanoid);
            HotKey.Add(HKCategory.Targets, LocString.TargCloseCriminalHuman, TargetCloseCriminalHumanoid);
        }

        private static void CombatantChange(PacketReader p, PacketHandlerEventArgs e)
        {
            Serial ser = p.ReadUInt32();

            if (ser.IsMobile && ser != World.Player.Serial && ser != Serial.Zero && ser != Serial.MinusOne)
                m_LastCombatant = ser;
        }

        private static void AttackLastComb()
        {
            if (m_LastCombatant.IsMobile)
                ClientCommunication.SendToServer(new AttackReq(m_LastCombatant));
        }

        private static void AttackLastTarg()
        {
            if (m_LastTarget != null && m_LastTarget.Serial.IsMobile)
                ClientCommunication.SendToServer(new AttackReq(m_LastTarget.Serial));
        }

        private static void OnClearQueue()
        {
            ClearQueue();

            if (Config.GetBool("ShowTargetSelfLastClearOverhead"))
                World.Player.OverheadMessage(LocString.TQCleared);
            else
                World.Player.SendMessage(MsgLevel.Force, LocString.TQCleared);
        }

        internal static void OneTimeTarget(TargetResponseCallback onTarget)
        {
            OneTimeTarget(false, onTarget, null);
        }

        internal static void OneTimeTarget(bool ground, TargetResponseCallback onTarget)
        {
            OneTimeTarget(ground, onTarget, null);
        }

        internal static void OneTimeTarget(TargetResponseCallback onTarget, CancelTargetCallback onCancel)
        {
            OneTimeTarget(false, onTarget, onCancel);
        }

        internal static void OneTimeTarget(bool ground, TargetResponseCallback onTarget, CancelTargetCallback onCancel)
        {
            if (m_Intercept && m_OnCancel != null)
            {
                m_OnCancel();
                CancelOneTimeTarget();
            }

            if (HasTarget && m_CurrentID != 0 && m_CurrentID != LocalTargID)
            {
                m_PreviousID = m_CurrentID;
                m_PreviousGround = m_AllowGround;
                m_PrevFlags = m_CurFlags;

                m_FilterCancel.Add(m_PreviousID);
            }

            m_Intercept = true;
            m_CurrentID = LocalTargID;
            m_OnTarget = onTarget;
            m_OnCancel = onCancel;

            m_ClientTarget = HasTarget = true;
            ClientCommunication.SendToClient(new Target(LocalTargID, ground));
            ClearQueue();
        }

        internal static void CancelOneTimeTarget()
        {
            m_ClientTarget = HasTarget = false;

            ClientCommunication.SendToClient(new CancelTarget(LocalTargID));
            EndIntercept();
        }

        public static void TargetSetLastTarget()
        {
            if (World.Player != null)
            {
                LTWasSet = false;
                OneTimeTarget(false, OnSetLastTarget, OnSLTCancel);
                World.Player.SendMessage(MsgLevel.Force, LocString.TargSetLT);
            }
        }

        private static void OnSLTCancel()
        {
            if (m_LastTarget != null)
                LTWasSet = true;
        }

        private static void OnSetLastTarget(bool location, Serial serial, Point3D p, ushort gfxid)
        {
            if (serial == World.Player.Serial)
            {
                OnSLTCancel();

                return;
            }

            m_LastBeneTarg = m_LastHarmTarg = m_LastGroundTarg = m_LastTarget = new TargetInfo();
            m_LastTarget.Flags = 0;
            m_LastTarget.Gfx = gfxid;
            m_LastTarget.Serial = serial;
            m_LastTarget.Type = (byte) (location ? 1 : 0);
            m_LastTarget.X = p.X;
            m_LastTarget.Y = p.Y;
            m_LastTarget.Z = p.Z;

            LTWasSet = true;

            World.Player.SendMessage(MsgLevel.Force, LocString.LTSet);

            if (serial.IsMobile)
            {
                LastTargetChanged();
                ClientCommunication.SendToClient(new ChangeCombatant(serial));
                m_LastCombatant = serial;
            }
        }

        private static void RemoveTextFlags(UOEntity m)
        {
            if (m != null)
            {
                bool oplchanged = false;

                oplchanged |= m.ObjPropList.Remove(Language.GetString(LocString.LastTarget));
                oplchanged |= m.ObjPropList.Remove(Language.GetString(LocString.HarmfulTarget));
                oplchanged |= m.ObjPropList.Remove(Language.GetString(LocString.BeneficialTarget));

                if (oplchanged)
                    m.OPLChanged();
            }
        }

        private static void AddTextFlags(UOEntity m)
        {
            if (m != null)
            {
                bool oplchanged = false;

                if (Config.GetBool("SmartLastTarget"))
                {
                    if (m_LastHarmTarg != null && m_LastHarmTarg.Serial == m.Serial)
                    {
                        oplchanged = true;
                        m.ObjPropList.Add(Language.GetString(LocString.HarmfulTarget));
                    }

                    if (m_LastBeneTarg != null && m_LastBeneTarg.Serial == m.Serial)
                    {
                        oplchanged = true;
                        m.ObjPropList.Add(Language.GetString(LocString.BeneficialTarget));
                    }
                }

                if (!oplchanged && m_LastTarget != null && m_LastTarget.Serial == m.Serial)
                {
                    oplchanged = true;
                    m.ObjPropList.Add(Language.GetString(LocString.LastTarget));
                }

                if (oplchanged)
                    m.OPLChanged();
            }
        }

        private static void LastTargetChanged()
        {
            if (m_LastTarget != null)
            {
                bool lth = Config.GetInt("LTHilight") != 0;

                if (m_OldLT.IsItem)
                    RemoveTextFlags(World.FindItem(m_OldLT));
                else
                {
                    Mobile m = World.FindMobile(m_OldLT);

                    if (m != null)
                    {
                        if (lth)
                            ClientCommunication.SendToClient(new MobileIncoming(m));

                        RemoveTextFlags(m);
                    }
                }

                if (m_LastTarget.Serial.IsItem)
                    AddTextFlags(World.FindItem(m_LastTarget.Serial));
                else
                {
                    Mobile m = World.FindMobile(m_LastTarget.Serial);

                    if (m != null)
                    {
                        if (IsLastTarget(m) && lth)
                            ClientCommunication.SendToClient(new MobileIncoming(m));

                        CheckLastTargetRange(m);

                        AddTextFlags(m);
                    }
                }

                m_OldLT = m_LastTarget.Serial;
            }
        }

        public static void TargetRandNonFriendly()
        {
            RandomTarget(3, 4, 5, 6);
        }

        public static void TargetRandFriendly()
        {
            RandomTarget(0, 1, 2);
        }

        public static void TargetRandEnemy()
        {
            RandomTarget(5);
        }

        public static void TargetRandEnemyMonster()
        {
            RandomMonsterTarget(5);
        }

        public static void TargetRandEnemyHumanoid()
        {
            RandomHumanoidTarget(5);
        }

        public static void TargetRandRed()
        {
            RandomTarget(6);
        }

        public static void TargetRandGrey()
        {
            RandomTarget(3, 4);
        }

        public static void TargetRandGreyMonster()
        {
            RandomMonsterTarget(3, 4);
        }

        public static void TargetRandGreyHumanoid()
        {
            RandomHumanoidTarget(3, 4);
        }

        public static void TargetRandCriminal()
        {
            RandomTarget(4);
        }

        public static void TargetRandCriminalHumanoid()
        {
            RandomHumanoidTarget(4);
        }

        public static void TargetRandInnocent()
        {
            RandomTarget(1);
        }

        public static void TargetRandInnocentHumanoid()
        {
            RandomHumanoidTarget(1);
        }

        public static void TargetRandAnyone()
        {
            RandomTarget();
        }

        public static void RandomTarget(params int[] noto)
        {
            if (!Windows.AllowBit(FeatureBit.RandomTargets))
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in World.MobilesInRange(12))
            {
                if ((!FriendsAgent.IsFriend(m) || noto.Length > 0 && noto[0] == 0) &&
                    !m.Blessed && !m.IsGhost && m.Serial != World.Player.Serial &&
                    Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                {
                    for (int i = 0; i < noto.Length; i++)
                    {
                        if (noto[i] == m.Notoriety)
                        {
                            list.Add(m);

                            break;
                        }
                    }

                    if (noto.Length == 0)
                        list.Add(m);
                }
            }

            if (list.Count > 0)
                SetLastTargetTo(list[Utility.Random(list.Count)]);
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);
        }

        public static void RandomHumanoidTarget(params int[] noto)
        {
            if (!Windows.AllowBit(FeatureBit.RandomTargets))
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in World.MobilesInRange(12))
            {
                if (m.Body != 0x0190 && m.Body != 0x0191 && m.Body != 0x025D && m.Body != 0x025E)
                    continue;

                if ((!FriendsAgent.IsFriend(m) || noto.Length > 0 && noto[0] == 0) &&
                    !m.Blessed && !m.IsGhost && m.Serial != World.Player.Serial &&
                    Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                {
                    for (int i = 0; i < noto.Length; i++)
                    {
                        if (noto[i] == m.Notoriety)
                        {
                            list.Add(m);

                            break;
                        }
                    }

                    if (noto.Length == 0)
                        list.Add(m);
                }
            }

            if (list.Count > 0)
                SetLastTargetTo(list[Utility.Random(list.Count)]);
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);
        }

        public static void RandomMonsterTarget(params int[] noto)
        {
            if (!Windows.AllowBit(FeatureBit.RandomTargets))
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in World.MobilesInRange(12))
            {
                if (!m.IsMonster)
                    continue;

                if ((!FriendsAgent.IsFriend(m) || noto.Length > 0 && noto[0] == 0) &&
                    !m.Blessed && !m.IsGhost && m.Serial != World.Player.Serial &&
                    Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                {
                    for (int i = 0; i < noto.Length; i++)
                    {
                        if (noto[i] == m.Notoriety)
                        {
                            list.Add(m);

                            break;
                        }
                    }

                    if (noto.Length == 0)
                        list.Add(m);
                }
            }

            if (list.Count > 0)
                SetLastTargetTo(list[Utility.Random(list.Count)]);
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);
        }


        public static void TargetCloseNonFriendly()
        {
            ClosestTarget(3, 4, 5, 6);
        }

        public static void TargetCloseFriendly()
        {
            ClosestTarget(0, 1, 2);
        }

        public static void TargetCloseEnemy()
        {
            ClosestTarget(5);
        }

        public static void TargetCloseEnemyHumanoid()
        {
            ClosestHumanoidTarget(5);
        }

        public static void TargetCloseEnemyMonster()
        {
            ClosestMonsterTarget(5);
        }

        public static void TargetCloseRed()
        {
            ClosestTarget(6);
        }

        public static void TargetCloseGrey()
        {
            ClosestTarget(3, 4);
        }

        public static void TargetCloseGreyMonster()
        {
            ClosestMonsterTarget(3, 4);
        }

        public static void TargetCloseGreyHumanoid()
        {
            ClosestHumanoidTarget(3, 4);
        }

        public static void TargetCloseCriminal()
        {
            ClosestTarget(4);
        }

        public static void TargetCloseCriminalHumanoid()
        {
            ClosestHumanoidTarget(4);
        }

        public static void TargetCloseInnocent()
        {
            ClosestTarget(1);
        }

        public static void TargetCloseInnocentHumanoid()
        {
            ClosestHumanoidTarget(1);
        }

        public static void TargetClosest()
        {
            ClosestTarget();
        }

        public static void ClosestTarget(params int[] noto)
        {
            if (!Windows.AllowBit(FeatureBit.ClosestTargets))
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in World.MobilesInRange(12))
            {
                if ((!FriendsAgent.IsFriend(m) || noto.Length > 0 && noto[0] == 0) &&
                    !m.Blessed && !m.IsGhost && m.Serial != World.Player.Serial &&
                    Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                {
                    for (int i = 0; i < noto.Length; i++)
                    {
                        if (noto[i] == m.Notoriety)
                        {
                            list.Add(m);

                            break;
                        }
                    }

                    if (noto.Length == 0)
                        list.Add(m);
                }
            }

            Mobile closest = null;
            double closestDist = double.MaxValue;

            foreach (Mobile m in list)
            {
                double dist = Utility.DistanceSqrt(m.Position, World.Player.Position);

                if (dist < closestDist || closest == null)
                {
                    closestDist = dist;
                    closest = m;
                }
            }

            if (closest != null)
                SetLastTargetTo(closest);
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);
        }

        public static void ClosestHumanoidTarget(params int[] noto)
        {
            if (!Windows.AllowBit(FeatureBit.ClosestTargets))
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in World.MobilesInRange(12))
            {
                if (m.Body != 0x0190 && m.Body != 0x0191 && m.Body != 0x025D && m.Body != 0x025E)
                    continue;

                if ((!FriendsAgent.IsFriend(m) || noto.Length > 0 && noto[0] == 0) &&
                    !m.Blessed && !m.IsGhost && m.Serial != World.Player.Serial &&
                    Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                {
                    for (int i = 0; i < noto.Length; i++)
                    {
                        if (noto[i] == m.Notoriety)
                        {
                            list.Add(m);

                            break;
                        }
                    }

                    if (noto.Length == 0)
                        list.Add(m);
                }
            }

            Mobile closest = null;
            double closestDist = double.MaxValue;

            foreach (Mobile m in list)
            {
                double dist = Utility.DistanceSqrt(m.Position, World.Player.Position);

                if (dist < closestDist || closest == null)
                {
                    closestDist = dist;
                    closest = m;
                }
            }

            if (closest != null)
                SetLastTargetTo(closest);
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);
        }

        public static void ClosestMonsterTarget(params int[] noto)
        {
            if (!Windows.AllowBit(FeatureBit.ClosestTargets))
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in World.MobilesInRange(12))
            {
                if (!m.IsMonster)
                    continue;

                if ((!FriendsAgent.IsFriend(m) || noto.Length > 0 && noto[0] == 0) &&
                    !m.Blessed && !m.IsGhost && m.Serial != World.Player.Serial &&
                    Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                {
                    for (int i = 0; i < noto.Length; i++)
                    {
                        if (noto[i] == m.Notoriety)
                        {
                            list.Add(m);

                            break;
                        }
                    }

                    if (noto.Length == 0)
                        list.Add(m);
                }
            }

            Mobile closest = null;
            double closestDist = double.MaxValue;

            foreach (Mobile m in list)
            {
                double dist = Utility.DistanceSqrt(m.Position, World.Player.Position);

                if (dist < closestDist || closest == null)
                {
                    closestDist = dist;
                    closest = m;
                }
            }

            if (closest != null)
                SetLastTargetTo(closest);
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);
        }

        public static void SetLastTargetTo(Mobile m)
        {
            SetLastTargetTo(m, 0);
        }

        public static void SetLastTargetTo(Mobile m, byte flagType)
        {
            TargetInfo targ = new TargetInfo();
            m_LastGroundTarg = m_LastTarget = targ;

            if (HasTarget && m_CurFlags == 1 || flagType == 1)
                m_LastHarmTarg = targ;
            else if (HasTarget && m_CurFlags == 2 || flagType == 2)
                m_LastBeneTarg = targ;
            else if (flagType == 0)
                m_LastHarmTarg = m_LastBeneTarg = targ;

            targ.Type = 0;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Flags = flagType;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);

            bool wasSmart = Config.GetBool("SmartLastTarget");

            if (wasSmart)
                Config.SetProperty("SmartLastTarget", false);
            LastTarget();

            if (wasSmart)
                Config.SetProperty("SmartLastTarget", true);
            LastTargetChanged();
        }

        private static void EndIntercept()
        {
            m_Intercept = false;
            m_OnTarget = null;
            m_OnCancel = null;
        }

        public static void TargetSelf()
        {
            TargetSelf(false);
        }

        public static void TargetSelf(bool forceQ)
        {
            if (World.Player == null)
                return;

            //if ( Macros.MacroManager.AcceptActions )
            //	MacroManager.Action( new TargetSelfAction() );

            if (HasTarget)
            {
                if (!DoTargetSelf())
                    ResendTarget();
            }
            else if (forceQ || Config.GetBool("QueueTargets"))
            {
                if (!forceQ)
                {
                    if (Config.GetBool("ShowTargetSelfLastClearOverhead"))
                        World.Player.OverheadMessage(LocString.QueuedTS);
                    else
                        World.Player.SendMessage(MsgLevel.Force, LocString.QueuedTS);
                }

                m_QueueTarget = TargetSelfAction;
            }
        }

        public static bool DoTargetSelf()
        {
            if (World.Player == null)
                return false;

            if (CheckHealPoisonTarg(m_CurrentID, World.Player.Serial))
                return false;

            CancelClientTarget();
            HasTarget = false;

            if (m_Intercept)
            {
                TargetInfo targ = new TargetInfo();
                targ.Serial = World.Player.Serial;
                targ.Gfx = World.Player.Body;
                targ.Type = 0;
                targ.X = World.Player.Position.X;
                targ.Y = World.Player.Position.Y;
                targ.Z = World.Player.Position.Z;
                targ.TargID = LocalTargID;
                targ.Flags = 0;

                OneTimeResponse(targ);
            }
            else
                ClientCommunication.SendToServer(new TargetResponse(m_CurrentID, World.Player));

            return true;
        }

        public static void LastTarget()
        {
            LastTarget(false);
        }

        public static void LastTarget(bool forceQ)
        {
            //if ( Macros.MacroManager.AcceptActions )
            //	MacroManager.Action( new LastTargetAction() );

            if (HasTarget)
            {
                if (!DoLastTarget())
                    ResendTarget();
            }
            else if (forceQ || Config.GetBool("QueueTargets"))
            {
                if (!forceQ)
                {
                    if (Config.GetBool("ShowTargetSelfLastClearOverhead"))
                        World.Player.OverheadMessage(LocString.QueuedLT);
                    else
                        World.Player.SendMessage(MsgLevel.Force, LocString.QueuedLT);
                }

                m_QueueTarget = LastTargetAction;
            }
        }

        public static bool DoLastTarget()
        {
            TargetInfo targ;

            if (Config.GetBool("SmartLastTarget") && Windows.AllowBit(FeatureBit.SmartLT))
            {
                if (m_AllowGround && m_LastGroundTarg != null)
                    targ = m_LastGroundTarg;
                else if (m_CurFlags == 1)
                    targ = m_LastHarmTarg;
                else if (m_CurFlags == 2)
                    targ = m_LastBeneTarg;
                else
                    targ = m_LastTarget;

                if (targ == null)
                    targ = m_LastTarget;
            }
            else
            {
                if (m_AllowGround && m_LastGroundTarg != null)
                    targ = m_LastGroundTarg;
                else
                    targ = m_LastTarget;
            }

            if (targ == null)
                return false;

            Point3D pos = Point3D.Zero;

            if (targ.Serial.IsMobile)
            {
                Mobile m = World.FindMobile(targ.Serial);

                if (m != null)
                {
                    pos = m.Position;

                    targ.X = pos.X;
                    targ.Y = pos.Y;
                    targ.Z = pos.Z;
                }
                else
                    pos = Point3D.Zero;
            }
            else if (targ.Serial.IsItem)
            {
                Item i = World.FindItem(targ.Serial);

                if (i != null)
                {
                    pos = i.GetWorldPosition();

                    targ.X = i.Position.X;
                    targ.Y = i.Position.Y;
                    targ.Z = i.Position.Z;
                }
                else
                {
                    pos = Point3D.Zero;
                    targ.X = targ.Y = targ.Z = 0;
                }
            }
            else
            {
                if (!m_AllowGround && (targ.Serial == Serial.Zero || targ.Serial >= 0x80000000))
                {
                    World.Player.SendMessage(MsgLevel.Warning, LocString.LTGround);

                    return false;
                }

                pos = new Point3D(targ.X, targ.Y, targ.Z);
            }

            if (Config.GetBool("RangeCheckLT") && Windows.AllowBit(FeatureBit.RangeCheckLT) && (pos == Point3D.Zero || !Utility.InRange(World.Player.Position, pos, Config.GetInt("LTRange"))))
            {
                if (Config.GetBool("QueueTargets"))
                    m_QueueTarget = LastTargetAction;
                World.Player.SendMessage(MsgLevel.Warning, LocString.LTOutOfRange);

                return false;
            }

            if (CheckHealPoisonTarg(m_CurrentID, targ.Serial))
                return false;

            CancelClientTarget();
            HasTarget = false;

            targ.TargID = m_CurrentID;

            if (m_Intercept)
                OneTimeResponse(targ);
            else
                ClientCommunication.SendToServer(new TargetResponse(targ));

            return true;
        }

        public static void ClearQueue()
        {
            m_QueueTarget = null;
        }

        private static void OneTimeResponse(object state)
        {
            TargetInfo info = state as TargetInfo;

            if (info != null)
            {
                if (info.X == 0xFFFF && info.X == 0xFFFF && (info.Serial == 0 || info.Serial >= 0x80000000))
                {
                    if (m_OnCancel != null)
                        m_OnCancel();
                }
                else
                {
                    if (MacroManager.AcceptActions)
                        MacroManager.Action(new AbsoluteTargetAction(info));

                    if (m_OnTarget != null)
                        m_OnTarget(info.Type == 1 ? true : false, info.Serial, new Point3D(info.X, info.Y, info.Z), info.Gfx);
                }
            }

            EndIntercept();
        }

        private static void CancelTarget()
        {
            OnClearQueue();
            CancelClientTarget();

            if (HasTarget)
            {
                ClientCommunication.SendToServer(new TargetCancelResponse(m_CurrentID));
                HasTarget = false;
            }
        }

        private static void CancelClientTarget()
        {
            if (m_ClientTarget)
            {
                m_FilterCancel.Add(m_CurrentID);
                ClientCommunication.SendToClient(new CancelTarget(m_CurrentID));
                m_ClientTarget = false;
            }
        }

        public static void Target(TargetInfo info)
        {
            if (m_Intercept)
                OneTimeResponse(info);
            else if (HasTarget)
            {
                info.TargID = m_CurrentID;
                m_LastGroundTarg = m_LastTarget = info;
                ClientCommunication.SendToServer(new TargetResponse(info));
            }

            CancelClientTarget();
            HasTarget = false;
        }

        public static void Target(Point3D pt)
        {
            TargetInfo info = new TargetInfo();
            info.Type = 1;
            info.Flags = 0;
            info.Serial = 0;
            info.X = pt.X;
            info.Y = pt.Y;
            info.Z = pt.Z;
            info.Gfx = 0;

            Target(info);
        }

        public static void Target(Point3D pt, int gfx)
        {
            TargetInfo info = new TargetInfo();
            info.Type = 1;
            info.Flags = 0;
            info.Serial = 0;
            info.X = pt.X;
            info.Y = pt.Y;
            info.Z = pt.Z;
            info.Gfx = (ushort) (gfx & 0x3FFF);

            Target(info);
        }

        public static void Target(Serial s)
        {
            TargetInfo info = new TargetInfo();
            info.Type = 0;
            info.Flags = 0;
            info.Serial = s;

            if (s.IsItem)
            {
                Item item = World.FindItem(s);

                if (item != null)
                {
                    info.X = item.Position.X;
                    info.Y = item.Position.Y;
                    info.Z = item.Position.Z;
                    info.Gfx = item.ItemID;
                }
            }
            else if (s.IsMobile)
            {
                Mobile m = World.FindMobile(s);

                if (m != null)
                {
                    info.X = m.Position.X;
                    info.Y = m.Position.Y;
                    info.Z = m.Position.Z;
                    info.Gfx = m.Body;
                }
            }

            Target(info);
        }

        public static void Target(object o)
        {
            if (o is Item)
            {
                Item item = (Item) o;
                TargetInfo info = new TargetInfo();
                info.Type = 0;
                info.Flags = 0;
                info.Serial = item.Serial;
                info.X = item.Position.X;
                info.Y = item.Position.Y;
                info.Z = item.Position.Z;
                info.Gfx = item.ItemID;
                Target(info);
            }
            else if (o is Mobile)
            {
                Mobile m = (Mobile) o;
                TargetInfo info = new TargetInfo();
                info.Type = 0;
                info.Flags = 0;
                info.Serial = m.Serial;
                info.X = m.Position.X;
                info.Y = m.Position.Y;
                info.Z = m.Position.Z;
                info.Gfx = m.Body;
                Target(info);
            }
            else if (o is Serial)
                Target((Serial) o);
            else if (o is TargetInfo) Target((TargetInfo) o);
        }

        public static void CheckTextFlags(Mobile m)
        {
            if (IgnoreAgent.IsIgnored(m.Serial)) m.OverheadMessage(Config.GetInt("SysColor"), "[Ignored]");

            if (Config.GetBool("SmartLastTarget") && Windows.AllowBit(FeatureBit.SmartLT))
            {
                bool harm = m_LastHarmTarg != null && m_LastHarmTarg.Serial == m.Serial;
                bool bene = m_LastBeneTarg != null && m_LastBeneTarg.Serial == m.Serial;

                if (harm)
                    m.OverheadMessage(0x90, string.Format("[{0}]", Language.GetString(LocString.HarmfulTarget)));

                if (bene)
                    m.OverheadMessage(0x3F, string.Format("[{0}]", Language.GetString(LocString.BeneficialTarget)));
            }

            if (m_LastTarget != null && m_LastTarget.Serial == m.Serial)
                m.OverheadMessage(0x3B2, string.Format("[{0}]", Language.GetString(LocString.LastTarget)));
        }

        public static bool IsLastTarget(Mobile m)
        {
            if (m != null)
            {
                if (Config.GetBool("SmartLastTarget") && Windows.AllowBit(FeatureBit.SmartLT))
                {
                    if (m_LastHarmTarg != null && m_LastHarmTarg.Serial == m.Serial)
                        return true;
                }
                else
                {
                    if (m_LastTarget != null && m_LastTarget.Serial == m.Serial)
                        return true;
                }
            }

            return false;
        }

        public static void NextTarget()
        {
            List<Mobile> list = World.MobilesInRange(12);
            TargetInfo targ = new TargetInfo();
            Mobile m = null, old = World.FindMobile(m_LastTarget == null ? Serial.Zero : m_LastTarget.Serial);

            if (list.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            for (int i = 0; i < 3; i++)
            {
                m_NextPrevTargIdx++;

                if (m_NextPrevTargIdx >= list.Count)
                    m_NextPrevTargIdx = 0;

                m = list[m_NextPrevTargIdx];

                if (m != null && m != World.Player && m != old)
                    break;

                m = null;
            }

            if (m == null)
                m = old;

            if (m == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            m_LastGroundTarg = m_LastTarget = targ;

            m_LastHarmTarg = m_LastBeneTarg = targ;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Type = 0;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);

            /*if ( m_HasTarget )
            {
                 DoLastTarget();
                 ClearQueue();
            }*/
        }

        public static void NextTargetHumanoid()
        {
            List<Mobile> mobiles = World.MobilesInRange(12);
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile mob in mobiles)
            {
                if (mob.Body == 0x0190 || mob.Body == 0x0191 || mob.Body == 0x025D || mob.Body == 0x025E)
                    list.Add(mob);
            }

            if (list.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            TargetInfo targ = new TargetInfo();
            Mobile m = null, old = World.FindMobile(m_LastTarget == null ? Serial.Zero : m_LastTarget.Serial);

            for (int i = 0; i < 3; i++)
            {
                m_NextPrevTargHumanoidIdx++;

                if (m_NextPrevTargHumanoidIdx >= list.Count)
                    m_NextPrevTargHumanoidIdx = 0;

                m = list[m_NextPrevTargHumanoidIdx];

                if (m != null && m != World.Player && m != old)
                    break;

                m = null;
            }

            if (m == null)
                m = old;

            if (m == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            m_LastGroundTarg = m_LastTarget = targ;

            m_LastHarmTarg = m_LastBeneTarg = targ;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Type = 0;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);

            /*if ( m_HasTarget )
            {
                 DoLastTarget();
                 ClearQueue();
            }*/
        }

        public static void NextTargetEnemyHumanoid()
        {
            List<Mobile> mobiles = World.MobilesInRange(12);
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile mob in mobiles)
            {
                if (mob.Body == 0x0190 || mob.Body == 0x0191 || mob.Body == 0x025D || mob.Body == 0x025E)
                {
                    if (mob.Notoriety == 5) // Check if they are red
                        list.Add(mob);
                }
            }

            if (list.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            TargetInfo targ = new TargetInfo();
            Mobile m = null, old = World.FindMobile(m_LastTarget == null ? Serial.Zero : m_LastTarget.Serial);

            for (int i = 0; i < 3; i++)
            {
                m_NextPrevTargEnemyHumanoidIdx++;

                if (m_NextPrevTargEnemyHumanoidIdx >= list.Count)
                    m_NextPrevTargEnemyHumanoidIdx = 0;

                m = list[m_NextPrevTargEnemyHumanoidIdx];

                if (m != null && m != World.Player && m != old)
                    break;

                m = null;
            }

            if (m == null)
                m = old;

            if (m == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            m_LastGroundTarg = m_LastTarget = targ;

            m_LastHarmTarg = m_LastBeneTarg = targ;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Type = 0;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);

            /*if ( m_HasTarget )
           {
               DoLastTarget();
               ClearQueue();
           }*/
        }

        public static void PrevTarget()
        {
            List<Mobile> list = World.MobilesInRange(12);
            TargetInfo targ = new TargetInfo();
            Mobile m = null, old = World.FindMobile(m_LastTarget == null ? Serial.Zero : m_LastTarget.Serial);

            if (list.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            for (int i = 0; i < 3; i++)
            {
                m_NextPrevTargIdx--;

                if (m_NextPrevTargIdx < 0)
                    m_NextPrevTargIdx = list.Count - 1;

                m = list[m_NextPrevTargIdx];

                if (m != null && m != World.Player && m != old)
                    break;

                m = null;
            }

            if (m == null)
                m = old;

            if (m == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            m_LastGroundTarg = m_LastTarget = targ;

            m_LastHarmTarg = m_LastBeneTarg = targ;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Type = 0;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);
        }

        public static void PrevTargetHumanoid()
        {
            List<Mobile> mobiles = World.MobilesInRange(12);
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile mob in mobiles)
            {
                if (mob.Body == 0x0190 || mob.Body == 0x0191 || mob.Body == 0x025D || mob.Body == 0x025E)
                    list.Add(mob);
            }

            if (list.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            TargetInfo targ = new TargetInfo();
            Mobile m = null, old = World.FindMobile(m_LastTarget == null ? Serial.Zero : m_LastTarget.Serial);

            for (int i = 0; i < 3; i++)
            {
                m_NextPrevTargHumanoidIdx--;

                if (m_NextPrevTargHumanoidIdx < 0)
                    m_NextPrevTargHumanoidIdx = list.Count - 1;

                m = list[m_NextPrevTargHumanoidIdx];

                if (m != null && m != World.Player && m != old)
                    break;

                m = null;
            }

            if (m == null)
                m = old;

            if (m == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            m_LastGroundTarg = m_LastTarget = targ;

            m_LastHarmTarg = m_LastBeneTarg = targ;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Type = 0;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);

            /*if ( m_HasTarget )
            {
                 DoLastTarget();
                 ClearQueue();
            }*/
        }

        public static void PrevTargetEnemyHumanoid()
        {
            List<Mobile> mobiles = World.MobilesInRange(12);
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile mob in mobiles)
            {
                if (mob.Body == 0x0190 || mob.Body == 0x0191 || mob.Body == 0x025D || mob.Body == 0x025E)
                {
                    if (mob.Notoriety == 5) // Check if they are red
                        list.Add(mob);
                }
            }

            if (list.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            TargetInfo targ = new TargetInfo();
            Mobile m = null, old = World.FindMobile(m_LastTarget == null ? Serial.Zero : m_LastTarget.Serial);

            for (int i = 0; i < 3; i++)
            {
                m_NextPrevTargEnemyHumanoidIdx--;

                if (m_NextPrevTargEnemyHumanoidIdx < 0)
                    m_NextPrevTargEnemyHumanoidIdx = list.Count - 1;

                m = list[m_NextPrevTargEnemyHumanoidIdx];

                if (m != null && m != World.Player && m != old)
                    break;

                m = null;
            }

            if (m == null)
                m = old;

            if (m == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.TargNoOne);

                return;
            }

            m_LastGroundTarg = m_LastTarget = targ;

            m_LastHarmTarg = m_LastBeneTarg = targ;

            if (HasTarget)
                targ.Flags = m_CurFlags;
            else
                targ.Type = 0;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientCommunication.SendToClient(new ChangeCombatant(m));
            m_LastCombatant = m.Serial;
            World.Player.SendMessage(MsgLevel.Force, LocString.NewTargSet);
        }

        public static void CheckLastTargetRange(Mobile m)
        {
            if (World.Player == null)
                return;

            if (HasTarget && m != null && m_LastTarget != null && m.Serial == m_LastTarget.Serial && m_QueueTarget == LastTargetAction)
            {
                if (Config.GetBool("RangeCheckLT") && Windows.AllowBit(FeatureBit.RangeCheckLT))
                {
                    if (Utility.InRange(World.Player.Position, m.Position, Config.GetInt("LTRange")))
                    {
                        if (m_QueueTarget())
                            ClearQueue();
                    }
                }
            }
        }

        private static bool CheckHealPoisonTarg(uint targID, Serial ser)
        {
            if (World.Player == null)
                return false;

            if (targID == SpellTargetID && ser.IsMobile && (World.Player.LastSpell == Spell.ToID(1, 4) || World.Player.LastSpell == Spell.ToID(4, 5)) && Config.GetBool("BlockHealPoison") && Windows.AllowBit(FeatureBit.BlockHealPoisoned))
            {
                Mobile m = World.FindMobile(ser);

                if (m != null && m.Poisoned)
                {
                    World.Player.SendMessage(MsgLevel.Warning, LocString.HealPoisonBlocked);

                    return true;
                }
            }

            return false;
        }

        private static void TargetResponse(PacketReader p, PacketHandlerEventArgs args)
        {
            TargetInfo info = new TargetInfo();
            info.Type = p.ReadByte();
            info.TargID = p.ReadUInt32();
            info.Flags = p.ReadByte();
            info.Serial = p.ReadUInt32();
            info.X = p.ReadUInt16();
            info.Y = p.ReadUInt16();
            info.Z = p.ReadInt16();
            info.Gfx = p.ReadUInt16();

            m_ClientTarget = false;

            //if (Config.GetBool("ShowAttackTargetOverhead"))
            //{
            //    Mobile m = World.FindMobile(info.Serial);

            //    if (m != null)
            //    {
            //        if (FriendsAgent.IsFriend(m))
            //        {
            //            World.Player.OverheadMessage(63, $"Target: {m.Name}");
            //        }
            //        else
            //        {
            //            World.Player.OverheadMessage(m.GetNotorietyColorInt(), $"Target: {m.Name}");
            //        }
            //    }
            //}            

            // check for cancel
            if (info.X == 0xFFFF && info.X == 0xFFFF && (info.Serial <= 0 || info.Serial >= 0x80000000))
            {
                HasTarget = false;

                if (m_Intercept)
                {
                    args.Block = true;
                    Timer.DelayedCallbackState(TimeSpan.Zero, m_OneTimeRespCallback, info).Start();
                    EndIntercept();

                    if (m_PreviousID != 0)
                    {
                        m_CurrentID = m_PreviousID;
                        m_AllowGround = m_PreviousGround;
                        m_CurFlags = m_PrevFlags;

                        m_PreviousID = 0;

                        ResendTarget();
                    }
                }
                else if (m_FilterCancel.Contains(info.TargID) || info.TargID == LocalTargID) args.Block = true;

                m_FilterCancel.Clear();

                return;
            }

            ClearQueue();

            if (m_Intercept)
            {
                if (info.TargID == LocalTargID)
                {
                    Timer.DelayedCallbackState(TimeSpan.Zero, m_OneTimeRespCallback, info).Start();

                    HasTarget = false;
                    args.Block = true;

                    if (m_PreviousID != 0)
                    {
                        m_CurrentID = m_PreviousID;
                        m_AllowGround = m_PreviousGround;
                        m_CurFlags = m_PrevFlags;

                        m_PreviousID = 0;

                        ResendTarget();
                    }

                    m_FilterCancel.Clear();

                    return;
                }

                EndIntercept();
            }

            HasTarget = false;

            if (CheckHealPoisonTarg(m_CurrentID, info.Serial))
            {
                ResendTarget();
                args.Block = true;
            }

            if (info.Serial != World.Player.Serial)
            {
                if (info.Serial.IsValid)
                {
                    // only let lasttarget be a non-ground target

                    m_LastTarget = info;

                    if (info.Flags == 1)
                        m_LastHarmTarg = info;
                    else if (info.Flags == 2)
                        m_LastBeneTarg = info;

                    LastTargetChanged();
                }

                m_LastGroundTarg = info; // ground target is the true last target

                if (MacroManager.AcceptActions)
                    MacroManager.Action(new AbsoluteTargetAction(info));
            }
            else
            {
                if (MacroManager.AcceptActions)
                {
                    KeyData hk = HotKey.Get((int) LocString.TargetSelf);

                    if (hk != null)
                        MacroManager.Action(new HotKeyAction(hk));
                    else
                        MacroManager.Action(new AbsoluteTargetAction(info));
                }
            }

            if (World.Player.LastSpell == 52 && !GateTimer.Running) GateTimer.Start();

            m_FilterCancel.Clear();
        }

        private static void NewTarget(PacketReader p, PacketHandlerEventArgs args)
        {
            bool prevAllowGround = m_AllowGround;
            uint prevID = m_CurrentID;
            byte prevFlags = m_CurFlags;
            bool prevClientTarget = m_ClientTarget;

            m_AllowGround = p.ReadBoolean(); // allow ground
            m_CurrentID = p.ReadUInt32(); // target uid
            m_CurFlags = p.ReadByte(); // flags
            // the rest of the packet is 0s

            // check for a server cancel command
            if (!m_AllowGround && m_CurrentID == 0 && m_CurFlags == 3)
            {
                HasTarget = false;
                m_ClientTarget = false;

                if (m_Intercept)
                {
                    EndIntercept();
                    World.Player.SendMessage(MsgLevel.Error, LocString.OTTCancel);
                }

                return;
            }

            if (Spell.LastCastTime + TimeSpan.FromSeconds(3.0) > DateTime.UtcNow && Spell.LastCastTime + TimeSpan.FromSeconds(0.5) <= DateTime.UtcNow && SpellTargetID == 0)
                SpellTargetID = m_CurrentID;

            HasTarget = true;
            m_ClientTarget = false;

            if (m_QueueTarget == null && MacroManager.AcceptActions && MacroManager.Action(new WaitForTargetAction()))
                args.Block = true;
            else if (m_QueueTarget != null && m_QueueTarget())
            {
                ClearQueue();
                args.Block = true;
            }

            if (args.Block)
            {
                if (prevClientTarget)
                {
                    m_AllowGround = prevAllowGround;
                    m_CurrentID = prevID;
                    m_CurFlags = prevFlags;

                    m_ClientTarget = true;

                    if (!m_Intercept)
                        CancelClientTarget();
                }
            }
            else
            {
                m_ClientTarget = true;

                if (m_Intercept)
                {
                    if (m_OnCancel != null)
                        m_OnCancel();
                    EndIntercept();
                    World.Player.SendMessage(MsgLevel.Error, LocString.OTTCancel);

                    m_FilterCancel.Add(prevID);
                }
            }
        }

        public static void ResendTarget()
        {
            if (!m_ClientTarget || !HasTarget)
            {
                CancelClientTarget();
                m_ClientTarget = HasTarget = true;
                ClientCommunication.SendToClient(new Target(m_CurrentID, m_AllowGround, m_CurFlags));
            }
        }

        private delegate bool QueueTarget();
    }
}