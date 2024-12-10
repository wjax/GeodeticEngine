using GeodeticEngine.Geodetic;
using System;
using System.Globalization;
using System.IO;
using static GeodeticEngine.Geodetic.Projections;

namespace GeodeticEngine.DTM.ASC;

public class ASCDataCell
{
    private float[,] Cache;
    private int noX = 0;
    private int noY = 0;
    private double centerX = 0;
    private double centerY = 0;
    private double top = 0;
    private double left = 0;
    private float noData = -9999;
    private float cellSizeMeters = 0;
    private double cellSizeDeg = 0;

    internal static ASCDataCell LoadDataCell(string filepath)
    {
            ASCDataCell cell = null;
            try
            {
                cell = new ASCDataCell(filepath);
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("it is being used by another process"))
                {
                    System.Diagnostics.Debug.WriteLine("Bad ASC File");
                    try
                    {
                        File.Delete(filepath);
                    }
                    finally { }
                }
                
            }
            return cell;
        }

    private ASCDataCell(string filepath)
    {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("File not found.", filepath);

            Cache = ReadFileToCacheEfficient(filepath);

        }



    #region Properties

    private byte[] HgtData { get; set; }

    private int PointsPerCell { get; set; }

    public int Latitude { get; private set; }

    public int Longitude { get; private set; }

    #endregion

    #region Methods

    //private MemoryStream ReadFileToMemory(string filename)
    //{
    //    FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

    //    byte[] file = new byte[fs.Length];
    //    fs.Read(file, 0, (int)fs.Length);

    //    MemoryStream m = new MemoryStream(file);

    //    fs.Close();

    //    return m;
    //}

    private float[,] ReadFileToCache(string filepath)
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
                        left = double.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("yllcorner", true, CultureInfo.InvariantCulture))
                    {
                        top = double.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("xllcenter", true, CultureInfo.InvariantCulture))
                    {
                        centerX = double.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("yllcenter", true, CultureInfo.InvariantCulture))
                    {
                        centerY = float.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("cellsize", true, CultureInfo.InvariantCulture))
                    {
                        cellSizeMeters = float.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("NODATA_value", true, CultureInfo.InvariantCulture))
                    {
                        noData = float.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else
                        break;
                }


                if (top !=0 && left!=0)
                {
                    top += cellSizeMeters * noY; 
                }
                else if (centerX != 0 && centerY != 0)
                {
                    top = centerY + cellSizeMeters * (noY - 0.5f);
                    left = centerX - cellSizeMeters / 2;
                }
                                

                cache = new float[noX, noY];

                // Actual Data extraction
                bool stop = false;
                int iRow = 0;
                while(!stop)
                {
                    line.Trim();
                    string[] data = line.Split(' ');

                    if (data.Length >= (noX + 1))
                    {
                        for (int i = 0; i < noX; i++)
                        {
                            if (float.TryParse(data[i], NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                            {
                                cache[i, iRow] = value;
                            }
                            else
                            {

                            }
                            //cache[i, iRow] = float.Parse(data[i], CultureInfo.InvariantCulture);
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

    private float[,] ReadFileToCacheEfficient(string filepath)
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
                        left = double.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("yllcorner", true, CultureInfo.InvariantCulture))
                    {
                        top = double.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("xllcenter", true, CultureInfo.InvariantCulture))
                    {
                        centerX = double.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("yllcenter", true, CultureInfo.InvariantCulture))
                    {
                        centerY = float.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("cellsize", true, CultureInfo.InvariantCulture))
                    {
                        cellSizeMeters = float.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("NODATA_value", true, CultureInfo.InvariantCulture))
                    {
                        noData = float.Parse(line.Substring(line.IndexOf(' ')), NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else
                        break;
                }


                if (top != 0 && left != 0)
                {
                    top += cellSizeMeters * noY;
                }
                else if (centerX != 0 && centerY != 0)
                {
                    top = centerY + cellSizeMeters * (noY - 0.5f);
                    left = centerX - cellSizeMeters / 2;
                }


                cache = new float[noX, noY];

                // Actual Data extraction
                bool stop = false;
                int iRow = 0;
                while (!stop)
                {
                    line.Trim();
                    ReadOnlySpan<char> lineSpan = line.AsSpan();
                    int colIndex = 0;

                    while (lineSpan.Length > 0)
                    {
                        int nextSpace = lineSpan.IndexOf(' ');
                        ReadOnlySpan<char> numberSpan;

                        if (nextSpace == -1)
                        {
                            numberSpan = lineSpan;
                            lineSpan = ReadOnlySpan<char>.Empty;
                        }
                        else
                        {
                            numberSpan = lineSpan.Slice(0, nextSpace);
                            lineSpan = lineSpan.Slice(nextSpace + 1);
                        }

                        if (float.TryParse(numberSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                        {
                            cache[colIndex++, iRow] = value;
                        }
                    }

                    iRow++;

                    if (sr.EndOfStream)
                        stop = true;
                    else
                        line = sr.ReadLine();
                }
            }

            return cache;
        }

    private double GetAlt(double lat, double lon)
    {
            double diffX = lon - left;
            double diffY = top - lat;

            if (diffX < 0 || diffY < 0)
                return 0;

            int iX = (int)(diffX / cellSizeDeg);
            int iY = (int)(diffY / cellSizeDeg);

            if (iX < noX && iY < noY)
                return Cache[iX, iY];
            else
                return double.MinValue;
        }

    private float GetAltUTM(double lat, double lon, bool interpolate=false)
    {
            UTMPosition pos = Projections.ToUTM(lat, lon);

            double diffX = pos.easting - left;
            double diffY = top - pos.northing;

            if (diffX < 0 || diffY < 0)
                return float.MinValue;

            float fX = (float)(diffX / cellSizeMeters);
            float fY = (float)(diffY / cellSizeMeters);

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

    private float GetValue(int x, int y, float fallbackValue = float.MinValue)
    {
            if (x < noX && y < noY && x >= 0 && y >= 0)
                return Cache[x, y];
            else
                return fallbackValue;
        }

    private float Average(float v1, float v2, float weight)
    {
            return v2 * weight + v1 * (1 - weight);
        }


    public ElevationResponse GetElevation(double lat, double lon, bool interpolate)
    {
            float elevationValue = GetAltUTM(lat, lon, interpolate);

            if (elevationValue > float.MinValue)
                return new ElevationResponse()
                {
                    Source = ElevationResponse.SOURCE.ASC,
                    TileType = ElevationResponse.TILE_TYPE.Valid,
                    Elevation = elevationValue,
                    Lat = lat,
                    Lon = lon
                };
            else
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.ASC);
        }

    #endregion
}