using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Assistant.Core;
using Assistant.Macros;
using Assistant.UI;

namespace Assistant
{
    public enum LockType : byte
    {
        Up = 0,
        Down = 1,
        Locked = 2
    }

    public enum MsgLevel
    {
        Debug = 0,
        Info = 0,
        Warning = 1,
        Error = 2,
        Force = 3
    }

    public class Skill
    {
        public static int Count = 55;
        private ushort m_Base;
        private short m_Delta;

        public Skill(int idx)
        {
            Index = idx;
        }

        public int Index { get; }

        public LockType Lock { get; set; }

        public ushort FixedValue { get; set; }

        public ushort FixedBase
        {
            get => m_Base;
            set
            {
                m_Delta += (short) (value - m_Base);
                m_Base = value;
            }
        }

        public ushort FixedCap { get; set; }

        public double Value
        {
            get => FixedValue / 10.0;
            set => FixedValue = (ushort) (value * 10.0);
        }

        public double Base
        {
            get => m_Base / 10.0;
            set => m_Base = (ushort) (value * 10.0);
        }

        public double Cap
        {
            get => FixedCap / 10.0;
            set => FixedCap = (ushort) (value * 10.0);
        }

        public double Delta
        {
            get => m_Delta / 10.0;
            set => m_Delta = (short) (value * 10);
        }
    }

    public enum SkillName
    {
        Alchemy = 0,
        Anatomy = 1,
        AnimalLore = 2,
        ItemID = 3,
        ArmsLore = 4,
        Parry = 5,
        Begging = 6,
        Blacksmith = 7,
        Fletching = 8,
        Peacemaking = 9,
        Camping = 10,
        Carpentry = 11,
        Cartography = 12,
        Cooking = 13,
        DetectHidden = 14,
        Discordance = 15,
        EvalInt = 16,
        Healing = 17,
        Fishing = 18,
        Forensics = 19,
        Herding = 20,
        Hiding = 21,
        Provocation = 22,
        Inscribe = 23,
        Lockpicking = 24,
        Magery = 25,
        MagicResist = 26,
        Tactics = 27,
        Snooping = 28,
        Musicianship = 29,
        Poisoning = 30,
        Archery = 31,
        SpiritSpeak = 32,
        Stealing = 33,
        Tailoring = 34,
        AnimalTaming = 35,
        TasteID = 36,
        Tinkering = 37,
        Tracking = 38,
        Veterinary = 39,
        Swords = 40,
        Macing = 41,
        Fencing = 42,
        Wrestling = 43,
        Lumberjacking = 44,
        Mining = 45,
        Meditation = 46,
        Stealth = 47,
        RemoveTrap = 48,
        Necromancy = 49,
        Focus = 50,
        Chivalry = 51,
        Bushido = 52,
        Ninjitsu = 53,
        SpellWeaving = 54
    }

    public enum MaleSounds
    {
        Ah = 0x41A,
        Ahha = 0x41B,
        Applaud = 0x41C,
        BlowNose = 0x41D,
        Burp = 0x41E,
        Cheer = 0x41F,
        ClearThroat = 0x420,
        Cough = 0x421,
        CoughBS = 0x422,
        Cry = 0x423,
        Fart = 0x429,
        Gasp = 0x42A,
        Giggle = 0x42B,
        Groan = 0x42C,
        Growl = 0x42D,
        Hey = 0x42E,
        Hiccup = 0x42F,
        Huh = 0x430,
        Kiss = 0x431,
        Laugh = 0x432,
        No = 0x433,
        Oh = 0x434,
        Oomph1 = 0x435,
        Oomph2 = 0x436,
        Oomph3 = 0x437,
        Oomph4 = 0x438,
        Oomph5 = 0x439,
        Oomph6 = 0x43A,
        Oomph7 = 0x43B,
        Oomph8 = 0x43C,
        Oomph9 = 0x43D,
        Oooh = 0x43E,
        Oops = 0x43F,
        Puke = 0x440,
        Scream = 0x441,
        Shush = 0x442,
        Sigh = 0x443,
        Sneeze = 0x444,
        Sniff = 0x445,
        Snore = 0x446,
        Spit = 0x447,
        Whistle = 0x448,
        Yawn = 0x449,
        Yea = 0x44A,
        Yell = 0x44B
    }

