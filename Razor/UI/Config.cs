using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

using Assistant.Filters;
using Assistant.Macros;
using Assistant.UI;

using ContainerLabels = Assistant.Core.ContainerLabels;
using OverheadMessages = Assistant.Core.OverheadMessages;

namespace Assistant
{
    public class Profile
    {
        private static readonly bool m_Warned = false;

        private static readonly Type[] ctorTypes = {typeof(string)};
        private Mutex m_Mutex;
        private string m_Name;
        private readonly Dictionary<string, object> m_Props;

        public Profile(string name)
        {
            Name = name;
            m_Props = new Dictionary<string, object>(16, StringComparer.OrdinalIgnoreCase);

            MakeDefault();
        }

        public string Name
        {
            get => m_Name;
            set
            {
                if (value != null && value.Trim() != "")
                {
                    StringBuilder sb = new StringBuilder(value);
                    sb.Replace('\\', '_');
                    sb.Replace('/', '_');
                    sb.Replace('\"', '\'');
                    sb.Replace(':', '_');
                    sb.Replace('?', '_');
                    sb.Replace('*', '_');
                    sb.Replace('<', '(');
                    sb.Replace('>', ')');
                    sb.Replace('|', '_');
                    m_Name = sb.ToString();
                }
                else
                    m_Name = "[No Name]";
            }
        }

        public void MakeDefault()
        {
            m_Props.Clear();

            AddProperty("ShowMobNames", false);
            AddProperty("ShowCorpseNames", false);
            AddProperty("DisplaySkillChanges", false);

            AddProperty("TitleBarText",
                        @"UO - {char} {crimtime}- {mediumstatbar} {bp} {bm} {gl} {gs} {mr} {ns} {ss} {sa} {aids}");
            AddProperty("TitleBarDisplay", true);
            AddProperty("AutoSearch", true);
            AddProperty("NoSearchPouches", true);
            AddProperty("CounterWarnAmount", 5);
            AddProperty("CounterWarn", true);
            AddProperty("ObjectDelay", 600);
            AddProperty("ObjectDelayEnabled", true);
            AddProperty("AlwaysOnTop", false);
            AddProperty("SortCounters", true);
            AddProperty("QueueActions", true);
            AddProperty("QueueTargets", true);
            AddProperty("WindowX", 400);
            AddProperty("WindowY", 400);
            AddProperty("CountStealthSteps", true);
            AddProperty("AlwaysStealth", false);

            AddProperty("SysColor", 0x03B1);
            AddProperty("WarningColor", 0x0025);
            AddProperty("ExemptColor", 0x0480);
            AddProperty("SpeechHue", 0x03B1);
            AddProperty("BeneficialSpellHue", 0x0005);
            AddProperty("HarmfulSpellHue", 0x0025);
            AddProperty("NeutralSpellHue", 0x03B1);
            AddProperty("ForceSpeechHue", false);
            AddProperty("ForceSpellHue", false);
            AddProperty("SpellFormat", @"{power} [{spell}]");

            AddProperty("ShowNotoHue", true);
            AddProperty("Opacity", 100);

            AddProperty("AutoOpenCorpses", false);
            AddProperty("CorpseRange", 2);

            AddProperty("BlockDismount", false);

            AddProperty("AutoCap", false);
            AddProperty("CapFullScreen", false);

            AddProperty("CapPath",
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "RazorScreenShots"));
            AddProperty("CapTimeStamp", true);
            AddProperty("ImageFormat", "jpg");

            AddProperty("UndressConflicts", true);
            AddProperty("HighlightReagents", true);
            AddProperty("Systray", false);
            AddProperty("TitlebarImages", true);

            AddProperty("SellAgentMax", 99);
            AddProperty("SkillListCol", -1);
            AddProperty("SkillListAsc", false);

            AddProperty("AutoStack", false);
            AddProperty("ActionStatusMsg", true);

            AddProperty("SpellUnequip", false);
            AddProperty("RangeCheckLT", true);
            AddProperty("LTRange", 12);

            AddProperty("FilterSnoopMsg", true);
            AddProperty("OldStatBar", false);

