using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Ultima;

namespace Assistant.JMap
{
    public class MapPanel : Panel
    {
        public MainForm mainForm;
        delegate void UpdateMapCallback();

        public delegate void InvokeDelegate();

        public JimmyMap jMapMain;

        public TileDisplay tileDisplay;
        public Tile[] lTiles = new Tile[64];
        public HuedTile[][][] sTiles = new HuedTile[64][][];
        public Ultima.Map map;

        private Point mouseDown;

        private bool m_Active;
        private MapRegion[] m_Regions;
        private Mobile m_Focus;

        public SizeF zoom;

        #region MAPS

        public bool drawTiles = true;
        public bool tilesDrawing = false;
        public Bitmap TileMap;

        public Bitmap _mapRegular_1;
        public Bitmap _mapRegular_2;
        public Bitmap _mapRegular_4;
        public Bitmap _mapRegular_8;

        public Image _mapRotated_1;
        public Bitmap _mapRotated_2;
        public Bitmap _mapRotated_4;
        public Bitmap _mapRotated_8;

        public Size _mapSize;

        public PointF bgReg;
        public PointF bgRot;
        public PointF offset = new PointF(1, 1);

        public bool mapRotated = false;
        public PointF zeroPoint;

        public Point mouseLastPos;
        public PointF mouseLastPosOnMap;
        public Point mousePosNow;
        public bool tiltChanged = false;

        public float mapWidth;
        public float mapHeight;

        public RectangleF renderingBounds;

        bool IsPanning = false;
        bool mouseOnMap = false;
        bool IsZooming = false;

        StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        // UO Styled Art
        static Bitmap borderH = new Bitmap($"{Config.GetInstallDirectory()}\\JMap\\Resources\\Art\\borderH.bmp");
        static Bitmap borderV = new Bitmap($"{Config.GetInstallDirectory()}\\JMap\\Resources\\Art\\borderV.bmp");



        ArrayList markedLocations = new ArrayList();

        public List<JMapButton> mapButtons = new List<JMapButton>();
        ArrayList bufferingMapButtons = new ArrayList();
        ArrayList visibleMapButtons = new ArrayList();

        public ArrayList hoverRegions = new ArrayList();

        public bool IsUpdatingMarkers;
        public BackgroundWorker mapMarkerWorker;
        public BackgroundWorker mouseHoverWorker;

        //public bool IsUpdatingGrid;
        public BackgroundWorker gridWorker;
        public ArrayList bufferingGrid = new ArrayList();
        public ArrayList visibleGrid = new ArrayList();
        public ArrayList gridPoints = new ArrayList();
        public bool GridBuilt = false;

        private int GridOpacity
        {
            get { return _GridOpacity; }
            set { _GridOpacity = (int)(value * 255) / 100; }
        }

        public int _GridOpacity;

        public bool IsMouseDown;

        public enum MarkerWorkerStates
        {
            UpdatingMarkers,
            UpdatingVisible,
            UpdateFinished,
            DrawingMarkers,
            IdleNoWork
        }
        public MarkerWorkerStates markerWorkerState = MarkerWorkerStates.IdleNoWork;

        public bool AddingMarker;
        public bool EditingMarker;
        public JMapButton MarkerToEdit;

        public enum GridWorkerStates
        {
            UpdatingGrid,
            UpdatingVisible,
            IdleNoWork
        }
        public GridWorkerStates gridWorkerStates = GridWorkerStates.IdleNoWork;


        private Dictionary<Serial, Mobile> PetList = new Dictionary<Serial, Mobile>();
        private Dictionary<Serial, Mobile> TrackableMobs = new Dictionary<Serial, Mobile>();
        private byte oldFollowersNum;

        // MAP BORDER
        public RectangleF borderTop { get { return new RectangleF(renderingBounds.Left, renderingBounds.Top, renderingBounds.Right, 4); } }
        public RectangleF borderBottom { get { return new RectangleF(renderingBounds.Left, renderingBounds.Bottom - 4, renderingBounds.Right, 4); } }
        public RectangleF borderLeft { get { return new RectangleF(renderingBounds.Left, renderingBounds.Top + 4, 5, renderingBounds.Bottom); } }
        public RectangleF borderRight { get { return new RectangleF(renderingBounds.Right - 5, renderingBounds.Top + 4, 5, renderingBounds.Bottom); } }
        public RectangleF borderTopLeft { get { return new RectangleF(0, 0, 5, 4); } }
        public RectangleF borderTopRight { get { return new RectangleF(renderingBounds.Right - 5, 0, 5, 4); } }
        public RectangleF borderBottomLeft { get { return new RectangleF(0, renderingBounds.Bottom - 4, 5, 4); } }
        public RectangleF borderBottomRight { get { return new RectangleF(renderingBounds.Right - 5, renderingBounds.Bottom - 4, 5, 4); } }


        #endregion

        #region ONPAINT VARIABLES

        public bool trackingPlayer = true;
        public Point3D mfocus;
        public PointF pntPlayer;
        public PointF playerRot;

        public RectangleF renderArea;
        PointF compassLoc;

        ArrayList regions = new ArrayList();

        SizeF coordTopSize;
        string coordTopString;

        Point mouseMapCoord;

        SizeF coordBottomSize;
        string coordBottomString;

        int xLong = 0, yLat = 0;
        int xMins = 0, yMins = 0;
        bool xEast = false, ySouth = false;
        #endregion

        #region MAP OPTIONS
        // Designer Stuff
        private System.ComponentModel.IContainer components;
        private ContextMenuStrip ContextOptions;
        private ToolStripSeparator SeperatorOptions;
        private ToolStripMenuItem OptionOverlays;
        public ToolStripMenuItem Menu_OverlaysAll;
        public ToolStripMenuItem Menu_OverlaysGuard;
        public ToolStripMenuItem Menu_OverlaysGrid;
        public ToolStripMenuItem OptionExit;
        public ToolStripMenuItem AddMapMarker;
        public ToolStripMenuItem OptionPositions;
        public ToolStripMenuItem Menu_ShowPlayerPosition;
        public ToolStripMenuItem Menu_ShowPartyPositions;
        public ToolStripMenuItem Menu_ShowPetPositions;
        public ToolStripMenuItem OptionAlignments;
        public ToolStripMenuItem TiltMap45;
        public ToolStripMenuItem Menu_ShowAllPositions;
        public ToolStripMenuItem Menu_TrackPlayerPosition;
        public ContextMenuStrip ContextMarkerMenu;
        public ToolStripMenuItem Menu_EditMarker;
        public ToolStripMenuItem Menu_DeleteMarker;
        public ProgressBar MapGenProgressBar;
        private ToolStripMenuItem OptionMarkers;
        private ToolStripMenuItem Menu_MarkerNames;
        private ToolStripMenuItem Menu_MarkerCoords;
        public ToolStripSeparator SeperatorOptions2;

        public bool HasAllOverlays { get; set; }
        public bool HasGuardLines { get; set; }
        public bool HasGridLines { get; set; }

        public bool IsShowAllPositions { get; set; }
        public bool IsShowPlayerPosition { get; set; }
        public bool IsShowPetPositions { get; set; }
        public bool IsShowPartyPositions { get; set; }
        public bool IsTrackPlayerPosition { get; set; }

        public bool DisplayMarkerNames { get; set; }
        public bool DisplayMarkerCoords { get; set; }

        public string GuardLinesFile { get; set; }

        #endregion

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        public MapPanel()
        {
            zoom.Width = 1;
            zoom.Height = 1;

            Debug.WriteLine("MapPanel Loaded!");

            ContextMenuStrip = ContextOptions;

            DoubleBuffered = true;

            SetStyle(
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.SupportsTransparentBackColor,
                    true);

            HorizontalScroll.Visible = false;
            VerticalScroll.Visible = false;

            this.MouseEnter += new EventHandler(mapPanel_MouseEnter);
            this.MouseLeave += new EventHandler(mapPanel_MouseLeave);
            this.MouseDown += new MouseEventHandler(mapPanel_MouseDown);
            this.MouseUp += new MouseEventHandler(mapPanel_MouseUp);
            this.MouseMove += new MouseEventHandler(mapPanel_MouseMove);
            this.MouseDoubleClick += new MouseEventHandler(mapPanel_DoubleClick);
            this.MouseWheel += new MouseEventHandler(mapPanel_OnMouseWheel);

            PublicMarkers.mapPanel = this;

        }

        public void LoadGuardLines()
        {
            GuardLinesFile = Config.GetString("GuardLinesFile");
            m_Regions = JMap.MapRegion.Load($"{Config.GetInstallDirectory()}\\" + GuardLinesFile + ".def");

            UpdateAll();
        }

        public void LoadMap()
        {
            map = JMap.Map.GetMap(1);
            //if (map == null)
            //    map = Ultima.Map.Felucca;

            _mapRegular_1 = ImportMaps(1);
            _mapSize = _mapRegular_1.Size; //Full scale reference Size
            mapWidth = _mapSize.Width;
            mapHeight = _mapSize.Height;

            //_mapHeight_1.MakeTransparent(Color.White);

            GuardLinesFile = Config.GetString("GuardLinesFile");
            LoadGuardLines();

            InitializeComponent();
            Active = true;

            HasGuardLines = Config.GetBool("MapGuardLines");
            HasGridLines = Config.GetBool("MapGridLines");

            HasAllOverlays = HasGridLines && HasGuardLines;

            IsShowPlayerPosition = Config.GetBool("MapShowPlayerPosition");
            IsShowPetPositions = Config.GetBool("MapShowPetPositions");
            IsShowPartyPositions = Config.GetBool("MapShowPartyPositions");
            IsTrackPlayerPosition = Config.GetBool("MapTrackPlayerPosition");



            IsShowAllPositions = IsShowPlayerPosition && IsShowPetPositions && IsShowPlayerPosition &&
                                 IsTrackPlayerPosition;

            mapRotated = Config.GetBool("MapTilt");

            DisplayMarkerNames = Config.GetBool("DisplayMarkerNames");
            DisplayMarkerCoords = Config.GetBool("DisplayMarkerCoords");

            TiltMap45.Checked = mapRotated;

            Menu_ShowPlayerPosition.Checked = IsShowPlayerPosition;
            Menu_ShowPetPositions.Checked = IsShowPetPositions;
            Menu_ShowPartyPositions.Checked = IsShowPartyPositions;
            Menu_TrackPlayerPosition.Checked = IsTrackPlayerPosition;

            Menu_OverlaysGrid.Checked = HasGridLines;
            Menu_OverlaysGuard.Checked = HasGuardLines;

            Menu_ShowAllPositions.Checked = IsShowPlayerPosition && IsShowPetPositions && IsShowPlayerPosition &&
                                            IsTrackPlayerPosition;

            Menu_OverlaysAll.Checked = HasGridLines && HasGuardLines;





            MouseHoverWorker();

            compassLoc = new PointF(renderArea.Right - 20, renderArea.Top + 20);
            UpdatePlayerPos();

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();

            jMapMain.Text = $"UO Map - {this.FocusMobile.Name}";
            UpdateAll();//additional one to fire off rendering again, because it's gay
        }

        #region GRID WORKER
        public void GridWorker()
        {
            if (gridWorker != null)
                gridWorker.Dispose();

            gridWorker = new BackgroundWorker();
            gridWorker.DoWork += new DoWorkEventHandler(GridWorker_DoWork);
            gridWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GridWorker_RunWorkerCompleted);
            gridWorker.WorkerReportsProgress = true;
            gridWorker.WorkerSupportsCancellation = true;
            gridWorker.RunWorkerAsync();

        }

