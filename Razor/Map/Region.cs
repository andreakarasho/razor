using System.Collections;
using System.IO;

namespace Assistant.MapUO
{
    internal class Region
    {
        public Region(string line)
        {
            string[] textArray1 = line.Split(' ');
            X = int.Parse(textArray1[0]);
            Y = int.Parse(textArray1[1]);
            Width = int.Parse(textArray1[2]);
            Length = int.Parse(textArray1[3]);
        }

        public Region(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Length = height;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Length { get; set; }

        public int Width { get; set; }



        public static Region[] Load(string path)
        {
            if (!File.Exists(path)) return new Region[0];

            ArrayList list1 = new ArrayList();

            try
            {
                using (StreamReader reader1 = new StreamReader(path))
                {
                    string text1;

                    while ((text1 = reader1.ReadLine()) != null)
                        if (text1.Length != 0 && !text1.StartsWith("#"))
                            list1.Add(new Region(text1));
                }
            }
            catch
            {
            }

            return (Region[]) list1.ToArray(typeof(Region));
        }
    }
}