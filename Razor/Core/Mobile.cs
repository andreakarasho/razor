using System;
using System.Collections.Generic;
using System.IO;

using Assistant.UI;

namespace Assistant
{
    [Flags]
    public enum Direction : byte
    {
        North = 0x0,
        Right = 0x1,
        East = 0x2,
        Down = 0x3,
        South = 0x4,
        Left = 0x5,
        West = 0x6,
        Up = 0x7,
        Mask = 0x7,
        Running = 0x80,
        ValueMask = 0x87
    }

    //public enum BodyType : byte
    //{
    //    Empty,
    //    Monster,
    //    Sea_Monster,
    //    Animal,
    //    Human,
    //    Equipment
    //}

    public class Mobile : UOEntity
    {
        // grey, blue, green, 'canbeattacked'
        private static readonly uint[] m_NotoHues = new uint[8]
        {
            // hue color #30
            0x000000, // black		unused 0
            0x30d0e0, // blue		0x0059 1 
            0x60e000, // green		0x003F 2
            0x9090b2, // greyish	0x03b2 3
            0x909090, // grey		   "   4
            0xd88038, // orange		0x0090 5
            0xb01000, // red		0x0022 6
            0xe0e000 // yellow		0x0035 7
        };

        private static readonly int[] m_NotoHuesInt = new int[8]
        {
            1, // black		unused 0
            0x059, // blue		0x0059 1
            0x03F, // green		0x003F 2
            0x3B2, // greyish	0x03b2 3
            0x3B2, // grey		   "   4
            0x090, // orange		0x0090 5
            0x022, // red		0x0022 6
            0x035 // yellow		0x0035 7
        };

        private Direction m_Direction;
        //end new

        private List<Serial> m_LoadSerials;

        private byte m_Map;
        private string m_Name;

        private byte m_Notoriety;
        protected ushort m_StamMax, m_Stam, m_ManaMax, m_Mana;

        //new

        public Mobile(BinaryReader reader, int version) : base(reader, version)
        {
            Body = reader.ReadUInt16();
            m_Direction = (Direction) reader.ReadByte();
            m_Name = reader.ReadString();
            m_Notoriety = reader.ReadByte();
            ProcessPacketFlags(reader.ReadByte());
            HitsMax = reader.ReadUInt16();
            Hits = reader.ReadUInt16();
            m_Map = reader.ReadByte();

            int count = reader.ReadInt32();
            m_LoadSerials = new List<Serial>();

            for (int i = count - 1; i >= 0; --i)
                m_LoadSerials.Add(reader.ReadUInt32());
        }

        public Mobile(Serial serial) : base(serial)
        {
            m_Map = World.Player == null ? (byte) 0 : World.Player.Map;
            Visible = true;

            Agent.InvokeMobileCreated(this);
        }

        public string Name
        {
            get
            {
                if (m_Name == null)
                    return "";

                return m_Name;
            }
            set
            {
                if (value != null)
                {
                    string trim = value.Trim();

                    if (trim.Length > 0)
                        m_Name = trim;
                }
            }
        }

        public ushort Body { get; set; }

        public Direction Direction
        {
            get => m_Direction;
            set
            {
                if (value != m_Direction)
                {
                    var oldDir = m_Direction;
                    m_Direction = value;
                    OnDirectionChanging(oldDir);
                }
            }
        }

        public bool Visible { get; set; }

        public bool Poisoned { get; set; }

        public bool Blessed { get; set; }

        public bool IsGhost => Body == 402
                               || Body == 403
                               || Body == 607
                               || Body == 608
                               || Body == 970;

        public bool IsHuman => Body >= 0
                               && (Body == 400
                                   || Body == 401
                                   || Body == 402
                                   || Body == 403
                                   || Body == 605
                                   || Body == 606
                                   || Body == 607
                                   || Body == 608
                                   || Body == 970);

        public bool IsMonster => !IsHuman;

        //new
        public bool Unknown { get; set; }

