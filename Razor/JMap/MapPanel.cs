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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant;
using Assistant.Properties;
using Ultima;

namespace Assistant.JMap
{
    public class MapPanel : Panel
    {
        delegate void UpdateMapCallback();

        public JimmyMap main;

        public TileDisplay tileDisplay;
        public Tile[] lTiles = new Tile[64];
        public HuedTile[][][] sTiles = new HuedTile[64][][];
        public Ultima.Map map;

        private Point mouseDown;
        private float _zoom = 1;

        private bool m_Active;
        private MapRegion[] m_Regions;
        //private ArrayList m_MapButtons;
        private Point prevPoint;
        private Mobile m_Focus;
        private const double RotateAngle = Math.PI / 4 + Math.PI;
        private DateTime LastRefresh;

        protected Point clickPosition;
        protected Point lastPosition;

        public SizeF zoom;

        #region MAPS

        public bool drawTiles = true;
        public bool tilesDrawing = false;
        public Bitmap TileMap;

        public Bitmap mapBuffer;

        public Bitmap _mapRegular_1;
        public Bitmap _mapRegular_2;
        public Bitmap _mapRegular_4;
        public Bitmap _mapRegular_8;

        public Image _mapRotated_1;
        public Bitmap _mapRotated_2;
        public Bitmap _mapRotated_4;
        public Bitmap _mapRotated_8;

        //public Bitmap _mapHeight_1;

        public Size _mapSize;

        public Image _oldBackground;

        public float bgLeft = 0;
        public float bgTop = 0;
        public float bgWidth = 0;
        public float bgHeight = 0;

        public float bgLeftBuf = 0;
        public float bgTopBuf = 0;

        public PointF offset = new PointF(1, 1);
        //public float offsetY = 1;

        public PointF bgRot;

        public bool mapRotated = true;
        public PointF zeroPoint;
        public Point mouseLastPos; //used for tilt via context menu / hotkey
        public bool tiltChanged = false;

        public float mapWidth;
        public float mapHeight;

        public GraphicsState primaryState;
        public GraphicsState tileUprightState;
        public GraphicsState tileRotatedState;

        RectangleF renderingBounds;

        bool IsPanning = false;
        bool IsZooming = false;

        StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        // UO Styled Art
        static Bitmap borderH = new Bitmap(@"F:\UOStuff\UOArt\Maps\borderH.bmp");
        //TextureBrush borderHBrush = new TextureBrush(borderH, WrapMode.Tile, new Rectangle(0, 0, borderH.Width, borderH.Height));
        static Bitmap borderV = new Bitmap(@"F:\UOStuff\UOArt\Maps\borderV.bmp");
        //TextureBrush borderVBrush = new TextureBrush(borderV, WrapMode.Tile, new Rectangle(0, 0, borderV.Width, borderV.Height));

        //Cursor mapPin = Markers.LoadCursor(@"F:\UOStuff\UOArt\Maps\Resources\Markers\mapPin3.cur");

        //Bitmap mapPinBmp = new Bitmap(@"F:\UOStuff\UOArt\Maps\Resources\Markers\mapPinA.png");

        ArrayList markedLocations = new ArrayList();

        //in progress
        ArrayList mapButtons = new ArrayList();

        // MAP BORDER
        public RectangleF borderTop { get { return new RectangleF(renderingBounds.Left, renderingBounds.Top, renderingBounds.Right, 4); } }
        public RectangleF borderBottom { get { return new RectangleF(renderingBounds.Left, renderingBounds.Bottom - 4, renderingBounds.Right, 4); } }
        public RectangleF borderLeft { get { return new RectangleF(renderingBounds.Left, renderingBounds.Top + 4, 5, renderingBounds.Bottom); } }
        public RectangleF borderRight { get { return new RectangleF(renderingBounds.Right - 5, renderingBounds.Top + 4, 5, renderingBounds.Bottom); } }
        public RectangleF borderTopLeft { get { return new RectangleF(0, 0, 5, 4); } }
        public RectangleF borderTopRight { get { return new RectangleF(renderingBounds.Right - 5, 0, 5, 4); } }
        public RectangleF borderBottomLeft { get { return new RectangleF(0, renderingBounds.Bottom - 4, 5, 4); } }
        public RectangleF borderBottomRight { get { return new RectangleF(renderingBounds.Right - 5, renderingBounds.Bottom - 4, 5, 4); } }

        // Designer Stuff
        private System.ComponentModel.IContainer components;
        private ContextMenuStrip ContextOptions;
        private ToolStripSeparator SeperatorOptions;
        private ToolStripMenuItem OptionOverlays;
        private ToolStripMenuItem Menu_OverlaysAll;
        private ToolStripMenuItem Menu_OverlaysGuard;
        private ToolStripMenuItem Menu_OverlaysGrid;
        private ToolStripMenuItem OptionExit;
        private ToolStripMenuItem OptionTilt;

        #endregion

        #region ONPAINT VARIABLES

        public bool trackingPlayer = true;
        public Point3D mfocus;
        public PointF pntPlayer;
        public PointF playerRot;

        RectangleF renderArea;
        PointF compassLoc;

        ArrayList regions = new ArrayList();
        ArrayList mButtons = new ArrayList();

        Pen linePen = new Pen(Brushes.Silver, 2);

        SizeF coordTopSize;
        string coordTopString;

