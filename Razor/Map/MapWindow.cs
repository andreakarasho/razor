using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Assistant.UI;

namespace Assistant.MapUO
{
    /// <summary>
    ///     Summary description for MapWindow.
    /// </summary>
    public class MapWindow : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private static Font m_RegFont = new Font("Courier New", 8);

        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private readonly Container components = null;
        private UOMapControl Map;

        public MapWindow()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            ContextMenu = new ContextMenu();
            ContextMenu.Popup += ContextMenu_Popup;
            Location = new Point(Config.GetInt("MapX"), Config.GetInt("MapY"));
            ClientSize = new Size(Config.GetInt("MapW"), Config.GetInt("MapH"));

            if (Location.X < -10 || Location.Y < -10)
                Location = Point.Empty;

            if (Width < 50)
                Width = 50;

            if (Height < 50)
                Height = 50;

            //
            // TODO: Add any constructor code after InitializeComponent call
            //

            Map.FullUpdate();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        private void ContextMenu_Popup(object sender, EventArgs e)
        {
            ContextMenu cm = ContextMenu;
            cm.MenuItems.Clear();

            if (World.Player != null && PacketHandlers.Party.Count > 0)
            {
                MapMenuItem mi = new MapMenuItem("You", FocusChange);
                mi.Tag = World.Player.Serial;
                cm.MenuItems.Add(mi);

                foreach (Serial s in PacketHandlers.Party)
                {
                    Mobile m = World.FindMobile(s);

                    if (m.Name != null)
                    {
                        mi = new MapMenuItem(m.Name, FocusChange);
                        mi.Tag = s;

                        if (Map.FocusMobile == m)
                            mi.Checked = true;
                        cm.MenuItems.Add(mi);
                    }
                }
            }

            ContextMenu = cm;
        }


        private void FocusChange(object sender, EventArgs e)
        {
            if (sender != null)
            {
                MapMenuItem mItem = sender as MapMenuItem;

                if (mItem != null)
                {
                    Serial s = (Serial) mItem.Tag;
                    Mobile m = World.FindMobile(s);
                    Map.FocusMobile = m;
                    Map.FullUpdate();
                }
            }
        }

        public static void Initialize()
        {
            new ReqPartyLocTimer().Start();

            HotKey.Add(HKCategory.Misc, LocString.ToggleMap, ToggleMap);
        }

