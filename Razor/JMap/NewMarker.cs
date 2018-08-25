using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Assistant.JMap
{
    public partial class NewMarker : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public MapPanel mapPanel;

        public string name;
        public int x;
        public int y;
        public string extra;
        public CheckBox IsPublic;
        public string MarkerOwner;
        public string id;

        public NewMarker(MapPanel mapPan)
        {
            InitializeComponent();
            mapPanel = mapPan;
            IsPublic = this.IsPublicCheckbox;
            

            if (mapPanel.AddingMarker)
                CreateMarker();

            if (mapPanel.EditingMarker)
                EditMarker(mapPanel.MarkerToEdit);

            this.MouseDown += NewMarker_MouseDown;
        }

        private void CreateMarker()
        {
            PointF lastPos = mapPanel.mouseLastPosOnMap;
            x = Convert.ToInt32(Math.Floor(lastPos.X));
            y = Convert.ToInt32(Math.Floor(lastPos.Y));

            this.newMarkerX.Text = x.ToString();
            this.newMarkerY.Text = y.ToString();
            this.OwnerLabel.Text = mapPanel.FocusMobile.Name;

            if (this.IsPublic.Checked)
                this.id = "PublicLocations";
            else
                this.id = "MarkedLocations";


            mapPanel.MarkerToEdit = null;
        }

        private void EditMarker(JMapButton markerToEdit)
        {
            x = Convert.ToInt32(Math.Floor(markerToEdit.mapLoc.X));
            y = Convert.ToInt32(Math.Floor(markerToEdit.mapLoc.Y));

            this.newMarkerName.Text = markerToEdit.displayText;
            this.newMarkerX.Text = x.ToString();
            this.newMarkerY.Text = y.ToString();
            this.newMarkerExtra.Text = markerToEdit.extraText;
            this.IsPublic.Checked = markerToEdit.IsPublic;
            this.OwnerLabel.Text = markerToEdit.MarkerOwner;

            if (this.IsPublic.Checked)
                this.id = "PublicLocations";
            else
                this.id = "MarkedLocations";

        }

        private void NewMarkerPoint(object sender, EventArgs e)
        {
            if (mapPanel.AddingMarker)
            {
                if (this.IsPublic.Checked)
                    this.id = "PublicLocations";
                else
                    this.id = "MarkedLocations";

                mapPanel.AddMarker(new Point(x, y), mapPanel.FocusMobile.Name, IsPublic.Checked, this.id, this.newMarkerName.Text, this.newMarkerExtra.Text);
            }
            if(mapPanel.EditingMarker)
            {
                if (this.IsPublic.Checked)
                    mapPanel.MarkerToEdit.id = "PublicLocations";
                else
                    mapPanel.MarkerToEdit.id = "MarkedLocations";

                mapPanel.MarkerToEdit.mapLoc = new Point(x, y);
                mapPanel.MarkerToEdit.displayText = this.newMarkerName.Text;
                mapPanel.MarkerToEdit.extraText = this.newMarkerExtra.Text;
                mapPanel.MarkerToEdit.MarkerOwner = this.OwnerLabel.Text;
                mapPanel.MarkerToEdit.IsPublic = this.IsPublic.Checked;

                mapPanel.MarkerToEdit.LoadButton();
            }
            if (this.IsPublic.Checked)
            {
                mapPanel.SendPublicMarker(new Point(x, y), this.MarkerOwner, this.IsPublic.Checked, this.newMarkerName.Text, this.newMarkerExtra.Text);
            }


            mapPanel.EditingMarker = false;
            mapPanel.AddingMarker = false;
            mapPanel.MarkerToEdit = null;

            mapPanel.UpdateAll();

            this.Dispose();

        }

        private void CancelPoint(object sender, EventArgs e)
        {
            mapPanel.EditingMarker = false;
            mapPanel.AddingMarker = false;
            mapPanel.MarkerToEdit = null;

            this.Dispose();
        }

        protected override void OnShown(EventArgs e)
        {
            this.Focus();
            this.BringToFront();
            this.newMarkerName.Focus();
            base.OnShown(e);
        }

        private void NewMarker_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
