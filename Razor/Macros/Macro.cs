using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using Assistant.UI;

namespace Assistant.Macros
{
    public class Macro
    {
        private static readonly Type[] ctorArgs = new Type[1] {typeof(string[])};

        private static readonly MacroWaitAction PauseB4Loop = new PauseAction(TimeSpan.FromSeconds(0.1));
        private readonly Stack m_IfStatus;
        private ListBox m_ListBox;
        private bool m_Loaded;
        private bool m_Loop;
        private MacroWaitAction m_Wait;

        public Macro(string path)
        {
            Actions = new ArrayList();
            Filename = path;
            m_Loaded = false;
            m_IfStatus = new Stack();
        }

        public string Filename { get; set; }

        public ArrayList Actions { get; }
        public bool Recording { get; private set; }
        public bool Playing { get; private set; }
        public bool StepThrough { get; set; }
        public bool Waiting => m_Wait != null;
        public int CurrentAction { get; private set; }

        public bool Loop
        {
            get => m_Loop && Windows.AllowBit(FeatureBit.LoopingMacros);
            set => m_Loop = value;
        }

        public void DisplayTo(ListBox list)
        {
            m_ListBox = list;

            m_ListBox.SafeAction(s => s.Items.Clear());

            if (!m_Loaded)
                Load();

            m_ListBox.SafeAction(s =>
            {
                s.BeginUpdate();

                if (Actions.Count > 0)
                    s.Items.AddRange((object[]) Actions.ToArray(typeof(object)));

                if (Playing && CurrentAction >= 0 && CurrentAction < Actions.Count)
                    s.SelectedIndex = CurrentAction;
                else
                    s.SelectedIndex = -1;
                s.EndUpdate();
            });
        }

        public override string ToString()
        {
            //return Path.GetFileNameWithoutExtension( m_Path );
            StringBuilder sb = new StringBuilder(Path.GetFullPath(Filename));
            sb.Remove(sb.Length - 6, 6);
            sb.Remove(0, Config.GetUserDirectory("Macros").Length + 1);

            return sb.ToString();
        }

        public void Insert(int idx, MacroAction a)
        {
            a.Parent = this;

            if (idx < 0 || idx > Actions.Count)
                idx = Actions.Count;
            Actions.Insert(idx, a);
        }

        public void Record()
        {
            Actions.Clear();

            if (m_ListBox != null)
                m_ListBox.SafeAction(s => s.Items.Clear());
            RecordAt(0);
        }

        public void RecordAt(int at)
        {
            Stop();
            Recording = true;
            m_Loaded = true;
            CurrentAction = at;

            if (CurrentAction > Actions.Count)
                CurrentAction = Actions.Count;
        }

        public void Play()
        {
            Stop();

            if (!m_Loaded)
                Load();
            else
                Save();

            if (Actions.Count > 0)
            {
                m_IfStatus.Clear();
                Playing = true;
                CurrentAction = -1;

                if (m_ListBox != null)
                    m_ListBox.SafeAction(s => s.SelectedIndex = -1);
            }
        }

        public void PlayAt(int at)
        {
            Stop();

            if (!m_Loaded)
                Load();
            else
                Save();

            if (Actions.Count > 0)
            {
                m_IfStatus.Clear();
                Playing = true;
                CurrentAction = at - 1;

                if (CurrentAction >= 0)
                    CurrentAction--;
            }
        }

        public void Reset()
        {
            if (Playing && World.Player != null && DragDropManager.Holding != null && DragDropManager.Holding == LiftAction.LastLift)
                ClientCommunication.SendToServer(new DropRequest(DragDropManager.Holding, World.Player.Serial));

            m_Wait = null;

            m_IfStatus.Clear();

            foreach (MacroAction a in Actions)
            {
                if (a is ForAction)
                    ((ForAction) a).Count = 0;
            }
        }

        public void Stop()
        {
            if (Recording)
                Save();

            Recording = Playing = false;
            Reset();
        }

        // returns true if the were waiting for this action
        public bool Action(MacroAction action)
        {
            if (Recording)
            {
                action.Parent = this;
                Actions.Insert(CurrentAction, action);

                if (m_ListBox != null)
                    m_ListBox.SafeAction(s => s.Items.Insert(CurrentAction, action));
                CurrentAction++;

                return false;
            }

            if (Playing && m_Wait != null && m_Wait.CheckMatch(action))
            {
                m_Wait = null;
                ExecNext();

                return true;
            }

            return false;
        }

