using System;
using System.Collections.Generic;
using System.IO;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using GeodeticEngine.DTM;
using GeodeticEngine.Geodetic;
using System.Data;

namespace GeodeticEngine.DTM.GeoTIFF;

public class GeoTIFF : ElevationProviderBase
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

        // Initialize GDAL
        GdalBase.ConfigureAll();
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
        ElevationResponse elevResponse = null;

        // Look in every file in the GeoTIFF folder
        for (int i = 0; i < GeoTiffFilePaths.Length; i++)
        {
            GeoTIFFDataCell dataCell;
            if (!DataCells.ContainsKey(GeoTiffFilePaths[i]))
            {
                // Load it
                dataCell = GeoTIFFDataCell.LoadDataCell(GeoTiffFilePaths[i]);
                if (dataCell != null)
                    DataCells.Add(GeoTiffFilePaths[i], dataCell);
            }
            else
                dataCell = DataCells[GeoTiffFilePaths[i]];

            if (dataCell == null)
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.GEOTIFF);

            if ((elevResponse = dataCell.GetElevation(latitude, longitude, interpolate)).TileType == ElevationResponse.TILE_TYPE.Valid)
                return elevResponse;
        }

        return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.GEOTIFF);
    }
}