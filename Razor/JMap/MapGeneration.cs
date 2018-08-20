using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Ultima;

namespace Assistant.JMap
{
    public class MapGeneration
    {
        int fwdfound = 0;
        int revfound = 0;
        int totaldiff = 0;
        int perfectMatch = 0;

        public Ultima.Map map;
        public Bitmap newMap;
        int sizeX = 0;
        int sizeY = 0;

        public Bitmap heightMap;

        public short[] data = new short[64];

        public Tile[] lTile = new Tile[64];
        public HuedTile[] sTile = new HuedTile[64];

        public TileDisplay tileDisplay;

        public MapGeneration(JimmyMap main, MapPanel mapPanel, int MapID, TileDisplay tileDisp)
        {
            /*if (!Directory.Exists($"{Config.GetInstallDirectory()}\\Map"))
            {
                Directory.CreateDirectory($"{Config.GetInstallDirectory()}\\Map");
            }*/

            //genProgressBar = new ProgressBar();
            


            this.tileDisplay = tileDisp;
            Debug.WriteLine("Gen called!");

            
            
            //DEFAULT, FELUCCA MAP
            map = JMap.Map.GetMap(MapID);
            if (map == null)
                map = Ultima.Map.Felucca;
            


            RiffPalette _loadedPalette = new RiffPalette();
            _loadedPalette.Load($"{Config.GetInstallDirectory()}\\JMap\\Resources\\Palettes\\felucca.pal");

            //RiffPalette _loadedHeightPalette = new RiffPalette();
            //_loadedHeightPalette.Load(@"F:\UOStuff\\UOArt\\Maps\\palettes\\heightmap.pal");

            //Debug.WriteLine("Map Width: {0}, Map Height: {1}", map.Width, map.Height);



            Color hue = Color.Empty;
            int match = -1;
            int heightMatch = -1;
            byte newCol = 0;

            int cIndex = 0;
            int col = 0;
            int row = 0;
            int tX = 0;
            int tY = 0;
            int height = 0;

            sizeX = map.Width;
            sizeY = map.Height;

            newMap = new Bitmap(sizeX, sizeY, PixelFormat.Format8bppIndexed);
            ColorPalette newPalette = newMap.Palette;

            for (int i = 0; i < 256; i++)
            {
                Color b = new Color();
                b = Color.FromArgb(_loadedPalette._palette[i].R, _loadedPalette._palette[i].G, _loadedPalette._palette[i].B);
                newPalette.Entries.SetValue(b, i);

            }
            newMap.Palette = newPalette;

            LockBitmap lockBitmap = new LockBitmap(newMap);
            lockBitmap.LockBits();

            ColorManager colorTable = new ColorManager();
            //RadarCol.ExportToCSV(@"F:\UOStuff\\UO Art\\Maps\\RadarCols\\radarColors.csv");


            #region HEIGHTMAP
            /*
            ColorManager heightColorTable = new ColorManager();

            heightMap = new Bitmap(sizeX, sizeY, PixelFormat.Format8bppIndexed);
            ColorPalette hmPalette = heightMap.Palette;

            for (int i = 0; i < 256; i++)
            {
                Color b = new Color();
                b = Color.FromArgb(_loadedHeightPalette._palette[i].R, _loadedHeightPalette._palette[i].G, _loadedHeightPalette._palette[i].B);
                hmPalette.Entries.SetValue(b, i);

            }
            heightMap.Palette = hmPalette;

            LockBitmap heightLockBitmap = new LockBitmap(heightMap);
            heightLockBitmap.LockBits();
            */
            #endregion
            int currentStep = 0;
            int maxStep = 512; //(map.Height / 8) + (map.Width / 8);

            //main.mapGenProgressBar.progressBar.Maximum = maxStep;

            Random rnd = new Random();

            for (int bY = 0; bY < map.Height / 8; ++bY)                             //Iterate through each row
            {
                for (int bX = 0; bX < map.Width / 8; ++bX)                          //Iterate through each colum position per row 0-64 
                {
                    //ushort[,] BlockCache = new ushort[8,8];                               //tile reference cache
                    //byte[,] StaticCache = new byte[8, 8];


                    //lTile = map.Tiles.GetLandBlock(bX, bY).ToArray();                     //tile stuff
                    //sTile = map.Tiles.GetStaticTiles(bX, bY).ToArray();
                    
                    data = map.GetRenderedBlock(bX, bY, true);                      //Grab data  data for current position
                    cIndex = 0;                                                     //reset color data index                    

                    for (tY = 0; tY < 8; ++tY)                                      //iterate our 8x8 Y
                    {
                        for (tX = 0; tX < 8; ++tX)                                  //iterate our 8x8 X  
                        {
                            //Debug.WriteLine("Tile ID:{0} Z:{1}", lTile[cIndex].ID, lTile[cIndex].Z);
                            //Debug.WriteLine("LTILE: {0 RAW LTILE: {1}", (byte)lTile[cIndex].ID, Art.GetRawLand((byte)lTile[cIndex].ID));

                            //if(Art.IsValidLand((byte)lTile[cIndex].ID))
                            //{
                                //BlockCache.SetValue(lTile[cIndex].ID, tX, tY);
                            //}

                            /*if(Art.IsValidStatic((byte)sTile[cIndex].ID))
                            {
                                StaticCache.SetValue((byte)sTile[cIndex].ID, tY, tX);
                            }*/
                            

                            //if(sTile[cIndex].ID != null)
                            //{
                            //
                            //}
                            /*
                            if (lTile[cIndex].Z < 0)
                            {
                                height = 0;
                            }    
                            else
                            {
                                height = lTile[cIndex].Z;
                                //height /= 2;
                            }

                            //normal values are just too dark
                            if(height == 0)
                            {
                                height = 255;
                            }
                            else
                            {
                                height += 70;
                            }
                            */

                            //Manual color pair table
                            //Debug.WriteLine("Index: " + data[cIndex]);
                            if (data[cIndex] == 234)                                //Force particular blue colour
                            {
                                match = 2;
                            }
                            if (data[cIndex] == 202)                                //Range of colours available for coastal water
                            {
                                match = rnd.Next(55, 69);
                            }
                            else
                            {
                                //Heightmap color lookup table
                                /*heightMatch = heightColorTable.GetPair((short)lTile[cIndex].Z);

                                if (heightMatch == -1)
                                {
                                    colorTable.AddPair(
                                    (short)lTile[cIndex].Z,
                                    (byte)heightMatch);
                                }*/

                                //UltimaSDK to Palette color matching lookup table - this table massively speeds generation up.
                                match = colorTable.GetPair(data[cIndex]);


                                //match = colorTable.GetPair((short)lTile[cIndex].ID);

                                if (match == -1)
                                {
                                    //Debug.WriteLine("No match in our color table");

                                    //hue = Hues.HueToColor(data[cIndex]);  //<<Originally called ultimasdk, we just do it ourselves instead
                                    
                                    //grab hue from tile, convert to rgb
                                    const int scaleR = 255 / 31;
                                    const int scaleG = 255 / 31;
                                    const int scaleB = 255 / 31;
                                    hue = Color.FromArgb(
                                                        (((data[cIndex] & 0x7c00) >> 10) * scaleR), //(short)lTile[cIndex].ID
                                                        (((data[cIndex] & 0x3e0) >> 5) * scaleG),   //(short)lTile[cIndex].ID
                                                        (((data[cIndex] & 0x1f)) * scaleB)          //(short)lTile[cIndex].ID
                                                        );

                                    //Adjust RGB value based on heightmap values
                                    /*if(hue != Color.FromArgb(0,0,0))
                                    {
                                        hue = Color.FromArgb(
                                                            Math.Max(hue.R - height, 0),
                                                            Math.Max(hue.G - height, 0),
                                                            Math.Max(hue.B - height, 0)
                                                            );
                                    }*/


                                    match = MatchColor(hue, newMap);

                                    //new index/color pair - Add it to the table!
                                    colorTable.AddPair(
                                                        data[cIndex],               
                                                        (byte)match);
                                }
                                else
                                {
                                    //Debug.WriteLine("We have this color already! Index: {0} Color: {1}", data[cIndex], color8b);
                                }
                            }

                            if ((tX) + col < map.Width)                                  //Ensure we arent outside array bounds                
                            {

                                //heightLockBitmap.SetPixel(tX + col, tY + row, (byte)height);
                                lockBitmap.SetPixel(tX + col, tY + row, (byte)match);

                                ++cIndex;                                                //advance color data index
                            }
                        }
                    }
                    if ((tX) + col >= map.Width)
                    {
                        row += 8;                       //we need a new row, so move 8 pixels down
                        col = 0;                        //we moved down, reset columns to zero
                    }
                    else
                    {
                        col += 8;                           //we need a new block, so move 8 pixels right    
                    }

                    //tileDisplay.CacheTiles(bX, bY, BlockCache);
                    //tileDisplay.CacheStatics(bX, bY, StaticCache);

                    //currentStep



                    //int step = Convert.ToInt32(Math.Floor((double)((currentStep / maxStep) * 100)));

                    //Debug.WriteLine($"Current:{currentStep} Max:{maxStep} ReportedStep:{step}");

                    //main.mapGenWorker.ReportProgress(step);

                }

                data = new short[0];

                currentStep++;
                int step = (currentStep / maxStep) * maxStep;
                //Debug.WriteLine($"Current:{currentStep} Max:{maxStep}");
                main.mapGenWorker.ReportProgress(currentStep);
            }


            Debug.WriteLine("DONE!");
            lockBitmap.UnlockBits();
            //heightLockBitmap.UnlockBits();

            totaldiff = fwdfound - revfound;

            //Debug.WriteLine("Forward Found: {0}, Reverse Found: {1}, Total Diff: {2}", fwdfound, revfound, totaldiff);
            //Debug.WriteLine("Perfect Matches: {0}", perfectMatch);
            /*
                        using (StreamWriter file = new StreamWriter(@"F: \UOStuff\\UOArt\\Maps\\RadarCols\\pairs.txt"))
                        {
                            foreach (KeyValuePair<short, int> pair in colorTable.colorTable)
                            {
                                file.WriteLine("[{0} {1}]", pair.Key, pair.Value);
                            }
                        }
            */

            newMap.Save($"{Config.GetInstallDirectory()}\\JMap\\MAP0-1.BMP", ImageFormat.Bmp);

            //Bitmap newMap2 = new Bitmap(newMap, sizeX / 2, sizeY / 2);
            //newMap2.Save($"{Config.GetInstallDirectory()}\\JMap\\MAP0-2.BMP", ImageFormat.Bmp);

            //Bitmap newMap4 = new Bitmap(newMap, sizeX / 4, sizeY / 4);
            //newMap4.Save($"{Config.GetInstallDirectory()}\\JMap\\MAP0-4.BMP", ImageFormat.Bmp);

            //Bitmap newMap8 = new Bitmap(newMap, sizeX / 8, sizeY / 8);
            //newMap8.Save($"{Config.GetInstallDirectory()}\\JMap\\MAP0-8.BMP", ImageFormat.Bmp);

            lockBitmap.source = null;
            lockBitmap.bitmapData = null;
            lockBitmap = null;
            colorTable.colorTable.Clear();
            colorTable = null;

            newMap.Dispose();
            //newMap2.Dispose();
            //newMap4.Dispose();
            //newMap8.Dispose();

            /*
            heightMap.Save(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-1-HM.BMP", ImageFormat.Bmp);

            Bitmap heightMap2 = new Bitmap(heightMap, sizeX / 2, sizeY / 2);
            heightMap2.Save(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-2-HM.BMP", ImageFormat.Bmp);

            Bitmap heightMap4 = new Bitmap(heightMap, sizeX / 4, sizeY / 4);
            heightMap4.Save(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-4-HM.BMP", ImageFormat.Bmp);

            Bitmap heightMap8 = new Bitmap(heightMap, sizeX / 8, sizeY / 8);
            heightMap8.Save(@"F:\UOStuff\\UOArt\\Maps\\Generated\\Regular\\MAP0-8-HM.BMP", ImageFormat.Bmp);

            //heightLockBitmap.source = null;
            //heightLockBitmap.bitmapData = null;
            //heightLockBitmap = null;

            heightMap.Dispose();
            heightMap2.Dispose();
            heightMap4.Dispose();
            heightMap8.Dispose();
            */
        }

