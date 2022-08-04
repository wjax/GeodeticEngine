using System.Collections.Generic;
using System.IO;

namespace GeodeticEngine.DTM.SRTM
{
    public class ASC : ElevationProviderBase
    {
        public string DataDirectory { get; private set; }
        private Dictionary<string, ASCDataCell> DataCells { get; set; }
        private string[] AscFilePaths;

        public ASC(string dataDirectory = "ASC")
        {
            DataDirectory = Tools.MTools.AbsolutizePath(dataDirectory);

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

            DataCells = new Dictionary<string, ASCDataCell>();

            // Get files in folder
            AscFilePaths = Directory.GetFiles(DataDirectory);
        }

        public void Unload()
        {
            DataCells.Clear();
        }

        public ElevationResponse GetElevation(double latitude, double longitude, bool interpolate = false)
        {
            ElevationResponse elevResponse = null;

            // Look in every file in the ASC folder
            for (int i = 0; i < AscFilePaths.Length; i++)
            {
                ASCDataCell dataCell;
                if (!DataCells.ContainsKey(AscFilePaths[i]))
                {
                    // Load it
                    dataCell = ASCDataCell.LoadDataCell(AscFilePaths[i]);
                    if (dataCell != null)
                        DataCells.Add(AscFilePaths[i], dataCell);
                }
                else
                    dataCell = DataCells[AscFilePaths[i]];

                if (dataCell == null)
                    return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.ASC);


                if ((elevResponse = dataCell.GetElevation(latitude, longitude, interpolate)).TileType == ElevationResponse.TILE_TYPE.Valid)
                    return elevResponse;

            }

            return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.ASC);
        }
    }
}
