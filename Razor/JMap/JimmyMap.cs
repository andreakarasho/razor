using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Ultima;
using Assistant;
using System.Collections;

namespace Assistant.JMap
{
    public partial class JimmyMap : Form
    {
        delegate void UpdateMapCallback();

        public MainForm mainForm;

        public MapPanel mapPanel;
        private TileDisplay tileDisplay;

        public MapGenProgressBar mapGenProgressBar;
        //public ProgressBar mapGenProgressBar;
        public BackgroundWorker mapGenWorker;

        public int currentGenStep = 0;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        public JimmyMap()
        {
            InitializeComponent();
            Enabled = false;
            SetTopLevel(true);

            DoubleBuffered = true;
            SetStyle(
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.OptimizedDoubleBuffer,
                    true);

            HorizontalScroll.Visible = false;
            VerticalScroll.Visible = false;

            ClientSize = new Size(400, 400);
            Size = new Size(416, 439);
            AllowTransparency = true;

            new GetPartyLocTimer().Start();

            //MaximumSize = new Size((int)(mapPanel.mapWidth - mapPanel.bgLeft), (int)(mapPanel.mapHeight - mapPanel.bgTop));

            this.MouseEnter += new EventHandler(main_MouseEnter);
            this.MouseLeave += new EventHandler(main_MouseLeave);
            this.MouseDown += new MouseEventHandler(main_MouseDown);
            this.MouseUp += new MouseEventHandler(main_MouseUp);
            this.MouseMove += new MouseEventHandler(main_MouseMove);
            this.MouseDoubleClick += new MouseEventHandler(main_DoubleClick);
            this.MouseWheel += new MouseEventHandler(main_OnMouseWheel);
            this.Closing += new CancelEventHandler(JMap_Closing);
            this.FormClosing += new FormClosingEventHandler(main_Closing_SaveAll);
            this.EnabledChanged += new EventHandler(main_Enabled);
            //FormClosing += new FormClosingEventHandler(main_Exit);

            Activate();
            
            
            Text = "UO Map";
            Size = new Size(416, 439);
            


        }

        private void main_Enabled(object sender, EventArgs e)
        {
            JimmyMap_Load();
        }

        private void JimmyMap_Load()
        {
            //tileDisplay = new TileDisplay();

            if (!File.Exists($"{Config.GetInstallDirectory()}\\JMap\\MAP0-1.BMP"))
            {
                mapGenProgressBar = new MapGenProgressBar();

                // Hack to make progress bar function.. sort of :)
                Ultima.Map map = JMap.Map.GetMap(1);
                if (map == null)
                    map = Ultima.Map.Felucca;

                int maxStep = 512;// (map.Height / 8) + (map.Width / 8);
                mapGenProgressBar.progressBar.Maximum = maxStep;
                // End hacky fix


                mapGenProgressBar.Show();
                mapGenProgressBar.BringToFront();

                GenerationWorker();
            }
            else
            {
                JimmyMap_ContinueLoad();
            }

            //Bitmap multiMap = MultiMap.GetMultiMap();
            //multiMap.Save(@"F:\UO Stuff\\UO Art\\Maps\\Generated\\Multis\\MULTI0-1.BMP", ImageFormat.Bmp);

        }

        private void JimmyMap_ContinueLoad()
        {
            try
            {
                if (mapPanel == null)
                {
                    Controls.Add
                    (
                        mapPanel = new JMap.MapPanel()
                        {
                            jMapMain = this,
                            Size = ClientRectangle.Size,
                            Parent = this,
                            BackColor = Color.Black,
                            BackgroundImageLayout = ImageLayout.Zoom,
                            Location = new Point(0, 0),
                            Name = "mapPanel",
                            TabIndex = 0,
                            tileDisplay = this.tileDisplay
                        }
                    );

                    mapPanel.mainForm = this.mainForm;
                    mapPanel.LoadMap();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"EXCEPTION: {e.ToString()}");
            }

            this.Show();
            this.BringToFront();

            ClientCommunication.SetMapWndHandle(this);

            mapPanel.UpdatePlayerPos();


            if (mapPanel.trackingPlayer)
                mapPanel.TrackPlayer();
            else
                mapPanel.UpdateAll();
        }
       
        #region MAP GEN WORKER

        public void GenerationWorker()
        {
            if (mapGenWorker != null)
                mapGenWorker.Dispose();

            mapGenWorker = new BackgroundWorker();
            mapGenWorker.DoWork += new DoWorkEventHandler(GenerationWorker_DoWork);
            mapGenWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Generation_RunWorkerCompleted);
            mapGenWorker.ProgressChanged += new ProgressChangedEventHandler(Generation_ReportProgress);
            mapGenWorker.WorkerReportsProgress = true;
            mapGenWorker.WorkerSupportsCancellation = true;
            mapGenWorker.RunWorkerAsync();

        }

