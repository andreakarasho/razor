using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using Assistant.UI;

namespace Assistant.Macros
{
    public class MacroManager
    {
        private static Macro m_PrevPlay;
        private static bool m_Paused;
        private static readonly MacroTimer m_Timer;

        static MacroManager()
        {
            List = new List<Macro>();
            m_Timer = new MacroTimer();
        }

        public static List<Macro> List { get; }
        public static bool Recording => Current != null && Current.Recording;
        public static bool Playing => Current != null && Current.Playing && m_Timer != null && m_Timer.Running;
        public static bool StepThrough => Current != null && Current.StepThrough && Current.Playing;
        public static Macro Current { get; private set; }
        public static bool AcceptActions => Recording || Playing && Current.Waiting;

        public static void Initialize()
        {
            HotKey.Add(HKCategory.Macros, LocString.StopCurrent, HotKeyStop);
            HotKey.Add(HKCategory.Macros, LocString.PauseCurrent, HotKeyPause);

            string path = Config.GetUserDirectory("Macros");
            Recurse(null, path);
        }

        /// <summary>
        ///     Saves all the macros and absolute target lists
        /// </summary>
        public static void Save()
        {
            Engine.EnsureDirectory(Config.GetUserDirectory("Macros"));

            foreach (Macro macro in List) macro.Save();
        }
        //public static bool IsWaiting{ get{ return Playing && m_Current != null && m_Current.Waiting; } }

        public static void Add(Macro m)
        {
            HotKey.Add(HKCategory.Macros, HKSubCat.None, Language.Format(LocString.PlayA1, m), HotKeyPlay, m);
            List.Add(m);
        }

        public static void Remove(Macro m)
        {
            HotKey.Remove(Language.Format(LocString.PlayA1, m));
            List.Remove(m);
        }

        public static void RecordAt(Macro m, int at)
        {
            if (Current != null)
                Current.Stop();
            Current = m;
            Current.RecordAt(at);
        }

        public static void Record(Macro m)
        {
            if (Current != null)
                Current.Stop();
            Current = m;
            Current.Record();
        }

        public static void PlayAt(Macro m, int at)
        {
            if (Current != null)
            {
                if (Current.Playing && Current.Loop && !m.Loop)
                    m_PrevPlay = Current;
                else
                    m_PrevPlay = null;

                Current.Stop();
            }
            else
                m_PrevPlay = null;

            LiftAction.LastLift = null;
            Current = m;
            Current.PlayAt(at);

            m_Timer.Macro = Current;

            if (!Config.GetBool("StepThroughMacro")) m_Timer.Start();

            if (Engine.MainWindow.WaitDisplay != null)
                Engine.MainWindow.SafeAction(s => s.WaitDisplay.Text = "");
        }

        private static void HotKeyPlay(ref object state)
        {
            HotKeyPlay(state as Macro);
        }

        public static void HotKeyPlay(Macro m)
        {
            if (m != null)
            {
                Play(m);
                World.Player.SendMessage(LocString.PlayingA1, m);
                Engine.MainWindow.SafeAction(s => s.PlayMacro(m));
            }
        }

        public static void Play(Macro m)
        {
            if (Current != null)
            {
                if (Current.Playing && Current.Loop && !m.Loop)
                    m_PrevPlay = Current;
                else
                    m_PrevPlay = null;

                Current.Stop();
            }
            else
                m_PrevPlay = null;

            LiftAction.LastLift = null;
            Current = m;
            Current.Play();

            m_Timer.Macro = Current;

            if (!Config.GetBool("StepThroughMacro")) m_Timer.Start();

            if (Engine.MainWindow.WaitDisplay != null)
                Engine.MainWindow.SafeAction(s => s.WaitDisplay.Text = "");
        }

        public static void PlayNext()
        {
            if (Current == null)
                return;

            m_Timer.PerformNextAction();
        }

        private static void HotKeyPause()
        {
            Pause();
        }

        private static void HotKeyStop()
        {
            Stop();
        }

        public static void Stop()
        {
            Stop(false);
        }

        public static void Stop(bool restartPrev)
        {
            m_Timer.Stop();

            if (Current != null)
            {
                Current.Stop();
                Current = null;
            }

            UOAssist.PostMacroStop();

            if (Engine.MainWindow.WaitDisplay != null)
                Engine.MainWindow.SafeAction(s => s.WaitDisplay.Text = "");

            Engine.MainWindow.SafeAction(s => s.OnMacroStop());

            //if ( restartPrev )
            //	Play( m_PrevPlay );
            m_PrevPlay = null;
        }