            AddProperty("SmartLastTarget", false);
            AddProperty("LastTargTextFlags", true);
            AddProperty("LTHilight", 0);

            AddProperty("AutoFriend", false);

            AddProperty("AutoOpenDoors", true);

            AddProperty("MessageLevel", 0);

            AddProperty("ForceIP", "");
            AddProperty("ForcePort", 0);

            AddProperty("PotionEquip", false);
            AddProperty("BlockHealPoison", false);

            AddProperty("SmoothWalk", false);

            AddProperty("MapX", 200);
            AddProperty("MapY", 200);
            AddProperty("MapW", 200);
            AddProperty("MapH", 200);

            AddProperty("LogPacketsByDefault", false);

            AddProperty("ShowHealth", false);
            AddProperty("HealthFmt", "[{0}%]");
            AddProperty("ShowPartyStats", false);
            AddProperty("PartyStatFmt", "[{0}% / {1}%]");

            AddProperty("JsonApi", false);
            AddProperty("HotKeyStop", false);
            AddProperty("DiffTargetByType", false);
            AddProperty("StepThroughMacro", false);

            // Map options
            /*AddProperty("ShowPlayerPosition", true);
            AddProperty("TrackPlayerPosition", true);
            AddProperty("ShowPartyMemberPositions", true);
            AddProperty("TiltMap", true);*/

            AddProperty("ShowTargetSelfLastClearOverhead", true);
            AddProperty("ShowOverheadMessages", false);

            //OverheadFormat
            AddProperty("OverheadFormat", "[{msg}]");
            AddProperty("OverheadStyle", 1);

            AddProperty("GoldPerDisplay", false);

            AddProperty("LightLevel", 31);
            AddProperty("LogSkillChanges", false);
            AddProperty("StealthOverhead", false);

            AddProperty("MapGuardLines", true);
            AddProperty("MapGridLines", false);
            AddProperty("MapTilt", true);
            AddProperty("DisplayMarkerNames", true);
            AddProperty("DisplayMarkerCoords", true);

            AddProperty("GuardLinesFile", "guardlines");

            AddProperty("MapSelectedPinList", "uopoints");

            AddProperty("ShowBuffDebuffOverhead", false);
            AddProperty("BuffDebuffFormat", "[{action}{name}]");

            AddProperty("BlockOpenCorpsesTwice", false);

            AddProperty("ScreenshotUploadNotifications", false);
            AddProperty("ScreenshotUploadClipboard", true);
            AddProperty("ScreenshotUploadOpenBrowser", true);

            AddProperty("ShowAttackTargetOverhead", true);

            AddProperty("RangeCheckTargetByType", false);
            AddProperty("RangeCheckDoubleClick", false);

            AddProperty("ShowContainerLabels", false);
            AddProperty("ContainerLabelFormat", "[{label}] ({type})");
            AddProperty("ContainerLabelColor", 88);
            AddProperty("ContainerLabelStyle", 1);

            AddProperty("RealSeason", false);
            AddProperty("Season", 5);

            AddProperty("BlockTradeRequests", false);
            AddProperty("BlockPartyInvites", false);
            AddProperty("AutoAcceptParty", false);

            AddProperty("MaxLightLevel", 31);
            AddProperty("MinLightLevel", 0);
            AddProperty("MinMaxLightLevelEnabled", false);

            Counter.Default();
            Filter.DisableAll();
            DressList.ClearAll();
            HotKey.ClearAll();
            Agent.ClearAll();
            OverheadMessages.ClearAll();
            ContainerLabels.ClearAll();
        }