        public static void ToggleMap()
        {
            if (World.Player != null && Engine.MainWindow != null)
            {
                if (Engine.MainWindow.MapWindow == null)
                {
                    Engine.MainWindow.SafeAction(s =>
                    {
                        s.MapWindow = new MapWindow();
                        s.MapWindow.Show();
                        s.MapWindow.BringToFront();
                    });
                }
                else
                {
                    if (Engine.MainWindow.MapWindow.Visible)
                    {
                        Engine.MainWindow.SafeAction(s =>
                        {
                            s.MapWindow.Hide();
                            s.BringToFront();
                        });
                        Windows.BringToFront(ClientCommunication.ClientWindow);
                    }
                    else
                    {
                        Engine.MainWindow.MapWindow.Show();
                        Engine.MainWindow.MapWindow.BringToFront();
                        Engine.MainWindow.MapWindow.TopMost = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                if (components != null)
                    components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MapWindow));
            this.Map = new Assistant.MapUO.UOMapControl();
            this.SuspendLayout();
            // 
            // Map
            // 
            this.Map.Active = true;
            this.Map.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Map.FocusMobile = null;
            this.Map.Location = new System.Drawing.Point(0, 0);
            this.Map.Name = "Map";
            this.Map.Size = new System.Drawing.Size(296, 272);
            this.Map.TabIndex = 0;
            this.Map.TabStop = false;
            this.Map.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Map_MouseDown);
            // 
            // MapWindow
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.Map);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon) (resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MapWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "UO Positioning System";
            this.TopMost = true;
            this.Resize += new System.EventHandler(this.MapWindow_Resize);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MapWindow_MouseDown);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MapWindow_Closing);
            this.Move += new System.EventHandler(this.MapWindow_Move);
            this.Deactivate += new System.EventHandler(this.MapWindow_Deactivate);
            this.ResumeLayout(false);
        }

        #endregion

        public void CheckLocalUpdate(Mobile mob)
        {
            if (mob.InParty)
                Map.FullUpdate();
        }

        private void RequestPartyLocations()
        {
            if (World.Player != null && PacketHandlers.Party.Count > 0)
                ClientCommunication.SendToServer(new QueryPartyLocs());
        }

        public void UpdateMap()
        {
            Map.UpdateMap();
        }

        private void MapWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Engine.Running)
            {
                e.Cancel = true;
                Hide();
                Engine.MainWindow.BringToFront();
                Windows.BringToFront(ClientCommunication.ClientWindow);
            }
        }

        public void PlayerMoved()
        {
            Console.WriteLine("Map window player update");

            if (Visible && Map != null)
                Map.FullUpdate();
        }

        private void MapWindow_Resize(object sender, EventArgs e)
        {
            Map.Height = Height;
            Map.Width = Width;

            if (Width < 50)
                Width = 50;

            if (Height < 50)
                Height = 50;

            Refresh();

            Config.SetProperty("MapX", Location.X);
            Config.SetProperty("MapY", Location.Y);
            Config.SetProperty("MapW", ClientSize.Width);
            Config.SetProperty("MapH", ClientSize.Height);
        }

        private void MapWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Clicks == 2)
            {
                if (FormBorderStyle == FormBorderStyle.None)
                    FormBorderStyle = FormBorderStyle.SizableToolWindow;
                else
                    FormBorderStyle = FormBorderStyle.None;
            }

            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr) HT_CAPTION, IntPtr.Zero);
                /*foreach ( Serial s in PacketHandlers.Party )
                {       
                Mobile m = World.FindMobile( s );
                if ( m == null )
                    continue;
                Rectangle rec = new Rectangle( m.ButtonPoint.X, m.ButtonPoint.Y, 75, 15 );
                if ( rec.Contains( e.X, e.Y ) )
                {
                    this.Map.FocusMobile = m;
                    this.Map.Refresh();
                }
                }*/
            }

            Map.MapClick(e);
        }

        private void Map_MouseDown(object sender, MouseEventArgs e)
        {
            MapWindow_MouseDown(sender, e);
        }

        private void MapWindow_Move(object sender, EventArgs e)
        {
            Config.SetProperty("MapX", Location.X);
            Config.SetProperty("MapY", Location.Y);
            Config.SetProperty("MapW", ClientSize.Width);
            Config.SetProperty("MapH", ClientSize.Height);
        }

        private void MapWindow_Deactivate(object sender, EventArgs e)
        {
            if (TopMost)
                TopMost = false;
        }

        public class MapMenuItem : MenuItem
        {
            public MapMenuItem(string text, EventHandler onClick) : base(text, onClick)
            {
                Tag = null;
            }
        }
        /*private   int ButtonRows;
        protected override void OnPaint(PaintEventArgs e)
        {
        base.OnPaint(e);
        if ( PacketHandlers.Party.Count > 0 )
        {
        //75x15
        int xcount = 0;
        int ycount = 0;
        Point org = new Point(0, (ButtonRows * 15));
        if (this.FormBorderStyle == FormBorderStyle.None)
        {
        org = new Point(0,  (ButtonRows * 15) + 32);
        }

        foreach ( Serial s in PacketHandlers.Party )
        {
        Mobile mob = World.FindMobile( s );
        if ( mob == null )
            continue;

        if (((75 * (xcount+1)) - this.Width) > 0)
        {
            xcount = 0;
            ycount++;
        }
        string name = mob.Name;
        if ( name != null && name.Length > 8)
        {
            name = name.Substring(0, 8);
            name += "...";
        }
        else if ( name == null || name.Length < 1 )
        {
            name = "(Not Seen)";
        }

        Point drawPoint = new Point(org.X + (75 * xcount), org.Y + (15*ycount));
        mob.ButtonPoint = new Point2D( drawPoint.X, drawPoint.Y );
        e.Graphics.FillRectangle( Brushes.Black, drawPoint.X, drawPoint.Y, 75, 15 );
        e.Graphics.DrawRectangle(Pens.Gray, drawPoint.X, drawPoint.Y, 75, 15 );
        e.Graphics.DrawString(name, m_RegFont, Brushes.White, drawPoint);
        xcount++;
        }
        if(ycount > 0)
        ButtonRows = ycount;
        }

        }*/

        private class ReqPartyLocTimer : Timer
        {
            public ReqPartyLocTimer() : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
            }

            protected override void OnTick()
            {
                if (Engine.MainWindow == null || Engine.MainWindow.MapWindow == null || !Engine.MainWindow.MapWindow.Visible)
                    return; // don't bother when the map window isnt visible

                if (World.Player != null && PacketHandlers.Party.Count > 0)
                {
                    if (PacketHandlers.SpecialPartySent > PacketHandlers.SpecialPartyReceived)
                    {
                        // If we sent more than we received then the server stopped responding
                        // in that case, wait a long while before trying again
                        PacketHandlers.SpecialPartySent = PacketHandlers.SpecialPartyReceived = 0;
                        Interval = TimeSpan.FromSeconds(5.0);

                        return;
                    }

                    Interval = TimeSpan.FromSeconds(1.0);

                    bool send = false;

                    foreach (Serial s in PacketHandlers.Party)
                    {
                        Mobile m = World.FindMobile(s);

                        if (m == World.Player)
                            continue;

                        if (m == null || Utility.Distance(World.Player.Position, m.Position) > World.Player.VisRange || !m.Visible)
                        {
                            send = true;

                            break;
                        }
                    }

                    if (send)
                    {
                        PacketHandlers.SpecialPartySent++;
                        ClientCommunication.SendToServer(new QueryPartyLocs());
                    }
                }
                else
                    Interval = TimeSpan.FromSeconds(1.0);
            }
        }
    }
}