    public enum FemaleSounds
    {
        Ah = 0x30B,
        Ahha = 0x30C,
        Applaud = 0x30D,
        BlowNose = 0x30E,
        Burp = 0x30F,
        Cheer = 0x310,
        ClearThroat = 0x311,
        Cough = 0x312,
        CoughBS = 0x313,
        Cry = 0x314,
        Fart = 0x319,
        Gasp = 0x31A,
        Giggle = 0x31B,
        Groan = 0x31C,
        Growl = 0x31D,
        Hey = 0x31E,
        Hiccup = 0x31F,
        Huh = 0x320,
        Kiss = 0x321,
        Laugh = 0x322,
        No = 0x323,
        Oh = 0x324,
        Oomph1 = 0x325,
        Oomph2 = 0x326,
        Oomph3 = 0x327,
        Oomph4 = 0x328,
        Oomph5 = 0x329,
        Oomph6 = 0x32A,
        Oomph7 = 0x32B,
        Oooh = 0x32C,
        Oops = 0x32D,
        Puke = 0x32E,
        Scream = 0x32F,
        Shush = 0x330,
        Sigh = 0x331,
        Sneeze = 0x332,
        Sniff = 0x333,
        Snore = 0x334,
        Spit = 0x335,
        Whistle = 0x336,
        Yawn = 0x337,
        Yea = 0x338,
        Yell = 0x339
    }

    public class PlayerData : Mobile
    {
        public enum SeasonFlag
        {
            Spring,
            Summer,
            Fall,
            Winter,
            Desolation
        }

        public static Timer m_SeasonTimer = new SeasonTimer();
        public string CurrentGumpRawData;

        public uint CurrentGumpS, CurrentGumpI;
        public List<string> CurrentGumpStrings = new List<string>();
        public ushort CurrentMenuI;
        public uint CurrentMenuS;
        public bool HasGump;
        public bool HasMenu;
        public GumpResponseAction LastGumpResponseAction;

        internal List<BuffsDebuffs> m_BuffsDebuffs = new List<BuffsDebuffs>();
        private DateTime m_CriminalStart = DateTime.MinValue;
        private Timer m_CriminalTime;

        private int m_MaxWeight = -1;


        public int VisRange = 18;

        public PlayerData(BinaryReader reader, int version) : base(reader, version)
        {
            int c;
            Str = reader.ReadUInt16();
            Dex = reader.ReadUInt16();
            Int = reader.ReadUInt16();
            m_StamMax = reader.ReadUInt16();
            m_Stam = reader.ReadUInt16();
            m_ManaMax = reader.ReadUInt16();
            m_Mana = reader.ReadUInt16();
            StrLock = (LockType) reader.ReadByte();
            DexLock = (LockType) reader.ReadByte();
            IntLock = (LockType) reader.ReadByte();
            Gold = reader.ReadUInt32();
            Weight = reader.ReadUInt16();

            if (version >= 4)
                Skill.Count = c = reader.ReadByte();
            else if (version == 3)
            {
                long skillStart = reader.BaseStream.Position;
                c = 0;
                reader.BaseStream.Seek(7 * 49, SeekOrigin.Current);

                for (int i = 48; i < 60; i++)
                {
                    ushort Base, Cap, Val;
                    byte Lock;

                    Base = reader.ReadUInt16();
                    Cap = reader.ReadUInt16();
                    Val = reader.ReadUInt16();
                    Lock = reader.ReadByte();

                    if (Base > 2000 || Cap > 2000 || Val > 2000 || Lock > 2)
                    {
                        c = i;

                        break;
                    }
                }

                if (c == 0)
                    c = 52;
                else if (c > 54)
                    c = 54;

                Skill.Count = c;

                reader.BaseStream.Seek(skillStart, SeekOrigin.Begin);
            }
            else
                Skill.Count = c = 52;

            Skills = new Skill[c];

            for (int i = 0; i < c; i++)
            {
                Skills[i] = new Skill(i);
                Skills[i].FixedBase = reader.ReadUInt16();
                Skills[i].FixedCap = reader.ReadUInt16();
                Skills[i].FixedValue = reader.ReadUInt16();
                Skills[i].Lock = (LockType) reader.ReadByte();
            }

            AR = reader.ReadUInt16();
            StatCap = reader.ReadUInt16();
            Followers = reader.ReadByte();
            FollowersMax = reader.ReadByte();
            Tithe = reader.ReadInt32();

            LocalLightLevel = reader.ReadSByte();
            GlobalLightLevel = reader.ReadByte();
            Features = reader.ReadUInt16();
            Season = reader.ReadByte();

            if (version >= 4)
                c = reader.ReadByte();
            else
                c = 8;
            MapPatches = new int[c];

            for (int i = 0; i < c; i++)
                MapPatches[i] = reader.ReadInt32();
        }

        public PlayerData(Serial serial) : base(serial)
        {
            Skills = new Skill[Skill.Count];

            for (int i = 0; i < Skills.Length; i++)
                Skills[i] = new Skill(i);
        }

