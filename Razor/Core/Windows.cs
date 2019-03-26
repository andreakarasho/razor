using System;
using System.Runtime.InteropServices;
using System.Text;

using Assistant.Core;
using Assistant.UI;

namespace Assistant
{
    public class FeatureBit
    {
        public static readonly uint WeatherFilter = 0;
        public static readonly uint LightFilter = 1;
        public static readonly uint SmartLT = 2;
        public static readonly uint RangeCheckLT = 3;
        public static readonly uint AutoOpenDoors = 4;
        public static readonly uint UnequipBeforeCast = 5;
        public static readonly uint AutoPotionEquip = 6;
        public static readonly uint BlockHealPoisoned = 7;
        public static readonly uint LoopingMacros = 8; // includes fors and macros running macros
        public static readonly uint UseOnceAgent = 9;
        public static readonly uint RestockAgent = 10;
        public static readonly uint SellAgent = 11;
        public static readonly uint BuyAgent = 12;
        public static readonly uint PotionHotkeys = 13;
        public static readonly uint RandomTargets = 14;
        public static readonly uint ClosestTargets = 15;
        public static readonly uint OverheadHealth = 16;

        public static readonly uint MaxBit = 16;
    }

    public static unsafe class Windows
    {
        private static Timer m_TBTimer;
        private static string m_LastStr = "";
        private static readonly StringBuilder m_TBBuilder = new StringBuilder();
        private static string m_LastPlayerName = "";

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        internal static extern ushort GlobalAddAtom(string str);

        [DllImport("kernel32.dll")]
        internal static extern ushort GlobalDeleteAtom(ushort atom);

        [DllImport("kernel32.dll")]
        internal static extern uint GlobalGetAtomName(ushort atom, StringBuilder buff, int bufLen);

        [DllImport("Advapi32.dll")]
        private static extern int GetUserNameA(StringBuilder buff, int* len);

        [DllImport("WinUtil.dll")]
        internal static extern IntPtr CaptureScreen(IntPtr handle, bool isFullScreen, string msgStr);

        [DllImport("WinUtil.dll")]
        internal static extern void BringToFront(IntPtr hWnd);

        [DllImport("WinUtil.dll")]
        internal static extern int HandleNegotiate(ulong word);

        [DllImport("WinUtil.dll")]
        internal static extern bool AllowBit(ulong bit);

        [DllImport("WinUtil.dll")]
        internal static extern void InitTitleBar(string path);

        [DllImport("WinUtil.dll")]
        internal static extern void DrawTitleBar(IntPtr handle, string path);

        [DllImport("WinUtil.dll")]
        internal static extern void FreeTitleBar();

        [DllImport("WinUtil.dll")]
        internal static extern void CreateUOAWindow(IntPtr razorWindow);

        [DllImport("WinUtil.dll")]
        internal static extern void DestroyUOAWindow();

        public static string GetWindowsUserName()
        {
            int len = 1024;
            StringBuilder sb = new StringBuilder(len);

            if (GetUserNameA(sb, &len) != 0)
                return sb.ToString();

            return "";
        }

        public static string EncodeColorStat(int val, int max)
        {
            double perc = val / (double) max;

            if (perc <= 0.25)
                return string.Format("~#FF0000{0}~#~", val);

            if (perc <= 0.75)
                return string.Format("~#FFFF00{0}~#~", val);

            return val.ToString();
        }

        public static void RequestTitleBarUpdate()
        {
            // throttle updates, since things like counters might request 1000000 million updates/sec
            if (m_TBTimer == null)
                m_TBTimer = new TitleBarThrottle();

            if (!m_TBTimer.Running)
                m_TBTimer.Start();
        }

