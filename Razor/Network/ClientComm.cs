using System;
using System.Net;
using System.Runtime.InteropServices;

using Assistant.Macros;
using Assistant.UI;

using CUO_API;

namespace Assistant
{
    public sealed unsafe class ClientCommunication
    {
        private static OnPacketSendRecv _sendToClient, _sendToServer, _recv, _send;
        private static OnGetPacketLength _getPacketLength;
        private static OnGetPlayerPosition _getPlayerPosition;
        private static OnCastSpell _castSpell;
        private static OnGetStaticImage _getStaticImage;

        private static OnHotkey _onHotkeyPressed;
        private static OnMouse _onMouse;
        private static OnUpdatePlayerPosition _onUpdatePlayerPosition;
        private static OnClientClose _onClientClose;
        private static OnInitialize _onInitialize;
        private static OnConnected _onConnected;
        private static OnDisconnected _onDisconnected;
        private static OnFocusGained _onFocusGained;
        private static OnFocusLost _onFocusLost;
        public static DateTime ConnectionStart { get; private set; }
        public static IPAddress LastConnection { get; }

        public static IntPtr ClientWindow { get; private set; } = IntPtr.Zero;

        internal static bool InstallHooks(ref PluginHeader* header)
        {
            _sendToClient = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header->Recv);
            _sendToServer = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header->Send);
            _getPacketLength = Marshal.GetDelegateForFunctionPointer<OnGetPacketLength>(header->GetPacketLength);
            _getPlayerPosition = Marshal.GetDelegateForFunctionPointer<OnGetPlayerPosition>(header->GetPlayerPosition);
            _castSpell = Marshal.GetDelegateForFunctionPointer<OnCastSpell>(header->CastSpell);
            _getStaticImage = Marshal.GetDelegateForFunctionPointer<OnGetStaticImage>(header->GetStaticImage);

            ClientWindow = header->HWND;

            _recv = OnRecv;
            _send = OnSend;
            _onHotkeyPressed = OnHotKeyHandler;
            _onMouse = OnMouseHandler;
            _onUpdatePlayerPosition = OnPlayerPositionChanged;
            _onClientClose = OnClientClosing;
            _onInitialize = OnInitialize;
            _onConnected = OnConnected;
            _onDisconnected = OnDisconnected;
            _onFocusGained = OnFocusGained;
            _onFocusLost = OnFocusLost;

            header->OnRecv = Marshal.GetFunctionPointerForDelegate(_recv);
            header->OnSend = Marshal.GetFunctionPointerForDelegate(_send);
            header->OnHotkeyPressed = Marshal.GetFunctionPointerForDelegate(_onHotkeyPressed);
            header->OnMouse = Marshal.GetFunctionPointerForDelegate(_onMouse);
            header->OnPlayerPositionChanged = Marshal.GetFunctionPointerForDelegate(_onUpdatePlayerPosition);
            header->OnClientClosing = Marshal.GetFunctionPointerForDelegate(_onClientClose);
            header->OnInitialize = Marshal.GetFunctionPointerForDelegate(_onInitialize);
            header->OnConnected = Marshal.GetFunctionPointerForDelegate(_onConnected);
            header->OnDisconnected = Marshal.GetFunctionPointerForDelegate(_onDisconnected);
            header->OnFocusGained = Marshal.GetFunctionPointerForDelegate(_onFocusGained);
            header->OnFocusLost = Marshal.GetFunctionPointerForDelegate(_onFocusLost);

