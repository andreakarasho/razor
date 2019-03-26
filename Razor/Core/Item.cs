using System;
using System.Collections.Generic;
using System.IO;

using Ultima;

namespace Assistant
{
    public enum Layer : byte
    {
        Invalid = 0x00,

        FirstValid = 0x01,

        RightHand = 0x01,
        LeftHand = 0x02,
        Shoes = 0x03,
        Pants = 0x04,
        Shirt = 0x05,
        Head = 0x06,
        Gloves = 0x07,
        Ring = 0x08,
        Unused_x9 = 0x09,
        Neck = 0x0A,
        Hair = 0x0B,
        Waist = 0x0C,
        InnerTorso = 0x0D,
        Bracelet = 0x0E,
        Unused_xF = 0x0F,
        FacialHair = 0x10,
        MiddleTorso = 0x11,
        Earrings = 0x12,
        Arms = 0x13,
        Cloak = 0x14,
        Backpack = 0x15,
        OuterTorso = 0x16,
        OuterLegs = 0x17,
        InnerLegs = 0x18,

        LastUserValid = 0x18,

        Mount = 0x19,
        ShopBuy = 0x1A,
        ShopResale = 0x1B,
        ShopSell = 0x1C,
        Bank = 0x1D,

        LastValid = 0x1D
    }

    public class Item : UOEntity
    {
        private static readonly List<Item> m_NeedContUpdate = new List<Item>();

        private static readonly List<Serial> m_AutoStackCache = new List<Serial>();

        private Layer m_Layer;
        private string m_Name;
        private object m_Parent;

        private Timer m_RemoveTimer;

        public Item(BinaryReader reader, byte version) : base(reader, version)
        {
            ItemID = reader.ReadUInt16();
            Amount = reader.ReadUInt16();
            Direction = reader.ReadByte();
            ProcessPacketFlags(reader.ReadByte());
            m_Layer = (Layer) reader.ReadByte();
            m_Name = reader.ReadString();
            m_Parent = (Serial) reader.ReadUInt32();

            if ((Serial) m_Parent == Serial.Zero)
                m_Parent = null;

            int count = reader.ReadInt32();
            Serial.Serials = new List<Serial>(count);

            for (int i = 0; i < count; i++)
                Serial.Serials.Add(reader.ReadUInt32());

            if (version > 2)
            {
                HouseRevision = reader.ReadInt32();

                if (HouseRevision != 0)
                {
                    int len = reader.ReadUInt16();
                    HousePacket = reader.ReadBytes(len);
                }
            }
            else
            {
                HouseRevision = 0;
                HousePacket = null;
            }
        }

        public Item(Serial serial) : base(serial)
        {
            Contains = new List<Item>();

            Visible = true;
            Movable = true;

            Agent.InvokeItemCreated(this);
        }

        public ItemID ItemID { get; set; }

        public ushort Amount { get; set; }

        public byte Direction { get; set; }

        public bool Visible { get; set; }