        int xLong = 0, yLat = 0;
        int xMins = 0, yMins = 0;
        bool xEast = false, ySouth = false;
        #endregion


        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        public MapPanel()
        {
            zoom.Width = 1;
            zoom.Height = 1;

            Debug.WriteLine("MapPanel Loaded!");

            //SET SOME THINGS UP
            //typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty
            //| BindingFlags.Instance | BindingFlags.NonPublic, null,
            //this, new object[] { true });
            

            ContextMenuStrip = ContextOptions;

            DoubleBuffered = true;

            //SetStyle(ControlStyles.ContainerControl, false);

            SetStyle(
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer|
                    ControlStyles.SupportsTransparentBackColor, 
                    true);

            

            HorizontalScroll.Visible = false;
            VerticalScroll.Visible = false;

            this.MouseEnter += new EventHandler(mapPanel_MouseEnter);
            //MouseLeave += new EventHandler(mapPanel_MouseLeave); //not working as intended
            this.MouseDown += new MouseEventHandler(mapPanel_MouseDown);
            this.MouseUp += new MouseEventHandler(mapPanel_MouseUp);
            this.MouseMove += new MouseEventHandler(mapPanel_MouseMove);
            this.MouseDoubleClick += new MouseEventHandler(mapPanel_DoubleClick);
            this.MouseWheel += new MouseEventHandler(mapPanel_OnMouseWheel);

            //Paint += new PaintEventHandler(mapPanel_Paint);
            //Paint += new PaintEventHandler(mapPanel_OnPaintBackground);



            //LOAD UP THE MAP
            
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

            m_Regions = JMap.MapRegion.Load("guardlines.def");
            Debug.WriteLine("MapPanel Loaded Guardlines!");



            InitializeComponent();

            ReadMarkers("markedLocations.csv");
            LoadMapButtons();

            // PLAYER POSITION
            //focus = this.FocusMobile.Position;
            //pntPlayer = new PointF(((focus.X * offset.X) + zeroPoint.X), (focus.Y * offset.Y) + zeroPoint.Y);

            compassLoc = new PointF(renderArea.Right - 20, renderArea.Top + 20);

            

            Active = true;

            Invalidate(true);
            UpdateAll();

            if (trackingPlayer)
                TrackPlayer(pntPlayer);

            Invalidate(true);
        }



