using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Assistant.JMap
{
    public enum JMapButtonType
    {
        GenericButton = 0,
        MapPin = 1
        //More to add                                   //separate enum for UOTypes? Razor has them?
    }

    public class UIElements
    {
        public static JMapButton NewButton(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "", string extraText = "")
        {
            /* When map is clicked in order to create a marker
             * we need to have a prompt. A little pop up next to the 
             * marker asking "Details?" could work.
             * It should disappear if not clicked in X seconds or
             * if another action occurs.
             */

            //Debug.WriteLine("NewButton Called");
            try
            {
                switch (type)
                {
                    case JMapButtonType.GenericButton: return GenericButton(mapPanel, type, mapLocX, mapLocY, buttonId, displayText, extraText);
                    case JMapButtonType.MapPin: return MapPin(mapPanel, type, mapLocX, mapLocY, buttonId, displayText, extraText);
                    default: return MapPin(mapPanel, type, mapLocX, mapLocY, buttonId, displayText, extraText);
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
                return MapPin(mapPanel, type, mapLocX, mapLocY, displayText, extraText);
            }

        }

        private static JMapButton MapPin(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "", string extraText = "")
        {
            try
            {
                return new JMapButton()
                {
                    curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Cursors\\mapPin32A21.cur",
                    //Size = new Size(32, 32),

                    mapPanel = mapPanel,
                    type = JMapButtonType.MapPin,
                    mapLoc = new PointF(mapLocX, mapLocY),
                    displayText = displayText,
                    extraText = extraText,
                    id = buttonId

                    //hasPane = hasPane,
                    //hasText = hasText,
                    //hasExtra = hasExtra,
                    //Name = displayText + "_" + mapLocX.ToString() + "_" + mapLocY.ToString(),
                    //Text = "",




                };
            }
            catch(Exception e)
            {
                Debug.WriteLine("BUTTON CREATION ERROR: " + e.ToString());
                return MapPin(mapPanel, type, mapLocX, mapLocY, displayText, extraText);
            }
        }

        private static JMapButton GenericButton(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "", string extraText = "") 
        {
            return new JMapButton()
            {
                type = JMapButtonType.GenericButton,
                //hasPane = hasPane,
                //hasText = hasText,
                //hasExtra = hasExtra
                id = buttonId
            };

        }
    }

    public partial class JMapButton //: Control, IButtonControl
    {
        public MapPanel mapPanel;

        // Core
        public JMapButtonType type { get; set; }
        public string id { get; set; }
        public Cursor cur { get; set; }
        public string curPath { get; set; }
        public Bitmap img { get; set; }
        public ImageAttributes imgAttr { get; set; }
        private Region hitbox { get; set; } //?? maybe not
        public PointF mapLoc { get; set; }
        public Point hotSpot { get; set; }
        public PointF renderPoint { get; set; }
        public PointF renderLoc { get; set; }
        public PointF offset;
        public PointF zeroPoint;
        public PointF bgRot;
        public PointF bgReg;
        public float bgLeft;
        public float bgTop;
        public RectangleF renderingBounds;
        // States
        public bool IsHovered { get; set; }
        public Point mousePos { get; set; }

        // Options
        //public bool hasPane { get; set; }
        //public bool hasText { get; set; }
        //public bool hasExtra { get; set; }

        // Data
        public string displayText { get { return _displayText != "" ? _displayText : ""; } set { _displayText = value; } }
        private string _displayText;
        public string extraText { get { return _extraText != "" ? _extraText : ""; } set { _extraText = value; } }
        private string _extraText;
        public Color textColor { get { return _textColor; } set { _textColor = value; } }
        private Color _textColor { get; set; }
        public Color extraTextColor { get { return _extraTextColor; } set { _extraTextColor = value; } }
        private Color _extraTextColor { get; set; }
        public Color rectColor { get { return _rectColor; } set { _rectColor = value; } }
        private Color _rectColor { get; set; }
        public Color highlightColor { get { return _highlightColor; } set { _highlightColor = value; } }
        private Color _highlightColor { get; set; }
        public PathGradientBrush highlightBrush { get { return _highlightBrush; } set { _highlightBrush = value; } }
        private PathGradientBrush _highlightBrush { get; set; }
        public GraphicsPath highlightArea { get { return _highlightArea; } set { _highlightArea = value; } }
        private GraphicsPath _highlightArea { get; set; }

        public bool Active { get; private set; }
        public DialogResult DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public JMapButton()
        {
            //Enabled = false;
            //this.EnabledChanged += new EventHandler(LoadButton);
            
            
        }

        public void UpdateButton()
        {
            bgReg = mapPanel.bgReg;
            bgTop = mapPanel.bgTop;
            bgRot = mapPanel.bgRot;
            offset = mapPanel.offset;

            if (mapPanel.mapRotated)
            {
                zeroPoint.X = bgRot.X;
                zeroPoint.Y = bgRot.Y;
            }
            else
            {
                zeroPoint.X = bgReg.X;
                zeroPoint.Y = bgReg.Y;
            }


            renderPoint = new PointF(
                                    Convert.ToInt32((Math.Floor((double)((mapLoc.X * offset.X) + zeroPoint.X) - hotSpot.X))),
                                    Convert.ToInt32((Math.Floor((double)(mapLoc.Y * offset.Y) + zeroPoint.Y) - hotSpot.Y)));

            renderLoc = renderPoint;
            if (mapPanel.mapRotated)
            {
                renderLoc = mapPanel.RotatePointF(renderPoint, mapPanel.zeroPoint, 45);
                renderLoc = new PointF(Convert.ToInt32(Math.Floor((double)renderLoc.X)), Convert.ToInt32(Math.Floor((double)renderLoc.Y)));
            }
        }

        
        /*protected override void OnPaint(PaintEventArgs pe)
        {
            
            //if (!Active)
            //    return;
            pe.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            pe.Graphics.PageUnit = GraphicsUnit.Pixel;

            offset = mapPanel.offset;
            zeroPoint = mapPanel.zeroPoint;

            renderPoint = new PointF(Convert.ToInt32(Math.Floor((double)((mapLoc.X * offset.X) + zeroPoint.X) - hotSpot.X)),
                        Convert.ToInt32(Math.Floor((double)(mapLoc.Y * offset.Y) + zeroPoint.Y) - hotSpot.Y));

            renderLoc = renderPoint;
            if (mapPanel.mapRotated)
            {
                renderLoc = mapPanel.RotatePointF(renderPoint, mapPanel.pntPlayer, 45);
            }

            this.Left = Convert.ToInt32(Math.Floor((double)renderLoc.X));
            this.Top = Convert.ToInt32(Math.Floor((double)renderLoc.Y));

            //Rectangle rectF = this.DisplayRectangle; //Default size, 32,32 as it is a cursor

            
            //pe.Graphics.DrawImage(img, 0, 0, 24, 24);
            
            
            //pe.Graphics.DrawImage(img, rectF);

            
            
            //ResumeLayout();
            //if (IsHovered)
            //{
            //    Debug.WriteLine("Should be highlighting");
            //    pe.Graphics.FillPath(highlightBrush, highlightArea);
            //}

            // loc 0,0 + 13,3    8x8 elipse
            //cur.DrawStretched(pe.Graphics, rectF);
            //base.OnPaint(pe);
            
        }*/

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            //if(IsHovered)
            //{
                //mousePos = e.Location;
            //}
            //RectangleF buttonRect = new RectangleF(Left, Top, Width, Height);

            //if (buttonRect.Contains(mousePos) && img.GetPixel(mousePos.X, mousePos.Y).A > 11)
            //{
            //    IsHovered = true;
            //    Debug.WriteLine("Mouse moved, but still within button");
            //}
            //Invalidate();
            //Update();
        }

        /*private void OnMouseEnter(object sender, EventArgs e)
        {
            Debug.WriteLine("Mouse entered, mouse pos: " + mousePos);
            RectangleF buttonRect = new RectangleF(Left, Top, Width, Height);

            if (buttonRect.Contains(mousePos) && img.GetPixel(mousePos.X, mousePos.Y).A > 11)
            {
                IsHovered = true;
                Debug.WriteLine("Opaque Hovered");
            }
            Invalidate();
            Update();
        }*/

        /*private void OnMouseHover(object sender, EventArgs e)
        {
            
            Debug.WriteLine("Hover triggered, mouse pos: " + mousePos);
            RectangleF buttonRect = new RectangleF(Left, Top, Width, Height);

            if (buttonRect.Contains(mousePos) && img.GetPixel(mousePos.X, mousePos.Y).A > 11)
            {
                IsHovered = true;
                Debug.WriteLine("Opaque Hovered");
            }
            Invalidate();
            Update();
        }*/

        /*private void OnMouseLeave(object sender, EventArgs e)
        {
            IsHovered = false;
            Invalidate();
            Update();
        }*/

        /*protected override void OnPaintBackground(PaintEventArgs pe)
        {
            //Prevent anything occurring.
            //base.OnPaintBackground(pe);
        }*/

        public void LoadButton()//object sender, EventArgs e)
        {
            try
            {
                //MouseHover += new EventHandler(OnMouseHover);
                //MouseMove += new MouseEventHandler(OnMouseMove);
                //MouseEnter += new EventHandler(OnMouseEnter);

                this.cur = Markers.LoadCursor(this.curPath);
                Icon i = Icon.ExtractAssociatedIcon(this.curPath);
                this.img = i.ToBitmap();
                this.hotSpot = cur.HotSpot;

                if(type == JMapButtonType.MapPin)
                {
                    this.hotSpot = new Point(this.hotSpot.X - 3, this.hotSpot.Y - 4);
                }
                //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                //SetStyle(ControlStyles.Opaque, true);
                //SetStyle(ControlStyles.ResizeRedraw, true);

                //this.BackColor = Color.Transparent;

                highlightColor = Color.Silver;
                highlightArea = new GraphicsPath();
                highlightArea.AddEllipse(10, 0, 14, 14);
                highlightBrush = new PathGradientBrush(highlightArea);
                highlightBrush.CenterColor = Color.FromArgb(0, highlightColor);
                Color[] colors = { highlightColor , Color.FromArgb(0, highlightColor) };
                highlightBrush.SurroundColors = colors;
                highlightBrush.FocusScales = new PointF(0.785f, 0.785f);

                //this.TabStop = false;
                //this.Margin = new Padding(0);

                //this.Name = displayText + "_" + mapLoc.X.ToString() + "_" + mapLoc.Y.ToString();
                //this.Text = displayText;
                //this.Active = true;
                //this.Visible = false;
                //this.Show();
                

            }
            catch (Exception ex)
            {
                Debug.WriteLine("BUTTON LOAD ERROR: " + ex.ToString());
            }
        }

        /*protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; //WS_EX_TRANSPARENT
                return cp;
            }
        }*/

        public void NotifyDefault(bool value)
        {
            //throw new NotImplementedException();
        }

        public void PerformClick()
        {
            //throw new NotImplementedException();
        }


    }
}
