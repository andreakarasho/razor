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
        public static JMapButton NewButton(MapPanel mapPanel, JMapButtonType type, string markerOwner, bool IsPublic, float mapLocX, float mapLocY, string displayText = "", string extraText = "")
        {
            try
            {
                switch (type)
                {
                    case JMapButtonType.GenericButton: return GenericButton(mapPanel, type, markerOwner, IsPublic, mapLocX, mapLocY, displayText, extraText);
                    case JMapButtonType.MapPin: return MapPin(mapPanel, type, markerOwner, IsPublic, mapLocX, mapLocY, displayText, extraText);
                    default: return MapPin(mapPanel, type, markerOwner, IsPublic, mapLocX, mapLocY, displayText, extraText);
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
                return MapPin(mapPanel, type, markerOwner, IsPublic, mapLocX, mapLocY, displayText, extraText);
            }

        }

        private static JMapButton MapPin(MapPanel mapPanel, JMapButtonType type, string markerOwner, bool IsPublic, float mapLocX, float mapLocY, string displayText = "", string extraText = "")
        {
            try
            {
                return new JMapButton()
                {
                    //curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Cursors\\mapPin32A21.cur",
                    //Size = new Size(32, 32),

                    mapPanel = mapPanel,
                    type = JMapButtonType.MapPin,
                    MarkerOwner = markerOwner,
                    IsPublic = IsPublic,
                    mapLoc = new PointF(mapLocX, mapLocY),
                    displayText = displayText,
                    extraText = extraText
                    
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
                return MapPin(mapPanel, type, markerOwner, IsPublic, mapLocX, mapLocY, displayText, extraText);
            }
        }

        private static JMapButton GenericButton(MapPanel mapPanel, JMapButtonType type, string markerOwner, bool IsPublic, float mapLocX, float mapLocY, string displayText = "", string extraText = "") 
        {
            return new JMapButton()
            {
                type = JMapButtonType.GenericButton
                //hasPane = hasPane,
                //hasText = hasText,
                //hasExtra = hasExtra
            };

        }
    }

    public partial class JMapButton //: Control, IButtonControl
    {
        public MapPanel mapPanel;

        // Core
        public JMapButtonType type { get; set; }
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
        public bool IsPublic { get; set; }
        public string MarkerOwner { get; set; }
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

        public void LoadButton()
        {
            try
            {
                if(this.type == JMapButtonType.MapPin)
                {
                    if (IsPublic)
                    {
                        curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Cursors\\mapPin32A21_Gold.cur";
                        textColor = Color.Yellow;
                    }
                    else if(!IsPublic)
                    {
                        curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Cursors\\mapPin32A21.cur";
                        textColor = Color.Red;
                    }
                }

                this.cur = Markers.LoadCursor(this.curPath);
                Icon i = Icon.ExtractAssociatedIcon(this.curPath);
                this.img = i.ToBitmap();
                this.hotSpot = cur.HotSpot;

                if(type == JMapButtonType.MapPin)
                {
                    this.hotSpot = new Point(this.hotSpot.X - 3, this.hotSpot.Y - 4);
                }

                highlightColor = Color.Silver;
                highlightArea = new GraphicsPath();
                highlightArea.AddEllipse(10, 0, 14, 14);
                highlightBrush = new PathGradientBrush(highlightArea);
                highlightBrush.CenterColor = Color.FromArgb(0, highlightColor);
                Color[] colors = { highlightColor , Color.FromArgb(0, highlightColor) };
                highlightBrush.SurroundColors = colors;
                highlightBrush.FocusScales = new PointF(0.785f, 0.785f);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BUTTON LOAD ERROR: " + ex.ToString());
            }
        }

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