        public int MultiVisRange => VisRange + 5;
        internal List<BuffsDebuffs> BuffsDebuffs => m_BuffsDebuffs;
        public List<uint> OpenedCorpses { get; } = new List<uint>();

        public ushort Str { get; set; }

        public ushort Dex { get; set; }

        public ushort Int { get; set; }

        public uint Gold { get; set; }

        public ushort Weight { get; set; }

        public ushort MaxWeight
        {
            get
            {
                if (m_MaxWeight == -1)
                    return (ushort) (Str * 3.5 + 40);

                return (ushort) m_MaxWeight;
            }
            set => m_MaxWeight = value;
        }

        public short FireResistance { get; set; }

        public short ColdResistance { get; set; }

        public short PoisonResistance { get; set; }

        public short EnergyResistance { get; set; }

        public short Luck { get; set; }

        public ushort DamageMin { get; set; }

        public ushort DamageMax { get; set; }

        public LockType StrLock { get; set; }

        public LockType DexLock { get; set; }

        public LockType IntLock { get; set; }

        public ushort StatCap { get; set; }

        public ushort AR { get; set; }

        public byte Followers { get; set; }

        public byte FollowersMax { get; set; }

        public int Tithe { get; set; }

        public Skill[] Skills { get; }

        public bool SkillsSent { get; set; }

        public int CriminalTime
        {
            get
            {
                if (m_CriminalStart != DateTime.MinValue)
                {
                    int sec = (int) (DateTime.UtcNow - m_CriminalStart).TotalSeconds;

                    if (sec > 300)
                    {
                        if (m_CriminalTime != null)
                            m_CriminalTime.Stop();
                        m_CriminalStart = DateTime.MinValue;

                        return 0;
                    }

                    return sec;
                }

                return 0;
            }
        }

        public ushort SpeechHue { get; set; }

        public sbyte LocalLightLevel { get; set; }

        public byte GlobalLightLevel { get; set; }

        public byte Season { get; set; }

        public byte DefaultSeason { get; set; }

        public ushort Features { get; set; }

        public int[] MapPatches { get; set; } = new int[10];
        public int LastSkill { get; set; } = -1;
        public Serial LastObject { get; private set; } = Serial.Zero;
        public int LastSpell { get; set; } = -1;


        public override void SaveState(BinaryWriter writer)
        {
            base.SaveState(writer);

            writer.Write(Str);
            writer.Write(Dex);
            writer.Write(Int);
            writer.Write(m_StamMax);
            writer.Write(m_Stam);
            writer.Write(m_ManaMax);
            writer.Write(m_Mana);
            writer.Write((byte) StrLock);
            writer.Write((byte) DexLock);
            writer.Write((byte) IntLock);
            writer.Write(Gold);
            writer.Write(Weight);

            writer.Write((byte) Skill.Count);

            for (int i = 0; i < Skill.Count; i++)
            {
                writer.Write(Skills[i].FixedBase);
                writer.Write(Skills[i].FixedCap);
                writer.Write(Skills[i].FixedValue);
                writer.Write((byte) Skills[i].Lock);
            }

            writer.Write(AR);
            writer.Write(StatCap);
            writer.Write(Followers);
            writer.Write(FollowersMax);
            writer.Write(Tithe);

            writer.Write(LocalLightLevel);
            writer.Write(GlobalLightLevel);
            writer.Write(Features);
            writer.Write(Season);

            writer.Write((byte) MapPatches.Length);

            for (int i = 0; i < MapPatches.Length; i++)
                writer.Write(MapPatches[i]);
        }

        private void AutoOpenDoors()
        {
            if (Body != 0x03DB &&
                !IsGhost &&
                (int) (Direction & Direction.Mask) % 2 == 0 &&
                Config.GetBool("AutoOpenDoors") &&
                Windows.AllowBit(FeatureBit.AutoOpenDoors))
            {
                int x = Position.X, y = Position.Y, z = Position.Z;

                /* Check if one more tile in the direction we just moved is a door */
                Utility.Offset(Direction, ref x, ref y);

                if (World.Items.Values.Any(s => s.IsDoor && s.Position.X == x && s.Position.Y == y && s.Position.Z - 15 <= z && s.Position.Z + 15 >= z)) ClientCommunication.SendToServer(new OpenDoorMacro());
            }
        }