        private void GridWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            UpdateGrid();
        }

        /*public void CorrectGridPoints()
        {
            foreach (RectangleF cell8x8 in visibleGrid)
            {
                LockBitmap lockCell = new LockBitmap(_mapRegular_1);
                lockCell.LockBits();

                //if(lockCell.GetPixel( Color c != )
            }
        }*/

        private void GridWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            gridWorkerStates = GridWorkerStates.UpdatingVisible;

            visibleGrid = bufferingGrid;

            gridWorkerStates = GridWorkerStates.IdleNoWork;
        }

        #endregion

        #region MOUSE HOVER WORKER
        public void MouseHoverWorker()
        {
            if (mouseHoverWorker != null)
                mouseHoverWorker.Dispose();

            mouseHoverWorker = new BackgroundWorker();
            mouseHoverWorker.DoWork += new DoWorkEventHandler(mouseHoverWorker_DoWork);
            mouseHoverWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mouseHoverWorker_RunWorkerCompleted);
            mouseHoverWorker.WorkerReportsProgress = true;
            mouseHoverWorker.WorkerSupportsCancellation = true;
            mouseHoverWorker.RunWorkerAsync();
        }

        private void mouseHoverWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            CheckHoverPoints();
        }

        public void CheckHoverPoints()
        {
            try
            {
                if (markerWorkerState != MarkerWorkerStates.UpdatingVisible && !IsMouseDown && !IsZooming)
                {
                    foreach (Tuple<RectangleF, int> btnRef in hoverRegions)
                    {
                        JMapButton btn = (JMapButton)visibleMapButtons[btnRef.Item2];
                        if (btnRef.Item1.Contains(mousePosNow))
                        {
                            btn.IsHovered = true;
                            
                        }
                        if (btn.IsHovered)
                        {
                            Brush btnColorBrush = new SolidBrush(btn.textColor);
                            Pen btnColorPen = new Pen(btnColorBrush);

                            Brush treasureFill = new SolidBrush(Color.FromArgb(8, 156, 125, 17));
                            Pen treasurePen = new Pen(Color.Gold);

                            MarkerToEdit = btn;
                            Graphics gfx = CreateGraphics();

                            if (btn.type == JMapButtonType.Treasure)
                            {
                                RectangleF highlightRect = new RectangleF((btn.renderLoc.X + btn.hotSpot.X) - ( offset.X / 2), 
                                                                          (btn.renderLoc.Y + btn.hotSpot.Y) - ( offset.Y / 2),
                                                                          1 * offset.X, 
                                                                          1 * offset.Y);

                                PointF rotPoint = new PointF((btn.renderLoc.X + btn.hotSpot.X),
                                                             (btn.renderLoc.Y + btn.hotSpot.Y));

                                PointF tl = new PointF(Convert.ToInt32(Math.Floor((double)(highlightRect.X))), Convert.ToInt32(Math.Floor((double)highlightRect.Y)));
                                PointF tr = new PointF(Convert.ToInt32(Math.Floor((double)(highlightRect.X + highlightRect.Width))), Convert.ToInt32(Math.Floor((double)highlightRect.Y)));
                                PointF bl = new PointF(Convert.ToInt32(Math.Floor((double)(highlightRect.X))), Convert.ToInt32(Math.Floor((double)highlightRect.Y + highlightRect.Height)));
                                PointF br = new PointF(Convert.ToInt32(Math.Floor((double)(highlightRect.X + highlightRect.Width))), Convert.ToInt32(Math.Floor((double)highlightRect.Y + highlightRect.Height)));

                                if (mapRotated)
                                {
                                    tl = RotatePointF(tl, rotPoint, 45);
                                    tr = RotatePointF(tr, rotPoint, 45);
                                    bl = RotatePointF(bl, rotPoint, 45);
                                    br = RotatePointF(br, rotPoint, 45);
                                }

                                PointF[] rgn = new PointF[] { tl, tr, br, bl };

                                gfx.FillPolygon(treasureFill, rgn);
                                gfx.DrawPolygon(treasurePen, rgn);
                                

                                gfx.ResetTransform();
                            }
                            if (btn.type != JMapButtonType.UOPoint && btn.type != JMapButtonType.Treasure)
                            {
                                RectangleF highlightRect = new RectangleF((btn.renderLoc.X + btn.hotSpot.X) - 4, (btn.renderLoc.Y + btn.hotSpot.Y), 6, 6);
                                gfx.DrawRectangle(btnColorPen, Rectangle.Round(highlightRect));
                            }

                            
                            SizeF strLength = gfx.MeasureString(btn.displayText, m_RegFont);
                            gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                            string displayString = "";

                            if (DisplayMarkerNames)
                            {
                                if (btn.displayText.Length > 0)
                                    displayString = btn.displayText;
                            }
                            if (DisplayMarkerCoords)
                            {
                                if (btn.displayText.Length > 0)
                                    displayString += "\n" + btn.mapLoc.X + "," + btn.mapLoc.Y;
                                else
                                    displayString = btn.mapLoc.X + "," + btn.mapLoc.Y;
                            }
                            if(displayString != "")
                            {
                                gfx.DrawString(displayString, m_RegFont, btnColorBrush, btn.renderLoc.X + btn.hotSpot.X, (btn.renderLoc.Y - 5) - (strLength.Height / 2), stringFormat);
                            }

                            treasurePen.Dispose();
                            btnColorPen.Dispose();
                            btnColorBrush.Dispose();
                            gfx.Dispose();
                        }
                        if (!btnRef.Item1.Contains(mousePosNow) && btn.IsHovered)
                        {
                            if(!AddingMarker && !EditingMarker)
                            {
                                MarkerToEdit = null;
                            }
                            btn.IsHovered = false;
                            Invalidate();
                        }
                    }
                }
            }
            catch { }

        }

        private void mouseHoverWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        #endregion

        #region MARKER UPDATE WORKER
        public void MarkerUpdateWorker()
        {
            if (mapMarkerWorker != null)
                mapMarkerWorker.Dispose();

            mapMarkerWorker = new BackgroundWorker();
            mapMarkerWorker.DoWork += new DoWorkEventHandler(mapMarkerWorker_DoWork);
            //mapMarkerWorker.ProgressChanged += new ProgressChangedEventHandler(mapMarkerWorker_ProgressChanged);
            mapMarkerWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mapMarkerWorker_RunWorkerCompleted);
            mapMarkerWorker.WorkerReportsProgress = true;
            mapMarkerWorker.WorkerSupportsCancellation = true;
            mapMarkerWorker.RunWorkerAsync();
        }

        private void mapMarkerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            UpdateMarkers();
        }

        private void mapMarkerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            markerWorkerState = MarkerWorkerStates.UpdatingVisible;
            visibleMapButtons = bufferingMapButtons;
            hoverRegions.Clear();
            foreach (JMapButton btn in visibleMapButtons)
            {
                Tuple<RectangleF, int> btnRef = MarkerReference(btn, visibleMapButtons.IndexOf(btn));

                if (!hoverRegions.Contains(btnRef))
                    hoverRegions.Add(btnRef);
                else
                    Debug.WriteLine("HoverRegions already contains this reference");
            }
            markerWorkerState = MarkerWorkerStates.IdleNoWork;
        }
        #endregion

        public Bitmap ImportMaps(int zLevel)
        {
            //Debug.WriteLine("IMPORTING");

            if (BackgroundImage != null)
                BackgroundImage.Dispose();

            switch (zLevel)
            {
                case 1:
                    _mapRegular_1 = new Bitmap($"{Config.GetInstallDirectory()}\\JMap\\MAP0-1.BMP", true);
                    //_mapHeight_1 = new Bitmap(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-1-HM.BMP", true);
                    return _mapRegular_1;
                /*case 2:
                    _mapRegular_2 = new Bitmap($"{Config.GetInstallDirectory()}\\JMap\\MAP0-2.BMP", true);
                    return _mapRegular_2;
                case 4:
                    _mapRegular_4 = new Bitmap($"{Config.GetInstallDirectory()}\\JMap\\MAP0-4.BMP", true);
                    return _mapRegular_4;
                case 8:
                    _mapRegular_8 = new Bitmap($"{Config.GetInstallDirectory()}\\JMap\\MAP0-8.BMP", true);
                    return _mapRegular_8;
                    */
            }

            return _mapRegular_1; // SHOULD NEVER REACH HERE
        }

        //BOUNDING BOX FOR NEW ROTATED IMAGE
        private static Rectangle boundingBox(Bitmap img, Matrix matrix)
        {
            GraphicsUnit gu = new GraphicsUnit();
            Rectangle rImg = Rectangle.Round(img.GetBounds(ref gu));

            // Transform the four points of the image, to get the resized bounding box.
            Point topLeft = new Point(rImg.Left, rImg.Top);
            Point topRight = new Point(rImg.Right, rImg.Top);
            Point bottomRight = new Point(rImg.Right, rImg.Bottom);
            Point bottomLeft = new Point(rImg.Left, rImg.Bottom);
            Point[] points = new Point[] { topLeft, topRight, bottomRight, bottomLeft };
            GraphicsPath gp = new GraphicsPath(points,
                                                new byte[] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line });
            gp.Transform(matrix);
            Rectangle rect = Rectangle.Round(gp.GetBounds());
            gp.Dispose();
            return rect;
        }

        private static Font m_BoldFont = new Font("Courier New", 10, FontStyle.Bold);
        private static Font m_SmallFont = new Font("Arial", 6);
        private static Font m_RegFont = new Font("Arial", 8);

        public Tuple<string, Color> MobParams(Mobile mob)
        {
            string str = "";
            Color col = Color.Silver;

            if (mob.IsGhost)
            {
                col = Color.DarkGray;
                str = "(Ghost) ";
            }
            else if (mob.Poisoned)
            {
                col = Color.Green;
                str = "";
            }



            return Tuple.Create<string, Color>(str, col);
        }

        public Tuple<RectangleF, int> MarkerReference(JMapButton btn, int index)
        {
            RectangleF rect = new RectangleF(btn.renderLoc.X, btn.renderLoc.Y, 24, 24);

            return Tuple.Create<RectangleF, int>(rect, index);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            try
            {
                if (Active)
                {
                    renderingBounds = pe.Graphics.VisibleClipBounds;

                    pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.None;
                    pe.Graphics.SmoothingMode = SmoothingMode.None;
                    pe.Graphics.PageUnit = GraphicsUnit.Pixel;
                    pe.Graphics.PageScale = 1f;

                    /*if (gridWorkerStates != GridWorkerStates.UpdatingVisible)
                    {
                        GridOpacity = 15;
                        Brush gridBrush = new SolidBrush(Color.FromArgb(GridOpacity, 153, 214, 255));
                        Pen gridPen = new Pen(gridBrush, 1 * offset.X);

                        foreach (RectangleF visGrid in visibleGrid)
                        {
                            pe.Graphics.DrawRectangle(gridPen,
                                            Rectangle.Round(visGrid)
                                            );
                        }

                        gridPen.Dispose();
                        gridBrush.Dispose();
                    }*/


                   
                    // PLAYER MARKERS
                    // Circle with dot
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    if (IsShowPlayerPosition)
                    {
                        if (trackingPlayer && IsTrackPlayerPosition)
                        {
                            pe.Graphics.DrawEllipse(Pens.Silver, (renderArea.Right / 2) - 4, (renderArea.Bottom / 2) - 4, 8, 8);
                            pe.Graphics.FillRectangle(Brushes.Silver, (renderArea.Right / 2), (renderArea.Bottom / 2), 0.5f, 0.5f);
                        }
                        else
                        {
                            pe.Graphics.DrawEllipse(Pens.Silver, pntPlayer.X - 4, pntPlayer.Y - 4, 8, 8);
                            pe.Graphics.FillRectangle(Brushes.Silver, pntPlayer.X, pntPlayer.Y, 0.5f, 0.5f);
                        }
                    }
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // Crosshair
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X - 4, pntPlayer.Y, pntPlayer.X + 4, pntPlayer.Y);
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X, pntPlayer.Y - 4, pntPlayer.X, pntPlayer.Y + 4);


                    if (mapRotated)
                    {
                        zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                        zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));
                    }
                    else
                    {
                        zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgReg.X));
                        zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgReg.Y));
                    }

                    /*Debug.WriteLine(
                    $"--------------------PLAYER CHAR--------------------\n" +
                    $"Char Name: '{this.FocusMobile.Name}'\n" +
                    $"Serial: {this.FocusMobile.Serial}\n" +
                    $"PacketFlags: {this.FocusMobile.GetPacketFlags()}\n");

                    Debug.WriteLine("LayerData");
                    for (byte i = 0x01; i <= 0x1D; i++)
                    {
                        Item foundItem = this.FocusMobile.GetItemOnLayer((Layer)i);
                        if (foundItem != null)
                            Debug.WriteLine($"Item On Layer {(Layer)i}: {foundItem.Name}\n");
                    }
                    
                    
                    Debug.WriteLine("Items in player root container:");
                    foreach (Item itm in this.FocusMobile.Contains)
                    {
                        Debug.WriteLine($"Item: {itm.ToString()}");
                    }
                    foreach (Item it in this.FocusMobile.Backpack.Contains)
                    {
                        Debug.WriteLine($"Object: {it.ToString()}");
                    }
                    Debug.WriteLine(
                        $"------------------END PLAYER CHAR------------------\n\n");
                    */


                    foreach (KeyValuePair<Serial, Mobile> m in TrackableMobs)
                    {
                        //Mobile mob = World.FindMobile(m.Serial);
                        Mobile mob = m.Value;

                        /*if (!World.MobilesInRange(18).Contains(mob))
                        {
                            Debug.WriteLine($"Mobile: {mob.Name} is out of visual range. Position was/is: {mob.Position} ");
                            //if (Utility.InRange(World.Player.Position, m.Position, World.Player.VisRange))
                            //    list.Add(m);
                        }*/


                        //Mobile mob = World.FindMobile(s);
                        //if (!mob.IsHuman)
                        //    break;
                        //Mobile mob = World.FindMobile(m.Key);
                        if (mob == null)
                            continue;

                        if (mob == this.FocusMobile && mob == World.Player)
                            continue;

                        string name = mob.Name;
                        if (name == null || name.Length < 1)
                            name = "(Not Seen)";
                        if (name != null && name.Length > 20)
                            name = name.Substring(0, 20);

                        //if (mob.GetNotorietyColor() == 0x60e000 || mob.GetNotorietyColor() == 0x30d0e0)
                        //{
                        /*Debug.WriteLine(
                            $"----------------------NEW MOBILE----------------------\n" +
                            $"Mob Name: '{mob.Name}'\n " +
                            $"Serial: {mob.Serial}\n" +
                            $"{mob.Name}'s Data \n " +
                            $"(mob.Notoriety): {mob.Notoriety}\n" +
                            $"(mob.Unknown): {mob.Unknown}\n" +
                            $"(mob.Unknown2): {mob.Unknown2}\n" +
                            $"(mob.Unknown3): {mob.Unknown3}\n" +
                            $"(mob.Hue): {mob.Hue}\n" + 
                            $"(mob.CanRename): {mob.CanRename}");

                        Debug.WriteLine($"ContextMenu Data");
                        foreach (KeyValuePair<ushort, ushort> cm in mob.ContextMenu)
                        {
                            Debug.WriteLine($"Key: {cm.Key} Value: {cm.Value}\n");
                        }

                        /Debug.WriteLine($"Mob Contains Obj:");
                        for(int i = 0; i < 20; i++)
                        {
                        Debug.WriteLine("LayerData");                       
                        for(byte i = 0x01; i <= 0x1D; i++)
                        {
                            Item foundItem = mob.GetItemOnLayer((Layer)i);
                            if (foundItem != null)
                                Debug.WriteLine($"Item On Layer {(Layer)i}: {foundItem.Name}\n");
                        }
                        Debug.WriteLine(
                            $"----------------------END MOBILE----------------------\n\n");
                            }
                        }
                        */
                        name = MobParams(mob).Item1 + name;

                        SizeF strLength = pe.Graphics.MeasureString(name, m_RegFont);

                        Brush mobBrush = new SolidBrush(MobParams(mob).Item2);

                        PointF drawPoint = new PointF(Convert.ToInt32(Math.Floor((double)((mob.Position.X * offset.X) + zeroPoint.X))),
                                                      Convert.ToInt32(Math.Floor((double)(mob.Position.Y * offset.Y) + zeroPoint.Y)));
                        PointF pointDisplay = drawPoint;
                        PointF stringDisplay = drawPoint;

                        if (mapRotated)
                        {
                            pointDisplay = RotatePointF(drawPoint, zeroPoint, 45);
                            stringDisplay = pointDisplay;
                        }

                        if (!renderingBounds.Contains(pointDisplay.X, pointDisplay.Y))
                        {
                            if (pointDisplay.X < renderingBounds.Left)
                            {
                                pointDisplay.X = renderingBounds.Left + 4;
                                stringDisplay = new PointF(pointDisplay.X + (strLength.Width / 2) + 8, pointDisplay.Y);
                            }
                            if (pointDisplay.X > renderingBounds.Right)
                            {
                                pointDisplay.X = renderingBounds.Right - 8;
                                stringDisplay = new PointF(pointDisplay.X - (strLength.Width / 2) - 4, pointDisplay.Y);
                            }
                            if (pointDisplay.Y < renderingBounds.Top)
                            {
                                pointDisplay.Y = renderingBounds.Top + 4;
                                stringDisplay = new PointF(pointDisplay.X, pointDisplay.Y + (strLength.Height / 2) + 6);
                            }
                            if (pointDisplay.Y > renderingBounds.Bottom)
                            {
                                pointDisplay.Y = renderingBounds.Bottom - 8;
                                stringDisplay = new PointF(pointDisplay.X, pointDisplay.Y - (strLength.Height / 2) - 6);
                            }

                            pe.Graphics.FillRectangle(Brushes.DarkGoldenrod, pointDisplay.X, pointDisplay.Y, 4f, 4f);

                            SizeF nameSize = pe.Graphics.MeasureString(name, m_RegFont);
                            using (SolidBrush nb = new SolidBrush(Color.FromArgb(32, 0, 0, 64))) // Transparent background box
                            {
                                pe.Graphics.FillRectangle(nb, (stringDisplay.X - 4) - (nameSize.Width / 2), (stringDisplay.Y - 2f) - (nameSize.Height / 2), nameSize.Width + 4, nameSize.Height + 4);
                                //pe.Graphics.DrawRectangle(Pens.Black, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);
                            }

                            pe.Graphics.DrawString(name, m_RegFont, Brushes.Gray, stringDisplay.X, stringDisplay.Y, stringFormat);
                        }
                        else
                        {
                            pe.Graphics.FillRectangle(mobBrush, pointDisplay.X, pointDisplay.Y, 4f, 4f);

                            SizeF nameSize = pe.Graphics.MeasureString(name, m_RegFont);
                            using (SolidBrush nb = new SolidBrush(Color.FromArgb(96, 0, 0, 128))) // Transparent background box
                            {
                                pe.Graphics.FillRectangle(nb, (stringDisplay.X - 2) - (nameSize.Width / 2), (stringDisplay.Y - 12f) - (nameSize.Height / 2), nameSize.Width + 4, nameSize.Height + 4);
                                //pe.Graphics.DrawRectangle(Pens.Black, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);
                            }

                            pe.Graphics.DrawString(name, m_RegFont, mobBrush, stringDisplay.X, stringDisplay.Y - 10f, stringFormat); // Name
                        }
                    }

                    //DEBUG CENTER LINE
                    //pe.Graphics.DrawLine(Pens.Green, renderArea.Width / 2, renderArea.Top, renderArea.Width / 2, renderArea.Bottom);
                    //pe.Graphics.DrawLine(Pens.Green, renderArea.Left, renderArea.Height / 2, renderArea.Right, renderArea.Height / 2);

                    if(HasGuardLines)
                    {
                        foreach (JMap.MapRegion region in regions)
                        {
                            PointF tl = new PointF(Convert.ToInt32(Math.Floor((double)((region.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(region.Y * offset.Y) + zeroPoint.Y)));
                            PointF tr = new PointF(Convert.ToInt32(Math.Floor((double)(((region.X + region.Width) * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(region.Y * offset.Y) + zeroPoint.Y)));
                            PointF bl = new PointF(Convert.ToInt32(Math.Floor((double)((region.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)((region.Y + region.Length) * offset.Y) + zeroPoint.Y)));
                            PointF br = new PointF(Convert.ToInt32(Math.Floor((double)(((region.X + region.Width) * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)((region.Y + region.Length) * offset.Y) + zeroPoint.Y)));

                            if (mapRotated)
                            {
                                tl = RotatePointF(tl, zeroPoint, 45);
                                tr = RotatePointF(tr, zeroPoint, 45);
                                br = RotatePointF(br, zeroPoint, 45);
                                bl = RotatePointF(bl, zeroPoint, 45);
                            }

                            PointF[] rgn = new PointF[] { tl, tr, br, bl };

                            pe.Graphics.DrawPolygon(Pens.Green, rgn);
                        }
                    }

                    //MAP MARKERS
                    foreach (JMapButton btn in visibleMapButtons)
                    {
                        btn.UpdateButton();

                        RectangleF rectF = new RectangleF(btn.renderLoc.X, btn.renderLoc.Y, btn.renderSize.Width, btn.renderSize.Height);
                        Rectangle pinRect = Rectangle.Round(rectF);



                        pe.Graphics.DrawImage(btn.img, pinRect);
                    }


                    // UOPS STYLE: COMPASS AT PLAYER LOCATION
                    //pe.Graphics.DrawString("W", m_BoldFont, Brushes.Red, pntPlayer.X - 35, pntPlayer.Y);
                    //pe.Graphics.DrawString("E", m_BoldFont, Brushes.Red, pntPlayer.X + 35, pntPlayer.Y);
                    //pe.Graphics.DrawString("N", m_BoldFont, Brushes.Red, pntPlayer.X, pntPlayer.Y - 35);
                    //pe.Graphics.DrawString("S", m_BoldFont, Brushes.Red, pntPlayer.X, pntPlayer.Y + 35);
                    
                    // UOPS STYLE: CROSSHAIR ROTATED AT PLAYER LOCATION
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X - 4, pntPlayer.Y, pntPlayer.X + 4, pntPlayer.Y);
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X, pntPlayer.Y - 4, pntPlayer.X, pntPlayer.Y + 4);

                    //Reset the rotation, otherwise we end up with everything we draw rotated
                    //if(mapRotated)
                    //    pe.Graphics.ResetTransform();

                    //Point pntTest2 = new Point((3256) - (mapOrigin.X << 3) - offset.X, (326) - (mapOrigin.Y << 3) - offset.Y);
                    //PointF pntTest2F = RotatePoint(new Point(xtrans, ytrans), pntTest2);
                    //pe.Graphics.FillRectangle(Brushes.LimeGreen, pntTest2F.X, pntTest2F.Y, 4, 4);

                    //pe.Graphics.ResetTransform();
                    // END UOPS STYLE


                    // COORDINATE BARS
                    //      PLAYER POS
                    pe.Graphics.FillRectangle(Brushes.Wheat, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);
                    pe.Graphics.DrawRectangle(Pens.Black, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);
                    pe.Graphics.DrawString(coordTopString, m_RegFont, Brushes.Black, 6, 6);
                    //      MOUSE POS
                    if(!mouseOnMap || IsMouseDown)
                    {
                        pe.Graphics.FillRectangle(Brushes.AliceBlue, 6, renderArea.Bottom - (6 + coordBottomSize.Height + 2), coordBottomSize.Width + 2, coordBottomSize.Height + 2);
                        pe.Graphics.DrawRectangle(Pens.Black, 6, renderArea.Bottom - -(6 + coordBottomSize.Height + 2), coordBottomSize.Width + 2, coordBottomSize.Height + 2);
                        pe.Graphics.DrawString(coordBottomString, m_RegFont, Brushes.Black, 6, renderArea.Bottom - (6 + coordBottomSize.Height + 2));
                    }
                    // COORDINATE BARS END

                    // MAP BORDER
                    pe.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    using (TextureBrush borderVBrush = new TextureBrush(borderV, WrapMode.Tile))
                    {
                        borderVBrush.TranslateTransform(borderLeft.X, borderLeft.Y);
                        pe.Graphics.FillRectangle(borderVBrush, borderLeft);
                        borderVBrush.ResetTransform(); //reset else its fucked
                        borderVBrush.TranslateTransform(borderRight.X, borderRight.Y);
                        pe.Graphics.FillRectangle(borderVBrush, borderRight);
                        borderVBrush.ResetTransform();
                    }
                    using (TextureBrush borderHBrush = new TextureBrush(borderH, WrapMode.Tile))
                    {
                        borderHBrush.TranslateTransform(borderTop.X, borderTop.Y);
                        pe.Graphics.FillRectangle(borderHBrush, borderTop);
                        borderHBrush.ResetTransform(); //reset else its fucked
                        borderHBrush.TranslateTransform(borderBottom.X, borderBottom.Y);
                        pe.Graphics.FillRectangle(borderHBrush, borderBottom);
                        borderHBrush.ResetTransform();
                    }
                    // MAP BORDER END

                    // COMPASS
                    if (mapRotated)
                    {
                        pe.Graphics.TranslateTransform(compassLoc.X, compassLoc.Y);
                        pe.Graphics.RotateTransform(45);
                        pe.Graphics.TranslateTransform(-compassLoc.X, -compassLoc.Y);
                    }
                    // Circle
                    //pe.Graphics.DrawEllipse(Pens.Silver, compassLoc.X - 18, compassLoc.Y - 18, 36, 36);

                    pe.Graphics.DrawLine(Pens.Silver, compassLoc.X - 6, compassLoc.Y, compassLoc.X + 6, compassLoc.Y);
                    pe.Graphics.DrawLine(Pens.Silver, compassLoc.X, compassLoc.Y - 6, compassLoc.X, compassLoc.Y + 6);
                    pe.Graphics.DrawString("W", m_BoldFont, Brushes.Red, compassLoc.X - 12, compassLoc.Y, stringFormat);
                    pe.Graphics.DrawString("E", m_BoldFont, Brushes.Red, compassLoc.X + 12, compassLoc.Y, stringFormat);
                    pe.Graphics.DrawString("N", m_BoldFont, Brushes.Red, compassLoc.X, compassLoc.Y - 12, stringFormat);
                    pe.Graphics.DrawString("S", m_BoldFont, Brushes.Red, compassLoc.X, compassLoc.Y + 12, stringFormat);

                    pe.Graphics.ResetTransform();
                    // COMPASS END
                }
            }
            catch { }
            base.OnPaint(pe);
        }

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.None;
            pe.Graphics.SmoothingMode = SmoothingMode.None;
            pe.Graphics.PageUnit = GraphicsUnit.Pixel;
            pe.Graphics.PageScale = 1f;

            pe.Graphics.Clear(Color.Black);

            if (tiltChanged)
            {
                if (mapRotated)
                {
                    bgRot = bgReg;
                    bgRot = RotatePointF(bgReg, bgReg, 45);
                }

                tiltChanged = false;
            }

            Matrix mtx = new Matrix();

            if (mapRotated)
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));

                mtx.RotateAt(45, zeroPoint);
                pe.Graphics.MultiplyTransform(mtx);

                pe.Graphics.DrawImage(_mapRegular_1, zeroPoint.X, zeroPoint.Y, mapWidth, mapHeight);
            }
            else
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgReg.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgReg.Y));

                pe.Graphics.DrawImage(_mapRegular_1, zeroPoint.X, zeroPoint.Y, mapWidth, mapHeight);
            }

            #region GRID LINE RENDERING
            if(HasGridLines)
            {
                if (offset.X >= 0.75f)
                {
                    GridOpacity = 5;
                    Brush gridBrush = new SolidBrush(Color.FromArgb(GridOpacity, 153, 214, 255));
                    Pen gridPen = new Pen(gridBrush, 1 * offset.X);

                    for (int bY = 0; bY < map.Height; bY += 8)
                    {
                        PointF pt1 = new PointF(Convert.ToInt32(Math.Floor((double)((0 * offset.X) + zeroPoint.X))),
                                                Convert.ToInt32(Math.Floor((double)(bY * offset.Y) + zeroPoint.Y)));
                        PointF pt2 = new PointF(Convert.ToInt32(Math.Floor((double)((mapWidth * offset.X) + zeroPoint.X))),
                                                Convert.ToInt32(Math.Floor((double)(bY * offset.Y) + zeroPoint.Y)));

                        pe.Graphics.DrawLine(gridPen, pt1, pt2);
                    }

                    for (int bX = 0; bX < map.Width; bX += 8)
                    {
                        PointF pt1 = new PointF(Convert.ToInt32(Math.Floor((double)((bX * offset.X) + zeroPoint.X))),
                                                Convert.ToInt32(Math.Floor((double)(0 * offset.Y) + zeroPoint.Y)));
                        PointF pt2 = new PointF(Convert.ToInt32(Math.Floor((double)((bX * offset.X) + zeroPoint.X))),
                                                Convert.ToInt32(Math.Floor((double)(mapHeight * offset.Y) + zeroPoint.Y)));

                        pe.Graphics.DrawLine(gridPen, pt1, pt2);
                    }

                    gridBrush.Dispose();
                    gridPen.Dispose();
                }
            }
            #endregion

            mtx.Dispose();

            #region TILE DRAWING BITBLT
            /* 
            if (drawTiles && !tilesDrawing)
            {
                DrawTiles(ref pe);

                //Thread tileThread = new Thread(DrawTiles);
                //tileThread.Start();

                //var t = Task.Factory.StartNew(() => DrawTiles());
            }
            //if(mapBuffer != null)
            //    DrawToBitmap(mapBuffer, this.Bounds);
            */
            #endregion

        }


        #region TILE DRAWING & TEST FUNCTIONS

        public void CreateChunk(ref PaintEventArgs pe, int bX, int bY, ushort[,] BlockCache)// ushort[,] StaticCache, Graphics gfx)          
        {
            Graphics g = pe.Graphics;

            int chunkScaleX = Convert.ToInt32(Math.Floor((double)(8 * offset.X)));
            int chunkScaleY = Convert.ToInt32(Math.Floor((double)(8 * offset.Y)));

            //using (Graphics g = Graphics.FromImage(worldChunk))
            //{
            //    g.InterpolationMode = InterpolationMode.NearestNeighbor;
            //    g.SmoothingMode = SmoothingMode.None;

            // MAYBE USE THIS? Bitmap worldChunk = new Bitmap(chunkScaleX, chunkScaleY);

            for (int tY = 0; tY < 8; ++tY)                                      //iterate our 8x8 Y
            {
                for (int tX = 0; tX < 8; ++tX)                                  //iterate our 8x8 X  
                {
                    float posX = tX * offset.X;
                    float posY = (tY - 1) * offset.Y;
                    float sizeX = 1.5f * offset.X;
                    float sizeY = 1.5f * offset.Y;

                    ushort id = (ushort)BlockCache.GetValue(tX, tY);

                    if (Art.IsValidLand(id))
                    {
                        bool patched;

                        using (Bitmap tile = new Bitmap(Art.GetLand(id, out patched)))
                        using (Graphics grSrc = Graphics.FromImage(tile))
                        {
                            IntPtr hdcDest = IntPtr.Zero;
                            IntPtr hdcSrc = IntPtr.Zero;
                            IntPtr hBitmap = IntPtr.Zero;
                            IntPtr hOldObject = IntPtr.Zero;

                            try
                            {
                                hdcDest = g.GetHdc();
                                hdcSrc = grSrc.GetHdc();
                                hBitmap = tile.GetHbitmap();

                                hOldObject = SelectObject(hdcSrc, hBitmap);
                                if (hOldObject == IntPtr.Zero)
                                    throw new Win32Exception();

                                if (!BitBlt(hdcDest, 0, 0, (int)sizeX, (int)sizeY,
                                    hdcSrc, 0, 0, 0x00CC0020U))
                                    throw new Win32Exception();
                            }
                            finally
                            {
                                if (hOldObject != IntPtr.Zero) SelectObject(hdcSrc, hOldObject);
                                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                                if (hdcDest != IntPtr.Zero) g.ReleaseHdc(hdcDest);
                                if (hdcSrc != IntPtr.Zero) grSrc.ReleaseHdc(hdcSrc);
                            }
                        }





                        //g.DrawImage(worldChunk, chunkX - (0.5f * offset.X), chunkY - (0.5f * offset.Y), 8 * offset.X, 8 * offset.Y);




                        //g.TranslateTransform(posX + (1.5f * offset.X), posY + (1.5f * offset.Y));
                        //g.RotateTransform(-45);
                        //g.TranslateTransform(-(posX + (1.5f * offset.X)), -(posY + (1.5f * offset.Y)));

                        //g.DrawImage(tile, 
                        //    posX, 
                        //    posY, 
                        //    sizeX, 
                        //    sizeY);

                        /*
                        IntPtr targetDC = g.GetHdc();
                        IntPtr sourceDC = GDI32.CreateCompatibleDC(targetDC);
                        IntPtr sourceBitmap = tile.GetHbitmap();
                        IntPtr originalBitmap = GDI32.SelectObject(sourceDC, sourceBitmap);

                        GDI32.BitBlt(targetDC,                                             //target device context
                            posX, posY,                                                           //dest XY
                            sizeX, sizeY,                                          //tile size
                            sourceDC,                                                      //source device context
                            0, 0,                                                           //source XY
                            GDI32.RasterOps.SRCCOPY);

                        GDI32.SelectObject(sourceDC, originalBitmap);
                        GDI32.DeleteObject(sourceBitmap);
                        GDI32.DeleteDC(sourceDC);
                        g.ReleaseHdc(targetDC);

                        //g.ResetTransform();
                        */

                    }
                    /*
                    ushort cid = (ushort)StaticCache.GetValue(tX, tY);

                    if (Art.IsValidStatic(cid) && cid > 1)
                    {
                        bool patched;
                        using (Bitmap tile = new Bitmap(Art.GetStatic(cid, out patched)))
                        {
                            float posX = tX * offset.X;
                            float posY = (tY - 1) * offset.Y;
                            float sizeX = ((tile.Width / 44) * 1.5f) * offset.X;
                            float sizeY = ((tile.Height / 44) * 1.5f) * offset.Y;

                            g.TranslateTransform(posX + (sizeX), posY + (sizeY));
                            g.RotateTransform(-45);
                            g.TranslateTransform(-(posX + (sizeX)), -(posY + (sizeY)));

                            g.DrawImage(tile,
                                posX,
                                posY,
                                sizeX,
                                sizeY);

                            g.ResetTransform();
                        }
                    }*/
                }
            }

            //}
            //worldChunk.Save(@"F:\UOStuff\UOArt\TilingTest\BlockTest\blockOutput" + bX.ToString() + bY.ToString() + ".bmp", ImageFormat.Bmp);
        }

        public void DrawTiles(ref PaintEventArgs pe)
        {

            Graphics g = pe.Graphics;
            //Graphics bgGfx = pe.Graphics;

            tilesDrawing = true;

            Pen p = new Pen(Color.FromArgb(64, 128, 0, 0));

            int brXMin = Convert.ToInt32(Math.Ceiling((double)(mfocus.X / 8))) - 1;
            int brXMax = Convert.ToInt32(Math.Ceiling((double)(mfocus.X / 8))) + 1;
            int brYMin = Convert.ToInt32(Math.Ceiling((double)(mfocus.Y / 8))) - 1;
            int brYMax = Convert.ToInt32(Math.Ceiling((double)(mfocus.Y / 8))) + 1;

            int chunkScaleX = Convert.ToInt32(Math.Floor((double)(8 * offset.X)));
            int chunkScaleY = Convert.ToInt32(Math.Floor((double)(8 * offset.Y)));

            ushort[,] BlockCache = new ushort[8, 8];
            //ushort[,] SetStaticCache;

            int cacheRange = 2;

            #region TILE CACHING

            //Graphics g;
            //IntPtr hdcDest = IntPtr.Zero;


            //if (brXMin == tileDisplay.oldXMin && brYMin == tileDisplay.oldYMin)
            //{
            //Debug.WriteLine("No changes, drawing from buffer.");
            /*
            float chunkPosX = (((brXMin * 8) * offset.X) + zeroPoint.X) - (0.5f * offset.X);
            float chunkPosY = (((brYMin * 8) * offset.Y) + zeroPoint.Y) - (0.5f * offset.Y);
            float chunkSizeX = (1f * offset.X);
            float chunkSizeY = (1f * offset.Y);

            g = Graphics.FromImage(mapBuffer);
            // new, trying this up here
            hdcDest = bgGfx.GetHdc();

            IntPtr hdcSrc = g.GetHdc();
            IntPtr hBitmap = mapBuffer.GetHbitmap();
            IntPtr hOldObject = IntPtr.Zero;

            hOldObject = SelectObject(hdcSrc, hBitmap);
            if (hOldObject == IntPtr.Zero)
                throw new Win32Exception();

            if (!BitBlt(
                hdcDest,
                (int)chunkPosX, (int)chunkPosY,
                (int)chunkSizeX, (int)chunkSizeY,
                hdcSrc,
                0, 0,
                (uint)GDI32.RasterOps.SRCCOPY)
                )
                throw new Win32Exception();

            if (hOldObject != IntPtr.Zero) SelectObject(hdcSrc, hOldObject);
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            if (hdcDest != IntPtr.Zero) g.ReleaseHdc(hdcDest);
            if (hdcSrc != IntPtr.Zero) g.ReleaseHdc(hdcSrc);
            */
            //}
            //else
            //{
            //Debug.WriteLine("Location changed, need new tiles.");

            for (int bY = brYMin - cacheRange; bY < brYMax + cacheRange; ++bY)                             //Iterate through each row
            {
                for (int bX = brXMin - cacheRange; bX < brXMax + cacheRange; ++bX)                          //Iterate through each colum position per row 0-64 
                {
                    //Debug.WriteLine("Rendering Tile Chunk X:{0} Y:{1}", bX, bY);

                    lTiles = map.Tiles.GetLandBlock(bX, bY).ToArray();                     //can just do this on the fly!?!?!?!?!?!?!?!?!?!
                                                                                           //sTiles = map.Tiles.GetStaticBlock(bX, bY).ToArray();

                    if (brXMin != tileDisplay.oldXMin || brYMin != tileDisplay.oldYMin)
                    {
                        BlockCache = new ushort[8, 8];
                        //ushort[,] SetStaticCache = new ushort[8, 8];
                    }
                    else
                    {
                        BlockCache = (ushort[,])tileDisplay.TileCache.GetValue(bX, bY);
                    }

                    int cIndex = 0;
                    for (int tY = 0; tY < 8; ++tY)                                      //iterate our 8x8 Y
                    {
                        for (int tX = 0; tX < 8; ++tX)                                  //iterate our 8x8 X  
                        {
                            //Debug.WriteLine("Pass X:{0} Y:{1}", tX, tY);

                            if (brXMin != tileDisplay.oldXMin || brYMin != tileDisplay.oldYMin)
                            {
                                BlockCache.SetValue((byte)lTiles[cIndex].ID, tX, tY);
                            }

                            //SetBlockCache.SetValue((byte)lTiles[cIndex].ID, tX, tY);

                            float posX = (((bX * 8 + tX) * offset.X) + zeroPoint.X) - (0.5f * offset.X);
                            float posY = (((bY * 8 + tY) * offset.Y) + zeroPoint.Y) - (0.5f * offset.Y);
                            float sizeX = (1f * offset.X);
                            float sizeY = (1f * offset.Y);

                            ushort id = (ushort)BlockCache.GetValue(tX, tY);

                            if (Art.IsValidLand(id))
                            {
                                bool patched;

                                //GDI+ - DrawImage
                                /*using (Bitmap tile = new Bitmap(Art.GetLand(id, out patched), (int)sizeX, (int)sizeY))
                                {
                                    g.TranslateTransform(posX + (sizeX), posY + (sizeY));
                                    g.RotateTransform(-45);
                                    g.TranslateTransform(-(posX + (sizeX)), -(posY + (sizeY)));

                                    g.DrawImage(tile, 
                                        posX, 
                                        posY, 
                                        sizeX, 
                                        sizeY);

                                    g.ResetTransform();

                                    g.DrawRectangle(p,
                                    (((bX * 8 + tX) * offset.X) + zeroPoint.X) - (0.5f * offset.X),
                                    (((bY * 8 + tY) * offset.Y) + zeroPoint.Y) - (0.5f * offset.Y),
                                    (1 * offset.X),
                                    (1 * offset.Y)
                                    );
                                }*/

                                //GDI - BitBlt
                                using (Bitmap tile = new Bitmap(Art.GetLand(id, out patched), (int)sizeX, (int)sizeY))
                                using (Graphics grSrc = Graphics.FromImage(tile))
                                {
                                    IntPtr hdcDest = IntPtr.Zero;
                                    IntPtr hdcSrc = IntPtr.Zero;
                                    IntPtr hBitmap = IntPtr.Zero;
                                    IntPtr hOldObject = IntPtr.Zero;

                                    grSrc.TranslateTransform(posX + (sizeX), posY + (sizeY));
                                    grSrc.RotateTransform(-45);
                                    grSrc.TranslateTransform(-(posX + (sizeX)), -(posY + (sizeY)));

                                    //mapBuffer = new Bitmap((int)renderArea.Width, (int)renderArea.Height);
                                    //g = Graphics.FromImage(mapBuffer);

                                    g.TranslateTransform(posX + (sizeX), posY + (sizeY));
                                    g.RotateTransform(-45);
                                    g.TranslateTransform(-(posX + (sizeX)), -(posY + (sizeY)));

                                    try
                                    {
                                        hdcDest = g.GetHdc();
                                        hdcSrc = grSrc.GetHdc();
                                        hBitmap = tile.GetHbitmap();

                                        hOldObject = SelectObject(hdcSrc, hBitmap);
                                        if (hOldObject == IntPtr.Zero)
                                            throw new Win32Exception();

                                        if (!BitBlt(
                                            hdcDest,
                                            (int)posX, (int)posY,
                                            (int)sizeX, (int)sizeY,
                                            hdcSrc,
                                            0, 0,
                                            (uint)GDI32.RasterOps.SRCCOPY)
                                            )
                                            throw new Win32Exception();
                                    }
                                    finally
                                    {
                                        if (hOldObject != IntPtr.Zero) SelectObject(hdcSrc, hOldObject);
                                        if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                                        if (hdcDest != IntPtr.Zero) g.ReleaseHdc(hdcDest);
                                        if (hdcSrc != IntPtr.Zero) grSrc.ReleaseHdc(hdcSrc);

                                        // HELPER GRID
                                        g.DrawRectangle(p,
                                                        (((bX * 8 + tX) * offset.X) + zeroPoint.X) - (0.5f * offset.X),
                                                        (((bY * 8 + tY) * offset.Y) + zeroPoint.Y) - (0.5f * offset.Y),
                                                        (1 * offset.X),
                                                        (1 * offset.Y)
                                                        );
                                        // END HELPER GRID

                                        grSrc.ResetTransform();
                                        g.ResetTransform();
                                    }
                                }
                            }

                            /*if (sTiles != null && sTiles[tX][tY].Length > 0)
                            {
                                //Debug.WriteLine("sTiles Length: {0}", sTiles[tX][tY].Length);
                                int zIndex = -999;
                                int? zMax = null;
                                for(int z = 0; z < sTiles[tX][tY].Length; ++z)
                                {
                                    int zHeight = sTiles[tX][tY][z].Z;
                                    if (zMax == null || zHeight > zMax.Value)
                                    {
                                        zMax = zHeight;
                                        zIndex = z;
                                    }                                       
                                }
                                if (zIndex != -999)
                                {
                                    //if (sTiles[tX][tY][zIndex].Z >= lTiles[cIndex].Z - 5)// && sTiles[tX][tY][zIndex].Z <= lTiles[cIndex].Z + 5)
                                    //{
                                        SetStaticCache.SetValue(sTiles[tX][tY][zIndex].ID, tX, tY);
                                    //}                                            
                                }                                        
                            }*/
                        }
                    }
                }
            }
            //tileDisplay.UpdateCache(brXMin, brYMin);
            //}

            #endregion
            p.Dispose();
            tilesDrawing = false;

        }

        private void Draw(ref PaintEventArgs pe)
        {
            Graphics g = pe.Graphics;

            using (Bitmap bmp = (Bitmap)Bitmap.FromFile(@"F:\UOStuff\UO Art\Mob 1-0_new3.bmp"))
            using (Graphics grSrc = Graphics.FromImage(bmp))
            {
                IntPtr hdcDest = IntPtr.Zero;
                IntPtr hdcSrc = IntPtr.Zero;
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr hOldObject = IntPtr.Zero;

                try
                {
                    hdcDest = g.GetHdc();
                    hdcSrc = grSrc.GetHdc();
                    hBitmap = bmp.GetHbitmap();

                    hOldObject = SelectObject(hdcSrc, hBitmap);
                    if (hOldObject == IntPtr.Zero)
                        throw new Win32Exception();

                    if (!BitBlt(hdcDest, 0, 0, this.Width, this.Height,
                        hdcSrc, 0, 0, 0x00CC0020U))
                        throw new Win32Exception();
                }
                finally
                {
                    if (hOldObject != IntPtr.Zero) SelectObject(hdcSrc, hOldObject);
                    if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                    if (hdcDest != IntPtr.Zero) g.ReleaseHdc(hdcDest);
                    if (hdcSrc != IntPtr.Zero) grSrc.ReleaseHdc(hdcSrc);
                }
            }
        }

        public int RGB(int r, int g, int b)
        {
            return ((int)(((byte)(r) | ((ushort)((byte)(g)) << 8)) | (((ushort)(byte)(b)) << 16)));
        }

        public void FillTransparentRect(Graphics g, Rectangle rc, Color c)
        {
            IntPtr hDc = g.GetHdc();
            IntPtr hDc2 = GDI32.CreateCompatibleDC(hDc);
            IntPtr hBmp = GDI32.CreateCompatibleBitmap(hDc, rc.Width, rc.Height);
            IntPtr hBrush = GDI32.CreateSolidBrush(c.ToArgb());
            IntPtr hOldBmp = GDI32.SelectObject(hDc2, hBmp);
            IntPtr hOldBrush = GDI32.SelectObject(hDc2, hBrush);
            bool bResult = GDI32.FloodFill(hDc2, 0, 0, c.ToArgb());
            bResult &= GDI32.BitBlt(hDc, rc.Left, rc.Top, rc.Width, rc.Height, hDc2, 0, 0, GDI32.RasterOps.SRCCOPY);
            GDI32.SelectObject(hDc2, hOldBmp);
            GDI32.SelectObject(hDc2, hOldBrush);
            GDI32.DeleteObject(hBmp);
            GDI32.DeleteObject(hBrush);
            GDI32.DeleteObject(hDc2);

            g.ReleaseHdc(); //Do this else expect GDI memory Leak
        }

        public void TestXOR()
        {
            Bitmap test = new Bitmap(100, 100);
            Graphics g = Graphics.FromImage(test);
            g.DrawLine(Pens.White, new Point(0, 0), new Point(100, 100));
            test.Save(@"F:\UOStuff\UOArt\testBefore.bmp", ImageFormat.Bmp);
            IntPtr testHDC = g.GetHdc();
            GDI32.SelectObject(testHDC, test.GetHbitmap());
            GDI32.BitBlt(testHDC, 0, 0, 100, 100, testHDC, 0, 0, GDI32.RasterOps.SRCINVERT);
            g.ReleaseHdc(testHDC);
            test.Save(@"F:\UOStuff\UOArt\testAfter.bmp", ImageFormat.Bmp);
            g.Dispose();
        }

        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern System.IntPtr SelectObject(
            [In()] System.IntPtr hdc,
            [In()] System.IntPtr h);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(
            [In()] System.IntPtr ho);

        [DllImport("gdi32.dll", EntryPoint = "BitBlt")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt(
            [In()] System.IntPtr hdc, int x, int y, int cx, int cy,
            [In()] System.IntPtr hdcSrc, int x1, int y1, uint rop);

        [DllImport("gdi32.dll", EntryPoint = "PlgBlt")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PlgBlt(
            [In()] System.IntPtr hdc, int x, int y, int cx, int cy,
            [In()] System.IntPtr hdcSrc, int x1, int y1, uint rop);


        #endregion


        public void UpdateAll()
        {
            if (!Active)
                return;

            TrackPetsParty();

            Graphics gfx = CreateGraphics();

            renderArea = jMapMain.ClientRectangle;

            // PLAYER POSITION
            mfocus = this.FocusMobile.Position;

            if (mapRotated)
            {
                bgRot = RotatePointF(bgReg, bgReg, 45);

                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));

                pntPlayer = new PointF(Convert.ToInt32(Math.Floor((double)((mfocus.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(mfocus.Y * offset.Y) + zeroPoint.Y)));
                pntPlayer = RotatePointF(pntPlayer, zeroPoint, 45);
            }
            else if (!mapRotated)
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgReg.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgReg.Y));

                pntPlayer = new PointF(Convert.ToInt32(Math.Floor((double)((mfocus.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(mfocus.Y * offset.Y) + zeroPoint.Y)));
            }

            compassLoc = new PointF(renderArea.Right - 30, renderArea.Top + 30);

            // GUARDLINES & OTHER REGIONS
            regions = RegionList(0, 0, _mapSize.Width);

            // COORDINATES BARS
            //      PLAYER POSITION
            if (Format(new Point(mfocus.X, mfocus.Y), Ultima.Map.Felucca, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                coordTopString = String.Format("{0}°{1}'{2} {3}°{4}'{5} | ({6},{7})", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W", mfocus.X, mfocus.Y); //World.Player.Position.X / Y
                coordTopSize = gfx.MeasureString(coordTopString, m_RegFont);
            }
            //      MOUSE POSITION
            if (Format(mouseMapCoord, Ultima.Map.Felucca, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                coordBottomString = String.Format("{0}°{1}'{2} {3}°{4}'{5} | ({6},{7})", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W", mouseMapCoord.X, mouseMapCoord.Y); //World.Player.Position.X / Y
                coordBottomSize = gfx.MeasureString(coordBottomString, m_RegFont);
            }

            gfx.Dispose();

            Invalidate();

            if (markerWorkerState == MarkerWorkerStates.IdleNoWork)
            {
                MarkerUpdateWorker();
            }
        }

        public void TrackPetsParty()
        {
            TrackableMobs.Clear();

            if (IsShowPetPositions)
            {
                foreach (KeyValuePair<Serial, Mobile> m in World.Mobiles)
                {
                    Mobile mob = World.FindMobile(m.Key);

                    //Mob is owned by the player but is not yet in the Pet List.
                    //Add it.
                    if (mob.CanRename && !PetList.ContainsKey(mob.Serial))
                    {
                        PetList.Add(mob.Serial, mob);
                        //Debug.WriteLine($"Pet '{mob.Name}' added to PetList");
                    }

                    //Mob is owned by player, is in Pet List (stabled after perhaps), but is not in the TrackableMobs list yet.
                    //Add it.
                    if (PetList.ContainsKey(mob.Serial) && !TrackableMobs.ContainsKey(mob.Serial))
                    {
                        TrackableMobs.Add(mob.Serial, mob);
                        //Debug.WriteLine($"Pet '{mob.Name}' added to TrackableMobs");
                    }
                    //Mob is no longer under players command
                    //Remove it you haxor!
                    if (!mob.CanRename && PetList.ContainsKey(mob.Serial))
                    {
                        PetList.Remove(mob.Serial);
                        TrackableMobs.Remove(mob.Serial);
                        //Debug.WriteLine($"Pet '{mob.Name}' removed from PetList");
                    }
                }
                
            }

            if (IsShowPartyPositions)
            {
                foreach (Serial s in PacketHandlers.Party)
                {
                    Mobile mob = World.FindMobile(s);

                    //Party member not yet in TrackedMobs list, add them! For honor and glory!
                    if (!TrackableMobs.ContainsKey(mob.Serial))
                        TrackableMobs.Add(mob.Serial, mob);

                    //Debug.WriteLine($"Character '{mob.Name}' added to TrackableMobs");
                }
            }
        }

        public void UpdateMapPos()
        {
            if (mapRotated)
            {
                bgRot = RotatePointF(bgReg, bgReg, 45);

                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));
            }
            else if (!mapRotated)
            {

                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgReg.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgReg.Y));
            }
        }

        public void UpdateGrid()
        {
            gridWorkerStates = GridWorkerStates.UpdatingGrid;

            RectangleF extendedBounds = new RectangleF(renderingBounds.Left - 100, renderingBounds.Top - 100, renderingBounds.Right + 100, renderingBounds.Bottom + 100);

            bufferingGrid.Clear();

            for (int bY = 0; bY < map.Height; bY += 8)
            {
                for (int bX = 0; bX < map.Width; bX += 8)
                {
                    RectangleF gridTest = new RectangleF(
                            (((bX) * offset.X) + zeroPoint.X),
                            (((bY) * offset.Y) + zeroPoint.Y),
                            (8 * offset.X),
                            (8 * offset.Y));

                    if (extendedBounds.Contains(gridTest.X, gridTest.Y))
                    {
                        //Debug.WriteLine("Adding Rect to Grid Array...");
                        bufferingGrid.Add(gridTest);
                    }
                    //if (!extendedBounds.Contains(gridTest.X, gridTest.Y))
                    //{
                    //Debug.WriteLine("Adding Rect to Grid Array...");
                    //  bufferingGrid.Remove(gridTest);
                    //}
                }
            }
        }

        public void UpdateMarkers()
        {
            markerWorkerState = MarkerWorkerStates.UpdatingMarkers;

            RectangleF extendedBounds = new RectangleF(renderingBounds.Left - 100, renderingBounds.Top - 100, renderingBounds.Right + 100, renderingBounds.Bottom + 100);

            foreach (JMapButton btn in mapButtons)
            {
                if (extendedBounds.Contains(btn.renderLoc.X, btn.renderLoc.Y))
                {
                    if (!bufferingMapButtons.Contains(btn))
                        bufferingMapButtons.Add(btn);
                }
                else if (!extendedBounds.Contains(btn.renderLoc.X, btn.renderLoc.Y))
                {
                    if (bufferingMapButtons.Contains(btn))
                        bufferingMapButtons.Remove(btn);

                    BeginInvoke(new InvokeDelegate(() => btn.UpdateButton()));
                }

            }
        }

        public void UpdatePlayerPos()
        {
            // PLAYER POSITION
            mfocus = this.FocusMobile.Position;

            if (mapRotated)
            {
                bgRot = RotatePointF(bgReg, bgReg, 45);

                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));

                pntPlayer = new PointF(Convert.ToInt32(Math.Floor((double)((mfocus.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(mfocus.Y * offset.Y) + zeroPoint.Y)));
                pntPlayer = RotatePointF(pntPlayer, zeroPoint, 45);
            }
            else if (!mapRotated)
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgReg.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgReg.Y));

                pntPlayer = new PointF(Convert.ToInt32(Math.Floor((double)((mfocus.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(mfocus.Y * offset.Y) + zeroPoint.Y)));
            }


        }

        public void TrackPlayer()
        {
            float targetX = mapWidth + (pntPlayer.X - mapWidth);
            float targetY = mapHeight + (pntPlayer.Y - mapHeight);

            float newX = (bgReg.X - targetX) + (renderArea.Right / 2);
            float newY = (bgReg.Y - targetY) + (renderArea.Bottom / 2);

            if(IsTrackPlayerPosition)
            {
                bgReg.X = Convert.ToInt32(Math.Floor((double)newX));
                bgReg.Y = Convert.ToInt32(Math.Floor((double)newY));
            }

            UpdateAll();
        }

        public void UpdateMap()
        {
            try
            {
                if (InvokeRequired)
                {
                    UpdateMapCallback d = new UpdateMapCallback(UpdateMap);
                    Invoke(d, new object[0]);
                }
                else
                {
                    UpdateAll();
                }
            }
            catch { }
        }

        public void MouseLastPosToMap(PointF p)
        {
            //CONVERSION FOR THE ROTATED MAP IN THIS, IS THE THE STUPIDEST THING EVER, BUT IT WORKS, SO IT STAYS FOR NOW
            if (mapRotated)
            {
                p = RotatePointF(p, zeroPoint, 45);
            }

            float targetX = mapWidth + (p.X - mapWidth);
            float targetY = mapHeight + (p.Y - mapHeight);

            float newX;
            float newY;

            if ((zeroPoint.X - targetX) > 0)
                newX = 0;
            else
                newX = Math.Abs(zeroPoint.X - targetX);

            if ((zeroPoint.Y - targetY) > 0)
                newY = 0;
            else
                newY = Math.Abs(zeroPoint.Y - targetY);

            if (mapRotated)
            {
                if ((zeroPoint.Y - targetY) > 0)
                    newX = 0;
                else
                    newX = Math.Abs(zeroPoint.Y - targetY);

                if ((zeroPoint.X - targetX) < 0)
                    newY = 0;
                else
                    newY = Math.Abs(zeroPoint.X - targetX);
            }

            PointF pnt1 = new PointF(newX + 0.5f, newY + 0.5f);

            mouseLastPosOnMap = new Point(Convert.ToInt32(Math.Floor(pnt1.X / offset.X)), Convert.ToInt32(Math.Floor(pnt1.Y / offset.Y)));
        }

        public void MouseToMap(PointF p)
        {
            //CONVERSION FOR THE ROTATED MAP IN THIS, IS THE THE STUPIDEST THING EVER, BUT IT WORKS, SO IT STAYS FOR NOW
            if (mapRotated)
            {
                p = RotatePointF(p, zeroPoint, 45);
            }

            float targetX = mapWidth + (p.X - mapWidth);
            float targetY = mapHeight + (p.Y - mapHeight);

            float newX;
            float newY;

            if ((zeroPoint.X - targetX) > 0)
                newX = 0;
            else
                newX = Math.Abs(zeroPoint.X - targetX);

            if ((zeroPoint.Y - targetY) > 0)
                newY = 0;
            else
                newY = Math.Abs(zeroPoint.Y - targetY);

            if (mapRotated)
            {
                if ((zeroPoint.Y - targetY) > 0)
                    newX = 0;
                else
                    newX = Math.Abs(zeroPoint.Y - targetY);

                if ((zeroPoint.X - targetX) < 0)
                    newY = 0;
                else
                    newY = Math.Abs(zeroPoint.X - targetX);
            }

            PointF pnt1 = new PointF(newX + 0.5f, newY + 0.5f);

            mouseMapCoord = new Point(Convert.ToInt32(Math.Floor(pnt1.X / offset.X)), Convert.ToInt32(Math.Floor(pnt1.Y / offset.Y)));
        }

        public void RenderMouseCoord()
        {
            //      DRAW MOUSE POS STRING
            if(mouseOnMap && !IsMouseDown)
            {
                Graphics gfx = CreateGraphics();

                //      MOUSE POSITION
                if (Format(mouseMapCoord, Ultima.Map.Felucca, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                {
                    coordBottomString = String.Format("{0}°{1}'{2} {3}°{4}'{5} | ({6},{7})", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W", mouseMapCoord.X, mouseMapCoord.Y); //World.Player.Position.X / Y
                    coordBottomSize = gfx.MeasureString(coordBottomString, m_RegFont);
                }

                gfx.FillRectangle(Brushes.AliceBlue, 6, renderArea.Bottom - (6 + coordBottomSize.Height + 2), coordBottomSize.Width + 2, coordBottomSize.Height + 2);
                gfx.DrawRectangle(Pens.Black, 6, renderArea.Bottom - -(6 + coordBottomSize.Height + 2), coordBottomSize.Width + 2, coordBottomSize.Height + 2);
                gfx.DrawString(coordBottomString, m_RegFont, Brushes.Black, 6, renderArea.Bottom - (6 + coordBottomSize.Height + 2));
                gfx.Dispose();
            }
        }

        public PointF RotatePointF(PointF pointToRotate, PointF centerPoint, float angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            return new PointF
            {
                X = (float)(cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (float)(sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        private ArrayList RegionList(int x, int y, int maxDist)
        {
            int count = m_Regions.Length;
            ArrayList aList = new ArrayList();
            for (int i = 0; i < count; ++i)
            {
                JMap.MapRegion rg1 = this.m_Regions[i];
                if (Utility.Distance((int)rg1.X, (int)rg1.Y, x, y) <= maxDist * 2)
                {
                    aList.Add(rg1);
                }
            }
            return aList;
        }

        public void DeleteMarker(JMapButton btnToDelete)
        {
            mapButtons.Remove(btnToDelete);
            visibleMapButtons.Remove(btnToDelete);
            MarkerToEdit = null;
        }

        public void WriteUpdatedCSV()
        {
            bool firstLine = true;

            foreach (JMapButton btn in mapButtons)
            {
                // Dont save other pins to this file that were from other files
                //if (!btn.id.Equals("MarkedLocations") || !btn.id.Equals("PublicLocations")) 
                //    continue;

                string fileName = btn.id;
                
                float x = btn.mapLoc.X;
                float y = btn.mapLoc.Y;
                string text = btn.displayText;
                string extra = btn.extraText;

                //Format the new line
                string newLine = string.Format($"{x},{y},{text},{extra}");

                if (firstLine)
                    File.WriteAllText($"{Config.GetInstallDirectory()}\\JMap\\" + fileName + ".csv", newLine);
                else
                    File.AppendAllText($"{Config.GetInstallDirectory()}\\JMap\\" + fileName + ".csv", Environment.NewLine + newLine);

                firstLine = false;
            }
        }

        public void SendPublicMarker(PointF markedLoc, string markerOwner, bool IsPublic, string optionalName = "", string optionalExtra = "")
        {
            string shareMarkerData = $"{markedLoc.X},{markedLoc.Y},{optionalName},{optionalExtra}";

            string partyMessage = $"New marker: {shareMarkerData}";

            if(PacketHandlers.Party.Count > 0)
            {
                ClientCommunication.SendToServer(new SendPartyMessage(partyMessage));
            }
        }

        public void AddMarker(PointF markedLoc, string markerOwner, bool IsPublic, string id, string optionalName = "", string optionalExtra = "")
        {
            string fileName = id;

            float xLoc = Convert.ToInt32(Math.Floor(markedLoc.X));
            float yLoc = Convert.ToInt32(Math.Floor(markedLoc.Y));

            float x = xLoc;
            float y = yLoc;
            string text = optionalName;
            string extra = optionalExtra;

            //Format the new line
            string newLine = string.Format($"{x},{y},{text},{extra}");

            Debug.WriteLine($"Writing button to {fileName}");

            if(new FileInfo($"{Config.GetInstallDirectory()}\\JMap\\" + fileName + ".csv").Length == 0)
            {
                File.AppendAllText($"{Config.GetInstallDirectory()}\\JMap\\" + fileName + ".csv", newLine);
            }
            else
            {
                File.AppendAllText($"{Config.GetInstallDirectory()}\\JMap\\" + fileName + ".csv", Environment.NewLine + newLine);
            }
            

            JMapButton btn = UIElements.NewButton(this, JMapButtonType.MapPin, x, y, markerOwner, IsPublic, id, text, extra);

            mapButtons.Add(btn);
            btn.LoadButton();

            UpdateAll();
        }

        public void ReadMarkers(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);

            JMapButtonType type = JMapButtonType.MapPin;
            bool IsPublic = false;

            switch (fileName)
            {
                case "MarkedLocations":
                    type = JMapButtonType.MapPin;
                    IsPublic = false;
                    break;
                case "PublicLocations":
                    type = JMapButtonType.MapPin;
                    IsPublic = true;
                    break;
                case "UOPoints":
                    type = JMapButtonType.UOPoint;
                    IsPublic = false;
                    break;
                case "PlayerHouses":
                    IsPublic = false;
                    type = JMapButtonType.PlayerHouse;
                    break;
                case "TreasureMapLocations":
                    type = JMapButtonType.Treasure;
                    IsPublic = false;
                    break;
            }

            using (StreamReader sr = new StreamReader(path))
            {
                var lines = new List<string[]>();
                int row = 0;
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split(',');
                    lines.Add(line);
                    ++row;
                }

                int i = 0;
                foreach (string[] line in lines)
                {
                    //markedLocations.Add(line);
                    
                    float x = float.Parse(line[0]);
                    float y = float.Parse(line[1]);
                    string text = line[2];
                    string extra = line[3];

                    string markerOwner = this.FocusMobile.Name;
                                                                                                     
                    JMapButton btn = UIElements.NewButton(this, (JMapButtonType)type, x, y, markerOwner, IsPublic, fileName, text, extra);

                    mapButtons.Add(btn);
                    btn.LoadButton();
                    ++i;
                }                    
            }
        }

        public void RemoveMarkers(string id)
        {
            List<JMapButton> markersToRemove = mapButtons.Where(x => x.id.Equals(id)).ToList();

            foreach (JMapButton jMapButton in markersToRemove)
            {
                DeleteMarker(jMapButton);
            }
        }

        public static bool Format(Point p, Ultima.Map map, ref int xLong, ref int yLat, ref int xMins, ref int yMins, ref bool xEast, ref bool ySouth)
        {
            if (map == null)
                return false;

            int x = p.X, y = p.Y;
            int xCenter, yCenter;
            int xWidth, yHeight;

            if (!ComputeMapDetails(map, x, y, out xCenter, out yCenter, out xWidth, out yHeight))
                return false;

            double absLong = (double)((x - xCenter) * 360) / xWidth;
            double absLat = (double)((y - yCenter) * 360) / yHeight;

            if (absLong > 180.0)
                absLong = -180.0 + (absLong % 180.0);

            if (absLat > 180.0)
                absLat = -180.0 + (absLat % 180.0);

            bool east = (absLong >= 0), south = (absLat >= 0);

            if (absLong < 0.0)
                absLong = -absLong;

            if (absLat < 0.0)
                absLat = -absLat;

            xLong = (int)absLong;
            yLat = (int)absLat;

            xMins = (int)((absLong % 1.0) * 60);
            yMins = (int)((absLat % 1.0) * 60);

            xEast = east;
            ySouth = south;

            return true;
        }

        public static bool ComputeMapDetails(Ultima.Map map, int x, int y, out int xCenter, out int yCenter, out int xWidth, out int yHeight)
        {
            xWidth = 5120; yHeight = 4096;

            if (map == Ultima.Map.Trammel || map == Ultima.Map.Felucca)
            {
                if (x >= 0 && y >= 0 && x < 5120 && y < map.Height)
                {
                    xCenter = 1323; yCenter = 1624;
                }
                else if (x >= 5120 && y >= 2304 && x < 6144 && y < map.Height)
                {
                    xCenter = 5936; yCenter = 3112;
                }
                else
                {
                    xCenter = 0; yCenter = 0;
                    return false;
                }
            }
            else if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
            {
                xCenter = 1323; yCenter = 1624;
            }
            else
            {
                xCenter = map.Width / 2; yCenter = map.Height / 2;
                return false;
            }

            return true;
        }

        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                m_Active = value;
                if (value)
                {
                    UpdateAll();
                }
            }
        }

        // Focus Mobile
        public Mobile FocusMobile
        {
            get
            {
                if (m_Focus == null || m_Focus.Deleted || !PacketHandlers.Party.Contains(m_Focus.Serial))
                {
                    if (World.Player == null)
                        return new Mobile(Serial.Zero);
                    else
                        return World.Player;
                }
                return m_Focus;
            }
            set { m_Focus = value; }
        }

        public void Constrain(Control view)
        {
            Rectangle pr = view.ClientRectangle;

            //if(!mapRotated)
            //{
            RectangleF cr = new RectangleF(bgReg.X, bgReg.Y, mapWidth, mapHeight);//  ctl.ClientRectangle;
            float x = Math.Min(0, Math.Max(bgReg.X, pr.Width - cr.Width));
            float y = Math.Min(0, Math.Max(bgReg.Y, pr.Height - cr.Height));

            bgReg.X = Convert.ToInt32(Math.Floor((double)x));
            bgReg.Y = Convert.ToInt32(Math.Floor((double)y));
            //}
            /* //Needs work!!
            if (mapRotated)
            {
                Rectangle cr = new Rectangle(bgReg.X, bgReg.Y, (int)mapWidth, (int)mapHeight);//  ctl.ClientRectangle;
                int x = Math.Min(0, Math.Max(bgReg.X, pr.Width - cr.Width));
                int y = Math.Min(0, Math.Max(bgReg.Y, pr.Height - cr.Height));

                bgReg.X = x;
                bgReg.Y = y;
            }
            */
        }

        public void mapPanel_MouseUp(object sender, EventArgs e)
        {
            jMapMain.Cursor = Cursors.Default;

            IsMouseDown = false;
        }

        public void mapPanel_MouseDown(object sender, EventArgs e)
        {
            MouseEventArgs mouse = e as MouseEventArgs;
            Point p = mouse.Location;

            IsMouseDown = true;

            if (mouse.Button == MouseButtons.Left)
            {
                mouseDown = mouse.Location;
                //MarkerToEdit = null;
            }

            else if (mouse.Button == MouseButtons.Right)
            {
                mouseLastPos = p;
                MouseLastPosToMap(mouseLastPos);

                if(MarkerToEdit == null)
                {
                    ContextOptions.Show(this, p);
                }
                else if (MarkerToEdit != null)
                {
                    ContextMarkerMenu.Show(this, p);
                }
            }
        }

        public void mapPanel_MouseMove(object sender, EventArgs e)
        {
            
            MouseEventArgs mouse = e as MouseEventArgs;
            mousePosNow = mouse.Location;

            int deltaX = mousePosNow.X - mouseDown.X;
            int deltaY = mousePosNow.Y - mouseDown.Y;

            float newX = bgReg.X + deltaX;
            float newY = bgReg.Y + deltaY;

            if (mouse.Button == MouseButtons.Left)
            {
                IsPanning = true;
                trackingPlayer = false;
                jMapMain.Cursor = Cursors.SizeAll;

                bgReg.X = Convert.ToInt32(Math.Floor((double)newX));
                bgReg.Y = Convert.ToInt32(Math.Floor((double)newY));

                UpdatePlayerPos();
                UpdateMapPos();
                UpdateAll();

                mouseDown = mouse.Location;
                IsPanning = false;
            }
            else
            {
                MouseToMap(mousePosNow);
                RenderMouseCoord();
                MouseHoverWorker();
            }

            
        }

        public void mapPanel_OnMouseWheel(object sender, MouseEventArgs e)
        {
            OnMouseWheel(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            IsZooming = true;
            Point mousePos = e.Location;
            PointF oldOffset = offset;          // get the old offset, need this, else bad.           
            int zoomMod = 1;

            if (oldOffset.X >= 0.09f && oldOffset.X < 451f)  //first check, best to not calculate anything if we don't have to
            {
                SuspendLayout();
                SendMessage(Handle, WM_SETREDRAW, false, 0);

                if (offset.X > 1f)
                    zoomMod = (int)offset.X;        //This mod maintains rate of zoom, without it we get big slow downs the further we zoom

                if (e.Delta > 0) //Zoom in
                {
                    if (oldOffset.X < 400f) //max zoom
                    {
                        if (offset.X < 0.5f) //Small map needs smaller values
                        {
                            offset.X += 0.05f;
                            offset.Y += 0.05f;
                        }
                        else
                        {
                            offset.X += 0.25f * zoomMod;
                            offset.Y += 0.25f * zoomMod;
                        }
                    }
                }

                if (e.Delta < 0) //Zoom out
                {
                    if (oldOffset.X > 0.15f) //min zoom           
                    {
                        if (offset.X <= 0.5f) //Small map needs smaller values
                        {
                            offset.X -= 0.05f;
                            offset.Y -= 0.05f;
                        }
                        else //Otherwise just reverse to zoom in
                        {
                            offset.X -= 0.25f * zoomMod;
                            offset.Y -= 0.25f * zoomMod;
                        }
                    }
                }

                zoom.Width = (_mapSize.Width * offset.X);
                zoom.Height = (_mapSize.Height * offset.Y);

                mapWidth = Convert.ToInt32(Math.Floor((double)zoom.Width));
                mapHeight = Convert.ToInt32(Math.Floor((double)zoom.Height));

                bgReg.X = Convert.ToInt32(Math.Floor((double)(mousePos.X - (offset.X / oldOffset.X) * (mousePos.X - zeroPoint.X))));
                bgReg.Y = Convert.ToInt32(Math.Floor((double)(mousePos.Y - (offset.Y / oldOffset.Y) * (mousePos.Y - zeroPoint.Y))));

                SendMessage(Handle, WM_SETREDRAW, true, 0);

                UpdatePlayerPos();
                
                if (trackingPlayer)
                    TrackPlayer();
                else
                    UpdateAll();

                ResumeLayout();

                if (offset.X < 0.1f || offset.Y < 0.1f)
                {
                    offset.X = 0.1f;
                    offset.Y = 0.1f;
                    Debug.WriteLine("Undersized, Resetting to minimum.");
                }
                if (offset.X > 450f || offset.Y > 450f)
                {
                    offset.X = 450f;
                    offset.Y = 450f;
                    Debug.WriteLine("Oversized, Resetting to maximum.");
                }

            }
            IsZooming = false;
        }     

        public void mapPanel_DoubleClick(object sender, MouseEventArgs e)
        {
            MouseEventArgs mouse = e as MouseEventArgs;

            if (mouse.Button == MouseButtons.Left)
            {
                Debug.WriteLine("MouseDoubleClick");
                
                    if (jMapMain.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable)
                        jMapMain.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    else 
                        jMapMain.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;              
            }
        }

        private void mapPanel_Tilt(object sender, EventArgs e)
        {
            mapRotated = TiltMap45.Checked;
            
            tiltChanged = true;

            Config.SetProperty("MapTilt", mapRotated);

            UpdatePlayerPos();
            UpdateMapPos();

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();

            UpdateAll(); //extra coz gay
        }

        private void mapPanel_OverlaysGuard(object sender, EventArgs e)
        {
            HasGuardLines = Menu_OverlaysGuard.Checked;

            Config.SetProperty("MapGuardLines", HasGuardLines);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_OverlaysGrid(object sender, EventArgs e)
        {
            HasGridLines = Menu_OverlaysGrid.Checked;
            
            Config.SetProperty("MapGridLines", HasGridLines);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_OverlaysAll(object sender, EventArgs e)
        {
            if (Menu_OverlaysAll.Checked)
            {
                this.Menu_OverlaysGuard.CheckState = CheckState.Checked;
                HasGuardLines = true;
                this.Menu_OverlaysGrid.CheckState = CheckState.Checked;
                HasGridLines = true;

                Config.SetProperty("MapGridLines", HasGridLines);
                Config.SetProperty("MapGuardLines", HasGuardLines);

                HasAllOverlays = true;
            }
            else
            {
                this.Menu_OverlaysGuard.CheckState = CheckState.Checked;
                HasGuardLines = true;
                this.Menu_OverlaysGrid.CheckState = CheckState.Checked;
                HasGridLines = true;

                Config.SetProperty("MapGridLines", HasGridLines);
                Config.SetProperty("MapGuardLines", HasGuardLines);

                HasAllOverlays = true;
            }

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_ShowAllPositions(object sender, EventArgs e)
        {
            if (IsShowAllPositions)
            {
                IsShowAllPositions = false;

                Menu_ShowPartyPositions.Checked = false;
                Menu_ShowPetPositions.Checked = false;
                Menu_ShowPlayerPosition.Checked = false;
                Menu_TrackPlayerPosition.Checked = false;

                Config.SetProperty("MapShowPlayerPosition", false);
                Config.SetProperty("MapShowPetPositions", false);
                Config.SetProperty("MapShowPartyPositions", false);
                Config.SetProperty("MapTrackPlayerPosition", false);
            }
            else
            {
                IsShowAllPositions = true;

                Menu_ShowPartyPositions.Checked = true;
                Menu_ShowPetPositions.Checked = true;
                Menu_ShowPlayerPosition.Checked = true;
                Menu_TrackPlayerPosition.Checked = true;

                Config.SetProperty("MapShowPlayerPosition", true);
                Config.SetProperty("MapShowPetPositions", true);
                Config.SetProperty("MapShowPartyPositions", true);
                Config.SetProperty("MapTrackPlayerPosition", true);
            }

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_ShowPlayerPosition(object sender, EventArgs e)
        {
            IsShowPlayerPosition = Menu_ShowPlayerPosition.Checked;

            Config.SetProperty("MapShowPlayerPosition", IsShowPlayerPosition);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_ShowPetPositions(object sender, EventArgs e)
        {
            IsShowPetPositions = Menu_ShowPetPositions.Checked;

            Config.SetProperty("MapShowPetPositions", IsShowPetPositions);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_ShowPartyPositions(object sender, EventArgs e)
        {
            IsShowPartyPositions = Menu_ShowPartyPositions.Checked;

            Config.SetProperty("MapShowPartyPositions", IsShowPartyPositions);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_IsTrackPlayerPosition(object sender, EventArgs e)
        {
            IsTrackPlayerPosition = Menu_TrackPlayerPosition.Checked;

            Config.SetProperty("MapTrackPlayerPosition", IsTrackPlayerPosition);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_AddMapMarker(object sender, EventArgs e)
        {
            AddingMarker = true;
            Form newMarker = new NewMarker(this);

            newMarker.Show();
            
            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_EditMapMarker(object sender, EventArgs e)
        {
            EditingMarker = true;
            Form newMarker = new NewMarker(this);

            newMarker.Show();


            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_DeleteMapMarker(object sender, EventArgs e)
        {
            DeleteMarker(MarkerToEdit);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_MarkerNames(object sender, EventArgs e)
        {
            DisplayMarkerNames = Menu_MarkerNames.Checked;

            Config.SetProperty("DisplayMarkerNames", DisplayMarkerNames);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        private void mapPanel_MarkerCoords(object sender, EventArgs e)
        {
            DisplayMarkerCoords = Menu_MarkerCoords.Checked;

            Config.SetProperty("DisplayMarkerCoords", DisplayMarkerCoords);

            if (trackingPlayer)
                TrackPlayer();
            else
                UpdateAll();
        }

        public void mapPanel_Exit(object sender, EventArgs e)
        {           
            jMapMain.Close();
        }

        public void mapPanel_MouseEnter(object sender, EventArgs e)
        {
            mouseOnMap = true;
 
            if (!jMapMain.Focused && !AddingMarker && !EditingMarker)
            {
                jMapMain.Focus();
                jMapMain.Activate();
            }             
        }

        public void mapPanel_MouseLeave(object sender, EventArgs e)
        {
            mouseOnMap = false;
        }

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

                if (borderTopLeft.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;
                else if (borderTopRight.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;
                else if (borderBottomLeft.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;
                else if (borderBottomRight.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;

                else if (borderTop.Contains(cursor))message.Result = (IntPtr)HTTRANSPARENT;
                else if (borderLeft.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;
                else if (borderRight.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;
                else if (borderBottom.Contains(cursor)) message.Result = (IntPtr)HTTRANSPARENT;
            }

            //if(main != null)
            //main.ResizeRelay(ref message); // PASS THE RESIZE MESSAGE TO THE MAIN WINDOW
        }


        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ContextOptions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.AddMapMarker = new System.Windows.Forms.ToolStripMenuItem();
            this.SeperatorOptions = new System.Windows.Forms.ToolStripSeparator();
            this.OptionPositions = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_ShowAllPositions = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_ShowPlayerPosition = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_ShowPartyPositions = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_ShowPetPositions = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_TrackPlayerPosition = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionOverlays = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_OverlaysAll = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_OverlaysGuard = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_OverlaysGrid = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionAlignments = new System.Windows.Forms.ToolStripMenuItem();
            this.TiltMap45 = new System.Windows.Forms.ToolStripMenuItem();
            this.SeperatorOptions2 = new System.Windows.Forms.ToolStripSeparator();
            this.OptionExit = new System.Windows.Forms.ToolStripMenuItem();
            this.ContextMarkerMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Menu_EditMarker = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_DeleteMarker = new System.Windows.Forms.ToolStripMenuItem();
            this.MapGenProgressBar = new System.Windows.Forms.ProgressBar();
            this.OptionMarkers = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_MarkerNames = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_MarkerCoords = new System.Windows.Forms.ToolStripMenuItem();
            this.ContextOptions.SuspendLayout();
            this.ContextMarkerMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ContextOptions
            // 
            this.ContextOptions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AddMapMarker,
            this.SeperatorOptions,
            this.OptionPositions,
            this.OptionMarkers,
            this.OptionOverlays,
            this.OptionAlignments,
            this.SeperatorOptions2,
            this.OptionExit});
            this.ContextOptions.Name = "ContextOptions";
            this.ContextOptions.Size = new System.Drawing.Size(181, 170);
            // 
            // AddMapMarker
            // 
            this.AddMapMarker.Name = "AddMapMarker";
            this.AddMapMarker.Size = new System.Drawing.Size(180, 22);
            this.AddMapMarker.Text = "Add Marker";
            this.AddMapMarker.Click += new System.EventHandler(this.mapPanel_AddMapMarker);
            // 
            // SeperatorOptions
            // 
            this.SeperatorOptions.Name = "SeperatorOptions";
            this.SeperatorOptions.Size = new System.Drawing.Size(177, 6);
            // 
            // OptionPositions
            // 
            this.OptionPositions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Menu_ShowAllPositions,
            this.Menu_ShowPlayerPosition,
            this.Menu_ShowPartyPositions,
            this.Menu_ShowPetPositions,
            this.Menu_TrackPlayerPosition});
            this.OptionPositions.Name = "OptionPositions";
            this.OptionPositions.Size = new System.Drawing.Size(180, 22);
            this.OptionPositions.Text = "Positions";
            // 
            // Menu_ShowAllPositions
            // 
            this.Menu_ShowAllPositions.Checked = true;
            this.Menu_ShowAllPositions.CheckOnClick = true;
            this.Menu_ShowAllPositions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_ShowAllPositions.Name = "Menu_ShowAllPositions";
            this.Menu_ShowAllPositions.Size = new System.Drawing.Size(184, 22);
            this.Menu_ShowAllPositions.Text = "Show All";
            this.Menu_ShowAllPositions.CheckedChanged += new System.EventHandler(this.mapPanel_ShowAllPositions);
            // 
            // Menu_ShowPlayerPosition
            // 
            this.Menu_ShowPlayerPosition.Checked = true;
            this.Menu_ShowPlayerPosition.CheckOnClick = true;
            this.Menu_ShowPlayerPosition.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_ShowPlayerPosition.Name = "Menu_ShowPlayerPosition";
            this.Menu_ShowPlayerPosition.Size = new System.Drawing.Size(184, 22);
            this.Menu_ShowPlayerPosition.Text = "Show Player Position";
            this.Menu_ShowPlayerPosition.CheckedChanged += new System.EventHandler(this.mapPanel_ShowPlayerPosition);
            // 
            // Menu_ShowPartyPositions
            // 
            this.Menu_ShowPartyPositions.Checked = true;
            this.Menu_ShowPartyPositions.CheckOnClick = true;
            this.Menu_ShowPartyPositions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_ShowPartyPositions.Name = "Menu_ShowPartyPositions";
            this.Menu_ShowPartyPositions.Size = new System.Drawing.Size(184, 22);
            this.Menu_ShowPartyPositions.Text = "Show Party Positions";
            this.Menu_ShowPartyPositions.CheckedChanged += new System.EventHandler(this.mapPanel_ShowPartyPositions);
            // 
            // Menu_ShowPetPositions
            // 
            this.Menu_ShowPetPositions.Checked = true;
            this.Menu_ShowPetPositions.CheckOnClick = true;
            this.Menu_ShowPetPositions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_ShowPetPositions.Name = "Menu_ShowPetPositions";
            this.Menu_ShowPetPositions.Size = new System.Drawing.Size(184, 22);
            this.Menu_ShowPetPositions.Text = "Show Pet Positions";
            this.Menu_ShowPetPositions.CheckedChanged += new System.EventHandler(this.mapPanel_ShowPetPositions);
            // 
            // Menu_TrackPlayerPosition
            // 
            this.Menu_TrackPlayerPosition.Checked = true;
            this.Menu_TrackPlayerPosition.CheckOnClick = true;
            this.Menu_TrackPlayerPosition.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_TrackPlayerPosition.Name = "Menu_TrackPlayerPosition";
            this.Menu_TrackPlayerPosition.Size = new System.Drawing.Size(184, 22);
            this.Menu_TrackPlayerPosition.Text = "Track Player Position";
            this.Menu_TrackPlayerPosition.CheckedChanged += new System.EventHandler(this.mapPanel_IsTrackPlayerPosition);
            // 
            // OptionOverlays
            // 
            this.OptionOverlays.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Menu_OverlaysAll,
            this.Menu_OverlaysGuard,
            this.Menu_OverlaysGrid});
            this.OptionOverlays.Name = "OptionOverlays";
            this.OptionOverlays.Size = new System.Drawing.Size(180, 22);
            this.OptionOverlays.Text = "Overlays";
            // 
            // Menu_OverlaysAll
            // 
            this.Menu_OverlaysAll.Checked = true;
            this.Menu_OverlaysAll.CheckOnClick = true;
            this.Menu_OverlaysAll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_OverlaysAll.Name = "Menu_OverlaysAll";
            this.Menu_OverlaysAll.Size = new System.Drawing.Size(136, 22);
            this.Menu_OverlaysAll.Text = "All";
            this.Menu_OverlaysAll.CheckedChanged += new System.EventHandler(this.mapPanel_OverlaysAll);
            // 
            // Menu_OverlaysGuard
            // 
            this.Menu_OverlaysGuard.Checked = true;
            this.Menu_OverlaysGuard.CheckOnClick = true;
            this.Menu_OverlaysGuard.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_OverlaysGuard.Name = "Menu_OverlaysGuard";
            this.Menu_OverlaysGuard.Size = new System.Drawing.Size(136, 22);
            this.Menu_OverlaysGuard.Text = "Guard Lines";
            this.Menu_OverlaysGuard.CheckedChanged += new System.EventHandler(this.mapPanel_OverlaysGuard);
            // 
            // Menu_OverlaysGrid
            // 
            this.Menu_OverlaysGrid.Checked = true;
            this.Menu_OverlaysGrid.CheckOnClick = true;
            this.Menu_OverlaysGrid.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_OverlaysGrid.Name = "Menu_OverlaysGrid";
            this.Menu_OverlaysGrid.Size = new System.Drawing.Size(136, 22);
            this.Menu_OverlaysGrid.Text = "Grid";
            this.Menu_OverlaysGrid.CheckedChanged += new System.EventHandler(this.mapPanel_OverlaysGrid);
            // 
            // OptionAlignments
            // 
            this.OptionAlignments.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TiltMap45});
            this.OptionAlignments.Name = "OptionAlignments";
            this.OptionAlignments.Size = new System.Drawing.Size(180, 22);
            this.OptionAlignments.Text = "Alignment";
            // 
            // TiltMap45
            // 
            this.TiltMap45.Checked = true;
            this.TiltMap45.CheckOnClick = true;
            this.TiltMap45.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TiltMap45.Name = "TiltMap45";
            this.TiltMap45.Size = new System.Drawing.Size(138, 22);
            this.TiltMap45.Text = "Tilt Map 45°";
            this.TiltMap45.CheckedChanged += new System.EventHandler(this.mapPanel_Tilt);
            // 
            // SeperatorOptions2
            // 
            this.SeperatorOptions2.Name = "SeperatorOptions2";
            this.SeperatorOptions2.Size = new System.Drawing.Size(177, 6);
            // 
            // OptionExit
            // 
            this.OptionExit.Name = "OptionExit";
            this.OptionExit.Size = new System.Drawing.Size(180, 22);
            this.OptionExit.Text = "Exit";
            this.OptionExit.Click += new System.EventHandler(this.mapPanel_Exit);
            // 
            // ContextMarkerMenu
            // 
            this.ContextMarkerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Menu_EditMarker,
            this.Menu_DeleteMarker});
            this.ContextMarkerMenu.Name = "ContextOptions";
            this.ContextMarkerMenu.Size = new System.Drawing.Size(148, 48);
            // 
            // Menu_EditMarker
            // 
            this.Menu_EditMarker.Name = "Menu_EditMarker";
            this.Menu_EditMarker.Size = new System.Drawing.Size(147, 22);
            this.Menu_EditMarker.Text = "Edit Marker";
            this.Menu_EditMarker.Click += new System.EventHandler(this.mapPanel_EditMapMarker);
            // 
            // Menu_DeleteMarker
            // 
            this.Menu_DeleteMarker.Name = "Menu_DeleteMarker";
            this.Menu_DeleteMarker.Size = new System.Drawing.Size(147, 22);
            this.Menu_DeleteMarker.Text = "Delete Marker";
            this.Menu_DeleteMarker.Click += new System.EventHandler(this.mapPanel_DeleteMapMarker);
            // 
            // MapGenProgressBar
            // 
            this.MapGenProgressBar.Location = new System.Drawing.Point(0, 0);
            this.MapGenProgressBar.Name = "MapGenProgressBar";
            this.MapGenProgressBar.Size = new System.Drawing.Size(100, 23);
            this.MapGenProgressBar.Step = 1;
            this.MapGenProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.MapGenProgressBar.TabIndex = 0;
            // 
            // OptionMarkers
            // 
            this.OptionMarkers.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Menu_MarkerNames,
            this.Menu_MarkerCoords});
            this.OptionMarkers.Name = "OptionMarkers";
            this.OptionMarkers.Size = new System.Drawing.Size(180, 22);
            this.OptionMarkers.Text = "Markers";
            // 
            // Menu_MarkerNames
            // 
            this.Menu_MarkerNames.Checked = true;
            this.Menu_MarkerNames.CheckOnClick = true;
            this.Menu_MarkerNames.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_MarkerNames.Name = "Menu_MarkerNames";
            this.Menu_MarkerNames.Size = new System.Drawing.Size(193, 22);
            this.Menu_MarkerNames.Text = "Display Marker Names";
            this.Menu_MarkerNames.CheckedChanged += new System.EventHandler(this.mapPanel_MarkerNames);
            // 
            // Menu_MarkerCoords
            // 
            this.Menu_MarkerCoords.Checked = true;
            this.Menu_MarkerCoords.CheckOnClick = true;
            this.Menu_MarkerCoords.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Menu_MarkerCoords.Name = "Menu_MarkerCoords";
            this.Menu_MarkerCoords.Size = new System.Drawing.Size(193, 22);
            this.Menu_MarkerCoords.Text = "Display Marker Coords";
            this.Menu_MarkerCoords.CheckedChanged += new System.EventHandler(this.mapPanel_MarkerCoords);
            // 
            // MapPanel
            // 
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Size = new System.Drawing.Size(400, 400);
            this.ContextOptions.ResumeLayout(false);
            this.ContextMarkerMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }

    public static class PublicMarkers
    {
        public static MapPanel mapPanel { get; set; }

        public static void ReceivePublicMarker(PointF markedLoc, string markerOwner, bool IsPublic, string id, string optionalName = "", string optionalExtra = "")
        {
            mapPanel.AddMarker(markedLoc, markerOwner, IsPublic, id, optionalName, optionalExtra);
        }
    }

}
