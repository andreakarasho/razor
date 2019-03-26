using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Assistant.Macros;

namespace Assistant
{
    public class Commands
    {
        private static DateTime m_LastSync;

        public static void Initialize()
        {
            Command.Register("AddUseOnce", AddUseOnce);
            Command.Register("Time", Time);
            Command.Register("Where", Where);
            Command.Register("Ping", Ping);
            Command.Register("Help", Command.ListCommands);
            Command.Register("Echo", Echo);
            Command.Register("Macro", MacroCmd);
            Command.Register("Hue", GetItemHue);
            Command.Register("Item", GetItemHue);
            Command.Register("ClearItems", ClearItems);
            Command.Register("Resync", Resync);
            Command.Register("Mobile", GetMobile);
            Command.Register("Weather", SetWeather);
            Command.Register("Season", SetSeason);
        }

        private static void Resync(string[] param)
        {
            if (DateTime.UtcNow - m_LastSync > TimeSpan.FromSeconds(1.0))
            {
                m_LastSync = DateTime.UtcNow;

                ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                    Language.CliLocName, "System", "Updating and resyncing with server"));

                ClientCommunication.SendToServer(new ResyncReq());
            }
        }

        private static void SetWeather(string[] param)
        {
            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", "Setting weather.."));

            ClientCommunication.SendToClient(new SetWeather(Convert.ToInt32(param[0]), Convert.ToInt32(param[1])));
        }

        private static void SetSeason(string[] param)
        {
            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", "Setting season.."));

            ClientCommunication.SendToClient(new SeasonChange(Convert.ToInt32(param[0]), true));
        }

        private static void GetItemHue(string[] param)
        {
            Targeting.OneTimeTarget(OnGetItemHueTarget);

            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", "Select an item to get the hue value"));
        }

        private static void OnGetItemHueTarget(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            Item item = World.FindItem(serial);

            if (item != null)
            {
                ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                    Language.CliLocName, "System", $"Item: '{item.Name}' '{item.ItemID.Value}'"));

                ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                    Language.CliLocName, "System", $"Hue: '{item.Hue}'"));
            }
        }

        private static void GetMobile(string[] param)
        {
            Targeting.OneTimeTarget(OnGetMobileTarget);

            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", "Select a mobile to get information on"));
        }

        private static void OnGetMobileTarget(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            Mobile mobile = World.FindMobile(serial);

            if (mobile != null)
            {
                ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                    Language.CliLocName, "System", $"Name: '{mobile.Name}'"));

                ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                    Language.CliLocName, "System", $"Serial: '{mobile.Serial}' Hue: '{mobile.Hue}' IsGhost: '{mobile.IsGhost}' IsHuman: '{mobile.IsHuman}' IsMonster: '{mobile.IsMonster}'"));
            }
        }


        private static void Echo(string[] param)
        {
            StringBuilder sb = new StringBuilder("Note To Self: ");

            for (int i = 0; i < param.Length; i++)
                sb.Append(param[i]);

            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", sb.ToString()));
        }

        private static void ClearItems(string[] param)
        {
            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", "Clearing all items from memory cache"));

            World.Items.Clear();
            Resync(param);

            ClientCommunication.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                                                                Language.CliLocName, "System", "All items in memory cache have been cleared"));
        }

        private static void AddUseOnce(string[] param)
        {
            string use = Language.GetString(LocString.UseOnce);

            for (int i = 0; i < Agent.List.Count; i++)
            {
                Agent a = Agent.List[i];

                if (a.Name == use)
                {
                    a.OnButtonPress(1);

                    break;
                }
            }
        }

        private static void Time(string[] param)
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.CurTime,
                                     Engine.MistedDateTime.ToString("MM/dd/yy HH:mm:ss.f"));
        }

        private static void Where(string[] param)
        {
            string mapStr;

            switch (World.Player.Map)
            {
                case 0:
                    mapStr = "Felucca";

                    break;
                case 1:
                    mapStr = "Trammel";

                    break;
                case 2:
                    mapStr = "Ilshenar";

                    break;
                case 3:
                    mapStr = "Malas";

                    break;
                case 4:
                    mapStr = "Tokuno";

                    break;
                case 0x7F:
                    mapStr = "Internal";

                    break;
                default:
                    mapStr = $"Unknown (#{World.Player.Map})";

                    break;
            }

            World.Player.SendMessage(MsgLevel.Force, LocString.CurLoc, World.Player.Position, mapStr);
        }

        private static void Ping(string[] param)
        {
            int count = 5;

            if (param.Length > 0)
                count = Utility.ToInt32(param[0], 5);

            if (count > 10)
                count = 10;

            Assistant.Ping.StartPing(count);
        }

        private static void MacroCmd(string[] param)
        {
            if (param.Length <= 0)
            {
                World.Player.SendMessage("You must enter a macro name.");

                return;
            }

            foreach (Macro m in MacroManager.List)
            {
                if (m.ToString() == param[0])
                {
                    MacroManager.HotKeyPlay(m);

                    break;
                }
            }
        }
    }

    public delegate void CommandCallback(string[] param);

    public class Command
    {
        static Command()
        {
            List = new Dictionary<string, CommandCallback>(16, StringComparer.OrdinalIgnoreCase);
            PacketHandler.RegisterClientToServerFilter(0xAD, OnSpeech);
        }

        public static Dictionary<string, CommandCallback> List { get; }

        public static void ListCommands(string[] param)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string cmd in List.Keys)
            {
                sb.Append(cmd);
                sb.Append(" ");
            }

            World.Player.SendMessage(MsgLevel.Force, LocString.CommandList);
            World.Player.SendMessage(MsgLevel.Force, sb.ToString());
        }

        public static void Register(string cmd, CommandCallback callback)
        {
            List[cmd] = callback;
        }

        public static CommandCallback FindCommand(string cmd)
        {
            return List[cmd];
        }

        public static void RemoveCommand(string cmd)
        {
            List.Remove(cmd);
        }

        public static void OnSpeech(Packet pvSrc, PacketHandlerEventArgs args)
        {
            MessageType type = (MessageType) pvSrc.ReadByte();
            ushort hue = pvSrc.ReadUInt16();
            ushort font = pvSrc.ReadUInt16();
            string lang = pvSrc.ReadString(4);
            string text = "";
            ArrayList keys = null;
            long txtOffset = 0;

            World.Player.SpeechHue = hue;

            if ((type & MessageType.Encoded) != 0)
            {
                int value = pvSrc.ReadInt16();
                int count = (value & 0xFFF0) >> 4;
                keys = new ArrayList();
                keys.Add((ushort) value);

                for (int i = 0; i < count; ++i)
                {
                    if ((i & 1) == 0)
                        keys.Add(pvSrc.ReadByte());
                    else
                    {
                        keys.Add(pvSrc.ReadByte());
                        keys.Add(pvSrc.ReadByte());
                    }
                }

                txtOffset = pvSrc.Position;
                text = pvSrc.ReadUTF8StringSafe();
                type &= ~MessageType.Encoded;
            }
            else
            {
                txtOffset = pvSrc.Position;
                text = pvSrc.ReadUnicodeStringSafe();
            }

            text = text.Trim();

            if (text.Length > 0)
            {
                if (text[0] != '-')
                    MacroManager.Action(new SpeechAction(type, hue, font, lang, keys, text));
                else
                {
                    text = text.Substring(1);
                    string[] split = text.Split(' ', '\t');

                    if (List.ContainsKey(split[0]))
                    {
                        CommandCallback call = List[split[0]];

                        if (call != null)
                        {
                            string[] param = new string[split.Length - 1];

                            for (int i = 0; i < param.Length; i++)
                                param[i] = split[i + 1];
                            call(param);

                            args.Block = true;
                        }
                    }
                    else
                        World.Player.SendMessage(MsgLevel.Force, "Unknown command");
                }
            }
        }
    }
}