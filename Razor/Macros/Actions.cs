using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Assistant.HotKeys;
using Assistant.UI;

using Ultima;

namespace Assistant.Macros
{
    public delegate void MacroMenuCallback(object[] Args);

    public class MacroMenuItem : MenuItem
    {
        private readonly object[] m_Args;
        private readonly MacroMenuCallback m_Call;

        public MacroMenuItem(LocString name, MacroMenuCallback call, params object[] args) : base(Language.GetString(name))
        {
            Click += OnMenuClick;
            m_Call = call;
            m_Args = args;
        }

        private void OnMenuClick(object sender, EventArgs e)
        {
            m_Call(m_Args);
        }
    }

    public abstract class MacroAction
    {
        protected Macro m_Parent;

        public Macro Parent
        {
            get => m_Parent;
            set => m_Parent = value;
        }

        public override string ToString()
        {
            return string.Format("?{0}?", GetType().Name);
        }

        public virtual string Serialize()
        {
            return GetType().FullName;
        }

        protected string DoSerialize(params object[] args)
        {
            StringBuilder sb = new StringBuilder(GetType().FullName);

            for (int i = 0; i < args.Length; i++)
                sb.AppendFormat("|{0}", args[i]);

            return sb.ToString();
        }

        public virtual MenuItem[] GetContextMenuItems()
        {
            return null;
        }

        public abstract bool Perform();
    }

    public abstract class MacroWaitAction : MacroAction
    {
        private MacroMenuItem m_MenuItem;
        protected TimeSpan m_Timeout = TimeSpan.FromMinutes(5);

        public TimeSpan Timeout => m_Timeout;
        public DateTime StartTime { get; set; }

        public MacroMenuItem EditTimeoutMenuItem
        {
            get
            {
                if (m_MenuItem == null)
                    m_MenuItem = new MacroMenuItem(LocString.EditTimeout, EditTimeout);

                return m_MenuItem;
            }
        }

        public abstract bool PerformWait();

        private void EditTimeout(object[] args)
        {
            if (InputBox.Show(Language.GetString(LocString.NewTimeout), Language.GetString(LocString.ChangeTimeout), ((int) m_Timeout.TotalSeconds).ToString()))
                m_Timeout = TimeSpan.FromSeconds(InputBox.GetInt(60));
        }

        public virtual bool CheckMatch(MacroAction a)
        {
            return false; // a.GetType() == this.GetType();
        }
    }

    public class MacroComment : MacroAction
    {
        private MenuItem[] m_MenuItems;

        public MacroComment(string comment)
        {
            if (comment == null)
                comment = "";

            Comment = comment.Trim();
        }

        public string Comment { get; set; }

        public override bool Perform()
        {
            return true;
        }

        public override string Serialize()
        {
            return ToString();
        }