        public bool Unknown2 { get; set; }

        public bool Unknown3 { get; set; }

        public bool CanRename //A pet! (where the health bar is open, we can add this to an arraylist of mobiles...
        {
            get;
            set;
        }
        //end new

        public bool Warmode { get; set; }

        public bool Female { get; set; }

        public byte Notoriety
        {
            get => m_Notoriety;
            set
            {
                if (value != Notoriety)
                {
                    OnNotoChange(m_Notoriety, value);
                    m_Notoriety = value;
                }
            }
        }

        public ushort HitsMax { get; set; }

        public ushort Hits { get; set; }

        public ushort Stam
        {
            get => m_Stam;
            set => m_Stam = value;
        }

        public ushort StamMax
        {
            get => m_StamMax;
            set => m_StamMax = value;
        }

        public ushort Mana
        {
            get => m_Mana;
            set => m_Mana = value;
        }

        public ushort ManaMax
        {
            get => m_ManaMax;
            set => m_ManaMax = value;
        }


        public byte Map
        {
            get => m_Map;
            set
            {
                if (m_Map != value)
                {
                    OnMapChange(m_Map, value);
                    m_Map = value;
                }
            }
        }

        public bool InParty => PacketHandlers.Party.Contains(Serial);

        public Item Backpack => GetItemOnLayer(Layer.Backpack);

        public Item Quiver
        {
            get
            {
                Item item = GetItemOnLayer(Layer.Cloak);

                if (item != null && item.IsContainer)
                    return item;

                return null;
            }
        }

        public List<Item> Contains { get; } = new List<Item>();

        internal Point2D ButtonPoint { get; set; } = Point2D.Zero;

        //private static BodyType[] m_Types;

        //public static void Initialize()
        //{
        //    using (StreamReader ip = new StreamReader(Path.Combine(Ultima.Files.RootDir, "mobtypes.txt")))
        //    {
        //        m_Types = new BodyType[0x1000];

        //        string line;

        //        while ((line = ip.ReadLine()) != null)
        //        {
        //            if (line.Length == 0 || line.StartsWith("#"))
        //                continue;

        //            string[] split = line.Split('\t');

        //            BodyType type;
        //            int bodyID;

        //            if (int.TryParse(split[0], out bodyID) && Enum.TryParse(split[1], true, out type) && bodyID >= 0 &&
        //                bodyID < m_Types.Length)
        //            {
        //                m_Types[bodyID] = type;
        //            }
        //        }
        //    }
        //}

        public override void SaveState(BinaryWriter writer)
        {
            base.SaveState(writer);

            writer.Write(Body);
            writer.Write((byte) m_Direction);
            writer.Write(m_Name == null ? "" : m_Name);
            writer.Write(m_Notoriety);
            writer.Write((byte) GetPacketFlags());
            writer.Write(HitsMax);
            writer.Write(Hits);
            writer.Write(m_Map);

            writer.Write(Contains.Count);

            for (int i = 0; i < Contains.Count; i++)
                writer.Write((uint) Contains[i].Serial);
            //writer.Write(	(int)0 );
        }

        public override void AfterLoad()
        {
            int count = m_LoadSerials.Count;

            for (int i = count - 1; i >= 0; --i)
            {
                Item it = World.FindItem(m_LoadSerials[i]);

                if (it != null)
                    Contains.Add(it);
            }

            m_LoadSerials = null; //per il GC e per liberare RAM
        }

        protected virtual void OnNotoChange(byte old, byte cur)
        {
        }

        public uint GetNotorietyColor()
        {
            if (m_Notoriety < 0 || m_Notoriety >= m_NotoHues.Length)
                return m_NotoHues[0];

            return m_NotoHues[m_Notoriety];
        }

        public int GetNotorietyColorInt()
        {
            if (m_Notoriety < 0 || m_Notoriety >= m_NotoHues.Length)
                return m_NotoHuesInt[0];

            return m_NotoHuesInt[m_Notoriety];
        }