            return true;
        }

        private static void OnClientClosing()
        {
            var last = Console.BackgroundColor;
            var lastFore = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Closing Razor instance");
            Console.BackgroundColor = last;
            Console.ForegroundColor = lastFore;

            Windows.FreeTitleBar();
            UOAssist.DestroyWindow();
        }

        private static void OnInitialize()
        {
            var last = Console.BackgroundColor;
            var lastFore = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Initialized Razor instance");
            Console.BackgroundColor = last;
            Console.ForegroundColor = lastFore;
        }

        private static void OnConnected()
        {
            ConnectionStart = DateTime.Now;

            try
            {
                //m_LastConnection = new IPAddress((uint)lParam);
            }
            catch
            {
            }
        }

        private static void OnDisconnected()
        {
            PacketHandlers.Party.Clear();

            Windows.SetTitleStr("");
            Engine.MainWindow.UpdateTitle();
            UOAssist.PostLogout();

            World.Player = null;
            World.Items.Clear();
            World.Mobiles.Clear();
            MacroManager.Stop();
            ActionQueue.Stop();
            Counter.Reset();
            GoldPerHourTimer.Stop();
            BandageTimer.Stop();
            GateTimer.Stop();
            BuffsTimer.Stop();
            StealthSteps.Unhide();
            Engine.MainWindow.OnLogout();

            if (Engine.MainWindow.MapWindow != null)
                Engine.MainWindow.MapWindow.Close();
            PacketHandlers.Party.Clear();
            PacketHandlers.IgnoreGumps.Clear();
            Config.Save();
        }

        private static void OnFocusGained()
        {
            var razor = Engine.MainWindow;

            if (razor == null)
                return;

            if (Config.GetBool("AlwaysOnTop"))
            {
                if (!razor.TopMost)
                {
                    razor.SafeAction(s =>
                    {
                        s.TopMost = true;
                        Windows.SetForegroundWindow(ClientWindow);
                    });
                }
            }

            // always use smartness for the map window
            if (razor.MapWindow != null && razor.MapWindow.Visible)
            {
                if (!razor.MapWindow.TopMost)
                {
                    razor.SafeAction(s =>
                    {
                        razor.MapWindow.TopMost = true;
                        Windows.SetForegroundWindow(ClientWindow);
                    });
                }
            }
        }

        private static void OnFocusLost()
        {
            var razor = Engine.MainWindow;

            if (razor == null)
                return;

            if (Config.GetBool("AlwaysOnTop"))
            {
                IntPtr ptr = Windows.GetForegroundWindow();

                if (ptr != ClientWindow && ptr != (IntPtr) Engine.MainWindow.Invoke(new Func<IntPtr>(() => Engine.MainWindow.Handle)))
                {
                    if (razor.TopMost)
                    {
                        razor.SafeAction(s =>
                        {
                            s.TopMost = false;
                            s.SendToBack();
                        });
                    }
                }
            }

            // always use smartness for the map window
            if (razor.MapWindow != null && razor.MapWindow.Visible)
            {
                if (razor.MapWindow.TopMost)
                {
                    razor.SafeAction(s =>
                    {
                        s.MapWindow.TopMost = false;
                        s.MapWindow.SendToBack();
                    });
                }
            }
        }

        private static bool OnHotKeyHandler(int key, int mod, bool ispressed)
        {
            if (ispressed)
            {
                bool code = HotKey.OnKeyDown(key | mod);

                return code;
            }

            return true;
        }

        private static void OnMouseHandler(int button, int wheel)
        {
            if (button > 4)
                button = 3;
            else if (button > 3)
                button = 2;
            else if (button > 2)
                button = 2;
            else if (button > 1)
                button = 1;

            HotKey.OnMouse(button, wheel);
        }

        private static void OnPlayerPositionChanged(int x, int y, int z)
        {
            Console.WriteLine("OnPlayerPositionChange");
            World.Player.Position = new Point3D(x, y, z);
        }

        private static bool OnRecv(byte[] data, int length)
        {
            fixed (byte* ptr = data)
            {
                PacketReader p = new PacketReader(ptr, length, PacketsTable.GetPacketLength(data[0]) < 0);
                Packet packet = new Packet(data, length, p.DynamicLength);

                return !PacketHandler.OnServerPacket(p.PacketID, p, packet);
            }
        }

        private static bool OnSend(byte[] data, int length)
        {
            fixed (byte* ptr = data)
            {
                PacketReader p = new PacketReader(ptr, length, PacketsTable.GetPacketLength(data[0]) < 0);
                Packet packet = new Packet(data, length, p.DynamicLength);

                return !PacketHandler.OnClientPacket(p.PacketID, p, packet);
            }
        }

        public static void CastSpell(int idx)
        {
            _castSpell(idx);
        }

        public static bool GetPlayerPosition(out int x, out int y, out int z)
        {
            return _getPlayerPosition(out x, out y, out z);
        }

        internal static void SendToServer(Packet p)
        {
            var len = p.Length;
            _sendToServer(p.Compile(), (int) len);
        }

        internal static void SendToClient(Packet p)
        {
            var len = p.Length;
            _sendToClient(p.Compile(), (int) len);
        }
    }
}