        public override void OnPositionChanging(Point3D oldPos)
        {
            if (!IsGhost)
                StealthSteps.OnMove();

            AutoOpenDoors();

            List<Mobile> mlist = new List<Mobile>(World.Mobiles.Values);

            for (int i = 0; i < mlist.Count; i++)
            {
                Mobile m = mlist[i];

                if (m != this)
                {
                    if (!Utility.InRange(m.Position, Position, VisRange))
                        m.Remove();
                    else
                        Targeting.CheckLastTargetRange(m);
                }
            }

            mlist = null;


            List<Item> ilist = new List<Item>(World.Items.Values);
            ScavengerAgent s = ScavengerAgent.Instance;

            for (int i = 0; i < ilist.Count; i++)
            {
                Item item = ilist[i];

                if (item.Deleted || item.Container != null)
                    continue;

                int dist = Utility.Distance(item.GetWorldPosition(), Position);

                if (item != DragDropManager.Holding && (dist > MultiVisRange || !item.IsMulti && dist > VisRange))
                    item.Remove();
                else if (!IsGhost && Visible && dist <= 2 && s.Enabled && item.Movable)
                    s.Scavenge(item);
            }

            ilist = null;

            Console.WriteLine("Player position changed");

            if (Engine.MainWindow != null && Engine.MainWindow.MapWindow != null)
                Engine.MainWindow.SafeAction(f => f.MapWindow.PlayerMoved());

            base.OnPositionChanging(oldPos);
        }

        public override void OnDirectionChanging(Direction oldDir)
        {
            AutoOpenDoors();
        }

        public override void OnMapChange(byte old, byte cur)
        {
            List<Mobile> list = new List<Mobile>(World.Mobiles.Values);

            for (int i = 0; i < list.Count; i++)
            {
                Mobile m = list[i];

                if (m != this && m.Map != cur)
                    m.Remove();
            }

            list = null;

            World.Items.Clear();
            Counter.Reset();

            for (int i = 0; i < Contains.Count; i++)
            {
                Item item = Contains[i];
                World.AddItem(item);
                item.Contains.Clear();
            }

            if (Config.GetBool("AutoSearch") && Backpack != null)
                DoubleClick(Backpack);

            UOAssist.PostMapChange(cur);

            if (Engine.MainWindow != null && Engine.MainWindow.MapWindow != null)
                Engine.MainWindow.SafeAction(s => s.MapWindow.PlayerMoved());
        }

        /*public override void OnMapChange( byte old, byte cur )
        {
             World.Mobiles.Clear();
             World.Items.Clear();
             Counter.Reset();

             Contains.Clear();

             World.AddMobile( this );

             UOAssist.PostMapChange( cur );
        }*/

        protected override void OnNotoChange(byte old, byte cur)
        {
            if ((old == 3 || old == 4) && cur != 3 && cur != 4)
            {
                // grey is turning off
                // SendMessage( "You are no longer a criminal." );
                if (m_CriminalTime != null)
                    m_CriminalTime.Stop();
                m_CriminalStart = DateTime.MinValue;
                Windows.RequestTitleBarUpdate();
            }
            else if ((cur == 3 || cur == 4) && old != 3 && old != 4 && old != 0)
            {
                // grey is turning on
                ResetCriminalTimer();
            }
        }

        public void ResetCriminalTimer()
        {
            if (m_CriminalStart == DateTime.MinValue || DateTime.UtcNow - m_CriminalStart >= TimeSpan.FromSeconds(1))
            {
                m_CriminalStart = DateTime.UtcNow;

                if (m_CriminalTime == null)
                    m_CriminalTime = new CriminalTimer(this);
                m_CriminalTime.Start();
                Windows.RequestTitleBarUpdate();
            }
        }

        internal void SendMessage(MsgLevel lvl, LocString loc, params object[] args)
        {
            SendMessage(lvl, Language.Format(loc, args));
        }

        internal void SendMessage(MsgLevel lvl, LocString loc)
        {
            SendMessage(lvl, Language.GetString(loc));
        }

        internal void SendMessage(LocString loc, params object[] args)
        {
            SendMessage(MsgLevel.Info, Language.Format(loc, args));
        }

        internal void SendMessage(LocString loc)
        {
            SendMessage(MsgLevel.Info, Language.GetString(loc));
        }

        /*internal void SendMessage( int hue, LocString loc, params object[] args )
        {
             SendMessage( hue, Language.Format( loc, args ) );
        }*/

        internal void SendMessage(MsgLevel lvl, string format, params object[] args)
        {
            SendMessage(lvl, string.Format(format, args));
        }

        internal void SendMessage(string format, params object[] args)
        {
            SendMessage(MsgLevel.Info, string.Format(format, args));
        }

        internal void SendMessage(string text)
        {
            SendMessage(MsgLevel.Info, text);
        }