        public byte GetStatusCode()
        {
            if (Poisoned)
                return 1;

            return 0;
        }

        public virtual void OnMapChange(byte old, byte cur)
        {
        }

        public void AddItem(Item item)
        {
            Contains.Add(item);
        }

        public void RemoveItem(Item item)
        {
            Contains.Remove(item);
        }

        public override void Remove()
        {
            List<Item> rem = new List<Item>(Contains);
            Contains.Clear();

            for (int i = 0; i < rem.Count; i++)
                rem[i].Remove();

            if (!InParty)
            {
                base.Remove();
                World.RemoveMobile(this);
            }
            else
                Visible = false;
        }

        public Item GetItemOnLayer(Layer layer)
        {
            for (int i = 0; i < Contains.Count; i++)
            {
                Item item = Contains[i];

                if (item.Layer == layer)
                    return item;
            }

            return null;
        }

        public Item FindItemByID(ItemID id)
        {
            for (int i = 0; i < Contains.Count; i++)
            {
                Item item = Contains[i];

                if (item.ItemID == id)
                    return item;
            }

            return null;
        }

        public override void OnPositionChanging(Point3D oldPos)
        {
            if (this != World.Player && Engine.MainWindow.MapWindow != null)
                Engine.MainWindow.SafeAction(s => s.MapWindow.CheckLocalUpdate(this));

            base.OnPositionChanging(oldPos);
        }

        public virtual void OnDirectionChanging(Direction oldDir)
        {
        }

        public int GetPacketFlags()
        {
            int flags = 0x0;

            if (Female)
                flags |= 0x02;

            if (Poisoned)
                flags |= 0x04;

            if (Blessed)
                flags |= 0x08;

            if (Warmode)
                flags |= 0x40;

            if (!Visible)
                flags |= 0x80;

            if (Unknown)
                flags |= 0x01;

            if (Unknown2)
                flags |= 0x10;

            if (Unknown3)
                flags |= 0x20;

            return flags;
        }

        public void ProcessPacketFlags(byte flags)
        {
            if (!PacketHandlers.UseNewStatus)
                Poisoned = (flags & 0x04) != 0;

            Unknown = (flags & 0x01) != 0; //new
            Female = (flags & 0x02) != 0;
            Blessed = (flags & 0x08) != 0;
            Unknown2 = (flags & 0x10) != 0; //new
            Unknown3 = (flags & 0x10) != 0; //new
            Warmode = (flags & 0x40) != 0;
            Visible = (flags & 0x80) == 0;
        }

        internal void OverheadMessageFrom(int hue, string from, string format, params object[] args)
        {
            OverheadMessageFrom(hue, from, string.Format(format, args));
        }

        internal void OverheadMessageFrom(int hue, string from, string text)
        {
            if (Config.GetInt("OverheadStyle") == 0)
                ClientCommunication.SendToClient(new AsciiMessage(Serial, Body, MessageType.Regular, hue, 3, Language.CliLocName, text));
            else
                ClientCommunication.SendToClient(new UnicodeMessage(Serial, Body, MessageType.Regular, hue, 3, Language.CliLocName, from, text));
        }

        internal void OverheadMessage(string text)
        {
            OverheadMessage(Config.GetInt("SysColor"), text);
        }

        internal void OverheadMessage(string format, params object[] args)
        {
            OverheadMessage(Config.GetInt("SysColor"), string.Format(format, args));
        }

        internal void OverheadMessage(int hue, string format, params object[] args)
        {
            OverheadMessage(hue, string.Format(format, args));
        }

        internal void OverheadMessage(int hue, string text)
        {
            OverheadMessageFrom(hue, "Razor", text);
        }

        internal void OverheadMessage(LocString str)
        {
            OverheadMessage(Language.GetString(str));
        }

        internal void OverheadMessage(LocString str, params object[] args)
        {
            OverheadMessage(Language.Format(str, args));
        }
    }
}