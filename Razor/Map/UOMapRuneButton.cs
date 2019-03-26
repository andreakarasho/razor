using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using Ultima;

namespace Assistant.MapUO
{
    internal class UOMapRuneButton
    {
        public UOMapRuneButton(int bookid, int runeSpot, int x, int y)
        {
            BookID = bookid;
            RuneSpot = runeSpot;
            X = x;
            Y = y;
            Icon = Art.GetStatic(7956);
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int BookID { get; set; }

        public int RuneSpot { get; set; }

        public Bitmap Icon { get; set; }

        public static ArrayList Load(string path)
        {
            ArrayList buttonlist = new ArrayList();
            //if (!File.Exists(path))
            // {
            //    return buttonlist;
            // }
            buttonlist.Add(new UOMapRuneButton(0, 0, 1158, 743));
            buttonlist.Add(new UOMapRuneButton(0, 0, 3230, 305));

            //XML shit
            return buttonlist;
        }

        public void OnClick(MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:

                    //recall
                    break;
                case MouseButtons.Right:

                    //gate
                    break;
            }
        }
    }
}