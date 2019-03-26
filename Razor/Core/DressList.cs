using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml;

using Assistant.Macros;

namespace Assistant
{
    public class DressList
    {
        private static readonly ArrayList m_List = new ArrayList();

        private Serial m_UndressBag;

        public DressList(string name)
        {
            Name = name;
            Items = new ArrayList();
            m_UndressBag = Serial.Zero;
        }

        public DressList(XmlElement xml)
        {
            Name = xml.GetAttribute("name");
        }

        public string Name { get; }
        public ArrayList Items { get; }

        public static void Redraw(ListBox box)
        {
            box.Items.Clear();

            box.Items.AddRange((object[]) m_List.ToArray(typeof(object)));
        }

        public static void ClearAll()
        {
            while (m_List.Count > 0)
                Remove((DressList) m_List[0]);
        }

        public static DressList Find(string name)
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                DressList list = (DressList) m_List[i];

                if (list.Name == name)
                    return list;
            }

            return null;
        }

        public static Item FindUndressBag(Item item)
        {
            Item undressBag = World.Player.Backpack;

            for (int i = 0; i < m_List.Count; i++)
            {
                DressList list = (DressList) m_List[i];

                if (list != null && (list.Items.Contains(item.Serial) || list.Items.Contains(item.ItemID)))
                {
                    if (list.m_UndressBag.IsValid)
                    {
                        Item bag = World.FindItem(list.m_UndressBag);

                        if (bag != null && (bag.RootContainer == World.Player || bag.RootContainer == null && Utility.InRange(bag.GetWorldPosition(), World.Player.Position, 2)))
                            undressBag = bag;
                    }

                    break;
                }
            }

            return undressBag;
        }

        public static void Load(XmlElement xml)
        {
            ClearAll();

            if (xml == null)
                return;

            foreach (XmlElement el in xml.GetElementsByTagName("list"))
            {
                try
                {
                    string name = el.GetAttribute("name");
                    DressList list = new DressList(name);
                    Add(list);

                    try
                    {
                        list.m_UndressBag = Serial.Parse(el.GetAttribute("undressbag"));
                    }
                    catch
                    {
                        list.m_UndressBag = Serial.Zero;
                    }

                    foreach (XmlElement el2 in el.GetElementsByTagName("item"))
                    {
                        try
                        {
                            string ser = el2.GetAttribute("serial");
                            list.Items.Add((Serial) Convert.ToUInt32(ser));
                        }
                        catch
                        {
                            try
                            {
                                string iid = el2.GetAttribute("id");
                                list.Items.Add((ItemID) Convert.ToUInt16(iid));
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public static void Save(XmlTextWriter xml)
        {
            foreach (DressList list in m_List)
            {
                xml.WriteStartElement("list");
                xml.WriteAttributeString("name", list.Name);

                if (list.m_UndressBag.IsItem)
                    xml.WriteAttributeString("undressbag", list.m_UndressBag.ToString());

                foreach (object o in list.Items)
                {
                    xml.WriteStartElement("item");

                    if (o is Serial)
                        xml.WriteAttributeString("serial", ((Serial) o).Value.ToString());
                    else if (o is ItemID)
                        xml.WriteAttributeString("id", ((ItemID) o).Value.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }
        }

        public static void Add(DressList list)
        {
            m_List.Add(list);
            HotKey.Add(HKCategory.Dress, HKSubCat.None, string.Format("Dress: {0}", list.Name), list.Dress);
            HotKey.Add(HKCategory.Dress, HKSubCat.None, string.Format("Undress: {0}", list.Name), list.Undress);
            HotKey.Add(HKCategory.Dress, HKSubCat.None, string.Format("Toggle: {0}", list.Name), list.Toggle);
        }

        public static void Remove(DressList list)
        {
            m_List.Remove(list);
            HotKey.Remove(string.Format("Dress: {0}", list.Name));
            HotKey.Remove(string.Format("Undress: {0}", list.Name));
            HotKey.Remove(string.Format("Toggle: {0}", list.Name));
        }

        public override string ToString()
        {
            return Name;
        }

        public void SetUndressBag(Serial serial)
        {
            if (serial == World.Player.Backpack.Serial || !serial.IsItem)
                m_UndressBag = Serial.Zero;
            else
                m_UndressBag = serial;
        }

        public void Toggle()
        {
            if (World.Player == null)
                return;

            int worn = 0;
            int total = Items.Count;

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = null;

                if (Items[i] is Serial)
                    item = World.FindItem((Serial) Items[i]);
                else if (Items[i] is ItemID)
                    item = World.Player.FindItemByID((ItemID) Items[i]);

                if (item == null)
                    total--;
                else if (item.Container == World.Player)
                    worn++;
            }

            if (Items.Count == 1)
            {
                if (worn != 0)
                    Undress();
                else
                    Dress();
            }
            else
            {
                if (worn > total / 2)
                    Undress();
                else
                    Dress();
            }
        }

        public void Undress()
        {
            if (World.Player == null)
                return;

            int count = 0;
            Item undressBag = World.Player.Backpack;

            if (undressBag == null)
            {
                World.Player.SendMessage(LocString.NoBackpack);

                return;
            }

            if (MacroManager.AcceptActions)
                MacroManager.Action(new UnDressAction(Name));

            if (m_UndressBag.IsValid)
            {
                Item bag = World.FindItem(m_UndressBag);

                if (bag != null && (bag.RootContainer == World.Player || bag.RootContainer == null && Utility.InRange(bag.GetWorldPosition(), World.Player.Position, 2)))
                    undressBag = bag;
                else
                    World.Player.SendMessage(LocString.UndressBagRange);
            }

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = null;

                if (Items[i] is Serial)
                    item = World.FindItem((Serial) Items[i]);
                else if (Items[i] is ItemID)
                    item = World.Player.FindItemByID((ItemID) Items[i]);

                if (item == null || DragDropManager.CancelDragFor(item.Serial) || item.Container != World.Player)
                    continue;

                DragDropManager.DragDrop(item, undressBag);
                count++;
            }

            World.Player.SendMessage(LocString.UndressQueued, count);
        }

        public static Layer GetLayerFor(Item item)
        {
            Layer layer = item.Layer;

            if (layer == Layer.Invalid || layer > Layer.LastUserValid)
                layer = (Layer) item.ItemID.ItemData.Quality;

            return layer;
        }

        public void Dress()
        {
            if (World.Player == null)
                return;

            int skipped = 0, gone = 0, done = 0;
            ArrayList list = new ArrayList();
            bool remConflicts = Config.GetBool("UndressConflicts");

            if (World.Player.Backpack == null)
            {
                World.Player.SendMessage(LocString.NoBackpack);

                return;
            }

            if (MacroManager.AcceptActions)
                MacroManager.Action(new DressAction(Name));

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = null;

                if (Items[i] is Serial)
                {
                    item = World.FindItem((Serial) Items[i]);

                    if (item == null)
                        gone++;
                    else
                        list.Add(item);
                }
                else if (Items[i] is ItemID)
                {
                    ItemID id = (ItemID) Items[i];

                    // search to make sure they are not already wearing this...
                    item = World.Player.FindItemByID(id);

                    if (item != null)
                        skipped++;
                    else
                    {
                        item = World.Player.Backpack.FindItemByID(id);

                        if (item == null)
                            gone++;
                        else
                            list.Add(item);
                    }
                }
            }

            foreach (Item item in list)
            {
                if (item.Container == World.Player)
                    skipped++;
                else if (item.IsChildOf(World.Player.Backpack) || item.RootContainer == null)
                {
                    Layer layer = GetLayerFor(item);

                    if (layer == Layer.Invalid || layer > Layer.LastUserValid)
                        continue;

                    if (remConflicts)
                    {
                        Item conflict = World.Player.GetItemOnLayer(layer);

                        if (conflict != null)
                            DragDropManager.DragDrop(conflict, FindUndressBag(conflict));

                        // try to also undress conflicting hand(s)
                        if (layer == Layer.RightHand)
                            conflict = World.Player.GetItemOnLayer(Layer.LeftHand);
                        else if (layer == Layer.LeftHand)
                            conflict = World.Player.GetItemOnLayer(Layer.RightHand);
                        else
                            conflict = null;

                        if (conflict != null && (conflict.IsTwoHanded || item.IsTwoHanded))
                            DragDropManager.DragDrop(conflict, FindUndressBag(conflict));
                    }

                    DragDropManager.DragDrop(item, World.Player, layer);
                    done++;
                }
            }

            if (done > 0)
                World.Player.SendMessage(LocString.DressQueued, done);

            if (skipped > 0)
                World.Player.SendMessage(LocString.AlreadyDressed, skipped);

            if (gone > 0)
                World.Player.SendMessage(LocString.ItemsNotFound, gone);
        }
    }
}