        public void Save()
        {
            if (Actions.Count == 0)
                return;

            using (StreamWriter writer = new StreamWriter(Filename))
            {
                if (m_Loop)
                    writer.WriteLine("!Loop");

                for (int i = 0; i < Actions.Count; i++)
                {
                    MacroAction a = (MacroAction) Actions[i];

                    try
                    {
                        writer.WriteLine(a.Serialize());
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void Load()
        {
            Actions.Clear();
            m_Loaded = false;

            if (!File.Exists(Filename))
                return;

            using (StreamReader reader = new StreamReader(Filename))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length <= 2)
                        continue;

                    if (line == "!Loop")
                    {
                        m_Loop = true;

                        continue;
                    }

                    if (line[0] == '#')
                    {
                        Actions.Add(new MacroComment(line.Substring(1)));

                        continue;
                    }

                    if (line[0] == '/' && line[1] == '/')
                    {
                        MacroAction a = new MacroComment(line.Substring(2));
                        a.Parent = this;
                        Actions.Add(a);

                        continue;
                    }

                    string[] args = line.Split('|');
                    object[] invokeArgs = new object[1] {args};

                    Type at = null;

                    try
                    {
                        at = Type.GetType(args[0], false);
                    }
                    catch
                    {
                    }

                    if (at == null)
                        continue;

                    if (args.Length > 1)
                    {
                        try
                        {
                            ConstructorInfo ctor = at.GetConstructor(ctorArgs);

                            if (ctor == null)
                                continue;

                            MacroAction a = (MacroAction) ctor.Invoke(invokeArgs);
                            a.Parent = this;
                            Actions.Add(a);
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        try
                        {
                            ConstructorInfo ctor = at.GetConstructor(Type.EmptyTypes);

                            if (ctor == null)
                                continue;

                            MacroAction a = (MacroAction) ctor.Invoke(null);
                            a.Parent = this;
                            Actions.Add(a);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            m_Loaded = true;
        }

        public void Convert(MacroAction old, MacroAction newAct)
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                if (Actions[i] == old)
                {
                    Actions[i] = newAct;
                    newAct.Parent = this;
                    Update();

                    break;
                }
            }
        }

        public void Update()
        {
            if (m_ListBox != null)
            {
                int sel = m_ListBox.SelectedIndex;
                DisplayTo(m_ListBox);

                try
                {
                    m_ListBox.SafeAction(s => s.SelectedIndex = sel);
                }
                catch
                {
                }
            }
        }

        private bool NextIsInstantWait()
        {
            int nextAct = CurrentAction + 1;

            if (nextAct >= 0 && nextAct < Actions.Count)
                return Actions[nextAct] is WaitForTargetAction || Actions[nextAct] is WaitForGumpAction || Actions[nextAct] is WaitForMenuAction; //|| m_Actions[nextAct] is ElseAction || m_Actions[nextAct] is EndIfAction;

            return false;
        }

        //return true to continue the macro, false to stop (macro's over)
        public bool ExecNext()
        {
            try
            {
                if (!Playing)
                    return false;

                if (m_Wait != null)
                {
                    TimeSpan waitLen = DateTime.UtcNow - m_Wait.StartTime;

                    if (!(m_Wait is PauseAction) && waitLen >= m_Wait.Timeout)
                    {
                        if (Loop)
                        {
                            if (Engine.MainWindow.WaitDisplay != null)
                                Engine.MainWindow.WaitDisplay.Text = "";
                            CurrentAction = -1;
                            m_IfStatus.Clear();
                            PauseB4Loop.Perform();
                            PauseB4Loop.Parent = this;
                            m_Wait = PauseB4Loop;

                            return true;
                        }

                        Stop();

                        return false;
                    }

                    if (!m_Wait.PerformWait())
                    {
                        m_Wait = null; // done waiting

                        if (Engine.MainWindow.WaitDisplay != null)
                            Engine.MainWindow.WaitDisplay.Text = "";
                    }
                    else
                    {
                        if (waitLen >= TimeSpan.FromSeconds(4.0) && Engine.MainWindow.WaitDisplay != null)
                        {
                            StringBuilder sb = new StringBuilder(Language.GetString(LocString.WaitingTimeout));
                            int s = (int) (m_Wait.Timeout - waitLen).TotalSeconds;
                            int m = 0;

                            if (s > 60)
                            {
                                m = s / 60;
                                s %= 60;

                                if (m > 60)
                                {
                                    sb.AppendFormat("{0}:", m / 60);
                                    m %= 60;
                                }
                            }

                            sb.AppendFormat("{0:00}:{1:00}", m, s);
                            Engine.MainWindow.WaitDisplay.Text = sb.ToString();
                        }

                        return true; // keep waiting
                    }
                }

                CurrentAction++;

                //MacroManager.ActionUpdate( this, m_CurrentAction );
                if (m_ListBox != null)
                {
                    if (CurrentAction < m_ListBox.Items.Count)
                        m_ListBox.SafeAction(s => s.SelectedIndex = CurrentAction);
                    else
                        m_ListBox.SafeAction(s => s.SelectedIndex = -1);
                }

                if (CurrentAction >= 0 && CurrentAction < Actions.Count)
                {
                    MacroAction action = (MacroAction) Actions[CurrentAction];

                    if (action is IfAction)
                    {
                        bool val = ((IfAction) action).Evaluate();
                        m_IfStatus.Push(val);

                        if (!val)
                        {
                            // false so skip to an else or an endif
                            int ifcount = 0;

                            while (CurrentAction + 1 < Actions.Count)
                            {
                                if (Actions[CurrentAction + 1] is IfAction)
                                    ifcount++;
                                else if (Actions[CurrentAction + 1] is ElseAction && ifcount <= 0)
                                    break;
                                else if (Actions[CurrentAction + 1] is EndIfAction)
                                {
                                    if (ifcount <= 0)
                                        break;

                                    ifcount--;
                                }

                                CurrentAction++;
                            }
                        }
                    }
                    else if (action is ElseAction && m_IfStatus.Count > 0)
                    {
                        bool val = (bool) m_IfStatus.Peek();

                        if (val)
                        {
                            // the if was true, so skip to an endif
                            int ifcount = 0;

                            while (CurrentAction + 1 < Actions.Count)
                            {
                                if (Actions[CurrentAction + 1] is IfAction)
                                    ifcount++;
                                else if (Actions[CurrentAction + 1] is EndIfAction)
                                {
                                    if (ifcount <= 0)
                                        break;

                                    ifcount--;
                                }

                                CurrentAction++;
                            }
                        }
                    }
                    else if (action is EndIfAction && m_IfStatus.Count > 0)
                        m_IfStatus.Pop();
                    else if (action is ForAction)
                    {
                        ForAction fa = (ForAction) action;
                        fa.Count++;

                        if (fa.Count > fa.Max)
                        {
                            fa.Count = 0;

                            int forcount = 0;
                            CurrentAction++;

                            while (CurrentAction < Actions.Count)
                            {
                                if (Actions[CurrentAction] is ForAction)
                                    forcount++;
                                else if (Actions[CurrentAction] is EndForAction)
                                {
                                    if (forcount <= 0)
                                        break;

                                    forcount--;
                                }

                                CurrentAction++;
                            }

                            if (CurrentAction < Actions.Count)
                                action = (MacroAction) Actions[CurrentAction];
                        }
                    }
                    else if (action is EndForAction && Windows.AllowBit(FeatureBit.LoopingMacros))
                    {
                        int ca = CurrentAction - 1;
                        int forcount = 0;

                        while (ca >= 0)
                        {
                            if (Actions[ca] is EndForAction)
                                forcount--;
                            else if (Actions[ca] is ForAction)
                            {
                                if (forcount >= 0)
                                    break;

                                forcount++;
                            }

                            ca--;
                        }

                        if (ca >= 0 && Actions[ca] is ForAction)
                            CurrentAction = ca - 1;
                    }

                    bool isWait = action is MacroWaitAction;

                    if (!action.Perform() && isWait)
                    {
                        m_Wait = (MacroWaitAction) action;
                        m_Wait.StartTime = DateTime.UtcNow;
                    }
                    else if (NextIsInstantWait() && !isWait) return ExecNext();
                }
                else
                {
                    if (Engine.MainWindow.WaitDisplay != null)
                        Engine.MainWindow.WaitDisplay.Text = "";

                    if (Loop)
                    {
                        CurrentAction = -1;

                        Reset();

                        PauseB4Loop.Perform();
                        PauseB4Loop.Parent = this;
                        m_Wait = PauseB4Loop;
                    }
                    else
                    {
                        Stop();

                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                new MessageDialog("Macro Exception", true, e.ToString()).Show();

                return false;
            }

            return true;
        }
    }
}