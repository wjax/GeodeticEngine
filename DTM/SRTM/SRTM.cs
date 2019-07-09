using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace GeodeticEngine.DTM.SRTM
{
    public class SRTM : ElevationProviderBase
    {
        public string DataDirectory { get; private set; }
        private Dictionary<int, SRTMDataCell> DataCells { get; set; }

        public SRTM(string dataDirectory = "SRTM")
        {
            DataDirectory = Tools.MTools.AbsolutizePath(dataDirectory);

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

            DataCells = new Dictionary<int, SRTMDataCell>();
        }

        public void Unload()
        {
            DataCells.Clear();
        }


        public ElevationResponse GetElevation(double latitude, double longitude, bool interpolate = false)
        {
            int cellLatitude = (int)Math.Floor(Math.Abs(latitude));
            if (latitude < 0)
            {
                cellLatitude *= -1;
                if (cellLatitude != latitude)
                { // if exactly equal, keep the current tile.
                    cellLatitude -= 1; // because negative so in bottom tile
                }
            }

            int cellLongitude = (int)Math.Floor(Math.Abs(longitude));
            if (longitude < 0)
            {
                cellLongitude *= -1;
                if (cellLongitude != longitude)
                { // if exactly equal, keep the current tile.
                    cellLongitude -= 1; // because negative so in left tile
                }
            }

            int cellID = cellLatitude * 1000 + cellLongitude;
            SRTMDataCell dataCell = null;
            DataCells.TryGetValue(cellID, out dataCell);
            if (dataCell != null)
                return dataCell.GetElevation(latitude, longitude, interpolate);

            string filename = string.Format("{0}{1:D2}{2}{3:D3}",
                cellLatitude < 0 ? "S" : "N",
                Math.Abs(cellLatitude),
                cellLongitude < 0 ? "W" : "E",
                Math.Abs(cellLongitude));


            // OCean
            if (filename.Contains("00W000") || filename.Contains("00W001") ||
                        filename.Contains("01W000") || filename.Contains("01W001") ||
                        filename.Contains("00E000") || filename.Contains("00E001") ||
                        filename.Contains("01E000") || filename.Contains("01E001"))
            {
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.SRTM);
            }

            var filePath = Path.Combine(DataDirectory, filename + ".hgt");
            var zipFilePath = Path.Combine(DataDirectory, filename + ".hgt.zip");

            // Try Download
            if (!File.Exists(filePath) && !File.Exists(zipFilePath))
            {
                USGSSource.GetMissingCell(DataDirectory, filename);
            }
            
            // Try Reading
            if (File.Exists(filePath))
            {
                dataCell = SRTMDataCell.LoadDataCell(filePath);
            }
            else if (File.Exists(zipFilePath))
            {
                dataCell = SRTMDataCell.LoadDataCell(zipFilePath);
            }

            if (dataCell == null)
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.SRTM);

            // add to cells.
            DataCells.Add(cellID,dataCell);

            // return requested elevation.
            return dataCell.GetElevation(latitude, longitude, interpolate);
        }
    }
}
