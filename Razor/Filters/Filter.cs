using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml;

namespace Assistant.Filters
{
    public abstract class Filter
    {
        private readonly PacketViewerCallback m_Callback;

        protected Filter()
        {
            Enabled = false;
            m_Callback = OnFilter;
        }

        public static ArrayList List { get; } = new ArrayList();
        public abstract byte[] PacketIDs { get; }
        public abstract LocString Name { get; }

        public bool Enabled { get; private set; }

        public static void Register(Filter filter)
        {
            List.Add(filter);
        }

        public static void Load(XmlElement xml)
        {
            DisableAll();

            if (xml == null)
                return;

            foreach (XmlElement el in xml.GetElementsByTagName("filter"))
            {
                try
                {
                    LocString name = (LocString) Convert.ToInt32(el.GetAttribute("name"));
                    string enable = el.GetAttribute("enable");

                    for (int i = 0; i < List.Count; i++)
                    {
                        Filter f = (Filter) List[i];

                        if (f.Name == name)
                        {
                            if (Convert.ToBoolean(enable))
                                f.OnEnable();

                            break;
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public static void DisableAll()
        {
            for (int i = 0; i < List.Count; i++)
                ((Filter) List[i]).OnDisable();
        }

        public static void Save(XmlTextWriter xml)
        {
            for (int i = 0; i < List.Count; i++)
            {
                Filter f = (Filter) List[i];

                if (f.Enabled)
                {
                    xml.WriteStartElement("filter");
                    xml.WriteAttributeString("name", ((int) f.Name).ToString());
                    xml.WriteAttributeString("enable", f.Enabled.ToString());
                    xml.WriteEndElement();
                }
            }
        }

        public static void Draw(CheckedListBox list)
        {
            list.BeginUpdate();
            list.Items.Clear();

            for (int i = 0; i < List.Count; i++)
            {
                Filter f = (Filter) List[i];
                list.Items.Add(f);
                list.SetItemChecked(i, f.Enabled);
            }

            list.EndUpdate();
        }

        public abstract void OnFilter(PacketReader p, PacketHandlerEventArgs args);

        public override string ToString()
        {
            return Language.GetString(Name);
        }

        public virtual void OnEnable()
        {
            Enabled = true;

            for (int i = 0; i < PacketIDs.Length; i++)
                PacketHandler.RegisterServerToClientViewer(PacketIDs[i], m_Callback);
        }

        public virtual void OnDisable()
        {
            Enabled = false;

            for (int i = 0; i < PacketIDs.Length; i++)
                PacketHandler.RemoveServerToClientViewer(PacketIDs[i], m_Callback);
        }

        public void OnCheckChanged(CheckState newValue)
        {
            if (Enabled && newValue == CheckState.Unchecked)
                OnDisable();
            else if (!Enabled && newValue == CheckState.Checked)
                OnEnable();
        }
    }
}