        public static void Pause()
        {
            if (m_Paused)
            {
                // unpause
                int sel = Current.CurrentAction;

                if (sel < 0 || sel > Current.Actions.Count)
                    sel = Current.Actions.Count;

                //m_Current.PlayAt(sel);
                m_Timer.Start();

                m_Paused = false;

                World.Player.SendMessage(LocString.MacroResuming);
            }
            else
            {
                // pause
                m_Timer.Stop();

                if (Engine.MainWindow.WaitDisplay != null)
                    Engine.MainWindow.SafeAction(s => s.WaitDisplay.Text = "Paused");

                World.Player.SendMessage(LocString.MacroPaused);

                m_Paused = true;
            }
        }

        public static void DisplayTo(TreeView tree)
        {
            tree.BeginUpdate();
            tree.Nodes.Clear();
            Recurse(tree.Nodes, Config.GetUserDirectory("Macros"));
            tree.EndUpdate();
            tree.Refresh();
            tree.Update();
        }

        public static void DisplayAbsoluteTargetsTo(ListBox list)
        {
            list.BeginUpdate();
            list.Items.Clear();

            foreach (AbsoluteTargets.AbsoluteTarget at in AbsoluteTargets.AbsoluteTargetList) list.Items.Add($"${at.TargetVariableName} ({at.TargetInfo.Serial})");

            list.EndUpdate();
            list.Refresh();
            list.Update();
        }


        private static void Recurse(TreeNodeCollection nodes, string path)
        {
            try
            {
                string[] macros = Directory.GetFiles(path, "*.macro");

                for (int i = 0; i < macros.Length; i++)
                {
                    Macro m = null;

                    for (int j = 0; j < List.Count; j++)
                    {
                        Macro check = List[j];

                        if (check.Filename == macros[i])
                        {
                            m = check;

                            break;
                        }
                    }

                    if (m == null)
                        Add(m = new Macro(macros[i]));

                    if (nodes != null)
                    {
                        TreeNode node = new TreeNode(Path.GetFileNameWithoutExtension(m.Filename));
                        node.Tag = m;
                        nodes.Add(node);
                    }
                }
            }
            catch
            {
            }

            try
            {
                string[] dirs = Directory.GetDirectories(path);

                for (int i = 0; i < dirs.Length; i++)
                {
                    if (dirs[i] != "" && dirs[i] != "." && dirs[i] != "..")
                    {
                        if (nodes != null)
                        {
                            TreeNode node = new TreeNode(string.Format("[{0}]", Path.GetFileName(dirs[i])));
                            node.Tag = dirs[i];
                            nodes.Add(node);
                            Recurse(node.Nodes, dirs[i]);
                        }
                        else
                            Recurse(null, dirs[i]);
                    }
                }
            }
            catch
            {
            }
        }

        public static void Select(Macro m, ListBox actionList, Button play, Button rec, CheckBox loop)
        {
            if (m == null)
                return;

            m.DisplayTo(actionList);

            if (Recording)
            {
                play.Enabled = false;
                play.Text = "Play";
                rec.Enabled = true;
                rec.Text = "Stop";
            }
            else
            {
                play.Enabled = true;

                if (m.Playing)
                {
                    play.Text = "Stop";
                    rec.Enabled = false;
                }
                else
                {
                    play.Text = "Play";
                    rec.Enabled = true;
                }

                rec.Text = "Record";
                loop.Checked = m.Loop;
            }
        }

        public static bool Action(MacroAction a)
        {
            if (Current != null)
                return Current.Action(a);

            return false;
        }

        private class MacroTimer : Timer
        {
            public MacroTimer() : base(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(0))
            {
            }

            public Macro Macro { get; set; }

            public void PerformNextAction()
            {
                ExecuteNextAction();
            }

            protected override void OnTick()
            {
                ExecuteNextAction();
            }

            private void ExecuteNextAction()
            {
                try
                {
                    if (Macro == null || World.Player == null)
                    {
                        Stop();
                        MacroManager.Stop();
                    }
                    else if (!Macro.ExecNext())
                    {
                        Stop();
                        MacroManager.Stop(true);
                        World.Player.SendMessage(LocString.MacroFinished, Macro);
                    }
                }
                catch
                {
                    Stop();
                    MacroManager.Stop();
                }
            }
        }
    }
}