        public bool Movable { get; set; }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Name))
                    return m_Name;

                return ItemID.ToString();
            }
            set
            {
                if (value != null)
                    m_Name = value.Trim();
                else
                    m_Name = null;
            }
        }

        public string DisplayName => TileData.ItemTable[ItemID.Value].Name;

        public Layer Layer
        {
            get
            {
                if ((m_Layer < Layer.FirstValid || m_Layer > Layer.LastValid) &&
                    ((ItemID.ItemData.Flags & TileFlag.Wearable) != 0 ||
                     (ItemID.ItemData.Flags & TileFlag.Armor) != 0 ||
                     (ItemID.ItemData.Flags & TileFlag.Weapon) != 0
                    ))
                    m_Layer = (Layer) ItemID.ItemData.Quality;

                return m_Layer;
            }
            set => m_Layer = value;
        }

        public object Container
        {
            get
            {
                if (m_Parent is Serial && UpdateContainer())
                    m_NeedContUpdate.Remove(this);

                return m_Parent;
            }
            set
            {
                if (m_Parent != null && m_Parent.Equals(value)
                    || value is Serial && m_Parent is UOEntity && ((UOEntity) m_Parent).Serial == (Serial) value
                    || m_Parent is Serial && value is UOEntity && ((UOEntity) value).Serial == (Serial) m_Parent)
                    return;

                if (m_Parent is Mobile)
                    ((Mobile) m_Parent).RemoveItem(this);
                else if (m_Parent is Item)
                    ((Item) m_Parent).RemoveItem(this);

                if (World.Player != null && (IsChildOf(World.Player.Backpack) || IsChildOf(World.Player.Quiver)))
                    Counter.Uncount(this);

                if (value is Mobile)
                    m_Parent = ((Mobile) value).Serial;
                else if (value is Item)
                    m_Parent = ((Item) value).Serial;
                else
                    m_Parent = value;

                if (!UpdateContainer() && m_NeedContUpdate != null)
                    m_NeedContUpdate.Add(this);
            }
        }

        public object RootContainer
        {
            get
            {
                int die = 100;
                object cont = Container;

                while (cont != null && cont is Item && die-- > 0)
                    cont = ((Item) cont).Container;

                return cont;
            }
        }

        public List<Item> Contains { get; private set; }

        // possibly 4 bit x/y - 16x16?
        public byte GridNum { get; set; }

        public bool OnGround => Container == null;

        public bool IsContainer
        {
            get
            {
                ushort iid = ItemID.Value;

                return Contains.Count > 0 && !IsCorpse || iid >= 0x9A8 && iid <= 0x9AC || iid >= 0x9B0 && iid <= 0x9B2 ||
                       iid >= 0xA2C && iid <= 0xA53 || iid >= 0xA97 && iid <= 0xA9E || iid >= 0xE3C && iid <= 0xE43 ||
                       iid >= 0xE75 && iid <= 0xE80 && iid != 0xE7B || iid == 0x1E80 || iid == 0x1E81 || iid == 0x232A || iid == 0x232B ||
                       iid == 0x2B02 || iid == 0x2B03 || iid == 0x2FB7 || iid == 0x3171;
            }
        }

        public bool IsBagOfSending => Hue >= 0x0400 && ItemID.Value == 0xE76;

        public bool IsInBank
        {
            get
            {
                if (m_Parent is Item)
                    return ((Item) m_Parent).IsInBank;

                if (m_Parent is Mobile)
                    return Layer == Layer.Bank;

                return false;
            }
        }

        public bool IsNew { get; set; }

        public bool AutoStack { get; set; }

        public bool IsMulti => ItemID.Value >= 0x4000;

        public bool IsPouch => ItemID.Value == 0x0E79;

        public bool IsCorpse => ItemID.Value == 0x2006 || ItemID.Value >= 0x0ECA && ItemID.Value <= 0x0ED2;

        public bool IsDoor
        {
            get
            {
                ushort iid = ItemID.Value;

                return iid >= 0x0675 && iid <= 0x06F6 || iid >= 0x0821 && iid <= 0x0875 || iid >= 0x1FED && iid <= 0x1FFC ||
                       iid >= 0x241F && iid <= 0x2424 || iid >= 0x2A05 && iid <= 0x2A1C;
            }
        }

        public bool IsResource
        {
            get
            {
                ushort iid = ItemID.Value;

                return iid >= 0x19B7 && iid <= 0x19BA || // ore
                       iid >= 0x09CC && iid <= 0x09CF || // fishes
                       iid >= 0x1BDD && iid <= 0x1BE2 || // logs
                       iid == 0x1779 || // granite / stone
                       iid == 0x11EA || iid == 0x11EB // sand
                    ;
            }
        }

        public bool IsPotion => ItemID.Value >= 0x0F06 && ItemID.Value <= 0x0F0D ||
                                ItemID.Value == 0x2790 || ItemID.Value == 0x27DB;

        public bool IsVirtueShield
        {
            get
            {
                ushort iid = ItemID.Value;

                return iid >= 0x1bc3 && iid <= 0x1bc5; // virtue shields
            }
        }

        public bool IsTwoHanded
        {
            get
            {
                ushort iid = ItemID.Value;

                return Layer == Layer.LeftHand &&
                       !(iid >= 0x1b72 && iid <= 0x1b7b || IsVirtueShield) || iid == 0x13fc || iid == 0x13fd || iid == 0x13AF || iid == 0x13b2 || iid >= 0x0F43 && iid <= 0x0F50 || iid == 0x1438 || iid == 0x1439 || iid == 0x1442 || iid == 0x1443 || iid == 0x1402 || iid == 0x1403 || iid == 0x26c1 || iid == 0x26cb || iid == 0x26c2 || iid == 0x26cc || iid == 0x26c3 || iid == 0x26cd // aos gay xbow
                    ;
            }
        }

        public int Price { get; set; }

        public string BuyDesc { get; set; }

        public int HouseRevision { get; set; }

        public byte[] HousePacket { get; set; }

        public override void SaveState(BinaryWriter writer)
        {
            base.SaveState(writer);

            writer.Write(ItemID);
            writer.Write(Amount);
            writer.Write(Direction);
            writer.Write(GetPacketFlags());
            writer.Write((byte) m_Layer);
            writer.Write(m_Name == null ? "" : m_Name);

            if (m_Parent is UOEntity)
                writer.Write((uint) ((UOEntity) m_Parent).Serial);
            else if (m_Parent is Serial)
                writer.Write((uint) (Serial) m_Parent);
            else
                writer.Write((uint) 0);

            //writer.Write( m_Items.Count );
            //for(int i=0;i<m_Items.Count;i++)
            //	writer.Write( (uint)((Item)m_Items[i]).Serial );
            writer.Write(0);

            if (HouseRevision != 0 && HousePacket == null)
                MakeHousePacket();

            if (HouseRevision != 0 && HousePacket != null)
            {
                writer.Write(HouseRevision);

                writer.Write((ushort) HousePacket.Length);
                writer.Write(HousePacket);
            }
            else
                writer.Write(0);
        }

        public override void AfterLoad()
        {
            Contains = new List<Item>();

            for (int i = 0; i < Serial.Serials.Count; i++)
            {
                Serial s = Serial.Serials[i];

                if (s.IsItem)
                {
                    Item item = World.FindItem(s);

                    if (item != null) Contains[i] = item;

                    Serial.Serials.RemoveAt(i);
                    i--;
                }
            }

            UpdateContainer();
        }

        public Item FindItemByID(ItemID id)
        {
            return FindItemByID(id, true);
        }

        public Item FindItemByID(ItemID id, bool recurse)
        {
            for (int i = 0; i < Contains.Count; i++)
            {
                Item item = Contains[i];

                if (item.ItemID == id)
                    return item;

                if (recurse)
                {
                    item = item.FindItemByID(id, true);

                    if (item != null)
                        return item;
                }
            }

            return null;
        }

        public int GetCount(ushort iid)
        {
            int count = 0;

            for (int i = 0; i < Contains.Count; i++)
            {
                Item item = Contains[i];

                if (item.ItemID == iid)
                    count += item.Amount;
                // fucking osi blank scrolls
                else if (item.ItemID == 0x0E34 && iid == 0x0EF3 || item.ItemID == 0x0EF3 && iid == 0x0E34)
                    count += item.Amount;
                count += item.GetCount(iid);
            }

            return count;
        }

        public bool UpdateContainer()
        {
            if (!(m_Parent is Serial) || Deleted)
                return true;

            object o = null;
            Serial contSer = (Serial) m_Parent;

            if (contSer.IsItem)
                o = World.FindItem(contSer);
            else if (contSer.IsMobile)
                o = World.FindMobile(contSer);

            if (o == null)
                return false;

            m_Parent = o;

            if (m_Parent is Item)
                ((Item) m_Parent).AddItem(this);
            else if (m_Parent is Mobile)
                ((Mobile) m_Parent).AddItem(this);

            if (World.Player != null && (IsChildOf(World.Player.Backpack) || IsChildOf(World.Player.Quiver)))
            {
                bool exempt = SearchExemptionAgent.IsExempt(this);

                if (!exempt)
                    Counter.Count(this);

                if (IsNew)
                {
                    if (AutoStack)
                        AutoStackResource();

                    if (IsContainer && !exempt && (!IsPouch || !Config.GetBool("NoSearchPouches")) && Config.GetBool("AutoSearch"))
                    {
                        PacketHandlers.IgnoreGumps.Add(this);
                        PlayerData.DoubleClick(this);

                        for (int c = 0; c < Contains.Count; c++)
                        {
                            Item icheck = Contains[c];

                            if (icheck.IsContainer && !SearchExemptionAgent.IsExempt(icheck) && (!icheck.IsPouch || !Config.GetBool("NoSearchPouches")))
                            {
                                PacketHandlers.IgnoreGumps.Add(icheck);
                                PlayerData.DoubleClick(icheck);
                            }
                        }
                    }
                }
            }

            AutoStack = IsNew = false;

            return true;
        }

        public static void UpdateContainers()
        {
            int i = 0;

            while (i < m_NeedContUpdate.Count)
            {
                if (m_NeedContUpdate[i].UpdateContainer())
                    m_NeedContUpdate.RemoveAt(i);
                else
                    i++;
            }
        }

        public void AutoStackResource()
        {
            if (!IsResource || !Config.GetBool("AutoStack") || m_AutoStackCache.Contains(Serial))
                return;

            foreach (Item check in World.Items.Values)
            {
                if (check.Container == null && check.ItemID == ItemID && check.Hue == Hue && Utility.InRange(World.Player.Position, check.Position, 2))
                {
                    DragDropManager.DragDrop(this, check);
                    m_AutoStackCache.Add(Serial);

                    return;
                }
            }

            DragDropManager.DragDrop(this, World.Player.Position);
            m_AutoStackCache.Add(Serial);
        }

        public bool IsChildOf(object parent)
        {
            Serial parentSerial = 0;

            if (parent is Mobile)
                return parent == RootContainer;

            if (parent is Item)
                parentSerial = ((Item) parent).Serial;
            else
                return false;

            object check = this;
            int die = 100;

            while (check != null && check is Item && die-- > 0)
            {
                if (((Item) check).Serial == parentSerial)
                    return true;

                check = ((Item) check).Container;
            }

            return false;
        }

        public Point3D GetWorldPosition()
        {
            int die = 100;
            object root = Container;

            while (root != null && root is Item && ((Item) root).Container != null && die-- > 0)
                root = ((Item) root).Container;

            if (root is Item)
                return ((Item) root).Position;

            if (root is Mobile)
                return ((Mobile) root).Position;

            return Position;
        }

        private void AddItem(Item item)
        {
            for (int i = 0; i < Contains.Count; i++)
            {
                if (Contains[i] == item)
                    return;
            }

            Contains.Add(item);
        }

        private void RemoveItem(Item item)
        {
            Contains.Remove(item);
        }

        public byte GetPacketFlags()
        {
            byte flags = 0;

            if (!Visible) flags |= 0x80;

            if (Movable) flags |= 0x20;

            return flags;
        }

        public int DistanceTo(Mobile m)
        {
            int x = Math.Abs(Position.X - m.Position.X);
            int y = Math.Abs(Position.Y - m.Position.Y);

            return x > y ? x : y;
        }

        public void ProcessPacketFlags(byte flags)
        {
            Visible = (flags & 0x80) == 0;
            Movable = (flags & 0x20) != 0;
        }

        public void RemoveRequest()
        {
            if (m_RemoveTimer == null)
                m_RemoveTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(0.25), Remove);
            else if (m_RemoveTimer.Running)
                m_RemoveTimer.Stop();

            m_RemoveTimer.Start();
        }

        public bool CancelRemove()
        {
            if (m_RemoveTimer != null && m_RemoveTimer.Running)
            {
                m_RemoveTimer.Stop();

                return true;
            }

            return false;
        }

        public override void Remove()
        {
            /*if ( IsMulti )
                UOAssist.PostRemoveMulti( this );*/

            List<Item> rem = new List<Item>(Contains);
            Contains.Clear();

            for (int i = 0; i < rem.Count; i++)
                rem[i].Remove();

            Counter.Uncount(this);

            if (m_Parent is Mobile)
                ((Mobile) m_Parent).RemoveItem(this);
            else if (m_Parent is Item)
                ((Item) m_Parent).RemoveItem(this);

            World.RemoveItem(this);
            base.Remove();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Serial);
        }

        public void MakeHousePacket()
        {
            HousePacket = null;

            try
            {
                // 3 locations... which is right? all of them? wtf?
                //"Desktop/{0}/{1}/{2}/Multicache.dat", World.AccountName, World.ShardName, World.OrigPlayerName
                //"Desktop/{0}/{1}/{2}/Multicache.dat", World.AccountName, World.ShardName, World.Player.Name );
                //"Desktop/{0}/Multicache.dat", World.AccountName );
                string path = Files.GetFilePath(string.Format("Desktop/{0}/{1}/{2}/Multicache.dat", World.AccountName, World.ShardName, World.OrigPlayerName));

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                using (StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    string line;
                    reader.ReadLine(); // ver 
                    int skip = 0;
                    int count = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (count++ < skip || line == "" || line[0] == ';')
                            continue;

                        string[] split = line.Split(' ', '\t');

                        if (split.Length <= 0)
                            return;

                        skip = 0;
                        Serial ser = (uint) Utility.ToInt32(split[0], 0);
                        int rev = Utility.ToInt32(split[1], 0);
                        int lines = Utility.ToInt32(split[2], 0);

                        if (ser == Serial)
                        {
                            HouseRevision = rev;
                            MultiTileEntry[] tiles = new MultiTileEntry[lines];
                            count = 0;

                            MultiComponentList mcl = Multis.GetComponents(ItemID);

                            while ((line = reader.ReadLine()) != null && count < lines)
                            {
                                split = line.Split(' ', '\t');

                                tiles[count] = new MultiTileEntry();
                                tiles[count].m_ItemID = (ushort) Utility.ToInt32(split[0], 0);
                                tiles[count].m_OffsetX = (short) (Utility.ToInt32(split[1], 0) + mcl.Center.X);
                                tiles[count].m_OffsetX = (short) (Utility.ToInt32(split[2], 0) + mcl.Center.Y);
                                tiles[count].m_OffsetX = (short) Utility.ToInt32(split[3], 0);

                                count++;
                            }

                            HousePacket = new DesignStateDetailed(Serial, HouseRevision, mcl.Min.X, mcl.Min.Y, mcl.Max.X, mcl.Max.Y, tiles).Compile();

                            break;
                        }

                        skip = lines;

                        count = 0;
                    }
                }
            }
            catch // ( Exception e )
            {
                //Engine.LogCrash( e );
            }
        }
    }
}