        private void GenerationWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            GenerateMaps();
        }

        private void Generation_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            if(e.ProgressPercentage > currentGenStep)
            {
                mapGenProgressBar.progressBar.PerformStep();
                currentGenStep = e.ProgressPercentage;
            }
            
        }

        private void Generation_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            JimmyMap_ContinueLoad();

            mapGenProgressBar.Dispose();
        }

        #endregion

        public void GenerateMaps()
        {
            
            MapGeneration mapGen = new MapGeneration(this, mapPanel, 1, tileDisplay);
            CleanUp(mapGen, 1);
        }

        public void CleanUp(MapGeneration mapGen, int MapID)
        {
            Debug.WriteLine("Cleaning up...");

            Ultima.Map.Reload();
            mapGen.map.ResetCache();

            mapGen.map = null;
            mapGen = null;

            GC.Collect(10);
        }

        protected override void OnResize(EventArgs e)
        {
            if (mapPanel != null)
            {
                SuspendLayout();
                mapPanel.Width = ClientRectangle.Width;
                mapPanel.Height = ClientRectangle.Height;
                mapPanel.UpdateAll();
                //mapPanel.Invalidate();
                ResumeLayout();
            }

            //MaximumSize = new Size((mapPanel.bgLeft + mapPanel.mapWidth), (mapPanel.bgTop + mapPanel.mapHeight));
            base.OnResize(e);
        }

        private void RequestPartyLocations()
        {
            if (World.Player != null && PacketHandlers.Party.Count > 0)
                ClientCommunication.SendToServer(new QueryPartyLocs());
        }

        public void UpdateMap()
        {
            ClientCommunication.SetMapWndHandle(this);
            mapPanel.UpdateMap();
        }

        public void PlayerMoved()
        {
            if (mapPanel == null)
                return;

            if(!mapPanel.trackingPlayer)
                mapPanel.trackingPlayer = true;

            //SuspendLayout();

            //mapPanel.UpdateAll();
            mapPanel.UpdatePlayerPos();

            if (mapPanel.trackingPlayer)
                mapPanel.TrackPlayer();
            else
                mapPanel.UpdateAll();
            //ResumeLayout();
            //mapPanel.UpdateAll();

        }

        public void CheckLocalUpdate(Mobile mob)
        {
            if (mob.InParty)
            {

                //mapPanel.Invalidate();
                //mapPanel.Update();
                mapPanel.UpdateAll();
            }

        }

        // THIS IS FOCUS CHANGE REGARDING MOBILES AND PLAYERS - NOT THE FORM/PANEL
        private void FocusChange(object sender, System.EventArgs e)
        {
            if (sender != null)
            {
                MapMenuItem mItem = sender as MapMenuItem;

                if (mItem != null)
                {
                    Serial s = (Serial)mItem.Tag;
                    Mobile m = World.FindMobile(s);
                    mapPanel.FocusMobile = m;
                    //mapPanel.Invalidate();
                    //mapPanel.Update();
                    mapPanel.UpdatePlayerPos();
                    mapPanel.UpdateAll();

                }
            }
        }

        public void main_OnMouseWheel(object s, MouseEventArgs e)
        {
            //Debug.WriteLine("MainMouseWheel");
            if (mapPanel != null)
            {
                mapPanel.mapPanel_OnMouseWheel(s, e);
            }
        }

        public void main_MouseEnter(object sender, EventArgs e)
        {

            //Debug.WriteLine("Main Mouse Enter");

            //if (!mapPanel.Focused)
            //    mapPanel.Focus();

            //Debug.WriteLine("MapPanel Has Focus");

            if (!Focused)
            {
                Activate();
                Focus();
            }

            if (mapPanel != null)
            {
                mapPanel.mapPanel_MouseEnter(sender, e);
            }
        }

        public void main_MouseLeave(object sender, EventArgs e)
        {
            //Debug.WriteLine("MouseLeave");
            //if (Focused && TopMost)
            //{//Doesn't work as intended, inactive function
            //    SendToBack();
            //    BringToFront();
            //}

            if (mapPanel != null)
            {
                mapPanel.mapPanel_MouseLeave(sender, e);
            }
        }

        public void main_MouseDown(object sender, MouseEventArgs e)
        {
            if (mapPanel != null)
            {
                mapPanel.mapPanel_MouseDown(sender, e);
            }
        }

        public void main_MouseUp(object sender, MouseEventArgs e)
        {
            //Cursor = Cursors.Default;
            if (mapPanel != null)
            {
                mapPanel.mapPanel_MouseUp(sender, e);
            }
        }

        public void main_MouseMove(object sender, MouseEventArgs e)
        {
            if (mapPanel != null)
            {
                mapPanel.mapPanel_MouseMove(sender, e);
            }
        }

        public void main_DoubleClick(object sender, MouseEventArgs e)
        {
            if (mapPanel != null)
            {
                mapPanel.mapPanel_DoubleClick(sender, e);
            }
        }

        private void main_Exit(object sender, FormClosingEventArgs e)
        {
            //ClientCommunication.SetMapWndHandle(null);
            Close();
        }

        private void JMap_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Assistant.Engine.Running)
            {
                e.Cancel = true;
                Hide();
                Engine.MainWindow.BringToFront();
                ClientCommunication.BringToFront(ClientCommunication.UOWindow);
            }
        }

        private void main_Closing_SaveAll(object sender, FormClosingEventArgs e)
        {
            if (mapPanel != null)
            {
                mapPanel.WriteUpdatedCSV();
            }
        }

        // MAP BORDER ZONES
        public RectangleF borderTop { get { return new RectangleF(ClientRectangle.Left, ClientRectangle.Top, ClientRectangle.Right, 4); } }
        public RectangleF borderBottom { get { return new RectangleF(ClientRectangle.Left, ClientRectangle.Bottom - 4, ClientRectangle.Right, 4); } }
        public RectangleF borderLeft { get { return new RectangleF(ClientRectangle.Left, ClientRectangle.Top + 4, 5, ClientRectangle.Bottom); } }
        public RectangleF borderRight { get { return new RectangleF(ClientRectangle.Right - 5, ClientRectangle.Top + 4, 5, ClientRectangle.Bottom); } }
        public RectangleF borderTopLeft { get { return new RectangleF(0, 0, 5, 4); } }
        public RectangleF borderTopRight { get { return new RectangleF(ClientRectangle.Right - 5, 0, 5, 4); } }
        public RectangleF borderBottomLeft { get { return new RectangleF(0, ClientRectangle.Bottom - 4, 5, 4); } }
        public RectangleF borderBottomRight { get { return new RectangleF(ClientRectangle.Right - 5, ClientRectangle.Bottom - 4, 5, 4); } }

        private const int //WM CODES FOR CUSTOM RESIZE
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTTRANSPARENT = -1;

        protected override void WndProc(ref Message message) //CUSTOM RESIZE
        {
            base.WndProc(ref message);

            if (message.Msg == 0x84) // WM_NCHITTEST
            {
                var cursor = PointToClient(Cursor.Position);

                if (borderTopLeft.Contains(cursor)) message.Result = (IntPtr)HTTOPLEFT;
                else if (borderTopRight.Contains(cursor)) message.Result = (IntPtr)HTTOPRIGHT;
                else if (borderBottomLeft.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMLEFT;
                else if (borderBottomRight.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMRIGHT;

                else if (borderTop.Contains(cursor))message.Result = (IntPtr)HTTOP;
                else if (borderLeft.Contains(cursor)) message.Result = (IntPtr)HTLEFT;
                else if (borderRight.Contains(cursor)) message.Result = (IntPtr)HTRIGHT;
                else if (borderBottom.Contains(cursor)) message.Result = (IntPtr)HTBOTTOM;
            } 
        }

        public void ResizeRelay(ref Message message) //RELAY RESIZE MESSAGE TO PROTECTED OVERRIDE
        {
            WndProc(ref message);
        }

       /*protected override void WndProc(ref Message message) //CUSTOM RESIZE
       {
            base.WndProc(ref message);
       }*/

        public class MapMenuItem : MenuItem
        {
            public MapMenuItem(System.String text, System.EventHandler onClick) : base(text, onClick)
            {
                Tag = null;
            }
        }

        private class GetPartyLocTimer : Timer
        {
            public GetPartyLocTimer() : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
            }

            protected override void OnTick()
            {
                // never send this packet to encrypted servers (could lead to OSI detecting razor)
                if (ClientCommunication.ServerEncrypted)
                {
                    Stop();
                    return;
                }

                if (Engine.MainWindow == null || Engine.MainWindow.JMap == null || !Engine.MainWindow.JMap.Visible)
                    return; // don't bother when the map window isnt visible

                if (World.Player != null && PacketHandlers.Party.Count > 0)
                {
                    if (PacketHandlers.SpecialPartySent > PacketHandlers.SpecialPartyReceived)
                    {
                        // If we sent more than we received then the server stopped responding
                        // in that case, wait a long while before trying again
                        PacketHandlers.SpecialPartySent = PacketHandlers.SpecialPartyReceived = 0;
                        this.Interval = TimeSpan.FromSeconds(5.0);
                        return;
                    }
                    else
                    {
                        this.Interval = TimeSpan.FromSeconds(1.0);
                    }

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
                {
                    this.Interval = TimeSpan.FromSeconds(1.0);
                }
            }
        }
    }

 

}