        // IMPORT MAPS
        public Bitmap ImportMaps(int zLevel)
        {
            Debug.WriteLine("IMPORTING");

            // GET MAPS
            if (BackgroundImage != null)
                BackgroundImage.Dispose();

            switch (zLevel)
            {
                case 1:
                    _mapRegular_1 = new Bitmap(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-1.BMP", true);
                    //_mapHeight_1 = new Bitmap(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-1-HM.BMP", true);
                    return _mapRegular_1;
                case 2:
                    _mapRegular_2 = new Bitmap(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-2.BMP", true);
                    return _mapRegular_2;
                case 4:
                    _mapRegular_4 = new Bitmap(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-4.BMP", true);
                    return _mapRegular_4;
                case 8:
                    _mapRegular_8 = new Bitmap(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-8.BMP", true);
                    return _mapRegular_8;
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

        /*public override void Refresh()
        {
            TimeSpan now = DateTime.UtcNow - LastRefresh;
            if (now.TotalMilliseconds <= 100 && !IsPanning && !IsZooming)
                return;
            LastRefresh = DateTime.UtcNow;
            base.Refresh();
        }*/

        private static Font m_BoldFont = new Font("Courier New", 8, FontStyle.Bold);
        private static Font m_SmallFont = new Font("Arial", 6);
        private static Font m_RegFont = new Font("Arial", 8);

        public Tuple<string, Color> MobParams(Mobile mob)
        {
            string str = "";
            Color col = Color.Silver;

            if(mob.IsGhost)
            {
                col = Color.DarkGray;
                str = "(Ghost) ";
            }
            else if(mob.Poisoned)
            {
                col = Color.Green;
                str = "";
            }

            return Tuple.Create<string, Color>(str, col);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            //Debug.WriteLine("OnPaint");
            try
            {
                if (Active)
                {
                    renderingBounds = pe.Graphics.VisibleClipBounds;

                    pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    //pe.Graphics.CompositingMode = CompositingMode.SourceCopy;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.None;
                    pe.Graphics.SmoothingMode = SmoothingMode.None;
                    pe.Graphics.PageUnit = GraphicsUnit.Pixel;
                    pe.Graphics.PageScale = 1f;

                    // PLAYER MARKERS
                    // Circle with dot
                    pe.Graphics.DrawEllipse(Pens.Silver, pntPlayer.X - 5, pntPlayer.Y - 5, 10, 10);
                    pe.Graphics.FillRectangle(Brushes.Silver, pntPlayer.X, pntPlayer.Y, 1, 1);
                    // Crosshair
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X - 4, pntPlayer.Y, pntPlayer.X + 4, pntPlayer.Y);
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X, pntPlayer.Y - 4, pntPlayer.X, pntPlayer.Y + 4);


                    /* // COMPASS - NO TRANSFORM
                    PointF north = new PointF(compassLoc.X, compassLoc.Y - 6);
                    PointF south = new PointF(compassLoc.X, compassLoc.Y + 6);
                    PointF east = new PointF(compassLoc.X + 6, compassLoc.Y);
                    PointF west = new PointF(compassLoc.X - 6, compassLoc.Y);

                    if (mapRotated)
                    {
                        north = RotatePointF(north, compassLoc, 45);
                        south = RotatePointF(south, compassLoc, 45);
                        east = RotatePointF(east, compassLoc, 45);
                        west = RotatePointF(west, compassLoc, 45);
                    }

                    pe.Graphics.DrawLine(Pens.Silver, north, south);
                    pe.Graphics.DrawLine(Pens.Silver, east, west);
                    pe.Graphics.DrawString("W", m_BoldFont, Brushes.Red, RotatePointF(new PointF(west.X - 6, west.Y), new PointF(west.X - 6, west.Y), 45), stringFormat);
                    pe.Graphics.DrawString("E", m_BoldFont, Brushes.Red, RotatePointF(new PointF(east.X + 6, east.Y), new PointF(east.X + 6, east.Y), 45), stringFormat);
                    pe.Graphics.DrawString("N", m_BoldFont, Brushes.Red, RotatePointF(new PointF(north.X, north.Y - 6), new PointF(north.X, north.Y - 6), 45), stringFormat);
                    pe.Graphics.DrawString("S", m_BoldFont, Brushes.Red, RotatePointF(new PointF(south.X, south.Y + 6), new PointF(south.X, south.Y + 6), 45), stringFormat);
                    */ // END COMPASS - NO TRANSFORM

                    if (mapRotated)
                    {
                        //bgRot = new PointF(bgLeft, bgTop);
                        //bgRot = RotatePointF(bgRot, bgRot, 45);
                        zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                        zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));
                        //zeroPoint.X = bgRot.X;
                        //zeroPoint.Y = bgRot.Y;
                    }
                    else
                    {
                        zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgLeft));
                        zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgTop));
                        //zeroPoint.X = bgLeft;
                        //zeroPoint.Y = bgTop;
                    }

                    //foreach (KeyValuePair<Serial, Mobile> m in World.Mobiles)
                    foreach (Serial s in PacketHandlers.Party)
                    {
                        
                        Mobile mob = World.FindMobile(s);
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

                        
                        name = MobParams(mob).Item1 + name;

                        SizeF strLength = pe.Graphics.MeasureString(name, m_RegFont);

                        Brush mobBrush = new SolidBrush(MobParams(mob).Item2);

                        PointF drawPoint = new PointF(Convert.ToInt32(Math.Floor((double)((mob.Position.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(mob.Position.Y * offset.Y) + zeroPoint.Y)));
                        PointF pointDisplay = drawPoint;
                        PointF stringDisplay = drawPoint;

                        if (mapRotated)
                        {
                            pointDisplay = RotatePointF(drawPoint, pntPlayer, 45);
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
                            pe.Graphics.FillRectangle(mobBrush, pointDisplay.X, pointDisplay.Y, 4f, 4f); // Location

                            SizeF nameSize = pe.Graphics.MeasureString(name, m_RegFont);
                            using (SolidBrush nb = new SolidBrush(Color.FromArgb(96, 0, 0, 128))) // Transparent background box
                            {
                                pe.Graphics.FillRectangle(nb, (stringDisplay.X - 2) - (nameSize.Width / 2), (stringDisplay.Y - 12f) - (nameSize.Height / 2), nameSize.Width + 4, nameSize.Height + 4);
                                //pe.Graphics.DrawRectangle(Pens.Black, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);

                               
                            }

                            pe.Graphics.DrawString(name, m_RegFont, mobBrush, stringDisplay.X, stringDisplay.Y - 10f, stringFormat); // Name
                        }
                    }

                    /*
                    if(mapRotated)
                    {
                        Matrix mtx = new Matrix();
                        mtx.RotateAt(45, pntPlayer, MatrixOrder.Append);
                        pe.Graphics.MultiplyTransform(mtx);

                        mtx.Dispose();
                    }
                    */

                    //DEBUG CENTER LINE
                    //pe.Graphics.DrawLine(Pens.Green, renderArea.Width / 2, renderArea.Top, renderArea.Width / 2, renderArea.Bottom);
                    //pe.Graphics.DrawLine(Pens.Green, renderArea.Left, renderArea.Height / 2, renderArea.Right, renderArea.Height / 2);

                    foreach (JMap.MapRegion region in regions)
                    {
                        PointF tl = new PointF(Convert.ToInt32(Math.Floor((double)((region.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(region.Y * offset.Y) + zeroPoint.Y)));
                        PointF tr = new PointF(Convert.ToInt32(Math.Floor((double)(((region.X + region.Width) * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(region.Y * offset.Y) + zeroPoint.Y)));
                        PointF bl = new PointF(Convert.ToInt32(Math.Floor((double)((region.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)((region.Y + region.Length) * offset.Y) + zeroPoint.Y)));
                        PointF br = new PointF(Convert.ToInt32(Math.Floor((double)(((region.X + region.Width) * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)((region.Y + region.Length) * offset.Y) + zeroPoint.Y)));

                        if(mapRotated)
                        {
                            tl = RotatePointF(tl, pntPlayer, 45);
                            tr = RotatePointF(tr, pntPlayer, 45);
                            br = RotatePointF(br, pntPlayer, 45);
                            bl = RotatePointF(bl, pntPlayer, 45);
                        }



                        PointF[] rgn = new PointF[] { tl, tr, br, bl };

                        pe.Graphics.DrawPolygon(Pens.Green, rgn);
                    }
         //FUNCTIONAL STATIC, NON INTERACTIVE, MAP MARKERS
                    /*foreach (string[] s in markedLocations)
                    {
                        string name = s[0];
                        float.TryParse(s[1], out float x);
                        float.TryParse(s[2], out float y);

                        PointF loc = new PointF((x * offset.X) + zeroPoint.X, (y * offset.Y) + zeroPoint.Y);

                        //Debug.WriteLine("Loc: " + loc);

                        if (mapRotated)
                        {
                            loc = RotatePointF(loc, pntPlayer, 45);
                        }

                        pe.Graphics.DrawString(name, m_RegFont, Brushes.Red, loc.X + 5, loc.Y - 25, stringFormat);
                        //pe.Graphics.FillRectangle(pinBrush, loc.X - 7, loc.Y - 15, 24, 24);

                        //just regular bitmap, works ok   pe.Graphics.DrawImage(mapPin, loc.X - 7, loc.Y - 15);

                        //using (Cursor mapPin = Markers.CursorFromBitmap(mapPinBmp))
                        //{
                            //Draw that cursor bitmap directly to the form canvas
                            //pe.Graphics.DrawImage(pinBitmap, loc.X, loc.Y, mapPin.Size.Width, mapPin.Size.Height);
                        RectangleF rectF = new RectangleF(loc.X - mapPin.HotSpot.X, loc.Y - mapPin.HotSpot.Y, 24, 24);

                        Rectangle pinRect = Rectangle.Round(rectF);
                        mapPin.DrawStretched(pe.Graphics, pinRect);

                            //Debug.WriteLine("MARKER BmpSize:{0} CursorSize:{1} X:{2} Y:{3} hX:{4} hY:{5}", 
                            //    mapPin.Size.ToString(), mapPin.Size.ToString(), pinRect.X, pinRect.Y, mapPin.HotSpot.X, mapPin.HotSpot.Y);
                        //}
                    }
                    */


                    //if (mapRotated)
                    //{
                    //    mtl = RotatePointF(mtl, pntPlayer, 45);
                    //    mtr = RotatePointF(mtr, pntPlayer, 45);
                    //    mbr = RotatePointF(mbr, pntPlayer, 45);
                    //    mbl = RotatePointF(mbl, pntPlayer, 45);
                    //}

                    // COMPASS AT PLAYER LOCATION (UOPS Style)
                    //pe.Graphics.DrawString("W", m_BoldFont, Brushes.Red, pntPlayer.X - 35, pntPlayer.Y);
                    //pe.Graphics.DrawString("E", m_BoldFont, Brushes.Red, pntPlayer.X + 35, pntPlayer.Y);
                    //pe.Graphics.DrawString("N", m_BoldFont, Brushes.Red, pntPlayer.X, pntPlayer.Y - 35);
                    //pe.Graphics.DrawString("S", m_BoldFont, Brushes.Red, pntPlayer.X, pntPlayer.Y + 35);
                    // CROSSHAIR ROTATED AT PLAYER LOCATION (UOPS Style)
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X - 4, pntPlayer.Y, pntPlayer.X + 4, pntPlayer.Y);
                    //pe.Graphics.DrawLine(Pens.Silver, pntPlayer.X, pntPlayer.Y - 4, pntPlayer.X, pntPlayer.Y + 4);

                    //Reset the rotation, otherwise we end up with everything we draw rotated
                    //if(mapRotated)
                    //    pe.Graphics.ResetTransform();

                    //Point pntTest2 = new Point((3256) - (mapOrigin.X << 3) - offset.X, (326) - (mapOrigin.Y << 3) - offset.Y);
                    //PointF pntTest2F = RotatePoint(new Point(xtrans, ytrans), pntTest2);
                    //pe.Graphics.FillRectangle(Brushes.LimeGreen, pntTest2F.X, pntTest2F.Y, 4, 4);

                    //pe.Graphics.ResetTransform();

                    /*
                    if (World.Player != null)
                    {
                        if (World.Player != this.FocusMobile)
                        {
                            Mobile mob = World.Player;
                            PointF drawPoint = new PointF(((mob.Position.X * offset.X) + zeroPoint.X), (mob.Position.Y * offset.Y) + zeroPoint.Y);
                            pe.Graphics.FillRectangle(Brushes.Red, drawPoint.X, drawPoint.Y, 2f, 2f);
                            drawPoint = new PointF(((mob.Position.X * offset.X) + zeroPoint.X), ((mob.Position.Y * offset.Y) + zeroPoint.Y));
                            string name = mob.Name;
                            if (name != null && name.Length > 20)
                                name = name.Substring(0, 20);

                            PointF drawString = new PointF(drawPoint.X, drawPoint.Y);
                            pe.Graphics.DrawString(name, m_RegFont, Brushes.Red, drawString.X, drawString.Y - 10f, stringFormat);
                        }
                    }
                    */

                    // COORDINATE BARS
                    pe.Graphics.FillRectangle(Brushes.Wheat, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);
                    pe.Graphics.DrawRectangle(Pens.Black, 6, 6, coordTopSize.Width + 2, coordTopSize.Height + 2);
                    pe.Graphics.DrawString(coordTopString, m_RegFont, Brushes.Black, 6, 6);
                    // COORDINATE BARS END

                    pe.Graphics.SmoothingMode = SmoothingMode.HighSpeed;

                    // MAP BORDER
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


                    /*
                    PointF mtl = new PointF(renderingBounds.X + 1.5f, renderingBounds.Y + 1.5f);
                    PointF mtr = new PointF((renderingBounds.X + renderingBounds.Right - 1.5f), renderingBounds.Y + 1.5f);
                    PointF mbl = new PointF(renderingBounds.X + 1.5f, (renderingBounds.Y + (renderingBounds.Bottom - 1.5f)));
                    PointF mbr = new PointF((renderingBounds.X + (renderingBounds.Width - 1.5f)), (renderingBounds.Y + (renderingBounds.Bottom - 1.5f)));

                    PointF[] mapRgn = new PointF[] { mtl, mtr, mbr, mbl };
                    
                    Pen pn = new Pen(borderVBrush, 5);
                    pe.Graphics.DrawLine(pn, mtl, mbl);
                    pe.Graphics.DrawLine(pn, mtr, mbr);

                    pn = new Pen(borderHBrush, 5);
                    pe.Graphics.DrawLine(pn, mtl, mtr);
                    pe.Graphics.DrawLine(pn, mbl, mbr);
                    */


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

        /*JUST A TEST FUNCTION  private void Draw(ref PaintEventArgs pe)
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
        }*/

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

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
            
            //Debug.WriteLine("OnPaintBackgroundStart pntPlayer{0}", pntPlayer);
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            //pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.None;
            pe.Graphics.SmoothingMode = SmoothingMode.None;
            //pe.Graphics.PageUnit = GraphicsUnit.Pixel;
            //pe.Graphics.PageScale = 1f;

            pe.Graphics.Clear(Color.Black);

            if (tiltChanged)
            {               
                if (mapRotated)
                {
                    bgRot = new PointF(bgLeft, bgTop);
                    bgRot = RotatePointF(bgRot, bgRot, 45);
                }
                
                tiltChanged = false;
            }

            Matrix mtx = new Matrix();

            if (mapRotated)
            {
                //zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                //zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));

                //_mapRegular_1 = new Bitmap(_mapRegular_1, (int)mapWidth + 2, (int)mapHeight + 2);
                //mygraphics = Graphics.FromImage(myBitmap);
                //pe.Graphics.TranslateTransform(pntPlayer.X, pntPlayer.Y);
                //pe.Graphics.RotateTransform(45);
                //pe.Graphics.TranslateTransform(-pntPlayer.X, -pntPlayer.Y);
                //pe.Graphics.DrawImage(_mapRegular_1, zeroPoint.X + 1, zeroPoint.Y + 1);//, mapWidth, mapHeight);

                // image crop
                //_mapRegular_1 = _mapRegular_1.Clone(new Rectangle(1, 1, (int)mapWidth, (int)mapHeight), _mapRegular_1.PixelFormat);

                // COMMENT THIS AND UNCOMMENT EXPERIMENT REGION TO MAKE IT USE TEXTURE BRUSH EXPERIMENT
                // WARNING - NON FUNCTIONAL

                //ORIGINAL AND WORKING WELL   EXCEPT FOR RETARDED 1 PIXEL OFFSET BULLSHIT
                mtx.RotateAt(45, pntPlayer);             
                pe.Graphics.MultiplyTransform(mtx);

                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));

                pe.Graphics.DrawImage(_mapRegular_1, zeroPoint.X, zeroPoint.Y, mapWidth, mapHeight);
                //END ORIGINAL
            }
            else
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgLeft));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgTop));

                // COMMENT THIS AND UNCOMMENT EXPERIMENT REGION TO MAKE IT USE TEXTURE BRUSH EXPERIMENT
                // WARNING - NON FUNCTIONAL

                //ORIGINAL AND WORKING WELL 
                pe.Graphics.DrawImage(_mapRegular_1, zeroPoint.X, zeroPoint.Y, mapWidth, mapHeight);
            }

            mtx.Dispose();

            #region BITBLT
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
            #region TEXTURE BRUSH EXPERIMENT
            /*  // TEXTURE BRUSH EXPERIMENT

            //PointF tl = new PointF(bgLeft, bgTop);
            //PointF tr = new PointF((bgLeft + mapWidth), bgTop);
            //PointF bl = new PointF(bgLeft, (bgTop + mapHeight));
            //PointF br = new PointF((bgLeft + mapWidth), (bgTop + mapHeight));

            PointF tl = new PointF(renderingBounds.Left, renderingBounds.Top);
            PointF tr = new PointF((renderingBounds.Left + renderingBounds.Right), renderingBounds.Top);
            PointF bl = new PointF(renderingBounds.Left, (renderingBounds.Top + renderingBounds.Bottom));
            PointF br = new PointF((renderingBounds.Left + renderingBounds.Right), (renderingBounds.Top + renderingBounds.Bottom));

            PointF mtl = new PointF(zeroPoint.X, zeroPoint.Y);
            PointF mtr = new PointF((zeroPoint.X + mapWidth), zeroPoint.Y);
            PointF mbl = new PointF(zeroPoint.X, (zeroPoint.Y + mapHeight));
            PointF mbr = new PointF((zeroPoint.X + mapWidth), (zeroPoint.Y + mapHeight));

            if (mapRotated)
            {
                tl = RotatePointF(tl, pntPlayer, 45);
                tr = RotatePointF(tr, pntPlayer, 45);
                br = RotatePointF(br, pntPlayer, 45);
                bl = RotatePointF(bl, pntPlayer, 45);

                mtl = RotatePointF(tl, pntPlayer, 45);
                mtr = RotatePointF(tr, pntPlayer, 45);
                mbr = RotatePointF(br, pntPlayer, 45);
                mbl = RotatePointF(bl, pntPlayer, 45);
            }

            PointF[] renderRgn = new PointF[] { tl, tr, br, bl };
            PointF[] mapRgn = new PointF[] { mtl, mtr, mbr, mbl };

            Pen pn = new Pen(Brushes.Green, 1);

            RectangleF mapPart = new RectangleF((Math.Max(0, pntPlayer.X - renderingBounds.Right / 2)),
                                                (Math.Max(0, pntPlayer.Y - renderingBounds.Bottom / 2)),
                                                (Math.Min(pntPlayer.X + renderingBounds.Right / 2, renderingBounds.Right)),
                                                (Math.Min(pntPlayer.Y + renderingBounds.Bottom / 2, renderingBounds.Bottom))
                                                );

            if (mapPart.Width < 50)
                mapPart.Width = 50;

            if (mapPart.Height < 50)
                mapPart.Height = 50;

            //Bitmap b = new Bitmap(, mapWidth, mapHeight);
            using (Bitmap c = _mapRegular_1.Clone(mapPart, PixelFormat.Format8bppIndexed))
            {
                using (TextureBrush mapBrush = new TextureBrush(c, WrapMode.Tile))
                {
                    ImageAttributes imageAttributes = new ImageAttributes();

                    mapBrush.TranslateTransform(pntPlayer.X, pntPlayer.Y, MatrixOrder.Append);
                    mapBrush.ScaleTransform(offset.X, offset.Y);
                    pe.Graphics.FillPolygon(mapBrush, renderRgn, FillMode.Winding);
                }
            }

            pe.Graphics.DrawPolygon(pn, mapRgn);

            pn.Dispose();

            */ // END EXPERIMENT
            #endregion

            bgLeftBuf = bgLeft;
            bgTopBuf = bgTop;
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

        public void UpdateAll()
        {
            if (!Active)
                return;

            Graphics gfx = CreateGraphics();

            renderArea = main.ClientRectangle;

            //Debug.WriteLine("UpdateAllStart pntPlayer{0}", pntPlayer);

            // ROTATE THE 0,0 POINT WE USE FOR OBJECT AND OVERLAY DRAWING
            if (mapRotated)
            {
                bgRot = new PointF(bgLeft, bgTop);
                bgRot = RotatePointF(bgRot, bgRot, 45);

                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));

                compassLoc = new PointF(renderArea.Right - 30, renderArea.Top + 30);
            }
            else if(!mapRotated)
            {
                
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgLeft));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgTop));

                compassLoc = new PointF(renderArea.Right - 30, renderArea.Top + 30);
            }



            // PLAYER POSITION
            mfocus = this.FocusMobile.Position;
            pntPlayer = new PointF(Convert.ToInt32(Math.Floor((double)((mfocus.X * offset.X) + zeroPoint.X))), Convert.ToInt32(Math.Floor((double)(mfocus.Y * offset.Y) + zeroPoint.Y)));


            //foreach (JMapButton btn in mapButtons)
            //{
            //btn.Invalidate(true);
            //btn.Update();
            //Debug.WriteLine("Invalidating: " + btn.Name);
            //}

            ArrayList mobArray = new ArrayList();

            foreach (KeyValuePair<Serial, Mobile> m in World.Mobiles)
            //foreach (Serial s in PacketHandlers.Party)
            {
                Mobile mob = World.FindMobile(m.Key);
                if (mob == null)
                    continue;

                if (mob == this.FocusMobile && mob == World.Player)
                    continue;

                mobArray.Add(mob);

            }

            // MAP MARKERS
            foreach(JMapButton btn in mapButtons)
            {
                btn.Invalidate(true);
                btn.Update();
            }

            // GUARDLINES & OTHER REGIONS
            regions = RegionList(0, 0, _mapSize.Width);

            // COORDINATES BARS
            if (Format(new Point(mfocus.X, mfocus.Y), Ultima.Map.Felucca, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                coordTopString = String.Format("{0}°{1}'{2} {3}°{4}'{5} | ({6},{7})", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W", mfocus.X, mfocus.Y); //World.Player.Position.X / Y
                coordTopSize = gfx.MeasureString(coordTopString, m_RegFont);
            }

            gfx.Dispose();
        }

        public void TrackPlayer(PointF playerPos)
        {
            //Debug.WriteLine("TrackPlayerStart pntPlayer{0}", pntPlayer);
            Debug.WriteLine("TrackPlayer playerPos: {0}", playerPos);

            float targetX = mapWidth + (playerPos.X - mapWidth);
            float targetY = mapHeight + (playerPos.Y - mapHeight);

            float newX = (bgLeft - targetX) + (renderArea.Right / 2);
            float newY = (bgTop - targetY) + (renderArea.Bottom / 2);

            bgLeft = Convert.ToInt32(Math.Floor((double)newX));
            bgTop = Convert.ToInt32(Math.Floor((double)newY));

            UpdateAll();
            Invalidate(true);
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
                    Invalidate(true);
                    Update();
                    //Refresh();
                }
            }
            catch { }
        }

        /*public void MapClick(MouseEventArgs e)
        {
            if (Active)
            {
                PointF clickedbox = MousePointToMapPoint(new PointF(e.X, e.Y));
                //UOMapRuneButton button = ButtonCheck(new Rectangle(clickedbox.X - 2, clickedbox.Y - 2, 5, 5));
                //if (button != null)
                //    button.OnClick(e);
            }
        }*/

        private PointF MousePointToMapPoint(PointF p)
        {
            double rad = (Math.PI / 180) * 45;
            int w = (Width) >> 3;
            int h = (Height) >> 3;
            Point3D focus = this.FocusMobile.Position;

            PointF mapOrigin = new PointF((focus.X) - (w / 2), (focus.Y) - (h / 2));
            PointF pnt1 = new PointF((mapOrigin.X) + (p.X), (mapOrigin.Y) + (p.Y));
            PointF check = new PointF(pnt1.X - focus.X, pnt1.Y - focus.Y);
            check = RotatePoint(new PointF((int)(check.X * 0.695), (int)(check.Y * 0.68)), rad, 1);
            return new PointF(check.X + focus.X, check.Y + focus.Y);
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

        private PointF RotatePoint(PointF p, double angle, double dist)
        {
            int x = (int)((p.X * Math.Cos(angle) + p.Y * Math.Sin(angle)) * dist);
            int y = (int)((-p.X * Math.Sin(angle) + p.Y * Math.Cos(angle)) * dist);

            return new Point(x, y);
        }

        private PointF RotatePoint(PointF center, PointF pos)
        {
            PointF newp = new PointF(center.X - pos.X, center.Y - pos.Y);
            double x = newp.X * Math.Cos(RotateAngle) - newp.Y * Math.Sin(RotateAngle);
            double y = newp.X * Math.Sin(RotateAngle) + newp.Y * Math.Sin(RotateAngle);
            return AdjustPoint(center, new PointF((float)(x) + center.X, (float)(y) + center.Y));
        }

        private PointF AdjustPoint(PointF center, PointF pos)
        {
            PointF newp = new PointF(center.X - pos.X, center.Y - pos.Y);
            float dis = (float)Distance(center, pos);
            dis += dis * 0.50f;
            float slope = 0;
            if (newp.X != 0)
                slope = (float)newp.Y / (float)newp.X;
            else
                return new PointF(0 + center.X, -1f * (newp.Y + (newp.Y * 0.25f)) + center.Y);
            slope *= -1;
            //Both of these algorithms oddly produce the same results.
            //float x = dis / (float)(Math.Sqrt(1f + Math.Pow(slope, 2)));
            float x = newp.X + (newp.X * 0.5f);
            // if (newp.X > 0)
            x *= -1;
            float y = (-1) * slope * x;

            PointF def = new PointF(x + center.X, y + center.Y);

            return def;
        }

        public double Distance(PointF center, PointF pos)
        {

            PointF newp = new PointF(center.X - pos.X, center.Y - pos.Y);
            double distX = Math.Pow(newp.X, 2);
            double distY = Math.Pow(newp.Y, 2);
            return Math.Sqrt(distX + distY);
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

        /*        private ArrayList ButtonList(int x, int y, int maxDist)
                {
                    if (this.m_MapButtons == null)
                        return null;
                    int count = this.m_MapButtons.Count;
                    ArrayList aList = new ArrayList();
                    for (int i = 0; i < count; ++i)
                    {
                        UOMapRuneButton btn = (UOMapRuneButton)this.m_MapButtons[i];
                        if (Utility.Distance(btn.X, btn.Y, x, y) <= maxDist * 2)
                        {
                            aList.Add(btn);
                        }
                    }
                    return aList;
                }
        */

        public void AddMarker(PointF markedLoc, string optionalName = "LOCATION")
        {
            int xLoc = (int)markedLoc.X;
            int yLoc = (int)markedLoc.Y;


            //StringBuilder builder = new StringBuilder();

            string locName = optionalName;
            string x = xLoc.ToString();
            string y = yLoc.ToString();

            //Format the new line
            string newLine = string.Format("{0},{1},{2}", locName, x, y);
            //builder.AppendLine(newLine);

            markedLocations.Add(newLine);
            //Write it to file
            File.AppendAllText("markedLocations.csv", newLine);
        }

        public void ReadMarkers(string path)
        {
            try
            {
                Debug.WriteLine("Marked Locations Found!");
                using (StreamReader sr = new StreamReader(path))
                {
                    //int row = 0;
                    var lines = new List<string[]>();
                    int row = 0;
                    while (!sr.EndOfStream)
                    {
                        string[] line = sr.ReadLine().Split(',');
                        lines.Add(line);
                        ++row;
                        //Debug.WriteLine("While: " + line);
                    }
                    foreach (string[] line in lines)
                    {
                        markedLocations.Add(line);

                        int type = int.Parse(line[0]);
                        float x = float.Parse(line[1]);
                        float y = float.Parse(line[2]);
                        string text = line[3];
                        string extra = line[4];

                        JMapButton btn = UIElements.NewButton(this, (JMapButtonType)type, x, y, text, extra);

                        mapButtons.Add(btn);

                    }                    
                }
            }
            catch
            {

            }

        }

        private void LoadMapButtons()
        {
            int i = 0;
            foreach (JMapButton btn in mapButtons)
            {
                ++i;
                Debug.WriteLine("Adding Button {0}/{1} Text:{2} X:{3} Y:{4}", i, mapButtons.Count, btn.displayText, btn.mapLoc.X, btn.mapLoc.Y);

                this.Controls.Add(btn);
                btn.Enabled = true;
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
            RectangleF cr = new RectangleF(bgLeft, bgTop, mapWidth, mapHeight);//  ctl.ClientRectangle;
            float x = Math.Min(0, Math.Max(bgLeft, pr.Width - cr.Width));
            float y = Math.Min(0, Math.Max(bgTop, pr.Height - cr.Height));

            bgLeft = Convert.ToInt32(Math.Floor((double)x));
            bgTop = Convert.ToInt32(Math.Floor((double)y));
            //}
            /* //Needs work!!
            if (mapRotated)
            {
                Rectangle cr = new Rectangle(bgLeft, bgTop, (int)mapWidth, (int)mapHeight);//  ctl.ClientRectangle;
                int x = Math.Min(0, Math.Max(bgLeft, pr.Width - cr.Width));
                int y = Math.Min(0, Math.Max(bgTop, pr.Height - cr.Height));

                bgLeft = x;
                bgTop = y;
            }
            */
        }

        public void mapPanel_MouseUp(object sender, EventArgs e)
        {
            main.Cursor = Cursors.Default;
        }

        public void mapPanel_MouseDown(object sender, EventArgs e)
        {
            MouseEventArgs mouse = e as MouseEventArgs;
            Point p = mouse.Location;

            if (mouse.Button == MouseButtons.Left)
            {
                mouseDown = mouse.Location;
            }

            else if (mouse.Button == MouseButtons.Right)
            {
                mouseLastPos = p;
                ContextMenuStrip.Show(this, p);
            }
        }

        public void mapPanel_MouseMove(object sender, EventArgs e)
        {
            IsPanning = true;
            MouseEventArgs mouse = e as MouseEventArgs;
            Point mousePosNow = mouse.Location;

            int deltaX = mousePosNow.X - mouseDown.X;
            int deltaY = mousePosNow.Y - mouseDown.Y;

            float newX = bgLeft + deltaX;  //Location.X + deltaX; 
            float newY = bgTop + deltaY;   //Location.Y + deltaY; 

            if (mouse.Button == MouseButtons.Left)
            {             
                trackingPlayer = false;
                main.Cursor = Cursors.SizeAll;
                main.SuspendLayout();

                //if (newX < 3)
                //{
                bgLeft = Convert.ToInt32(Math.Floor((double)newX));
                /*
                if (bgLeft > 0)
                {
                    bgLeft = 0;
                }

                if (bgLeft < (int)-mapWidth)
                {
                    bgLeft = (int)-mapWidth;
                }
                */
                //}

                //if (newY < 3)
                //{
                bgTop = Convert.ToInt32(Math.Floor((double)newY));
                /*
                if (bgTop > 0)
                {
                    bgTop = 0;
                }

                if (bgTop < (int)-mapHeight)
                {
                    bgTop = (int)-mapHeight;
                }
                */
                //}

                //Constrain(main);





                //Refresh();
                
                //Update();
                
                main.ResumeLayout();
                UpdateAll();
                Invalidate(true);


                mouseDown = mouse.Location;
                //trackingPlayer = true;
                IsPanning = false;
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


                //if(_mapSize.Width * offset.X >= main.Width && _mapSize.Height * offset.Y >= main.Height)
                //{
                //zoom.Width = Convert.ToInt32(Math.Floor((double)(_mapSize.Width * offset.X));
                //zoom.Height = Convert.ToInt32(Math.Floor((double)(_mapSize.Height * offset.Y));

                zoom.Width = (_mapSize.Width * offset.X);
                zoom.Height = (_mapSize.Height * offset.Y);

                mapWidth = Convert.ToInt32(Math.Floor((double)zoom.Width));
                mapHeight = Convert.ToInt32(Math.Floor((double)zoom.Height));

                bgLeft = Convert.ToInt32(Math.Floor((double)(mousePos.X - (offset.X / oldOffset.X) * (mousePos.X - zeroPoint.X))));
                bgTop = Convert.ToInt32(Math.Floor((double)(mousePos.Y - (offset.Y / oldOffset.Y) * (mousePos.Y - zeroPoint.Y))));

                SendMessage(Handle, WM_SETREDRAW, true, 0);

                //Refresh();
                Invalidate(true);
                UpdateAll();
                ResumeLayout();

                if (trackingPlayer)
                    TrackPlayer(pntPlayer);

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
                
                    if (main.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable) //main.ControlBox = false;                
                        main.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    else //main.ControlBox = true;
                        main.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                
            }

        }

        private void mapPanel_Tilt(object sender, EventArgs e)
        {
            if (!mapRotated)
            {
                mapRotated = true;
            }

            else if (mapRotated)
            {
                mapRotated = false;
            }
            tiltChanged = true;

            Invalidate(true);
            UpdateAll();

            if (trackingPlayer)
                TrackPlayer(pntPlayer);

            Invalidate(true);
        }

        public void mapPanel_Exit(object sender, EventArgs e)
        {           
            //ClientCommunication.SetMapWndHandle(null);
            main.Close();
        }

        public void mapPanel_MouseEnter(object sender, EventArgs e)
        {
            //Debug.WriteLine("MouseEnter");
            //Debug.WriteLine("Mouse Enter");
            //Activate();
            //this.Focus();
            //this.Capture = true;
            if (!main.Focused)
            {
                main.Focus();
                main.Activate();
            }
                
        }

        /*public void mapPanel_MouseLeave(object sender, EventArgs e)
        {
            Debug.WriteLine("MouseLeave");
            //if (main.Focused && main.TopMost)
            //{//Doesn't work as intended, inactive function
            //    main.SendToBack();
            //    main.BringToFront();
            //}

        }*/

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
            this.OptionOverlays = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_OverlaysAll = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_OverlaysGuard = new System.Windows.Forms.ToolStripMenuItem();
            this.Menu_OverlaysGrid = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionTilt = new System.Windows.Forms.ToolStripMenuItem();
            this.SeperatorOptions = new System.Windows.Forms.ToolStripSeparator();
            this.OptionExit = new System.Windows.Forms.ToolStripMenuItem();
            this.ContextOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // ContextOptions
            // 
            this.ContextOptions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OptionOverlays,
            this.OptionTilt,
            this.SeperatorOptions,
            this.OptionExit});
            this.ContextOptions.Name = "ContextOptions";
            this.ContextOptions.Size = new System.Drawing.Size(117, 76);
            // 
            // OptionOverlays
            // 
            this.OptionOverlays.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Menu_OverlaysAll,
            this.Menu_OverlaysGuard,
            this.Menu_OverlaysGrid});
            this.OptionOverlays.Name = "OptionOverlays";
            this.OptionOverlays.Size = new System.Drawing.Size(116, 22);
            this.OptionOverlays.Text = "Options";
            // 
            // Menu_OverlaysAll
            // 
            this.Menu_OverlaysAll.CheckOnClick = true;
            this.Menu_OverlaysAll.Name = "Menu_OverlaysAll";
            this.Menu_OverlaysAll.Size = new System.Drawing.Size(136, 22);
            this.Menu_OverlaysAll.Text = "All";
            // 
            // Menu_OverlaysGuard
            // 
            this.Menu_OverlaysGuard.CheckOnClick = true;
            this.Menu_OverlaysGuard.Name = "Menu_OverlaysGuard";
            this.Menu_OverlaysGuard.Size = new System.Drawing.Size(136, 22);
            this.Menu_OverlaysGuard.Text = "Guard Lines";
            // 
            // Menu_OverlaysGrid
            // 
            this.Menu_OverlaysGrid.CheckOnClick = true;
            this.Menu_OverlaysGrid.Enabled = false;
            this.Menu_OverlaysGrid.Name = "Menu_OverlaysGrid";
            this.Menu_OverlaysGrid.Size = new System.Drawing.Size(136, 22);
            this.Menu_OverlaysGrid.Text = "Grid";
            // 
            // OptionTilt
            // 
            this.OptionTilt.CheckOnClick = true;
            this.OptionTilt.Name = "OptionTilt";
            this.OptionTilt.Size = new System.Drawing.Size(116, 22);
            this.OptionTilt.Text = "Tilt";
            this.OptionTilt.CheckedChanged += new System.EventHandler(this.mapPanel_Tilt);
            // 
            // SeperatorOptions
            // 
            this.SeperatorOptions.Name = "SeperatorOptions";
            this.SeperatorOptions.Size = new System.Drawing.Size(113, 6);
            // 
            // OptionExit
            // 
            this.OptionExit.Name = "OptionExit";
            this.OptionExit.Size = new System.Drawing.Size(116, 22);
            this.OptionExit.Text = "Exit";
            this.OptionExit.Click += new System.EventHandler(this.mapPanel_Exit);
            // 
            // MapPanel
            // 
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ContextMenuStrip = this.ContextOptions;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Size = new System.Drawing.Size(400, 400);
            this.ContextOptions.ResumeLayout(false);
            this.ResumeLayout(false);
            
        }
    }
}