        internal void SendMessage(MsgLevel lvl, string text)
        {
            if (lvl >= (MsgLevel) Config.GetInt("MessageLevel") && text.Length > 0)
            {
                int hue;

                switch (lvl)
                {
                    case MsgLevel.Error:
                    case MsgLevel.Warning:
                        hue = Config.GetInt("WarningColor");

                        break;

                    default:
                        hue = Config.GetInt("SysColor");

                        break;
                }

                ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, hue, 3, Language.CliLocName, "System", text));

                PacketHandlers.SysMessages.Add(text);

                if (PacketHandlers.SysMessages.Count >= 25)
                    PacketHandlers.SysMessages.RemoveRange(0, 10);
            }
        }

        /// <summary>
        ///     Sets the player's season, set a default to revert back if required
        /// </summary>
        /// <param name="defaultSeason"></param>
        public void SetSeason(byte defaultSeason = 0)
        {
            if (Config.GetInt("Season") < 5)
            {
                byte season = (byte) Config.GetInt("Season");

                if (Config.GetBool("RealSeason")) season = World.Player.WhichSeason();

                World.Player.Season = season;
                World.Player.DefaultSeason = defaultSeason;

                if (!m_SeasonTimer.Running)
                    m_SeasonTimer.Start();
            }
            else
            {
                World.Player.Season = defaultSeason;
                World.Player.DefaultSeason = defaultSeason;
            }
        }

        public byte WhichSeason()
        {
            DateTime now = DateTime.UtcNow;

            /* Astronomically Spring begins on March 21st, the 80th day of the year. 
               * Summer begins on the 172nd day, Autumn, the 266th and Winter the 355th.
               * Of course, on a leap year add one day to each, 81, 173, 267 and 356. */

            int doy = now.DayOfYear - Convert.ToInt32(DateTime.IsLeapYear(now.Year) && now.DayOfYear > 59);

            if (doy < 80 || doy >= 355) return (byte) SeasonFlag.Winter;

            if (doy >= 80 && doy < 172) return (byte) SeasonFlag.Spring;

            if (doy >= 172 && doy < 266) return (byte) SeasonFlag.Summer;

            return (byte) SeasonFlag.Fall;
        }

        //private UOEntity m_LastCtxM = null;
        //public UOEntity LastContextMenu { get { return m_LastCtxM; } set { m_LastCtxM = value; } }

        public static bool DoubleClick(object clicked)
        {
            return DoubleClick(clicked, true);
        }

        public static bool DoubleClick(object clicked, bool silent)
        {
            Serial s;

            if (clicked is Mobile)
                s = ((Mobile) clicked).Serial.Value;
            else if (clicked is Item)
                s = ((Item) clicked).Serial.Value;
            else if (clicked is Serial)
                s = ((Serial) clicked).Value;
            else
                s = Serial.Zero;

            if (s != Serial.Zero)
            {
                Item free = null, pack = World.Player.Backpack;

                if (s.IsItem && pack != null && Config.GetBool("PotionEquip") && Windows.AllowBit(FeatureBit.AutoPotionEquip))
                {
                    Item i = World.FindItem(s);

                    if (i != null && i.IsPotion && i.ItemID != 3853) // dont unequip for exploison potions
                    {
                        // dont worry about uneqipping RuneBooks or SpellBooks
                        Item left = World.Player.GetItemOnLayer(Layer.LeftHand);
                        Item right = World.Player.GetItemOnLayer(Layer.RightHand);

                        if (left != null && (right != null || left.IsTwoHanded))
                            free = left;
                        else if (right != null && right.IsTwoHanded)
                            free = right;

                        if (free != null)
                        {
                            if (DragDropManager.HasDragFor(free.Serial))
                                free = null;
                            else
                                DragDropManager.DragDrop(free, pack);
                        }
                    }
                }

                ActionQueue.DoubleClick(silent, s);

                if (free != null)
                    DragDropManager.DragDrop(free, World.Player, free.Layer, true);

                if (s.IsItem)
                    World.Player.LastObject = s;
            }

            return false;
        }

        private class CriminalTimer : Timer
        {
            private PlayerData m_Player;

            public CriminalTimer(PlayerData player) : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            {
                m_Player = player;
            }

            protected override void OnTick()
            {
                Windows.RequestTitleBarUpdate();
            }
        }

        private class SeasonTimer : Timer
        {
            public SeasonTimer() : base(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3))
            {
            }

            protected override void OnTick()
            {
                ClientCommunication.SendToClient(new SeasonChange(World.Player.Season, true));
                m_SeasonTimer.Stop();
            }
        }
    }
}