using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using Assistant.UI;

namespace Assistant
{
    public class CounterLVIComparer : IComparer
    {
        private static CounterLVIComparer m_Instance;

        public static CounterLVIComparer Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new CounterLVIComparer();

                return m_Instance;
            }
        }

        public int Compare(object a, object b)
        {
            return ((IComparable) ((ListViewItem) a).Tag).CompareTo(((ListViewItem) b).Tag);
        }
    }

    public class Counter : IComparable
    {
        private static bool m_NeedXMLSave;
        private static readonly Dictionary<Item, ushort> m_Cache = new Dictionary<Item, ushort>();
        private int m_Count;
        private bool m_Enabled;
        private DateTime m_LastWarning;

        static Counter()
        {
            List = new List<Counter>();
            m_Cache = new Dictionary<Item, ushort>();
            Load();
        }

        public Counter(string name, string fmt, ushort iid, int hue, bool dispImg)
        {
            Name = name;
            Format = fmt;
            ItemID = iid;
            Hue = hue;
            ViewItem = new ListViewItem(new string[2]);
            ViewItem.SubItems[0].Text = ToString();
            ViewItem.Tag = this;
            ViewItem.Checked = m_Enabled = false;
            m_Count = 0;
            DisplayImage = dispImg;

            m_NeedXMLSave = true;
        }

        public Counter(XmlElement node)
        {
            Name = GetText(node["name"], "");
            Format = GetText(node["format"], "");
            ItemID = (ushort) GetInt(GetText(node["itemid"], "0"), 0);
            Hue = GetInt(GetText(node["hue"], "-1"), -1);

            ViewItem = new ListViewItem(new string[2] {ToString(), ""});
            ViewItem.Tag = this;
            ViewItem.Checked = m_Enabled = false;

            DisplayImage = true;
        }

        public string Name { get; private set; }
        public string Format { get; private set; }
        public ushort ItemID { get; private set; }
        public int Hue { get; private set; }
        public bool Flag { get; set; }

        public ListViewItem ViewItem { get; }

        public int Amount
        {
            get => m_Count;
            set
            {
                if (m_Count != value)
                {
                    if (m_Enabled)
                    {
                        if (!SupressWarnings && m_LastWarning + TimeSpan.FromSeconds(1.0) < DateTime.UtcNow &&
                            World.Player != null && value < m_Count && Config.GetBool("CounterWarn") && value < Config.GetInt("CounterWarnAmount"))
                        {
                            World.Player.SendMessage(MsgLevel.Warning, LocString.CountLow, Name, value);
                            m_LastWarning = DateTime.UtcNow;
                        }

                        if (UOAssist.NotificationCount > 0)
                        {
                            int wp = 0;

                            if (Format == "bm")
                                wp = 1;
                            else if (Format == "bp")
                                wp = 2;
                            else if (Format == "gl")
                                wp = 3;
                            else if (Format == "gs")
                                wp = 4;
                            else if (Format == "mr")
                                wp = 5;
                            else if (Format == "ns")
                                wp = 6;
                            else if (Format == "sa")
                                wp = 7;
                            else if (Format == "ss")
                                wp = 8;
                            else if (Format == "bw")
                                wp = 100;
                            else if (Format == "db")
                                wp = 101;
                            else if (Format == "gd")
                                wp = 102;
                            else if (Format == "nc")
                                wp = 103;
                            else if (Format == "pi")
                                wp = 104;

                            if (wp != 0)
                                UOAssist.PostCounterUpdate(wp, value);
                        }

                        m_Count = value;

                        if (m_Count < 0)
                            m_Count = 0;

                        //Engine.MainWindow.RefreshCounters();
                        Windows.RequestTitleBarUpdate();
                    }

                    ViewItem.SubItems[1].Text = m_Count.ToString();
                }
            }
        }

        public bool Enabled
        {
            get => m_Enabled;
            set
            {
                if (m_Enabled != value)
                {
                    ViewItem.Checked = value;
                    SetEnabled(value);
                }
            }
        }

        public bool DisplayImage { get; set; }

        public static List<Counter> List { get; } = new List<Counter>();

        public static bool SupressWarnings { get; set; }

        public static bool SupressChecks { get; private set; }

        public int CompareTo(object comp)
        {
            if (!(comp is Counter))
                return 1;

            if (Enabled && ((Counter) comp).Enabled)
                return Name == null ? 1 : ((Counter) comp).Name == null ? -1 : Name.CompareTo(((Counter) comp).Name);

            if (!Enabled && ((Counter) comp).Enabled)
                return 1;

            if (Enabled && !((Counter) comp).Enabled)
                return -1;

            return Name == null ? 1 : ((Counter) comp).Name == null ? -1 : Name.CompareTo(((Counter) comp).Name);
        }

        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("counter");

            xml.WriteStartElement("name");
            xml.WriteString(Name);
            xml.WriteEndElement();

            xml.WriteStartElement("format");
            xml.WriteString(Format);
            xml.WriteEndElement();

            xml.WriteStartElement("itemid");
            xml.WriteString(ItemID.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("hue");
            xml.WriteString(Hue.ToString());
            xml.WriteEndElement();

            xml.WriteEndElement();
        }

        public void Set(ushort iid, int hue, string name, string fmt, bool dispImg)
        {
            ItemID = iid;
            Hue = hue;
            Name = name;
            Format = fmt;
            DisplayImage = dispImg;

            ViewItem.SubItems[0].Text = ToString();
            m_NeedXMLSave = true;
        }

        public string GetTitlebarString(bool dispImg)
        {
            StringBuilder sb = new StringBuilder();

            if (dispImg)
            {
                sb.AppendFormat("~I{0:X4}", ItemID);

                if (Hue > 0 && Hue < 0xFFFF)
                    sb.Append(Hue.ToString("X4"));
                else
                    sb.Append('~');
                sb.Append(": ");
            }

            if (Flag && Config.GetBool("HighlightReagents"))
                sb.AppendFormat("~^C00000{0}~#~", m_Count);
            else if (m_Count == 0 || m_Count < Config.GetInt("CounterWarnAmount"))
                sb.AppendFormat("~#FF0000{0}~#~", m_Count);
            else
                sb.Append(m_Count.ToString());

            return sb.ToString();
        }

        public void SetEnabled(bool value)
        {
            m_Enabled = value;

            if (m_Enabled)
            {
                if (!SupressChecks)
                    QuickRecount();
                ViewItem.SubItems[1].Text = m_Count.ToString();
            }
            else
                ViewItem.SubItems[1].Text = "";
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Format);
        }

        private static void Load()
        {
            string file = Path.Combine(Config.GetUserDirectory(), "counters.xml");

            if (!File.Exists(file))
                return;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                XmlElement root = doc["counters"];

                if (root != null)
                {
                    foreach (XmlElement node in root.GetElementsByTagName("counter"))
                        List.Add(new Counter(node));
                }
            }
            catch
            {
                MessageBox.Show(Engine.ActiveWindow, Language.GetString(LocString.CounterFux), "Counters.xml Load Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            m_NeedXMLSave = false;
        }

        public static void Save()
        {
            if (!m_NeedXMLSave)
                return;

            try
            {
                string file = Path.Combine(Config.GetUserDirectory(), "counters.xml");

                using (StreamWriter op = new StreamWriter(file))
                {
                    XmlTextWriter xml = new XmlTextWriter(op);

                    xml.Formatting = Formatting.Indented;
                    xml.IndentChar = '\t';
                    xml.Indentation = 1;

                    xml.WriteStartDocument(true);

                    xml.WriteStartElement("counters");

                    foreach (Counter c in List)
                        c.Save(xml);

                    xml.WriteEndElement();
                    xml.Close();
                }

                m_NeedXMLSave = false;
            }
            catch
            {
            }
        }

        public static void SaveProfile(XmlTextWriter xml)
        {
            for (int i = 0; i < List.Count; i++)
            {
                Counter c = List[i];

                if (c.Enabled)
                {
                    xml.WriteStartElement("counter");
                    xml.WriteAttributeString("name", c.Name);
                    xml.WriteAttributeString("enabled", c.Enabled.ToString());
                    xml.WriteAttributeString("image", c.DisplayImage.ToString());
                    xml.WriteEndElement();
                }
            }
        }

        public static void Default()
        {
            for (int i = 0; i < List.Count; i++)
            {
                Counter c = List[i];

                if (c.Format == "bp" || c.Format == "bm" || c.Format == "gl" || c.Format == "gs" ||
                    c.Format == "mr" || c.Format == "ns" || c.Format == "ss" || c.Format == "sa" ||
                    c.Format == "aids")
                    c.Enabled = true;
            }
        }

        public static void DisableAll()
        {
            for (int i = 0; i < List.Count; i++)
                List[i].Enabled = false;
        }

        public static void LoadProfile(XmlElement xml)
        {
            Reset();
            DisableAll();

            if (xml == null)
                return;

            foreach (XmlElement el in xml.GetElementsByTagName("counter"))
            {
                try
                {
                    string name = el.GetAttribute("name");
                    string en = el.GetAttribute("enabled");
                    string img = el.GetAttribute("image");

                    for (int i = 0; i < List.Count; i++)
                    {
                        Counter c = List[i];

                        if (c.Name == name)
                        {
                            c.Enabled = Convert.ToBoolean(en);

                            try
                            {
                                c.DisplayImage = Convert.ToBoolean(img);
                            }
                            catch
                            {
                                c.DisplayImage = true;
                            }

                            break;
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }

        private static int GetInt(string value, int def)
        {
            try
            {
                return XmlConvert.ToInt32(value);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(value);
                }
                catch
                {
                    return def;
                }
            }
        }

        public static void Register(Counter c)
        {
            List.Add(c);
            m_NeedXMLSave = true;
            Engine.MainWindow.SafeAction(s => s.RedrawCounters());
        }

        public static void Uncount(Item item)
        {
            for (int i = 0; i < item.Contains.Count; i++)
                Uncount(item.Contains[i]);

            for (int i = 0; i < List.Count; i++)
            {
                Counter c = List[i];

                if (c.Enabled)
                {
                    if (c.ItemID == item.ItemID && (c.Hue == item.Hue || c.Hue == -1 || c.Hue == 0xFFFF))
                    {
                        if (m_Cache.TryGetValue(item, out ushort rem))
                        {
                            if (rem >= c.Amount)
                                c.Amount = 0;
                            else
                                c.Amount -= rem;

                            m_Cache.Remove(item);
                        }

                        break;
                    }
                }
            }
        }

        public static void Count(Item item)
        {
            for (int i = 0; i < List.Count; i++)
            {
                Counter c = List[i];

                if (c.Enabled)
                {
                    if (c.ItemID == item.ItemID && (c.Hue == item.Hue || c.Hue == 0xFFFF || c.Hue == -1))
                    {
                        if (m_Cache.TryGetValue(item, out var old))
                        {
                            if (old == item.Amount)
                                break; // dont change result cause we dont need an update
                        }

                        c.Amount += item.Amount - old;
                        m_Cache[item] = item.Amount;

                        break;
                    }
                }
            }

            for (int c = 0; c < item.Contains.Count; c++)
                Count(item.Contains[c]);
        }

        public static void QuickRecount()
        {
            Reset();

            SupressWarnings = true;
            Item pack = World.Player == null ? null : World.Player.Backpack;

            if (pack != null)
                Count(pack);
            pack = World.Player == null ? null : World.Player.Quiver;

            if (pack != null)
                Count(pack);
            SupressWarnings = false;
        }

        public static void FullRecount()
        {
            Reset();

            if (World.Player != null)
            {
                SupressWarnings = true;

                if (World.Player.Backpack != null)
                {
                    while (World.Player.Backpack.Contains.Count > 0)
                        World.Player.Backpack.Contains[0].Remove();

                    PacketHandlers.IgnoreGumps.Add(World.Player.Backpack);
                    PlayerData.DoubleClick(World.Player.Backpack);
                }

                if (World.Player.Quiver != null)
                {
                    while (World.Player.Quiver.Contains.Count > 0)
                        World.Player.Quiver.Contains[0].Remove();

                    PacketHandlers.IgnoreGumps.Add(World.Player.Quiver);
                    PlayerData.DoubleClick(World.Player.Quiver);
                }

                if (!Config.GetBool("AutoSearch"))
                    World.Player.SendMessage(MsgLevel.Info, LocString.NoAutoCount);
                SupressWarnings = false;
            }
        }

        public static void Reset()
        {
            SupressWarnings = true;
            m_Cache.Clear();

            for (int i = 0; i < List.Count; i++)
                List[i].Amount = 0;
            SupressWarnings = false;
        }

        public static void Redraw(ListView list)
        {
            SupressChecks = true;
            list.BeginUpdate();
            list.Items.Clear();

            for (int i = 0; i < List.Count; i++)
                list.Items.Add(List[i].ViewItem);
            list.EndUpdate();
            SupressChecks = false;
        }
    }
}