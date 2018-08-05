using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Ultima;

namespace Assistant.JMap
{
    public class TileDisplay
    {
        private static FileIndex m_FileIndex = new FileIndex("Artidx.mul", "Art.mul", "artLegacyMUL.uop", 0x10000/*0x13FDC*/, 4, ".tga", 0x13FDC, false);

        #region TILE REFERENCE CACHING

        public IntPtr[,] WorldChunkDCs;
        public Array[,] TileCache;
        public Array[,] StaticCache;

        public int sizeX;
        public int sizeY;

        public TileDisplay()
        {
            TileCache = new Array[896, 512];//[sizeX / 8, sizeY / 8];
            StaticCache = new Array[896, 512];//[sizeX / 8, sizeY / 8];
        }
                                                      
        public void CacheTiles(int bX, int bY, ushort[,] BlockData)
        {
            ushort[,] BlockCache = BlockData;

            TileCache.SetValue(BlockData, bX, bY);
        }

        public void CacheStatics(int bX, int bY, ushort[,] BlockData)
        {
            ushort[,] BlockCache = BlockData;

            StaticCache.SetValue(BlockData, bX, bY);
        }

        public int oldXMin = 9999;
        public int oldYMin = 9999;
        public int newXMin = 9999;
        public int newYMin = 9999;

        public void UpdateCache(int nXMin, int nYMin)
        {
            if (oldXMin == 9999)
                oldXMin = nXMin;           
            if (oldYMin == 9999)
                oldYMin = nYMin;

            newXMin = nXMin;
            newYMin = nYMin;

            if (newYMin > oldYMin + 2)
            {
                for (int oX = oldXMin - 2; oX < oldXMin + 4; oX++)
                {
                    TileCache.SetValue(null, oX, oldYMin);
                    StaticCache.SetValue(null, oX, oldYMin);
                }
            }
            else if (newYMin < oldYMin - 2)
            {
                for (int oX = oldXMin - 2; oX < oldXMin + 4; oX++)
                {
                    TileCache.SetValue(null, oX, oldYMin + 2);
                    StaticCache.SetValue(null, oX, oldYMin + 2);
                }
            }

            if (newXMin > oldXMin + 2)
            {
                for (int oY = oldYMin - 2; oY < oldYMin + 4; oY++)
                {
                    TileCache.SetValue(null, oY, oldXMin);
                    StaticCache.SetValue(null, oY, oldXMin);
                }
            }
            else if (newXMin < oldXMin - 2)
            {
                for (int oY = oldYMin - 2; oY < oldYMin + 4; oY++)
                {
                    TileCache.SetValue(null, oY, oldXMin + 2);
                    StaticCache.SetValue(null, oY, oldXMin + 2);
                }
            }

            oldXMin = nXMin;
            oldYMin = nYMin;
        }

        #endregion
    }

    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public byte[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new byte[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 2, PixelFormat.Format16bppRgb565, BitsHandle.AddrOfPinnedObject());
        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            byte col = (byte)colour.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }
}
