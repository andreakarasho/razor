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
        MapPin = 1,
        UOPoint = 2,
        PlayerHouse = 3,
        Treasure = 4
    }

    public class UIElements
    {
        public static JMapButton NewButton(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string markerOwner, bool IsPublic, string buttonId, string displayText = "", string extraText = "")
        {
            switch (type)
            {
                case JMapButtonType.GenericButton: return GenericButton(mapPanel, type, mapLocX, mapLocY, buttonId, displayText);
                case JMapButtonType.MapPin: return MapPin(mapPanel, type, mapLocX, mapLocY, markerOwner, IsPublic, buttonId, displayText, extraText);
                case JMapButtonType.UOPoint: return UOPoint(mapPanel, type, mapLocX, mapLocY, buttonId, displayText, extraText);
                case JMapButtonType.PlayerHouse: return PlayerHouse(mapPanel, type, mapLocX, mapLocY, buttonId, displayText, extraText);
                case JMapButtonType.Treasure: return Treasure(mapPanel, type, mapLocX, mapLocY, buttonId, displayText);
                default: return MapPin(mapPanel, type, mapLocX, mapLocY, markerOwner, IsPublic, buttonId, displayText, extraText);
            }
        }

        private static JMapButton GenericButton(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "")
        {
            return new JMapButton()
            {
                type = JMapButtonType.GenericButton,
                id = buttonId
            };
        }

        private static JMapButton MapPin(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string markerOwner, bool IsPublic, string buttonId, string displayText = "", string extraText = "")
        {
            try
            {
                return new JMapButton()
                {
                    mapPanel = mapPanel,
                    type = JMapButtonType.MapPin,
                    mapLoc = new PointF(mapLocX, mapLocY),
                    MarkerOwner = markerOwner,
                    IsPublic = IsPublic,
                    displayText = displayText,
                    extraText = extraText,
                    id = buttonId
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Button Creation Error: {e.ToString()}");
                return MapPin(mapPanel, type, mapLocX, mapLocY, markerOwner, IsPublic, buttonId, displayText, extraText);
            }

        }

        private static JMapButton UOPoint(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "", string variant = "")
        {
            try
            {
                return new JMapButton()
                {
                    mapPanel = mapPanel,
                    type = JMapButtonType.UOPoint,
                    Variant = variant.ToUpper(),
                    mapLoc = new PointF(mapLocX, mapLocY),
                    displayText = displayText,
                    id = buttonId
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Button Creation Error: {e.ToString()}");
                return GenericButton(mapPanel, type, mapLocX, mapLocY, buttonId, displayText);
            }

        }
        private static JMapButton PlayerHouse(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "", string variant = "")
        {
            try
            {
                return new JMapButton()
                {
                    mapPanel = mapPanel,
                    type = JMapButtonType.PlayerHouse,
                    Variant = variant.ToUpper(),
                    mapLoc = new PointF(mapLocX, mapLocY),
                    displayText = displayText,
                    id = buttonId
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Button Creation Error: {e.ToString()}");
                return GenericButton(mapPanel, type, mapLocX, mapLocY, buttonId, displayText);
            }
            
        }
        private static JMapButton Treasure(MapPanel mapPanel, JMapButtonType type, float mapLocX, float mapLocY, string buttonId, string displayText = "")
        {
            try
            {
                return new JMapButton()
                {
                    mapPanel = mapPanel,
                    type = JMapButtonType.Treasure,
                    mapLoc = new PointF(mapLocX, mapLocY),
                    displayText = displayText,
                    id = buttonId
                };
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Button Creation Error: {e.ToString()}");
                return GenericButton(mapPanel, type, mapLocX, mapLocY, buttonId, displayText);
            }

        }
    }

    public partial class JMapButton 
    {
        public MapPanel mapPanel;

        // Core
        public JMapButtonType type { get; set; }
        public string id { get; set; }
        public Cursor cur { get; set; }
        public Icon icon { get; set; }
        public string curPath { get; set; }
        public Bitmap img { get; set; }
        public ImageAttributes imgAttr { get; set; }
        private Region hitbox { get; set; } //?? maybe not
        public PointF mapLoc { get; set; }
        public PointF adjustLoc { get; set; }

        public Point hotSpot { get; set; }
        public PointF renderPoint { get; set; }
        public PointF renderLoc { get; set; }

        public Size renderSize { get; set; }

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
        public string MarkerOwner { get; set; }
        public bool IsPublic { get; set; }

        //Variation for houses/signs
        public string Variant { get; set; }

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
            bgRot = mapPanel.bgRot;
            offset = mapPanel.offset;

            if (mapPanel.mapRotated)
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgRot.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgRot.Y));
            }
            else
            {
                zeroPoint.X = Convert.ToInt32(Math.Floor((double)bgReg.X));
                zeroPoint.Y = Convert.ToInt32(Math.Floor((double)bgReg.Y));
            }

            renderPoint = new PointF(
                                    Convert.ToInt32((Math.Floor((double)(((mapLoc.X) * offset.X) + zeroPoint.X)))),
                                    Convert.ToInt32((Math.Floor((double)((mapLoc.Y) * offset.Y) + zeroPoint.Y))));


            adjustLoc = new PointF(adjustLoc.X * offset.X, adjustLoc.Y * offset.Y);

            renderLoc = renderPoint;

            //Treasure type adjustments here simply make it look far prettier :)
            if (type == JMapButtonType.Treasure)
            {
                this.hotSpot = new Point(this.renderSize.Width / 2,
                                         this.renderSize.Height + (2 * Convert.ToInt32(Math.Floor((double)offset.Y))));
            }

            if (mapPanel.mapRotated)
            {
                renderLoc = mapPanel.RotatePointF(renderPoint, zeroPoint, 45);
                renderLoc = new PointF(Convert.ToInt32(Math.Floor((double)(renderLoc.X))), Convert.ToInt32(Math.Floor((double)(renderLoc.Y))));
            }

            if(type == JMapButtonType.PlayerHouse)
                renderLoc = new PointF((renderLoc.X), (renderLoc.Y));   //////RAGE!!!!!
            else
                renderLoc = new PointF(renderLoc.X - hotSpot.X, renderLoc.Y - hotSpot.Y);
        }

        public void LoadButton()
        {
            try
            {
                if (type == JMapButtonType.MapPin)
                {
                    if (IsPublic)
                    {
                        curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Markers\\mapPin32A21_Gold.cur";
                        textColor = Color.Yellow;
                    }
                    else
                    {
                        curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Markers\\mapPin32A21.cur";
                        textColor = Color.Red;
                    }
                    this.renderSize = new Size(24, 24);
                    this.hotSpot = new Point(this.hotSpot.X + 4, this.hotSpot.Y);

                    this.cur = Markers.LoadCursor(this.curPath);
                    Icon i = Icon.ExtractAssociatedIcon(this.curPath);
                    this.img = i.ToBitmap();
                    this.hotSpot = cur.HotSpot;
                }
                else if (type == JMapButtonType.Treasure)
                {
                    curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Markers\\treasureGold.cur";
                    this.renderSize = new Size(20, 20);
                    
                    this.textColor = Color.GhostWhite;

                    this.cur = Markers.LoadCursor(this.curPath);
                    Icon i = Icon.ExtractAssociatedIcon(this.curPath);
                    this.img = i.ToBitmap();
                    //this.hotSpot = cur.HotSpot;
                    this.hotSpot = new Point(this.renderSize.Width / 2, this.renderSize.Height + 10);
                }
                else if(type == JMapButtonType.PlayerHouse)
                {
                    curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\Houses\\" + Variant + ".cur";

                    this.textColor = Color.GhostWhite;

                    this.cur = Markers.LoadCursor(this.curPath);
                    Icon i = Icon.ExtractAssociatedIcon(this.curPath);
                    this.img = i.ToBitmap();
                    this.hotSpot = cur.HotSpot;

                    switch (Variant)
                    {
                        case "CASTLE":
                            this.renderSize = new Size(32, 32);
                            break;
                        case "KEEP":
                            this.renderSize = new Size(24, 24);
                            break;
                        case "LARGEHOUSE":
                            this.renderSize = new Size(15, 15);
                            break;
                        case "LOGCABIN":
                            this.renderSize = new Size(8, 14);
                            break;
                        case "MARPATIO":
                            this.renderSize = new Size(15, 15);
                            break;
                        case "PATIO":
                            this.renderSize = new Size(16, 15);
                            break;
                        case "SMALLHOUSE":
                            this.renderSize = new Size(8, 8);
                            break;
                        case "SMLMSHOP":
                            this.renderSize = new Size(7, 8);
                            break;
                        case "SMLSSHOP":
                            this.renderSize = new Size(8, 8);
                            break;
                        case "SMLTOWER":
                            this.renderSize = new Size(8, 9);
                            this.adjustLoc = new PointF(-3, 1);
                            break;
                        case "SNDPATIO":
                            this.renderSize = new Size(12, 9);
                            break;
                        case "TENT":
                            this.renderSize = new Size(8, 8);
                            break;
                        case "TOWER":
                            this.renderSize = new Size(24, 16);
                            break;
                        case "TWOSTORY":
                            this.renderSize = new Size(14, 15);
                            break;
                        case "VILLA":
                            this.renderSize = new Size(12, 12);
                            break;
                    }
                }

                else if (type == JMapButtonType.UOPoint)
                {
                    curPath = $"{Config.GetInstallDirectory()}\\JMap\\Resources\\UOPoints\\" + Variant + ".cur";
                    
                    this.textColor = Color.GhostWhite;

                    this.cur = Markers.LoadCursor(this.curPath);
                    Icon i = Icon.ExtractAssociatedIcon(this.curPath);
                    this.img = i.ToBitmap();
                    //this.hotSpot = cur.HotSpot;
                    this.renderSize = new Size(24, 16); //All signs

                    switch (Variant)
                    {
                        case "BODYOFWATER":
                            this.renderSize = new Size(20, 8);
                            break;
                        case "BRIDGE":
                            this.renderSize = new Size(15, 7);
                            break;
                        case "DOCKS":
                            this.renderSize = new Size(10, 10);
                            break;
                        case "DUNGEON":
                            this.renderSize = new Size(19, 20);
                            break;
                        case "EXIT":
                            this.renderSize = new Size(7, 6);
                            break;
                        case "GATE":
                            this.renderSize = new Size(11, 10);
                            break;
                        case "GEM":
                            this.renderSize = new Size(16, 16);
                            break;
                        case "GRAVEYARD":
                            this.renderSize = new Size(7, 8);
                            break;
                        case "INTEREST":
                            this.renderSize = new Size(14, 8);
                            break;
                        case "ISLAND":
                            this.renderSize = new Size(19, 11);
                            break;
                        case "LANDMARK":
                            this.renderSize = new Size(11, 8);
                            break;
                        case "MOONGATE":
                            this.renderSize = new Size(9, 19);
                            break;
                        case "POINT":
                            this.renderSize = new Size(5, 5);
                            break;
                        case "RUINS":
                            this.renderSize = new Size(7, 7);
                            break;
                        case "SCENIC":
                            this.renderSize = new Size(11, 6);
                            break;
                        case "SHIP":
                            this.renderSize = new Size(32, 32);
                            break;
                        case "SHRINE":
                            this.renderSize = new Size(11, 13);
                            break;
                        case "STAIRS":
                            this.renderSize = new Size(10, 9);
                            break;
                        case "TELEPORTER":
                            this.renderSize = new Size(8, 8);
                            break;
                        case "TERRAIN":
                            this.renderSize = new Size(14, 6);
                            break;
                        case "TOWN":
                            this.renderSize = new Size(19, 16);
                            break;
                        case "TREASURE":
                            this.renderSize = new Size(32, 32);
                            break;
                    }

                    this.hotSpot = new Point(this.renderSize.Width /2, this.renderSize.Height / 2);

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
                Debug.WriteLine($"Button Load Error:  {ex.ToString()}");
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