        public bool Load()
        {
            if (m_Name == null || m_Name.Trim() == "")
                return false;

            string path = Config.GetUserDirectory("Profiles");
            string file = Path.Combine(path, string.Format("{0}.xml", m_Name));

            if (!File.Exists(file))
                return false;

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(file);
            }
            catch
            {
                MessageBox.Show(Engine.ActiveWindow, Language.Format(LocString.ProfileCorrupt, file), "Profile Load Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                return false;
            }

            XmlElement root = doc["profile"];

            if (root == null)
                return false;

            Assembly exe = Assembly.GetCallingAssembly();

            if (exe == null)
                return false;

            foreach (XmlElement el in root.GetElementsByTagName("property"))
            {
                try
                {
                    string name = el.GetAttribute("name");
                    string typeStr = el.GetAttribute("type");
                    string val = el.InnerText;

                    if (typeStr == "-null-" || name == "LimitSize" || name == "VisRange")
                    {
                        //m_Props[name] = null;
                        if (m_Props.ContainsKey(name))
                            m_Props.Remove(name);
                    }
                    else
                    {
                        Type type = Type.GetType(typeStr);

                        if (type == null)
                            type = exe.GetType(typeStr);

                        if (m_Props.ContainsKey(name))
                        {
                            if (type == null)
                                m_Props.Remove(name);
                            else
                                m_Props[name] = GetObjectFromString(val, type);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(Engine.ActiveWindow, Language.Format(LocString.ProfileLoadEx, e.ToString()), "Profile Load Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            Filter.Load(root["filters"]);
            Counter.LoadProfile(root["counters"]);
            Agent.LoadProfile(root["agents"]);
            DressList.Load(root["dresslists"]);
            HotKey.Load(root["hotkeys"]);
            OverheadMessages.Load(root["overheadmessages"]);
            ContainerLabels.Load(root["containerlabels"]);
            AbsoluteTargets.Load(root["absolutetargets"]);

            GoldPerHourTimer.Stop();

            return true;
        }

        public void Unload()
        {
            try
            {
                if (m_Mutex != null)
                {
                    m_Mutex.ReleaseMutex();
                    m_Mutex.Close();
                    m_Mutex = null;
                }
            }
            catch
            {
            }
        }

        public void Save()
        {
            string profileDir = Config.GetUserDirectory("Profiles");
            string file = Path.Combine(profileDir, string.Format("{0}.xml", m_Name));

            if (m_Name != "default" && !m_Warned)
            {
                try
                {
                    m_Mutex = new Mutex(true, string.Format("Razor_Profile_{0}", m_Name));

                    if (!m_Mutex.WaitOne(10, false))
                        throw new Exception("Can't grab profile mutex, must be in use!");
                }
                catch
                {
                    //MessageBox.Show( Engine.ActiveWindow, Language.Format( LocString.ProfileInUse, m_Name ), "Profile In Use", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                    //m_Warned = true;
                    return; // refuse to overwrite profiles that we don't own.
                }
            }

            XmlTextWriter xml;

            try
            {
                xml = new XmlTextWriter(file, Encoding.Default);
            }
            catch
            {
                return;
            }

            xml.Formatting = Formatting.Indented;
            xml.IndentChar = '\t';
            xml.Indentation = 1;

            xml.WriteStartDocument(true);
            xml.WriteStartElement("profile");

            foreach (KeyValuePair<string, object> de in m_Props)
            {
                xml.WriteStartElement("property");
                xml.WriteAttributeString("name", de.Key);

                if (de.Value == null)
                    xml.WriteAttributeString("type", "-null-");
                else
                {
                    xml.WriteAttributeString("type", de.Value.GetType().FullName);
                    xml.WriteString(de.Value.ToString());
                }

                xml.WriteEndElement();
            }

            xml.WriteStartElement("filters");
            Filter.Save(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("counters");
            Counter.SaveProfile(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("agents");
            Agent.SaveProfile(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("dresslists");
            DressList.Save(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("hotkeys");
            HotKey.Save(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("overheadmessages");
            OverheadMessages.Save(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("containerlabels");
            ContainerLabels.Save(xml);
            xml.WriteEndElement();

            xml.WriteStartElement("absolutetargets");
            AbsoluteTargets.Save(xml);
            xml.WriteEndElement();

            xml.WriteEndElement(); // end profile section

            xml.Close();
        }

        private object GetObjectFromString(string val, Type type)
        {
            if (type == typeof(string))
                return val;

            try
            {
                ConstructorInfo ctor = type.GetConstructor(ctorTypes);

                if (ctor != null)
                    return ctor.Invoke(new object[] {val});
            }
            catch
            {
            }

            return Convert.ChangeType(val, type);
        }

        public object GetProperty(string name)
        {
            if (!m_Props.ContainsKey(name))
                throw new Exception(Language.Format(LocString.NoProp, name));

            return m_Props[name];
        }

        public void SetProperty(string name, object val)
        {
            if (!m_Props.ContainsKey(name))
                throw new Exception(Language.Format(LocString.NoProp, name));

            m_Props[name] = val;
        }

        public void AddProperty(string name, object val)
        {
            m_Props[name] = val;
        }
    }

    public class Config
    {
        private static Profile m_Current;
        private static Dictionary<Serial, string> m_Chars;

        public static Profile CurrentProfile
        {
            get
            {
                if (m_Current == null)
                    LoadLastProfile();

                return m_Current;
            }
        }

        public static string LastProfileName
        {
            get => GetAppSetting<string>("LastProfile");
            set => SetAppSetting("LastProfile", value);
        }

        public static void Save()
        {
            if (m_Current != null)
                m_Current.Save();
            SaveCharList();
        }

        public static bool LoadProfile(string name)
        {
            Profile p = new Profile(name);

            if (p.Load())
            {
                LastProfileName = p.Name;

                if (m_Current != null)
                    m_Current.Unload();
                m_Current = p;

                return true;
            }

            return false;
        }

        public static void NewProfile(string name)
        {
            if (m_Current != null)
                m_Current.Unload();
            m_Current = new Profile(name);
        }

        public static void LoadCharList()
        {
            if (m_Chars == null)
                m_Chars = new Dictionary<Serial, string>();
            else
                m_Chars.Clear();

            string file = Path.Combine(GetUserDirectory("Profiles"), "chars.lst");

            if (!File.Exists(file))
                return;

            using (StreamReader reader = new StreamReader(file))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length <= 0 || line[0] == ';' || line[0] == '#')
                        continue;

                    string[] split = line.Split('=');

                    try
                    {
                        m_Chars.Add(Serial.Parse(split[0]), split[1]);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void SaveCharList()
        {
            if (m_Chars == null)
                m_Chars = new Dictionary<Serial, string>();

            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(GetUserDirectory("Profiles"), "chars.lst")))
                    foreach (KeyValuePair<Serial, string> de in m_Chars)
                        writer.WriteLine("{0}={1}", de.Key, de.Value);
            }
            catch
            {
            }
        }

        public static void LoadProfileFor(PlayerData player)
        {
            if (m_Chars == null)
                m_Chars = new Dictionary<Serial, string>();

            string prof;

            if (m_Chars.TryGetValue(player.Serial, out prof) && prof != null)
            {
                if (m_Current != null && (m_Current.Name == prof || m_Current.Name.Trim() == prof.Trim()))
                    return;

                Save();

                if (!LoadProfile(prof))
                {
                    if (prof != "default")
                    {
                        if (!LoadProfile("default"))
                            m_Current.MakeDefault();
                    }
                    else
                        m_Current.MakeDefault();
                }

                Engine.MainWindow.InitConfig();
            }
            else
                m_Chars[player.Serial] = m_Current != null ? m_Current.Name : "default";

            Engine.MainWindow.SafeAction(s => s.SelectProfile(m_Current != null ? m_Current.Name : "default"));
        }

        public static void SetProfileFor(PlayerData player)
        {
            if (m_Current != null)
                m_Chars[player.Serial] = m_Current.Name;
        }

        public static bool LoadLastProfile()
        {
            string name = LastProfileName;
            bool failed = true;
            Profile p = null;

            if (name != null)
            {
                p = new Profile(name);
                failed = !p.Load();
            }

            if (failed)
            {
                if (p == null)
                    p = new Profile("default");
                else
                    p.Name = "default";

                if (!p.Load())
                {
                    p.MakeDefault();
                    p.Save();
                }

                LastProfileName = "default";
            }

            if (p != null)
            {
                if (m_Current != null)
                    m_Current.Unload();
                m_Current = p;
            }

            return !failed;
        }

        public static void SetupProfilesList(ComboBox list, string selectName)
        {
            if (list == null || list.Items == null)
                return;

            string[] files = Directory.GetFiles(GetUserDirectory("Profiles"), "*.xml");
            string compare = string.Empty;

            if (selectName != null)
                compare = selectName.ToLower();

            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);

                if (name == null || name.Length <= 0)
                    name = files[i];

                if (name == null)
                    continue;

                list.Items.Add(name);

                if (name.ToLower() == compare)
                    list.SelectedIndex = i;
            }
        }

        public static T GetAppSetting<T>(string key)
        {
            try
            {
                var appSetting = ConfigurationManager.AppSettings[key];

                if (!string.IsNullOrWhiteSpace(appSetting))
                {
                    var converter = TypeDescriptor.GetConverter(typeof(T));

                    return (T) converter.ConvertFromInvariantString(appSetting);
                }

                return default(T);
            }
            catch
            {
                return default(T);
            }
        }

        public static object GetProperty(string name)
        {
            return CurrentProfile.GetProperty(name);
        }

        public static void SetProperty(string name, object val)
        {
            CurrentProfile.SetProperty(name, val);
        }

        public static void AddProperty(string name, object val)
        {
            CurrentProfile.AddProperty(name, val);
        }

        public static bool GetBool(string name)
        {
            return (bool) CurrentProfile.GetProperty(name);
        }

        public static string GetString(string name)
        {
            return (string) CurrentProfile.GetProperty(name);
        }

        public static int GetInt(string name)
        {
            return (int) CurrentProfile.GetProperty(name);
        }

        public static bool SetAppSetting(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Remove(key);
                config.AppSettings.Settings.Add(key, value);

                config.Save(ConfigurationSaveMode.Modified, true);

                ConfigurationManager.RefreshSection("appSettings");

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void RecursiveCopy(string oldDir, string newDir, bool overWrite = false)
        {
            Engine.EnsureDirectory(newDir);

            if (!Directory.Exists(oldDir))
                return;

            string[] files = Directory.GetFiles(oldDir);

            foreach (string f in files)
                File.Copy(Path.Combine(oldDir, Path.GetFileName(f)), Path.Combine(newDir, Path.GetFileName(f)), overWrite);

            string[] dirs = Directory.GetDirectories(oldDir);

            foreach (string d in dirs)
                RecursiveCopy(Path.Combine(oldDir, Path.GetDirectoryName(d)), Path.Combine(newDir, Path.GetDirectoryName(d)), overWrite);
        }

        public static void CopyUserFiles(string appDir, string name)
        {
            RecursiveCopy(Path.Combine(GetInstallDirectory(), name), Path.Combine(appDir, name));
        }

        public static void ImportProfilesMacros(string appDataSource)
        {
            RecursiveCopy(Path.Combine(appDataSource, "Profiles"), Path.Combine(GetInstallDirectory(), "Profiles"), true);

            RecursiveCopy(Path.Combine(appDataSource, "Macros"), Path.Combine(GetInstallDirectory(), "Macros"), true);

            File.Copy(Path.Combine(appDataSource, "counters.xml"), Path.Combine(GetInstallDirectory(), "counters.xml"), true);
        }

        public static string GetUserDirectory(string name)
        {
            string appDir = GetInstallDirectory();

            if (!Directory.Exists($"{appDir}\\Macros")) Directory.CreateDirectory($"{appDir}\\Macros");

            if (!Directory.Exists($"{appDir}\\Profiles")) Directory.CreateDirectory($"{appDir}\\Profiles");

            name = name.Length > 0 ? Path.Combine(appDir, name) : appDir;

            Engine.EnsureDirectory(name);

            return name;
        }

        public static string GetUserDirectory()
        {
            return GetUserDirectory("");
        }

        public static string GetInstallDirectory(string name)
        {
            string dir = Engine.RootPath;

            if (name.Length > 0) dir = Path.Combine(dir, name);

            Engine.EnsureDirectory(dir);

            return dir;
        }

        public static string GetInstallDirectory()
        {
            return GetInstallDirectory("");
        }
    }
}