using System;
using System.IO;

namespace GeodeticEngine.DTM.SRTM
{
    public class SRTMDataCell
    {
        private short[,] Cache;

        internal static SRTMDataCell LoadDataCell(string filepath)
        {
            SRTMDataCell cell = null;
            try
            {
                cell = new SRTMDataCell(filepath);
            }
            catch (Exception e)
            {
                //if (!e.Message.Contains("it is being used by another process"))
                //{
                //    System.Diagnostics.Debug.WriteLine("Bad SRTM File");
                //    try
                //    {
                //        File.Delete(filepath);
                //    }
                //    finally { }
                //}
                
            }
            return cell;
        }

        private SRTMDataCell(string filepath)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("File not found.", filepath);

            var filename = Path.GetFileName(filepath);
            filename = filename.Substring(0, filename.IndexOf('.')).ToLower(); // Path.GetFileNameWithoutExtension(filepath).ToLower();
            var fileCoordinate = filename.Split(new[] { 'e', 'w' });
            if (fileCoordinate.Length != 2)
                throw new ArgumentException("Invalid filename.", filepath);

            fileCoordinate[0] = fileCoordinate[0].TrimStart(new[] { 'n', 's' });

            Latitude = int.Parse(fileCoordinate[0]);
            if (filename.Contains("s"))
                Latitude *= -1;

            Longitude = int.Parse(fileCoordinate[1]);
            if (filename.Contains("w"))
                Longitude *= -1;

            if (filepath.EndsWith(".zip"))
            {
                using (var stream = File.OpenRead(filepath))
                using (var archive = new System.IO.Compression.ZipArchive(stream))
                using (var memoryStream = new MemoryStream())
                {
                    using (var hgt = archive.Entries[0].Open())
                    {
                        hgt.CopyTo(memoryStream);
                        //HgtData = memoryStream.ToArray();
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        Cache = ReadFileToCache(memoryStream);
                    }
                }
            }
            else
            {
                //HgtData = File.ReadAllBytes(filepath);
                using (var stream = File.OpenRead(filepath))
                {
                    Cache = ReadFileToCache(stream);
                }
            }

    
        }



        #region Properties

        private byte[] HgtData { get; set; }

        private int PointsPerCell { get; set; }

        public int Latitude { get; private set; }

        public int Longitude { get; private set; }

        #endregion

        #region Methods

        private short[,] ReadFileToCache(Stream _stream)
        {
            switch (_stream.Length)
            {
                case 1201 * 1201 * 2: // SRTM-3
                    PointsPerCell = 1201;
                    break;
                case 3601 * 3601 * 2: // SRTM-1
                    PointsPerCell = 3601;
                    break;
                default:
                    throw new ArgumentException("Invalid file size");
            }

            short[,] cache = new short[PointsPerCell, PointsPerCell];
            byte[] auxbytes = new byte[2];

            int altlat = 0;
            int altlng = 0;

            while (_stream.Read(auxbytes, 0, 2) != 0)
            {
                cache[altlat, altlng] = (short)((auxbytes[0] << 8) + auxbytes[1]);

                altlat++;
                if (altlat >= PointsPerCell)
                {
                    altlng++;
                    altlat = 0;
                }
            }

            return cache;
        }

        private float GetValue(int x, int y, float fallbackValue = float.MinValue)
        {
            if (x < PointsPerCell && y < PointsPerCell && x >= 0 && y >= 0)
                return Cache[x, y];
            else
                return fallbackValue;
        }

        private float Average(float v1, float v2, float weight)
        {
            return v2 * weight + v1 * (1 - weight);
        }

        public ElevationResponse GetElevationBad(double latitude, double longitude)
        {
            int localLat = (int)((latitude - Latitude) * PointsPerCell);
            int localLon = (int)(((longitude - Longitude)) * PointsPerCell);
            int bytesPos = ((PointsPerCell - localLat - 1) * PointsPerCell * 2) + localLon * 2;

            if (bytesPos < 0 || bytesPos > PointsPerCell * PointsPerCell * 2)
                throw new ArgumentOutOfRangeException("Coordinates out of range.", "coordinates");

            if (bytesPos >= HgtData.Length)
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.SRTM);

            if ((HgtData[bytesPos] == 0x80) && (HgtData[bytesPos + 1] == 0x00))
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.SRTM);

            // Motorola "big-endian" order with the most significant byte first
            int elevationValue = (HgtData[bytesPos]) << 8 | HgtData[bytesPos + 1];
            return new ElevationResponse()
            {
                Source = ElevationResponse.SOURCE.SRTM,
                TileType = ElevationResponse.TILE_TYPE.Valid,
                Elevation = (double)elevationValue,
                Lat = latitude,
                Lon = longitude
            };
        }

        public ElevationResponse GetElevation(double lat, double lon, bool interpolate)
        {
            // Base
            int x = (int)Math.Floor(lon);
            int y = (int)Math.Floor(lat);

            // remove base
            var latAux = lat - y;
            var lonAux = lon - x;

            // values should be 0-1199, 1200 is for interpolation
            double xf = lonAux * (PointsPerCell - 1);
            double yf = latAux * (PointsPerCell - 1);

            int x_int = (int)xf;
            float x_frac = (float)(xf - x_int);

            int y_int = (int)yf;
            float y_frac = (float)(yf - y_int);

            y_int = (PointsPerCell - 2) - y_int;


            //          XY   X1+Y
            //          XY1  X1Y1
            //
            //
            double elevationValue = double.MinValue;

            if (interpolate)
            {
                float alt00 = GetValue(x_int, y_int);
                float alt10 = GetValue(x_int + 1, y_int);
                float alt01 = GetValue(x_int, y_int + 1);
                float alt11 = GetValue(x_int + 1, y_int + 1);

                float v1 = Average(alt00, alt10, x_frac);
                float v2 = Average(alt01, alt11, x_frac);
                elevationValue = Average(v1, v2, 1 - y_frac);
            }
            else
            {
                elevationValue = GetValue(x_int, y_int);
            }
            

            return new ElevationResponse()
            {
                Source = ElevationResponse.SOURCE.SRTM,
                TileType = ElevationResponse.TILE_TYPE.Valid,
                Elevation = elevationValue,
                Lat = lat,
                Lon = lon
            };

        }

        #endregion
    }
}