        public override string ToString()
        {
            if (Comment == null)
                Comment = "";

            return string.Format("// {0}", Comment);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit)
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            if (InputBox.Show(Language.GetString(LocString.InsComment), Language.GetString(LocString.InputReq), Comment))
            {
                if (Comment == null)
                    Comment = "";

                Comment = InputBox.GetString();

                if (Comment == null)
                    Comment = "";

                if (m_Parent != null)
                    m_Parent.Update();
            }
        }
    }

    public class DoubleClickAction : MacroAction
    {
        private ushort m_Gfx;

        private MenuItem[] m_MenuItems;
        private Serial m_Serial;

        public DoubleClickAction(Serial obj, ushort gfx)
        {
            m_Serial = obj;
            m_Gfx = gfx;
        }

        public DoubleClickAction(string[] args)
        {
            m_Serial = Serial.Parse(args[1]);
            m_Gfx = Convert.ToUInt16(args[2]);
        }

        public override bool Perform()
        {
            PlayerData.DoubleClick(m_Serial);

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Serial.Value, m_Gfx);
        }

        public override string ToString()
        {
            return Language.Format(LocString.DClickA1, m_Serial);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ReTarget, ReTarget),
                    new MacroMenuItem(LocString.Conv2DCT, ConvertToByType)
                };
            }

            return m_MenuItems;
        }

        private void ConvertToByType(object[] args)
        {
            if (m_Gfx != 0 && m_Serial.IsItem && m_Parent != null)
                m_Parent.Convert(this, new DoubleClickTypeAction(m_Gfx, m_Serial.IsItem));
        }

        private void ReTarget(object[] args)
        {
            Targeting.OneTimeTarget(OnReTarget);
            World.Player.SendMessage(MsgLevel.Force, LocString.SelTargAct);
        }

        private void OnReTarget(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            if (serial.IsItem)
            {
                m_Serial = serial;
                m_Gfx = gfx;
            }

            Engine.MainWindow.SafeAction(s => s.ShowMe());

            if (m_Parent != null)
                m_Parent.Update();
        }
    }

    public class DoubleClickTypeAction : MacroAction
    {
        private ushort m_Gfx;
        public bool m_Item;

        private MenuItem[] m_MenuItems;

        public DoubleClickTypeAction(string[] args)
        {
            m_Gfx = Convert.ToUInt16(args[1]);

            try
            {
                m_Item = Convert.ToBoolean(args[2]);
            }
            catch
            {
            }
        }

        public DoubleClickTypeAction(ushort gfx, bool item)
        {
            m_Gfx = gfx;
            m_Item = item;
        }

        public override bool Perform()
        {
            Serial click = Serial.Zero;

            if (m_Item)
            {
                Item item = World.Player.Backpack != null ? World.Player.Backpack.FindItemByID(m_Gfx) : null;
                ArrayList list = new ArrayList();

                if (item == null)
                {
                    foreach (Item i in World.Items.Values)
                    {
                        if (i.ItemID == m_Gfx && i.RootContainer == null)
                        {
                            if (Config.GetBool("RangeCheckDoubleClick"))
                            {
                                if (Utility.InRange(World.Player.Position, i.Position, 2)) list.Add(i);
                            }
                            else
                                list.Add(i);
                        }
                    }

                    if (list.Count == 0)
                    {
                        foreach (Item i in World.Items.Values)
                        {
                            if (i.ItemID == m_Gfx && !i.IsInBank)
                            {
                                if (Config.GetBool("RangeCheckDoubleClick"))
                                {
                                    if (Utility.InRange(World.Player.Position, i.Position, 2)) list.Add(i);
                                }
                                else
                                    list.Add(i);
                            }
                        }
                    }

                    if (list.Count > 0)
                        click = ((Item) list[Utility.Random(list.Count)]).Serial;
                }
                else
                    click = item.Serial;
            }
            else
            {
                ArrayList list = new ArrayList();

                foreach (Mobile m in World.MobilesInRange())
                {
                    if (m.Body == m_Gfx)
                    {
                        if (Config.GetBool("RangeCheckDoubleClick"))
                        {
                            if (Utility.InRange(World.Player.Position, m.Position, 2)) list.Add(m);
                        }
                        else
                            list.Add(m);
                    }
                }

                if (list.Count > 0)
                    click = ((Mobile) list[Utility.Random(list.Count)]).Serial;
            }

            if (click != Serial.Zero)
                PlayerData.DoubleClick(click);
            else
                World.Player.SendMessage(MsgLevel.Force, LocString.NoItemOfType, m_Item ? ((ItemID) m_Gfx).ToString() : string.Format("(Character) 0x{0:X}", m_Gfx));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Gfx, m_Item);
        }

        public override string ToString()
        {
            return Language.Format(LocString.DClickA1, m_Item ? ((ItemID) m_Gfx).ToString() : string.Format("(Character) 0x{0:X}", m_Gfx));
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ReTarget, ReTarget)
                };
            }

            return m_MenuItems;
        }

        private void ReTarget(object[] args)
        {
            Targeting.OneTimeTarget(OnReTarget);
            World.Player.SendMessage(LocString.SelTargAct);
        }

        private void OnReTarget(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            m_Gfx = gfx;
            m_Item = serial.IsItem;

            Engine.MainWindow.SafeAction(s => s.ShowMe());

            if (m_Parent != null)
                m_Parent.Update();
        }
    }

    public class LiftAction : MacroWaitAction
    {
        private ushort m_Amount;
        private readonly ushort m_Gfx;

        private int m_Id;

        private MenuItem[] m_MenuItems;
        private readonly Serial m_Serial;

        public LiftAction(string[] args)
        {
            m_Serial = Serial.Parse(args[1]);
            m_Amount = Convert.ToUInt16(args[2]);
            m_Gfx = Convert.ToUInt16(args[3]);
        }

        public LiftAction(Serial ser, ushort amount, ushort gfx)
        {
            m_Serial = ser;
            m_Amount = amount;
            m_Gfx = gfx;
        }

        public static Item LastLift { get; set; }

        public override bool Perform()
        {
            Item item = World.FindItem(m_Serial);

            if (item != null)
            {
                //DragDropManager.Holding = item;
                LastLift = item;
                m_Id = DragDropManager.Drag(item, m_Amount <= item.Amount ? m_Amount : item.Amount);
            }
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.MacroItemOutRange);

            return false;
        }

        public override bool PerformWait()
        {
            return DragDropManager.LastIDLifted < m_Id;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Serial.Value, m_Amount, m_Gfx);
        }

        public override string ToString()
        {
            return Language.Format(LocString.LiftA10, m_Serial, m_Amount);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ConvLiftByType, ConvertToByType),
                    new MacroMenuItem(LocString.Edit, EditAmount)
                };
            }

            return m_MenuItems;
        }

        private void EditAmount(object[] args)
        {
            if (InputBox.Show(Engine.MainWindow, Language.GetString(LocString.EnterAmount), Language.GetString(LocString.InputReq), m_Amount.ToString()))
            {
                m_Amount = (ushort) InputBox.GetInt(m_Amount);

                if (m_Parent != null)
                    m_Parent.Update();
            }
        }

        private void ConvertToByType(object[] args)
        {
            if (m_Gfx != 0 && m_Parent != null)
                m_Parent.Convert(this, new LiftTypeAction(m_Gfx, m_Amount));
        }
    }

    public class LiftTypeAction : MacroWaitAction
    {
        private ushort m_Amount;
        private readonly ushort m_Gfx;

        private int m_Id;

        private MenuItem[] m_MenuItems;

        public LiftTypeAction(string[] args)
        {
            m_Gfx = Convert.ToUInt16(args[1]);
            m_Amount = Convert.ToUInt16(args[2]);
        }

        public LiftTypeAction(ushort gfx, ushort amount)
        {
            m_Gfx = gfx;
            m_Amount = amount;
        }

        public override bool Perform()
        {
            Item item = World.Player.Backpack != null ? World.Player.Backpack.FindItemByID(m_Gfx) : null;
            /*if ( item == null )
            {
                 ArrayList list = new ArrayList();

                 foreach ( Item i in World.Items.Values )
                 {
                      if ( i.ItemID == m_Gfx && ( i.RootContainer == null || i.IsChildOf( World.Player.Quiver ) ) )
                           list.Add( i );
                 }

                 if ( list.Count > 0 )
                      item = (Item)list[ Utility.Random( list.Count ) ];
            }*/

            if (item != null)
            {
                //DragDropManager.Holding = item;
                ushort amount = m_Amount;

                if (item.Amount < amount)
                    amount = item.Amount;
                LiftAction.LastLift = item;
                //ActionQueue.Enqueue( new LiftRequest( item, amount ) );
                m_Id = DragDropManager.Drag(item, amount);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.NoItemOfType, (ItemID) m_Gfx);
                //MacroManager.Stop();
            }

            return false;
        }

        public override bool PerformWait()
        {
            return DragDropManager.LastIDLifted < m_Id && !DragDropManager.Empty;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Gfx, m_Amount);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, EditAmount)
                };
            }

            return m_MenuItems;
        }

        private void EditAmount(object[] args)
        {
            if (InputBox.Show(Engine.MainWindow, Language.GetString(LocString.EnterAmount), Language.GetString(LocString.InputReq), m_Amount.ToString()))
            {
                m_Amount = (ushort) InputBox.GetInt(m_Amount);

                if (m_Parent != null)
                    m_Parent.Update();
            }
        }

        public override string ToString()
        {
            return Language.Format(LocString.LiftA10, m_Amount, (ItemID) m_Gfx);
        }
    }

    public class DropAction : MacroAction
    {
        private readonly Point3D m_At;
        private readonly Layer m_Layer;

        private MenuItem[] m_MenuItems;
        private readonly Serial m_To;

        public DropAction(string[] args)
        {
            m_To = Serial.Parse(args[1]);
            m_At = Point3D.Parse(args[2]);

            try
            {
                m_Layer = (Layer) byte.Parse(args[3]);
            }
            catch
            {
                m_Layer = Layer.Invalid;
            }
        }

        public DropAction(Serial to, Point3D at) : this(to, at, 0)
        {
        }

        public DropAction(Serial to, Point3D at, Layer layer)
        {
            m_To = to;
            m_At = at;
            m_Layer = layer;
        }

        public override bool Perform()
        {
            if (DragDropManager.Holding != null)
            {
                if (m_Layer > Layer.Invalid && m_Layer <= Layer.LastUserValid)
                {
                    Mobile m = World.FindMobile(m_To);

                    if (m != null)
                        DragDropManager.Drop(DragDropManager.Holding, m, m_Layer);
                }
                else
                    DragDropManager.Drop(DragDropManager.Holding, m_To, m_At);
            }
            else
                World.Player.SendMessage(MsgLevel.Warning, LocString.MacroNoHold);

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_To, m_At, (byte) m_Layer);
        }

        public override string ToString()
        {
            if (m_Layer != Layer.Invalid)
                return Language.Format(LocString.EquipTo, m_To, m_Layer);

            return Language.Format(LocString.DropA2, m_To.IsValid ? m_To.ToString() : "Ground", m_At);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_To.IsValid)
                return null; // Dont allow conversion(s)

            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ConvRelLoc, ConvertToRelLoc)
                };
            }

            return m_MenuItems;
        }

        private void ConvertToRelLoc(object[] args)
        {
            if (!m_To.IsValid && m_Parent != null)
                m_Parent.Convert(this, new DropRelLocAction((sbyte) (m_At.X - World.Player.Position.X), (sbyte) (m_At.Y - World.Player.Position.Y), (sbyte) (m_At.Z - World.Player.Position.Z)));
        }
    }

    public class DropRelLocAction : MacroAction
    {
        private readonly sbyte[] m_Loc;

        public DropRelLocAction(string[] args)
        {
            m_Loc = new sbyte[3]
            {
                Convert.ToSByte(args[1]),
                Convert.ToSByte(args[2]),
                Convert.ToSByte(args[3])
            };
        }

        public DropRelLocAction(sbyte x, sbyte y, sbyte z)
        {
            m_Loc = new sbyte[3] {x, y, z};
        }

        public override bool Perform()
        {
            if (DragDropManager.Holding != null)
                DragDropManager.Drop(DragDropManager.Holding, null, new Point3D((ushort) (World.Player.Position.X + m_Loc[0]), (ushort) (World.Player.Position.Y + m_Loc[1]), (short) (World.Player.Position.Z + m_Loc[2])));
            else
                World.Player.SendMessage(LocString.MacroNoHold);

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Loc[0], m_Loc[1], m_Loc[2]);
        }

        public override string ToString()
        {
            return Language.Format(LocString.DropRelA3, m_Loc[0], m_Loc[1], m_Loc[2]);
        }
    }

    public class GumpResponseAction : MacroAction
    {
        private int m_ButtonID;

        private MenuItem[] m_MenuItems;
        private int[] m_Switches;
        private GumpTextEntry[] m_TextEntries;

        public GumpResponseAction(string[] args)
        {
            m_ButtonID = Convert.ToInt32(args[1]);
            m_Switches = new int[Convert.ToInt32(args[2])];

            for (int i = 0; i < m_Switches.Length; i++)
                m_Switches[i] = Convert.ToInt32(args[3 + i]);
            m_TextEntries = new GumpTextEntry[Convert.ToInt32(args[3 + m_Switches.Length])];

            for (int i = 0; i < m_TextEntries.Length; i++)
            {
                string[] split = args[4 + m_Switches.Length + i].Split('&');
                m_TextEntries[i].EntryID = Convert.ToUInt16(split[0]);
                m_TextEntries[i].Text = split[1];
            }
        }

        public GumpResponseAction(int button, int[] switches, GumpTextEntry[] entries)
        {
            m_ButtonID = button;
            m_Switches = switches;
            m_TextEntries = entries;
        }

        public override bool Perform()
        {
            ClientCommunication.SendToClient(new CloseGump(World.Player.CurrentGumpI));
            ClientCommunication.SendToServer(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, m_ButtonID, m_Switches, m_TextEntries));
            World.Player.HasGump = false;

            return true;
        }

        public override string Serialize()
        {
            ArrayList list = new ArrayList(3 + m_Switches.Length + m_TextEntries.Length);
            list.Add(m_ButtonID);
            list.Add(m_Switches.Length);
            list.AddRange(m_Switches);
            list.Add(m_TextEntries.Length);

            for (int i = 0; i < m_TextEntries.Length; i++)
                list.Add(string.Format("{0}&{1}", m_TextEntries[i].EntryID, m_TextEntries[i].Text));

            return DoSerialize((object[]) list.ToArray(typeof(object)));
        }

        public override string ToString()
        {
            if (m_ButtonID != 0)
                return Language.Format(LocString.GumpRespB, m_ButtonID);

            return Language.Format(LocString.CloseGump);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.UseLastGumpResponse, UseLastResponse),
                    new MacroMenuItem(LocString.Edit, Edit)
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            if (InputBox.Show(Language.GetString(LocString.EnterNewText), "Input Box", m_ButtonID.ToString()))
                m_ButtonID = InputBox.GetInt();

            Parent?.Update();
        }

        private void UseLastResponse(object[] args)
        {
            m_ButtonID = World.Player.LastGumpResponseAction.m_ButtonID;
            m_Switches = World.Player.LastGumpResponseAction.m_Switches;
            m_TextEntries = World.Player.LastGumpResponseAction.m_TextEntries;

            World.Player.SendMessage(MsgLevel.Force, "Set GumpResponse to last response");

            Parent?.Update();
        }
    }

    public class MenuResponseAction : MacroAction
    {
        private readonly ushort m_Index;
        private readonly ushort m_ItemID;
        private readonly ushort m_Hue;

        public MenuResponseAction(string[] args)
        {
            m_Index = Convert.ToUInt16(args[1]);
            m_ItemID = Convert.ToUInt16(args[2]);
            m_Hue = Convert.ToUInt16(args[3]);
        }

        public MenuResponseAction(ushort idx, ushort iid, ushort hue)
        {
            m_Index = idx;
            m_ItemID = iid;
            m_Hue = hue;
        }

        public override bool Perform()
        {
            ClientCommunication.SendToServer(new MenuResponse(World.Player.CurrentMenuS, World.Player.CurrentMenuI, m_Index, m_ItemID, m_Hue));
            World.Player.HasMenu = false;

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Index, m_ItemID, m_Hue);
        }

        public override string ToString()
        {
            return Language.Format(LocString.MenuRespA1, m_Index);
        }
    }

    public class AbsoluteTargetAction : MacroAction
    {
        private readonly TargetInfo m_Info;

        private MenuItem[] m_MenuItems;

        public AbsoluteTargetAction(string[] args)
        {
            m_Info = new TargetInfo();

            m_Info.Type = Convert.ToByte(args[1]);
            m_Info.Flags = Convert.ToByte(args[2]);
            m_Info.Serial = Convert.ToUInt32(args[3]);
            m_Info.X = Convert.ToUInt16(args[4]);
            m_Info.Y = Convert.ToUInt16(args[5]);
            m_Info.Z = Convert.ToInt16(args[6]);
            m_Info.Gfx = Convert.ToUInt16(args[7]);
        }

        public AbsoluteTargetAction(TargetInfo info)
        {
            m_Info = new TargetInfo();
            m_Info.Type = info.Type;
            m_Info.Flags = info.Flags;
            m_Info.Serial = info.Serial;
            m_Info.X = info.X;
            m_Info.Y = info.Y;
            m_Info.Z = info.Z;
            m_Info.Gfx = info.Gfx;
        }

        public override bool Perform()
        {
            Targeting.Target(m_Info);

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Info.Type, m_Info.Flags, m_Info.Serial.Value, m_Info.X, m_Info.Y, m_Info.Z, m_Info.Gfx);
        }

        public override string ToString()
        {
            return Language.GetString(LocString.AbsTarg);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ReTarget, ReTarget),
                    new MacroMenuItem(LocString.ConvLT, ConvertToLastTarget),
                    new MacroMenuItem(LocString.ConvTargType, ConvertToByType),
                    new MacroMenuItem(LocString.ConvRelLoc, ConvertToRelLoc)
                };
            }

            return m_MenuItems;
        }

        private void ReTarget(object[] args)
        {
            Targeting.OneTimeTarget(!m_Info.Serial.IsValid, ReTargetResponse);
            World.Player.SendMessage(MsgLevel.Force, LocString.SelTargAct);
        }

        private void ReTargetResponse(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            m_Info.Gfx = gfx;
            m_Info.Serial = serial;
            m_Info.Type = (byte) (ground ? 1 : 0);
            m_Info.X = pt.X;
            m_Info.Y = pt.Y;
            m_Info.Z = pt.Z;

            Engine.MainWindow.SafeAction(s => s.ShowMe());

            if (m_Parent != null)
                m_Parent.Update();
        }

        private void ConvertToLastTarget(object[] args)
        {
            if (m_Parent != null)
                m_Parent.Convert(this, new LastTargetAction());
        }

        private void ConvertToByType(object[] args)
        {
            if (m_Parent != null)
                m_Parent.Convert(this, new TargetTypeAction(m_Info.Serial.IsMobile, m_Info.Gfx));
        }

        private void ConvertToRelLoc(object[] args)
        {
            if (m_Parent != null)
                m_Parent.Convert(this, new TargetRelLocAction((sbyte) (m_Info.X - World.Player.Position.X), (sbyte) (m_Info.Y - World.Player.Position.Y))); //, (sbyte)(m_Info.Z - World.Player.Position.Z) ) );
        }
    }

    /// <summary>
    ///     Action to handle variable macros to alleviate the headache of having multiple macros for the same thing
    ///     This Action does break the pattern that you see in every other action because the data that is stored for this
    ///     action exists not in the Macro file, but in a different file that has all the variables
    ///     TODO: Re-eval this concept and instead store all data
    /// </summary>
    public class AbsoluteTargetVariableAction : MacroAction
    {
        private readonly string _variableName;
        private TargetInfo _target;

        public AbsoluteTargetVariableAction(string[] args)
        {
            _variableName = args[1];
        }

        public override bool Perform()
        {
            _target = null;

            foreach (AbsoluteTargets.AbsoluteTarget at in AbsoluteTargets.AbsoluteTargetList)
            {
                if (at.TargetVariableName.Equals(_variableName))
                {
                    _target = at.TargetInfo;

                    break;
                }
            }

            if (_target != null)
            {
                Targeting.Target(_target);

                return true;
            }

            return false;
        }

        public override string Serialize()
        {
            return DoSerialize(_variableName);
        }

        public override string ToString()
        {
            return $"{Language.GetString(LocString.AbsTarg)} (${_variableName})";
        }

        /*private MenuItem[] m_MenuItems;
        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new MacroMenuItem[]
                {
                    new MacroMenuItem( LocString.ReTarget, ReTarget )
                };
            }

            return m_MenuItems;
        }

        private void ReTarget(object[] args)
        {
            Targeting.OneTimeTarget(!_target.Serial.IsValid, new Targeting.TargetResponseCallback(ReTargetResponse));
            World.Player.SendMessage(MsgLevel.Force, LocString.SelTargAct);
        }

        private void ReTargetResponse(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            _target.Gfx = gfx;
            _target.Serial = serial;
            _target.Type = (byte)(ground ? 1 : 0);
            _target.X = pt.X;
            _target.Y = pt.Y;
            _target.Z = pt.Z;

            Engine.MainWindow.SafeAction(s => s.ShowMe());

            m_Parent?.Update();
        }*/
    }

    public class TargetTypeAction : MacroAction
    {
        private object _previousObject;
        private ushort m_Gfx;

        private MenuItem[] m_MenuItems;
        private bool m_Mobile;

        public TargetTypeAction(string[] args)
        {
            m_Mobile = Convert.ToBoolean(args[1]);
            m_Gfx = Convert.ToUInt16(args[2]);
        }

        public TargetTypeAction(bool mobile, ushort gfx)
        {
            m_Mobile = mobile;
            m_Gfx = gfx;
        }

        public override bool Perform()
        {
            ArrayList list = new ArrayList();

            if (m_Mobile)
            {
                foreach (Mobile find in World.MobilesInRange())
                {
                    if (find.Body == m_Gfx)
                    {
                        if (Config.GetBool("RangeCheckTargetByType"))
                        {
                            if (Utility.InRange(World.Player.Position, find.Position, 2)) list.Add(find);
                        }
                        else
                            list.Add(find);
                    }
                }
            }
            else
            {
                foreach (Item i in World.Items.Values)
                {
                    if (i.ItemID == m_Gfx && !i.IsInBank)
                    {
                        if (Config.GetBool("RangeCheckTargetByType"))
                        {
                            if (Utility.InRange(World.Player.Position, i.Position, 2)) list.Add(i);
                        }
                        else
                            list.Add(i);
                    }
                }
            }

            if (list.Count > 0)
            {
                if (Config.GetBool("DiffTargetByType") && list.Count > 1)
                {
                    object currentObject = list[Utility.Random(list.Count)];

                    while (_previousObject != null && _previousObject == currentObject) currentObject = list[Utility.Random(list.Count)];

                    Targeting.Target(currentObject);

                    _previousObject = currentObject;
                }
                else
                    Targeting.Target(list[Utility.Random(list.Count)]);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.NoItemOfType,
                                         m_Mobile ? string.Format("Character [{0}]", m_Gfx) : ((ItemID) m_Gfx).ToString());
            }

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Mobile, m_Gfx);
        }

        public override string ToString()
        {
            if (m_Mobile)
                return Language.Format(LocString.TargByType, m_Gfx);

            return Language.Format(LocString.TargByType, (ItemID) m_Gfx);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ReTarget, ReTarget),
                    new MacroMenuItem(LocString.ConvLT, ConvertToLastTarget)
                };
            }

            return m_MenuItems;
        }

        private void ReTarget(object[] args)
        {
            Targeting.OneTimeTarget(false, ReTargetResponse);
            World.Player.SendMessage(MsgLevel.Force, LocString.SelTargAct);
        }

        private void ReTargetResponse(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            if (!ground && serial.IsValid)
            {
                m_Mobile = serial.IsMobile;
                m_Gfx = gfx;
            }

            Engine.MainWindow.SafeAction(s => s.ShowMe());

            if (m_Parent != null)
                m_Parent.Update();
        }

        private void ConvertToLastTarget(object[] args)
        {
            if (m_Parent != null)
                m_Parent.Convert(this, new LastTargetAction());
        }
    }

    public class TargetRelLocAction : MacroAction
    {
        private MenuItem[] m_MenuItems;
        private sbyte m_X, m_Y;

        public TargetRelLocAction(string[] args)
        {
            m_X = Convert.ToSByte(args[1]);
            m_Y = Convert.ToSByte(args[2]);
        }

        public TargetRelLocAction(sbyte x, sbyte y)
        {
            m_X = x;
            m_Y = y;
        }

        public override bool Perform()
        {
            ushort x = (ushort) (World.Player.Position.X + m_X);
            ushort y = (ushort) (World.Player.Position.Y + m_Y);
            short z = (short) World.Player.Position.Z;

            try
            {
                HuedTile tile = Map.GetTileNear(World.Player.Map, x, y, z);
                Targeting.Target(new Point3D(x, y, tile.Z), tile.ID);
            }
            catch (Exception e)
            {
                World.Player.SendMessage(MsgLevel.Debug, "Error Executing TargetRelLoc: {0}", e.Message);
            }

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_X, m_Y);
        }

        public override string ToString()
        {
            return Language.Format(LocString.TargRelLocA3, m_X, m_Y, 0);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.ReTarget, ReTarget)
                };
            }

            return m_MenuItems;
        }

        private void ReTarget(object[] args)
        {
            Engine.MainWindow.SafeAction(s => s.ShowMe());

            Targeting.OneTimeTarget(true, ReTargetResponse);
            World.Player.SendMessage(LocString.SelTargAct);
        }

        private void ReTargetResponse(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            m_X = (sbyte) (pt.X - World.Player.Position.X);
            m_Y = (sbyte) (pt.Y - World.Player.Position.Y);

            // m_Z = (sbyte)(pt.Z - World.Player.Position.Z);
            if (m_Parent != null)
                m_Parent.Update();
        }
    }

    public class LastTargetAction : MacroAction
    {
        public override bool Perform()
        {
            if (!Targeting.DoLastTarget()) //Targeting.LastTarget( true );
                Targeting.ResendTarget();

            return true;
        }

        public override string ToString()
        {
            return string.Format("Exec: {0}", Language.GetString(LocString.LastTarget));
        }
    }

    public class SetLastTargetAction : MacroWaitAction
    {
        public override bool Perform()
        {
            Targeting.TargetSetLastTarget();

            return !PerformWait();
        }

        public override bool PerformWait()
        {
            return !Targeting.LTWasSet;
        }

        public override string ToString()
        {
            return Language.GetString(LocString.SetLT);
        }
    }

    public class SpeechAction : MacroAction
    {
        private readonly ushort m_Font;
        private readonly ushort m_Hue;
        private readonly ArrayList m_Keywords;
        private readonly string m_Lang;

        private MenuItem[] m_MenuItems;
        private string m_Speech;
        private readonly MessageType m_Type;

        public SpeechAction(string[] args)
        {
            m_Type = (MessageType) Convert.ToInt32(args[1]) & ~MessageType.Encoded;
            m_Hue = Convert.ToUInt16(args[2]);
            m_Font = Convert.ToUInt16(args[3]);
            m_Lang = args[4];

            int count = Convert.ToInt32(args[5]);

            if (count > 0)
            {
                m_Keywords = new ArrayList(count);
                m_Keywords.Add(Convert.ToUInt16(args[6]));

                for (int i = 1; i < count; i++)
                    m_Keywords.Add(Convert.ToByte(args[6 + i]));
            }

            m_Speech = args[6 + count];
        }

        public SpeechAction(MessageType type, ushort hue, ushort font, string lang, ArrayList kw, string speech)
        {
            m_Type = type;
            m_Hue = hue;
            m_Font = font;
            m_Lang = lang;
            m_Keywords = kw;
            m_Speech = speech;
        }

        public override bool Perform()
        {
            if (m_Speech.Length > 1 && m_Speech[0] == '-')
            {
                string text = m_Speech.Substring(1);
                string[] split = text.Split(' ', '\t');
                CommandCallback call = Command.List[split[0]];

                if (call == null && text[0] == '-')
                {
                    call = Command.List["-"];

                    if (call != null && split.Length > 1 && split[1] != null && split[1].Length > 1)
                        split[1] = split[1].Substring(1);
                }

                if (call != null)
                {
                    ArrayList list = new ArrayList();

                    for (int i = 1; i < split.Length; i++)
                    {
                        if (split[i] != null && split[i].Length > 0)
                            list.Add(split[i]);
                    }

                    call((string[]) list.ToArray(typeof(string)));

                    return true;
                }
            }

            int hue = m_Hue;

            if (m_Type != MessageType.Emote)
            {
                if (World.Player.SpeechHue == 0)
                    World.Player.SpeechHue = m_Hue;
                hue = World.Player.SpeechHue;
            }

            ClientCommunication.SendToServer(new ClientUniMessage(m_Type, hue, m_Font, m_Lang, m_Keywords, m_Speech));

            return true;
        }

        public override string Serialize()
        {
            ArrayList list = new ArrayList(6);
            list.Add((int) m_Type);
            list.Add(m_Hue);
            list.Add(m_Font);
            list.Add(m_Lang);

            if (m_Keywords != null && m_Keywords.Count > 1)
            {
                list.Add(m_Keywords.Count);

                for (int i = 0; i < m_Keywords.Count; i++)
                    list.Add(m_Keywords[i]);
            }
            else
                list.Add("0");

            list.Add(m_Speech);

            return DoSerialize((object[]) list.ToArray(typeof(object)));
        }

        public override string ToString()
        {
            //return Language.Format( LocString.SayQA1, m_Speech );
            StringBuilder sb = new StringBuilder();

            switch (m_Type)
            {
                case MessageType.Emote:
                    sb.Append("Emote: ");

                    break;
                case MessageType.Whisper:
                    sb.Append("Whisper: ");

                    break;
                case MessageType.Yell:
                    sb.Append("Yell: ");

                    break;
                case MessageType.Regular:
                default:
                    sb.Append("Say: ");

                    break;
            }

            sb.Append(m_Speech);

            return sb.ToString();
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new MacroMenuItem[1]
                {
                    new MacroMenuItem(LocString.Edit, Edit)
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            if (InputBox.Show(Language.GetString(LocString.EnterNewText), "Input Box", m_Speech))
                m_Speech = InputBox.GetString();

            if (Parent == null)
                return;

            Parent.Update();
        }
    }

    public class UseSkillAction : MacroAction
    {
        private readonly int m_Skill;

        public UseSkillAction(string[] args)
        {
            m_Skill = Convert.ToInt32(args[1]);
        }

        public UseSkillAction(int sk)
        {
            m_Skill = sk;
        }

        public override bool Perform()
        {
            ClientCommunication.SendToServer(new UseSkill(m_Skill));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Skill);
        }

        public override string ToString()
        {
            return Language.Format(LocString.UseSkillA1, Language.Skill2Str(m_Skill));
        }
    }

    public class ExtCastSpellAction : MacroAction
    {
        private readonly Serial m_Book;
        private readonly Spell m_Spell;

        public ExtCastSpellAction(string[] args)
        {
            m_Spell = Spell.Get(Convert.ToInt32(args[1]));
            m_Book = Serial.Parse(args[2]);
        }

        public ExtCastSpellAction(int s, Serial book)
        {
            m_Spell = Spell.Get(s);
            m_Book = book;
        }

        public ExtCastSpellAction(Spell s, Serial book)
        {
            m_Spell = s;
            m_Book = book;
        }

        public override bool Perform()
        {
            m_Spell.OnCast(new ExtCastSpell(m_Book, (ushort) m_Spell.GetID()));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Spell.GetID(), m_Book.Value);
        }

        public override string ToString()
        {
            return Language.Format(LocString.CastSpellA1, m_Spell);
        }
    }

    public class BookCastSpellAction : MacroAction
    {
        private readonly Serial m_Book;
        private readonly Spell m_Spell;

        public BookCastSpellAction(string[] args)
        {
            m_Spell = Spell.Get(Convert.ToInt32(args[1]));
            m_Book = Serial.Parse(args[2]);
        }

        public BookCastSpellAction(int s, Serial book)
        {
            m_Spell = Spell.Get(s);
            m_Book = book;
        }

        public BookCastSpellAction(Spell s, Serial book)
        {
            m_Spell = s;
            m_Book = book;
        }

        public override bool Perform()
        {
            m_Spell.OnCast(new CastSpellFromBook(m_Book, (ushort) m_Spell.GetID()));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Spell.GetID(), m_Book.Value);
        }

        public override string ToString()
        {
            return Language.Format(LocString.CastSpellA1, m_Spell);
        }
    }

    public class MacroCastSpellAction : MacroAction
    {
        private readonly Spell m_Spell;

        public MacroCastSpellAction(string[] args)
        {
            m_Spell = Spell.Get(Convert.ToInt32(args[1]));
        }

        public MacroCastSpellAction(int s)
        {
            m_Spell = Spell.Get(s);
        }

        public MacroCastSpellAction(Spell s)
        {
            m_Spell = s;
        }

        public override bool Perform()
        {
            m_Spell.OnCast(new CastSpellFromMacro((ushort) m_Spell.GetID()));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Spell.GetID());
        }

        public override string ToString()
        {
            return Language.Format(LocString.CastSpellA1, m_Spell);
        }
    }

    public class SetAbilityAction : MacroAction
    {
        private readonly AOSAbility m_Ability;

        public SetAbilityAction(string[] args)
        {
            m_Ability = (AOSAbility) Convert.ToInt32(args[1]);
        }

        public SetAbilityAction(AOSAbility a)
        {
            m_Ability = a;
        }

        public override bool Perform()
        {
            ClientCommunication.SendToServer(new UseAbility(m_Ability));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize((int) m_Ability);
        }

        public override string ToString()
        {
            return Language.Format(LocString.SetAbilityA1, m_Ability);
        }
    }

    public class DressAction : MacroWaitAction
    {
        private readonly string m_Name;

        public DressAction(string[] args)
        {
            m_Name = args[1];
        }

        public DressAction(string name)
        {
            m_Name = name;
        }

        public override bool Perform()
        {
            DressList list = DressList.Find(m_Name);

            if (list != null)
            {
                list.Dress();

                return false;
            }

            return true;
        }

        public override bool PerformWait()
        {
            return !ActionQueue.Empty;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Name);
        }

        public override string ToString()
        {
            return Language.Format(LocString.DressA1, m_Name);
        }
    }

    public class UnDressAction : MacroWaitAction
    {
        private readonly byte m_Layer;
        private readonly string m_Name;

        public UnDressAction(string[] args)
        {
            try
            {
                m_Layer = Convert.ToByte(args[2]);
            }
            catch
            {
                m_Layer = 255;
            }

            if (m_Layer == 255)
                m_Name = args[1];
            else
                m_Name = "";
        }

        public UnDressAction(string name)
        {
            m_Name = name;
            m_Layer = 255;
        }

        public UnDressAction(byte layer)
        {
            m_Layer = layer;
            m_Name = "";
        }

        public override bool Perform()
        {
            if (m_Layer == 255)
            {
                DressList list = DressList.Find(m_Name);

                if (list != null)
                {
                    list.Undress();

                    return false;
                }

                return true;
            }

            if (m_Layer == 0)
            {
                UndressHotKeys.OnUndressAll();

                return false;
            }

            return !UndressHotKeys.Unequip((Layer) m_Layer);
        }

        public override bool PerformWait()
        {
            return !ActionQueue.Empty;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Name, m_Layer);
        }

        public override string ToString()
        {
            if (m_Layer == 255)
                return Language.Format(LocString.UndressA1, m_Name);

            if (m_Layer == 0)
                return Language.GetString(LocString.UndressAll);

            return Language.Format(LocString.UndressLayerA1, (Layer) m_Layer);
        }
    }

    public class WalkAction : MacroWaitAction
    {
        private static readonly uint WM_KEYDOWN = 0x100;
        private static uint WM_KEYUP = 0x101;
        private readonly Direction m_Dir;

        public WalkAction(string[] args)
        {
            m_Dir = (Direction) Convert.ToByte(args[1]) & Direction.Mask;
        }

        public WalkAction(Direction dir)
        {
            m_Dir = dir & Direction.Mask;
        }

        public static DateTime LastWalkTime { get; set; } = DateTime.MinValue;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        //private static int m_LastSeq = -1;
        public override bool Perform()
        {
            return !PerformWait();
        }

        //public static bool IsMacroWalk(byte seq)
        //{
        //    return m_LastSeq != -1 && m_LastSeq == (int)seq && World.Player.HasWalkEntry((byte)m_LastSeq);
        //}

        public override bool PerformWait()
        {
            if (LastWalkTime + TimeSpan.FromSeconds(0.4) >= DateTime.UtcNow)
                return true;

            //m_LastSeq = World.Player.WalkSequence;
            LastWalkTime = DateTime.UtcNow;

            //ClientCommunication.SendToClient(new MobileUpdate(World.Player));
            //ClientCommunication.SendToClient(new ForceWalk(m_Dir));
            //ClientCommunication.SendToServer(new WalkRequest(m_Dir, World.Player.WalkSequence));
            //World.Player.MoveReq(m_Dir, World.Player.WalkSequence);

            int direction;

            switch (m_Dir)
            {
                case Direction.Down:
                    direction = (int) KeyboardDir.Down;

                    break;
                case Direction.East:
                    direction = (int) KeyboardDir.East;

                    break;
                case Direction.Left:
                    direction = (int) KeyboardDir.Left;

                    break;
                case Direction.North:
                    direction = (int) KeyboardDir.North;

                    break;
                case Direction.Right:
                    direction = (int) KeyboardDir.Right;

                    break;
                case Direction.South:
                    direction = (int) KeyboardDir.South;

                    break;
                case Direction.Up:
                    direction = (int) KeyboardDir.Up;

                    break;
                case Direction.West:
                    direction = (int) KeyboardDir.West;

                    break;
                default:
                    direction = (int) KeyboardDir.Up;

                    break;
            }

            SendMessage(ClientCommunication.ClientWindow, WM_KEYDOWN, (IntPtr) direction, (IntPtr) 1);

            return false;
        }

        public override string Serialize()
        {
            return DoSerialize((byte) m_Dir);
        }

        public override string ToString()
        {
            return Language.Format(LocString.WalkA1, m_Dir != Direction.Mask ? m_Dir.ToString() : "Up");
        }

        private enum KeyboardDir
        {
            North = 0x21, //page up
            Right = 0x27, // right
            East = 0x22, // page down
            Down = 0x28, // down
            South = 0x23, // end
            Left = 0x25, // left
            West = 0x24, // home
            Up = 0x26 // up
        }
    }

    public class WaitForMenuAction : MacroWaitAction
    {
        private readonly uint m_MenuID;

        private MenuItem[] m_MenuItems;

        public WaitForMenuAction(uint gid)
        {
            m_MenuID = gid;
        }

        public WaitForMenuAction(string[] args)
        {
            if (args.Length > 1)
                m_MenuID = Convert.ToUInt32(args[1]);

            try
            {
                m_Timeout = TimeSpan.FromSeconds(Convert.ToDouble(args[2]));
            }
            catch
            {
            }
        }

        public override bool Perform()
        {
            return !PerformWait();
        }

        public override bool PerformWait()
        {
            return !(World.Player.HasMenu && (World.Player.CurrentGumpI == m_MenuID || m_MenuID == 0));
        }

        public override string ToString()
        {
            if (m_MenuID == 0)
                return Language.GetString(LocString.WaitAnyMenu);

            return Language.Format(LocString.WaitMenuA1, m_MenuID);
        }

        public override string Serialize()
        {
            return DoSerialize(m_MenuID, m_Timeout.TotalSeconds);
        }

        public override bool CheckMatch(MacroAction a)
        {
            if (a is WaitForMenuAction)
            {
                if (m_MenuID == 0 || ((WaitForMenuAction) a).m_MenuID == m_MenuID)
                    return true;
            }

            return false;
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit),
                    EditTimeoutMenuItem
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            new MacroInsertWait(this).ShowDialog(Engine.MainWindow);
        }
    }

    public class WaitForGumpAction : MacroWaitAction
    {
        private readonly uint m_GumpID;

        private MenuItem[] m_MenuItems;
        private bool m_Strict;

        public WaitForGumpAction()
        {
            m_GumpID = 0;
            m_Strict = false;
        }

        public WaitForGumpAction(uint gid)
        {
            m_GumpID = gid;
            m_Strict = false;
        }

        public WaitForGumpAction(string[] args)
        {
            m_GumpID = Convert.ToUInt32(args[1]);

            try
            {
                m_Strict = Convert.ToBoolean(args[2]);
            }
            catch
            {
                m_Strict = false;
            }

            try
            {
                m_Timeout = TimeSpan.FromSeconds(Convert.ToDouble(args[3]));
            }
            catch
            {
            }
        }

        public override bool Perform()
        {
            return !PerformWait();
        }

        public override bool PerformWait()
        {
            return !(World.Player.HasGump && (World.Player.CurrentGumpI == m_GumpID || !m_Strict || m_GumpID == 0));

            //if (!World.Player.HasGump) // Does the player even have a gump?
            //    return true;

            //if ((int)World.Player.CurrentGumpI != (int)m_GumpID && m_Strict)
            //    return m_GumpID > 0;

            //return false;
        }

        public override string ToString()
        {
            if (m_GumpID == 0 || !m_Strict)
                return Language.GetString(LocString.WaitAnyGump);

            return Language.Format(LocString.WaitGumpA1, m_GumpID);
        }

        public override string Serialize()
        {
            return DoSerialize(m_GumpID, m_Strict, m_Timeout.TotalSeconds);
        }

        public override bool CheckMatch(MacroAction a)
        {
            if (a is WaitForGumpAction)
            {
                if (m_GumpID == 0 || ((WaitForGumpAction) a).m_GumpID == m_GumpID)
                    return true;
            }

            return false;
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit),
                    new MacroMenuItem(LocString.Null, ToggleStrict),
                    EditTimeoutMenuItem
                };
            }

            if (!m_Strict)
                m_MenuItems[1].Text = string.Format("Change to \"{0}\"", Language.Format(LocString.WaitGumpA1, m_GumpID));
            else
                m_MenuItems[1].Text = string.Format("Change to \"{0}\"", Language.GetString(LocString.WaitAnyGump));
            m_MenuItems[1].Enabled = m_GumpID != 0 || m_Strict;

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            new MacroInsertWait(this).ShowDialog(Engine.MainWindow);
        }

        private void ToggleStrict(object[] args)
        {
            m_Strict = !m_Strict;

            if (m_Parent != null)
                m_Parent.Update();
        }
    }

    public class WaitForTargetAction : MacroWaitAction
    {
        private MenuItem[] m_MenuItems;

        public WaitForTargetAction()
        {
            m_Timeout = TimeSpan.FromSeconds(30.0);
        }

        public WaitForTargetAction(string[] args)
        {
            try
            {
                m_Timeout = TimeSpan.FromSeconds(Convert.ToDouble(args[1]));
            }
            catch
            {
                m_Timeout = TimeSpan.FromSeconds(30.0);
            }
        }

        public override bool Perform()
        {
            return !PerformWait();
        }

        public override bool PerformWait()
        {
            return !Targeting.HasTarget;
        }

        public override string ToString()
        {
            return Language.GetString(LocString.WaitTarg);
        }

        public override string Serialize()
        {
            return DoSerialize(m_Timeout.TotalSeconds);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit),
                    EditTimeoutMenuItem
                };
            }

            return m_MenuItems;
        }

        public override bool CheckMatch(MacroAction a)
        {
            return a is WaitForTargetAction;
        }

        private void Edit(object[] args)
        {
            new MacroInsertWait(this).ShowDialog(Engine.MainWindow);
        }
    }

    public class PauseAction : MacroWaitAction
    {
        private MenuItem[] m_MenuItems;

        public PauseAction(string[] args)
        {
            m_Timeout = TimeSpan.Parse(args[1]);
        }

        public PauseAction(int ms)
        {
            m_Timeout = TimeSpan.FromMilliseconds(ms);
        }

        public PauseAction(TimeSpan time)
        {
            m_Timeout = time;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Timeout);
        }

        public override bool Perform()
        {
            StartTime = DateTime.UtcNow;

            return !PerformWait();
        }

        public override bool PerformWait()
        {
            return StartTime + m_Timeout >= DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Language.Format(LocString.PauseA1, m_Timeout.TotalSeconds);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit)
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            new MacroInsertWait(this).ShowDialog(Engine.MainWindow);
        }
    }

    public class WaitForStatAction : MacroWaitAction
    {
        private MenuItem[] m_MenuItems;

        public WaitForStatAction(string[] args)
        {
            Stat = (IfAction.IfVarType) Convert.ToInt32(args[1]);
            Op = Convert.ToByte(args[2]);
            Amount = Convert.ToInt32(args[3]);

            try
            {
                m_Timeout = TimeSpan.FromSeconds(Convert.ToDouble(args[4]));
            }
            catch
            {
                m_Timeout = TimeSpan.FromMinutes(60.0);
            }
        }

        public WaitForStatAction(IfAction.IfVarType stat, byte dir, int val)
        {
            Stat = stat;
            Op = dir;
            Amount = val;

            m_Timeout = TimeSpan.FromMinutes(60.0);
        }

        public byte Op { get; }
        public int Amount { get; }
        public IfAction.IfVarType Stat { get; }

        public override string Serialize()
        {
            return DoSerialize((int) Stat, Op, Amount, m_Timeout.TotalSeconds);
        }

        public override bool Perform()
        {
            return !PerformWait();
        }

        public override bool PerformWait()
        {
            if (Op > 0)
            {
                // wait for m_Stat >= m_Value
                switch (Stat)
                {
                    case IfAction.IfVarType.Hits:

                        return World.Player.Hits < Amount;
                    case IfAction.IfVarType.Mana:

                        return World.Player.Mana < Amount;
                    case IfAction.IfVarType.Stamina:

                        return World.Player.Stam < Amount;
                }
            }
            else
            {
                // wait for m_Stat <= m_Value
                switch (Stat)
                {
                    case IfAction.IfVarType.Hits:

                        return World.Player.Hits > Amount;
                    case IfAction.IfVarType.Mana:

                        return World.Player.Mana > Amount;
                    case IfAction.IfVarType.Stamina:

                        return World.Player.Stam > Amount;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return Language.Format(LocString.WaitA3, Stat, Op > 0 ? ">=" : "<=", Amount);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit),
                    EditTimeoutMenuItem
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            new MacroInsertWait(this).ShowDialog(Engine.MainWindow);
        }
    }

    public class IfAction : MacroAction
    {
        public enum IfVarType
        {
            Hits = 0,
            Mana,
            Stamina,
            Poisoned,
            SysMessage,
            Weight,
            Mounted,
            RHandEmpty,
            LHandEmpty,

            BeginCountersMarker,

            Counter = 50
        }

        private Counter m_CountObj;

        private MenuItem[] m_MenuItems;

        public IfAction(string[] args)
        {
            Variable = (IfVarType) Convert.ToInt32(args[1]);

            try
            {
                Op = Convert.ToSByte(args[2]);

                if (Op > 1)
                    Op = 0;
            }
            catch
            {
                Op = -1;
            }

            if (Variable != IfVarType.SysMessage)
                Value = Convert.ToInt32(args[3]);
            else
                Value = args[3].ToLower();

            if (Variable == IfVarType.Counter)
                Counter = args[4];
        }

        public IfAction(IfVarType var, sbyte dir, int val)
        {
            Variable = var;
            Op = dir;
            Value = val;
        }

        public IfAction(IfVarType var, sbyte dir, int val, string counter)
        {
            Variable = var;
            Op = dir;
            Value = val;
            Counter = counter;
        }

        public IfAction(IfVarType var, string text)
        {
            Variable = var;
            Value = text.ToLower();
        }

        public sbyte Op { get; }
        public object Value { get; }
        public IfVarType Variable { get; }
        public string Counter { get; }

        public override string Serialize()
        {
            if (Variable == IfVarType.Counter && Counter != null)
                return DoSerialize((int) Variable, Op, Value, Counter);

            return DoSerialize((int) Variable, Op, Value);
        }

        public override bool Perform()
        {
            return true;
        }

        public bool Evaluate()
        {
            switch (Variable)
            {
                case IfVarType.Hits:
                case IfVarType.Mana:
                case IfVarType.Stamina:
                case IfVarType.Weight:

                {
                    int val = (int) Value;

                    if (Op > 0)
                    {
                        // if stat >= m_Value
                        switch (Variable)
                        {
                            case IfVarType.Hits:

                                return World.Player.Hits >= val;
                            case IfVarType.Mana:

                                return World.Player.Mana >= val;
                            case IfVarType.Stamina:

                                return World.Player.Stam >= val;
                            case IfVarType.Weight:

                                return World.Player.Weight >= val;
                        }
                    }
                    else
                    {
                        // if stat <= m_Value
                        switch (Variable)
                        {
                            case IfVarType.Hits:

                                return World.Player.Hits <= val;
                            case IfVarType.Mana:

                                return World.Player.Mana <= val;
                            case IfVarType.Stamina:

                                return World.Player.Stam <= val;
                            case IfVarType.Weight:

                                return World.Player.Weight <= val;
                        }
                    }

                    return false;
                }

                case IfVarType.Poisoned:

                {
                    if (Windows.AllowBit(FeatureBit.BlockHealPoisoned))
                        return World.Player.Poisoned;

                    return false;
                }

                case IfVarType.SysMessage:

                {
                    string text = (string) Value;

                    for (int i = PacketHandlers.SysMessages.Count - 1; i >= 0; i--)
                    {
                        string sys = PacketHandlers.SysMessages[i];

                        if (sys.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            PacketHandlers.SysMessages.RemoveRange(0, i + 1);

                            return true;
                        }
                    }

                    return false;
                }

                case IfVarType.Mounted:

                {
                    return World.Player.GetItemOnLayer(Layer.Mount) != null;
                }

                case IfVarType.RHandEmpty:

                {
                    return World.Player.GetItemOnLayer(Layer.RightHand) == null;
                }

                case IfVarType.LHandEmpty:

                {
                    return World.Player.GetItemOnLayer(Layer.LeftHand) == null;
                }

                case IfVarType.Counter:

                {
                    if (m_CountObj == null)
                    {
                        foreach (Counter c in Assistant.Counter.List)
                        {
                            if (c.Name == Counter)
                            {
                                m_CountObj = c;

                                break;
                            }
                        }
                    }

                    if (m_CountObj == null || !m_CountObj.Enabled)
                        return false;

                    if (Op > 0)
                        return m_CountObj.Amount >= (int) Value;

                    return m_CountObj.Amount <= (int) Value;
                }

                default:

                    return false;
            }
        }

        public override string ToString()
        {
            switch (Variable)
            {
                case IfVarType.Hits:
                case IfVarType.Mana:
                case IfVarType.Stamina:
                case IfVarType.Weight:

                    return string.Format("If ( {0} {1} {2} )", Variable, Op > 0 ? ">=" : "<=", Value);
                case IfVarType.Poisoned:

                    return "If ( Poisoned )";
                case IfVarType.SysMessage:

                {
                    string str = (string) Value;

                    if (str.Length > 10)
                        str = str.Substring(0, 7) + "...";

                    return string.Format("If ( SysMessage \"{0}\" )", str);
                }
                case IfVarType.Mounted:

                    return "If ( Mounted )";
                case IfVarType.RHandEmpty:

                    return "If ( R-Hand Empty )";
                case IfVarType.LHandEmpty:

                    return "If ( L-Hand Empty )";
                case IfVarType.Counter:

                    return string.Format("If ( \"{0} count\" {1} {2} )", Counter, Op > 0 ? ">=" : "<=", Value);
                default:

                    return "If ( ??? )";
            }
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit)
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            new MacroInsertIf(this).ShowDialog(Engine.MainWindow);
        }
    }

    public class ElseAction : MacroAction
    {
        public override bool Perform()
        {
            return true;
        }

        public override string ToString()
        {
            return "Else";
        }
    }

    public class EndIfAction : MacroAction
    {
        public override bool Perform()
        {
            return true;
        }

        public override string ToString()
        {
            return "End If";
        }
    }

    public class HotKeyAction : MacroAction
    {
        private readonly KeyData m_Key;

        public HotKeyAction(KeyData hk)
        {
            m_Key = hk;
        }

        public HotKeyAction(string[] args)
        {
            try
            {
                int loc = Convert.ToInt32(args[1]);

                if (loc != 0)
                    m_Key = HotKey.Get(loc);
            }
            catch
            {
            }

            if (m_Key == null)
                m_Key = HotKey.Get(args[2]);

            if (m_Key == null)
                throw new Exception("HotKey not found.");
        }

        public override bool Perform()
        {
            if (Windows.AllowBit(FeatureBit.LoopingMacros) || m_Key.DispName.IndexOf(Language.GetString(LocString.PlayA1).Replace(@"{0}", "")) == -1)
                m_Key.Callback();

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Key.LocName, m_Key.StrName == null ? "" : m_Key.StrName);
        }

        public override string ToString()
        {
            return string.Format("Exec: {0}", m_Key.DispName);
        }
    }

    public class ForAction : MacroAction
    {
        private MenuItem[] m_MenuItems;

        public ForAction(string[] args)
        {
            Max = Convert.ToInt32(args[1]);
        }

        public ForAction(int max)
        {
            Max = max;
        }

        public int Count { get; set; }

        public int Max { get; private set; }

        public override string Serialize()
        {
            return DoSerialize(Max);
        }

        public override bool Perform()
        {
            return true;
        }

        public override string ToString()
        {
            return string.Format("For ( 1 to {0} )", Max);
        }

        public override MenuItem[] GetContextMenuItems()
        {
            if (m_MenuItems == null)
            {
                m_MenuItems = new[]
                {
                    new MacroMenuItem(LocString.Edit, Edit)
                };
            }

            return m_MenuItems;
        }

        private void Edit(object[] args)
        {
            if (InputBox.Show(Language.GetString(LocString.NumIter), "Input Box", Max.ToString()))
                Max = InputBox.GetInt(Max);

            if (Parent != null)
                Parent.Update();
        }
    }

    public class EndForAction : MacroAction
    {
        public override bool Perform()
        {
            return true;
        }

        public override string ToString()
        {
            return "End For";
        }
    }

    public class ContextMenuAction : MacroAction
    {
        private readonly ushort m_CtxName;
        private readonly Serial m_Entity;
        private readonly ushort m_Idx;

        public ContextMenuAction(UOEntity ent, ushort idx, ushort ctxName)
        {
            m_Entity = ent != null ? ent.Serial : Serial.MinusOne;

            if (World.Player != null && World.Player.Serial == m_Entity)
                m_Entity = Serial.Zero;

            m_Idx = idx;
            m_CtxName = ctxName;
        }

        public ContextMenuAction(string[] args)
        {
            m_Entity = Serial.Parse(args[1]);
            m_Idx = Convert.ToUInt16(args[2]);

            try
            {
                m_CtxName = Convert.ToUInt16(args[3]);
            }
            catch
            {
            }
        }

        public override bool Perform()
        {
            Serial s = m_Entity;

            if (s == Serial.Zero && World.Player != null)
                s = World.Player.Serial;

            ClientCommunication.SendToServer(new ContextMenuRequest(s));
            ClientCommunication.SendToServer(new ContextMenuResponse(s, m_Idx));

            return true;
        }

        public override string Serialize()
        {
            return DoSerialize(m_Entity, m_Idx, m_CtxName);
        }

        public override string ToString()
        {
            string ent;

            if (m_Entity == Serial.Zero)
                ent = "(self)";
            else
                ent = m_Entity.ToString();

            return string.Format("ContextMenu: {1} ({0})", ent, m_Idx);
        }
    }
}