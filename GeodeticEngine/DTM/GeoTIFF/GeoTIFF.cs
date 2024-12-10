using System;
using System.Collections.Generic;
using System.IO;
using BitMiracle.LibTiff.Classic;
using GeodeticEngine.Geodetic;
using static GeodeticEngine.Geodetic.Projections;

namespace GeodeticEngine.DTM.GeoTIFF
{
    public class GeoTIFF : ElevationProviderBase, IDisposable
    {
        public string DataDirectory { get; private set; }
        private Dictionary<string, GeoTIFFDataCell> DataCells { get; set; }
        private string[] GeoTiffFilePaths;

        public GeoTIFF(string dataDirectory = "GeoTIFF")
        {
            DataDirectory = Tools.MTools.AbsolutizePath(dataDirectory);

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

            DataCells = new Dictionary<string, GeoTIFFDataCell>();

            // Get files in folder
            GeoTiffFilePaths = Directory.GetFiles(DataDirectory, "*.tif");
        }

        public void Unload()
        {
            foreach (var dataCell in DataCells.Values)
            {
                dataCell.Dispose();
            }
            DataCells.Clear();
        }

        public ElevationResponse GetElevation(double latitude, double longitude, bool interpolate = false)
        {
            foreach (var filePath in GeoTiffFilePaths)
            {
                if (!DataCells.TryGetValue(filePath, out var dataCell))
                {
                    dataCell = GeoTIFFDataCell.LoadDataCell(filePath);
                    if (dataCell != null)
                        DataCells[filePath] = dataCell;
                }

                if (dataCell == null)
                    continue;

                var elevResponse = dataCell.GetElevation(latitude, longitude, interpolate);
                if (elevResponse.TileType == ElevationResponse.TILE_TYPE.Valid)
                    return elevResponse;
            }

            return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.GEOTIFF);
        }

        public void Dispose()
        {
            Unload();
        }
    }
}