        public byte rgbStrongCheck(Color color)
        {
            byte rgbStrength = 0;

            // Determine the strongest R,G or B value
            if (color.R > color.G && color.R > color.B)
                rgbStrength = 1;
            else if (color.G > color.R && color.G > color.B)
                rgbStrength = 2;
            else
                rgbStrength = 3;

            return rgbStrength;
        }

        public byte MatchColor(Color original, Bitmap map)
        {
            int leastDistanceFwd = int.MaxValue;
            int leastDistanceRev = int.MaxValue;
            //int alpha = original.A;
            int red = original.R;
            int green = original.G;
            int blue = original.B;
            byte colorIndexFwd = 0;
            byte colorIndexRev = 0;
            Color col = Color.Empty;

            int distanceFwd = 0;
            int distanceRev = 0;

            byte originalStrong = 0; //represents which rgb value is higher than the others r=1, b=2, c=3 - original color
            byte paletteStrongFwd = 0; //represents which rgb value is higher than the others r=1, b=2, c=3 - fwdCheck
            byte paletteStrongRev = 0; //represents which rgb value is higher than the others r=1, b=2, c=3 - revCheck

            originalStrong = rgbStrongCheck(original);



            // Loop through the entire palette BACKWARD, looking for the closest color match
            for (int indexR = map.Palette.Entries.Length - 1; indexR > -1; indexR--)
            {
                //if (distanceFwd == 0)
                //    break;
                // Lookup the color from the palette
                Color paletteColor = map.Palette.Entries[indexR];

                // Check the strongest color value
                paletteStrongRev = rgbStrongCheck(paletteColor);

                // Compute the distance from our source color to the palette color
                int redDistance = (paletteColor.R - red);// * (100 / 100);
                int greenDistance = (paletteColor.G - green);// * (100 / 100);
                int blueDistance = (paletteColor.B - blue);// * (100 / 100);

                distanceRev = (redDistance * redDistance) +
                              (greenDistance * greenDistance) +
                              (blueDistance * blueDistance);

                // If the color is closer than any other found so far, use it
                if (distanceRev < leastDistanceRev)
                {
                    colorIndexRev = (byte)indexR;
                    leastDistanceRev = distanceRev;

                    // And if it's an exact match, exit the loop
                    if (distanceRev == 0)
                    {
                        perfectMatch++;
                        break;
                    }
                }
            }

            // Loop through the entire palette FORWARD, looking for the closest color match
            for (int indexF = 0; indexF < map.Palette.Entries.Length; indexF++)
            {
                // Lookup the color from the palette
                Color paletteColor = map.Palette.Entries[indexF];

                // Set the strongest color value
                paletteStrongFwd = rgbStrongCheck(paletteColor);

                // Compute the distance from our source color to the palette color
                int redDistance = (paletteColor.R - red) * (100 / 100);
                int greenDistance = (paletteColor.G - green) * (100 / 100);
                int blueDistance = (paletteColor.B - blue) * (100 / 100);
                
                distanceFwd = (redDistance * redDistance) +
                              (greenDistance * greenDistance) +
                              (blueDistance * blueDistance);

                // If the color is closer than any other found so far, use it
                if (distanceFwd < leastDistanceFwd)// && paletteStrongFwd == originalStrong)
                {
                    colorIndexFwd = (byte)indexF;
                    leastDistanceFwd = distanceFwd;

                    // And if it's an exact match, exit the loop
                    if (distanceFwd == 0)// && paletteStrongFwd == originalStrong)
                    {
                        perfectMatch++;
                        break;
                    }

                }
            }

            //Debug.WriteLine("LeastDistanceFwd: {0} With Index: {1} ", leastDistanceFwd, colorIndexFwd);
            //Debug.WriteLine("LeastDistanceRev: {0} With Index: {1} ", leastDistanceRev, colorIndexRev);
            
            int indexDiff = colorIndexFwd - colorIndexRev;
            
            if(distanceFwd == distanceRev) // Conflict? Which has the matching high value?
            {
                if (paletteStrongFwd > paletteStrongRev)
                {
                    //Debug.WriteLine("Conflict won by Fwd, high value: " + paletteStrongFwd);
                    //fwdfound++;
                    return colorIndexFwd;
                }
                //Debug.WriteLine("Conflict won by Rev, high value: " + paletteStrongRev);
                revfound++;
                return colorIndexRev;
            }
            
            if(distanceFwd < distanceRev)
            {
                //Debug.WriteLine("Forward was best. {0} indices different.", indexDiff);
                //fwdfound++;
                return colorIndexFwd;
            }
            /*
            Debug.WriteLine("Reverse was best. {0} indices different.", indexDiff);
            revfound++;
            
            */
            return colorIndexRev;



            //return colorIndexRev;
            //return colorIndexFwd;
        }

    }

    public class ColorManager
    {
        //create a cached color look up array
        public Dictionary<short, int> colorTable = new Dictionary<short, int>();

        //add index/color pairs
        public void AddPair(short index, int color)
        {
            colorTable.Add(index, color);
        }
        //retrieve index/color pairs
        public int GetPair(short index)
        {
            //Color color = Color.Empty;
            //colorTable.TryGetValue(index, out short color);
            if (colorTable.TryGetValue(index, out int color))
            {
                return color;
            }
            return -1;
        }
    }

    public class LockBitmap
    {
        public Bitmap source = null;
        public IntPtr Iptr = IntPtr.Zero;
        public BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = source.Width;
                Height = source.Height;

                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                Rectangle rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.WriteOnly,
                                             source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            Color clr = Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, byte color)
        {
            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x * cCount);
            /*
            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            */
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color;
                //Debug.WriteLine("ByteColor: " + Pixels[i]); 
            }
        }
    }


}
