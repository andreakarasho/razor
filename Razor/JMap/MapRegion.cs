using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace Assistant.JMap
{
    class MapRegion
    {
        private float m_Height;
        private float m_Width;
        private float m_X;
        private float m_Y;
        public MapRegion(string line)
        {
            string[] textArray1 = line.Split(new char[] { ' ' });
            this.m_X = float.Parse(textArray1[0]);
            this.m_Y = float.Parse(textArray1[1]);
            this.m_Width = float.Parse(textArray1[2]);
            this.m_Height = float.Parse(textArray1[3]);
        }

        public MapRegion(float x, float y, float width, float height)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Width = width;
            this.m_Height = height;
        }

        public static MapRegion[] Load(string path)
        {
            if (!File.Exists(path))
            {
                
                return new MapRegion[0];
                
            }
            ArrayList list1 = new ArrayList();
            try
            {
                Debug.WriteLine("Guard Def Found!");
                using (StreamReader reader1 = new StreamReader(path))
                {
                    string text1;
                    while ((text1 = reader1.ReadLine()) != null)
                    {
                        if ((text1.Length != 0) && !text1.StartsWith("#"))
                        {
                            list1.Add(new MapRegion(text1));
                        }
                    }
                }
            }
            catch
            {
            }
            return (MapRegion[])list1.ToArray(typeof(MapRegion));
        }

        public float X
        {
            get { return m_X; }
            set { m_X = value; }
        }
        public float Y
        {
            get { return m_Y; }
            set { m_Y = value; }
        }
        public float Length
        {
            get { return m_Height; }
            set { m_Height = value; }
        }
        public float Width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }

    }
}