        private static void UpdateTitleBar()
        {
            if (ClientCommunication.ClientWindow == IntPtr.Zero)
                return;

            if (World.Player != null && Config.GetBool("TitleBarDisplay"))
            {
                // reuse the same sb each time for less damn allocations
                m_TBBuilder.Remove(0, m_TBBuilder.Length);
                m_TBBuilder.Insert(0, Config.GetString("TitleBarText"));
                StringBuilder sb = m_TBBuilder;
                //StringBuilder sb = new StringBuilder( Config.GetString( "TitleBarText" ) ); // m_TitleCapacity

                PlayerData p = World.Player;

                if (p.Name != m_LastPlayerName)
                {
                    m_LastPlayerName = p.Name;

                    Engine.MainWindow.SafeAction(s => s.UpdateTitle());
                }

                sb.Replace(@"{char}",
                           Config.GetBool("ShowNotoHue") ? $"~#{p.GetNotorietyColor() & 0x00FFFFFF:X6}{p.Name}~#~" : p.Name);

                sb.Replace(@"{shard}", World.ShardName);

                sb.Replace(@"{crimtime}", p.CriminalTime != 0 ? $"~^C0C0C0{p.CriminalTime}~#~" : "-");

                sb.Replace(@"{str}", p.Str.ToString());
                sb.Replace(@"{hpmax}", p.HitsMax.ToString());

                sb.Replace(@"{hp}", p.Poisoned ? $"~#FF8000{p.Hits}~#~" : EncodeColorStat(p.Hits, p.HitsMax));

                sb.Replace(@"{dex}", World.Player.Dex.ToString());
                sb.Replace(@"{stammax}", World.Player.StamMax.ToString());
                sb.Replace(@"{stam}", EncodeColorStat(p.Stam, p.StamMax));
                sb.Replace(@"{int}", World.Player.Int.ToString());
                sb.Replace(@"{manamax}", World.Player.ManaMax.ToString());
                sb.Replace(@"{mana}", EncodeColorStat(p.Mana, p.ManaMax));

                sb.Replace(@"{ar}", p.AR.ToString());
                sb.Replace(@"{tithe}", p.Tithe.ToString());

                sb.Replace(@"{physresist}", p.AR.ToString());
                sb.Replace(@"{fireresist}", p.FireResistance.ToString());
                sb.Replace(@"{coldresist}", p.ColdResistance.ToString());
                sb.Replace(@"{poisonresist}", p.PoisonResistance.ToString());
                sb.Replace(@"{energyresist}", p.EnergyResistance.ToString());

                sb.Replace(@"{luck}", p.Luck.ToString());

                sb.Replace(@"{damage}", string.Format("{0}-{1}", p.DamageMin, p.DamageMax));

                sb.Replace(@"{weight}",
                           World.Player.Weight >= World.Player.MaxWeight
                               ? $"~#FF0000{World.Player.Weight}~#~"
                               : World.Player.Weight.ToString());

                sb.Replace(@"{maxweight}", World.Player.MaxWeight.ToString());

                sb.Replace(@"{followers}", World.Player.Followers.ToString());
                sb.Replace(@"{followersmax}", World.Player.FollowersMax.ToString());

                sb.Replace(@"{gold}", World.Player.Gold.ToString());

                sb.Replace(@"{gps}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldPerSecond:N2}" : "-");
                sb.Replace(@"{gpm}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldPerMinute:N2}" : "-");
                sb.Replace(@"{gph}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldPerHour:N2}" : "-");
                sb.Replace(@"{goldtotal}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldSinceStart}" : "-");
                sb.Replace(@"{goldtotalmin}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.TotalMinutes:N2} min" : "-");

                sb.Replace(@"{bandage}", BandageTimer.Running ? $"~#FF8000{BandageTimer.Count}~#~" : "-");

                sb.Replace(@"{skill}", SkillTimer.Running ? $"{SkillTimer.Count}" : "-");
                sb.Replace(@"{gate}", GateTimer.Running ? $"{GateTimer.Count}" : "-");

                sb.Replace(@"{stealthsteps}", StealthSteps.Counting ? StealthSteps.Count.ToString() : "-");
                //ClientCommunication.ConnectionStart != DateTime.MinValue )
                //time = (int)((DateTime.UtcNow - ClientCommunication.ConnectionStart).TotalSeconds);
                sb.Replace(@"{uptime}", ClientCommunication.ConnectionStart != DateTime.MinValue ? Utility.FormatTime((int) (DateTime.UtcNow - ClientCommunication.ConnectionStart).TotalSeconds) : "-");

                string buffList = string.Empty;

                if (BuffsTimer.Running)
                {
                    StringBuilder buffs = new StringBuilder();

                    foreach (BuffsDebuffs buff in World.Player.BuffsDebuffs)
                    {
                        int timeLeft = 0;

                        if (buff.Duration > 0)
                        {
                            TimeSpan diff = DateTime.UtcNow - buff.Timestamp;
                            timeLeft = buff.Duration - (int) diff.TotalSeconds;
                        }

                        buffs.Append(timeLeft <= 0
                                         ? $"{buff.ClilocMessage1}, "
                                         : $"{buff.ClilocMessage1} ({timeLeft}), ");
                    }

                    buffs.Length = buffs.Length - 2;
                    buffList = buffs.ToString();
                    sb.Replace(@"{buffsdebuffs}", buffList);
                }
                else
                {
                    sb.Replace(@"{buffsdebuffs}", "-");
                    buffList = string.Empty;
                }

                string statStr = string.Format("{0}{1:X2}{2:X2}{3:X2}",
                                               (int) p.GetStatusCode(),
                                               (int) (World.Player.HitsMax == 0 ? 0 : (double) World.Player.Hits / World.Player.HitsMax * 99),
                                               (int) (World.Player.ManaMax == 0 ? 0 : (double) World.Player.Mana / World.Player.ManaMax * 99),
                                               (int) (World.Player.StamMax == 0 ? 0 : (double) World.Player.Stam / World.Player.StamMax * 99));

                sb.Replace(@"{statbar}", $"~SR{statStr}");
                sb.Replace(@"{mediumstatbar}", $"~SL{statStr}");
                sb.Replace(@"{largestatbar}", $"~SX{statStr}");

                bool dispImg = Config.GetBool("TitlebarImages");

                for (int i = 0; i < Counter.List.Count; i++)
                {
                    Counter c = Counter.List[i];

                    if (c.Enabled)
                        sb.Replace($"{{{c.Format}}}", c.GetTitlebarString(dispImg && c.DisplayImage));
                }

                SetTitleStr(sb.ToString());
            }
            else
                SetTitleStr("");
        }

        public static void SetTitleStr(string str)
        {
            if (m_LastStr == str)
                return;

            m_LastStr = str;

            DrawTitleBar(ClientCommunication.ClientWindow, str);
        }

        private class TitleBarThrottle : Timer
        {
            public TitleBarThrottle() : base(TimeSpan.FromSeconds(0.25))
            {
            }

            protected override void OnTick()
            {
                UpdateTitleBar();
            }
        }
    }
}