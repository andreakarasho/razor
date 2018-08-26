using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assistant.Core
{
    public class MessageInBottleCapture
    {
        private static readonly string _mibGumpLayout =
            "{ page 0 }{ resizepic 0 40 2520 350 300 }{ xmfhtmlgump 30 80 285 160 1018326 1 1 }{ htmlgump 35 240 230 20 0 0 0 }{ button 35 265 4005 4007 1 0 0 }{ xmfhtmlgump 70 265 100 20 1011036 0 0 }";

        public static bool IsMibGump(string layout)
        {
            return _mibGumpLayout.IndexOf(layout, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static void CaptureMibCoordinates(string coords)
        {
            string mibLog =
                $"{Config.GetInstallDirectory()}\\JMap\\MIBPins.csv";

            if (!Directory.Exists($"{Config.GetInstallDirectory()}\\JMap"))
            {
                Directory.CreateDirectory($"{Config.GetInstallDirectory()}\\JMap");
            }

            if (!File.Exists(mibLog))
            {
                using (StreamWriter sr = File.CreateText(mibLog))
                {
                    sr.WriteLine("Timestamp,Coordinates,X,Y,UOAM");
                }

                File.Create(mibLog);
            }

            // 130°15'N,63°16'W

            int xAxis = 0;
            int yAxis = 0;

            ConvertCoords(coords, ref xAxis, ref yAxis);

            using (StreamWriter sw = File.AppendText(mibLog))
            {
                sw.WriteLine($"{xAxis},{yAxis},mib,");
            }
        }


        private static void ConvertCoords(string coords, ref int xAxis, ref int yAxis)
        {
            string[] coordsSplit = coords.Split(',');

            string yCoord = coordsSplit[0];
            string xCoord = coordsSplit[1];

            // Calc Y first
            string[] ySplit = yCoord.Split('°');
            double yDegree = Convert.ToDouble(ySplit[0]);
            double yMinute = Convert.ToDouble(ySplit[1].Substring(0, ySplit[1].IndexOf("'", StringComparison.Ordinal)));

            if (yCoord.Substring(yCoord.Length - 1).Equals("N"))
            {
                yAxis = (int) (1624 - (yMinute / 60) * (4096.0 / 360) - yDegree * (4096.0 / 360));
            }
            else
            {
                yAxis = (int) (1624 + (yMinute / 60) * (4096.0 / 360) + yDegree * (4096.0 / 360));
            }

            // Calc X next
            string[] xSplit = xCoord.Split('°');
            double xDegree = Convert.ToDouble(xSplit[0]);
            double xMinute = Convert.ToDouble(xSplit[1].Substring(0, xSplit[1].IndexOf("'", StringComparison.Ordinal)));
            
            if (xCoord.Substring(xCoord.Length - 1).Equals("W"))
            {
                xAxis = (int) (1323 - (xMinute / 60) * (5120.0 / 360) - xDegree * (5120.0 / 360));
            }
            else
            {
                xAxis = (int) (1323 + (xMinute / 60) * (5120.0 / 360) + xDegree * (5120.0 / 360));
            }

            // Normalize values outside of map range.
            if (xAxis < 0)
                xAxis += 5120;
            else if (xAxis > 5120)
                xAxis -= 5120;

            if (yAxis < 0)
                yAxis += 4096;
            else if (yAxis > 4096)
                yAxis -= 4096;
        }
    }
}