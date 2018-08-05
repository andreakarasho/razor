using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Assistant.JMap
{
    static class Markers
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursorFromFile(string path);
        
        [DllImport("user32")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO pIconInfo);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        public static Bitmap BitmapFromCursor(Cursor cur)
        {
            ICONINFO ii;
            GetIconInfo(cur.Handle, out ii);

            Bitmap bmp = Bitmap.FromHbitmap(ii.hbmColor);

            //DeleteObject(ii.hbmColor);
            //DeleteObject(ii.hbmMask);

            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, cur.Size.Width, cur.Size.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Bitmap dstBitmap = new Bitmap(bmData.Width, bmData.Height, bmData.Stride, PixelFormat.Format32bppArgb, bmData.Scan0);
            bmp.UnlockBits(bmData);



            return new Bitmap(dstBitmap);
        }
        
        public static Cursor LoadCursor(string path)
        {
            IntPtr handle = LoadCursorFromFile(path);
            if (handle == IntPtr.Zero) throw new Win32Exception();
            var curs = new Cursor(handle);
            // Note: force the cursor to own the handle so it gets released properly
            var fi = typeof(Cursor).GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(curs, true);
            return curs;
        }

        public static Cursor CursorFromBitmap(Bitmap bmp)
        {
            IntPtr handle = bmp.GetHbitmap(Color.White);
            if (handle == IntPtr.Zero) throw new Win32Exception();
            var curs = new Cursor(handle);
            var fi = typeof(Cursor).GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(curs, true);
            return curs;
        }
    }
}
