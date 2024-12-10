using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GeodeticEngine.Geodetic;

public static class Geoid
{
    // Based in ASC File
    private static float[,] Cache;
    private static int noX = 0;
    private static int noY = 0;
    private static double centerX = 0;
    private static double centerY = 0;
    private static double top = 0;
    private static double left = 0;
    private static int noData = -9999;
    private static float cellSizeMeters = 0;
    private static double cellSizeDeg = 0;

    private static bool ready = false;
    public static bool Ready { get { return ready; } }

    static Geoid()
    {
            Task.Run (() => Load());
        }

    public static void Load(string path = @"GeodeticResources\egm96.asc")
    {
            Cache = ReadFileToCache(path);
            ready = true;
        }

    private static float[,] ReadFileToCache(string filepath)
    {
            float[,] cache = null;

            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line = "";

                // Read header data from file before actually parsing it
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.StartsWith("ncols", true, CultureInfo.InvariantCulture))
                    {
                        noX = int.Parse(line.Substring(line.IndexOf(' ')));
                    }
                    else if (line.StartsWith("nrows", true, CultureInfo.InvariantCulture))
                    {
                        noY = int.Parse(line.Substring(line.IndexOf(' ')));
                    }
                    else if (line.StartsWith("xllcorner", true, CultureInfo.InvariantCulture))
                    {
                        left = double.Parse(line.Substring(line.IndexOf(' ')), CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("yllcorner", true, CultureInfo.InvariantCulture))
                    {
                        top = double.Parse(line.Substring(line.IndexOf(' ')), CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("xllcenter", true, CultureInfo.InvariantCulture))
                    {
                        centerX = double.Parse(line.Substring(line.IndexOf(' ')), CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("yllcenter", true, CultureInfo.InvariantCulture))
                    {
                        centerY = float.Parse(line.Substring(line.IndexOf(' ')), CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("cellsize", true, CultureInfo.InvariantCulture))
                    {
                        cellSizeDeg = float.Parse(line.Substring(line.IndexOf(' ')), CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("NODATA_value", true, CultureInfo.InvariantCulture))
                    {
                        noData = int.Parse(line.Substring(line.IndexOf(' ')));
                    }
                    else
                        break;
                }

                if (top != 0 && left != 0)
                {
                    top += cellSizeDeg * noY;
                }
                else if (centerX != 0 && centerY != 0)
                {
                    top = centerY + cellSizeDeg * (noY - 0.5f);
                    left = centerX - cellSizeDeg / 2;
                }


                cache = new float[noX, noY];

                // Actual Data extraction
                bool stop = false;
                int iRow = 0;
                while (!stop)
                {
                    string[] data = line.Split(' ');

                    if (data.Length >= (noX))
                    {
                        for (int i = 0; i < noX; i++)
                        {
                            cache[i, iRow] = float.Parse(data[i], CultureInfo.InvariantCulture);
                        }
                        iRow++;
                    }

                    if (sr.EndOfStream)
                        stop = true;
                    else
                        line = sr.ReadLine();
                }
            }

            return cache;
        }

    private static float GetValue(int x, int y, float fallbackValue = float.MinValue)
    {
            if (x < noX && y < noY && x >= 0 && y >= 0)
                return Cache[x, y];
            else
                return fallbackValue;
        }


    public static float GetUndulation(double lat, double lon, bool interpolate = false)
    {
            if (!ready)
                return float.MinValue;

            double lonW = Tools.MathHelper.WrapAngle360(lon);

            double diffX = Math.Abs(lonW - left);
            double diffY = Math.Abs(top - lat);

            float fX = (float)(diffX / cellSizeDeg);
            float fY = (float)(diffY / cellSizeDeg);

            int iX = (int)fX;
            int iY = (int)fY;

            if (interpolate)
            {
                float alt00 = GetValue(iX, iY);
                float alt10 = GetValue(iX + 1, iY, alt00);
                float alt01 = GetValue(iX, iY + 1, alt00);
                float alt11 = GetValue(iX + 1, iY + 1, alt00);

                float v1 = Average(alt00, alt10, fX - iX);
                float v2 = Average(alt01, alt11, fX - iX);
                float elevationValue = Average(v1, v2, 1 - (fY - iY));

                return elevationValue;
            }
            else
            {
                return GetValue(iX, iY);
            }
        }

    private static float Average(float v1, float v2, float weight)
    {
            return v2 * weight + v1 * (1 - weight);
        }
}