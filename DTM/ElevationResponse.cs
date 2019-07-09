using GeodeticEngine.Geodetic;

namespace GeodeticEngine.DTM
{
    public enum ELEVATION_TYPE
    {
        EGM96,
        ELLIPSOIDAL
    }

    public class ElevationResponse
    {
        public enum TILE_TYPE
        {
            Valid,
            Invalid,
            Ocean
        }

        public enum SOURCE
        {
            UNKNOWN,
            SRTM,
            ASC
        }


        public TILE_TYPE TileType = TILE_TYPE.Invalid;
        public SOURCE Source = SOURCE.SRTM;

        // Elevation in EGM96
        private double elevation = 0;
        public double Elevation
        {
            private get => elevation;
            set => elevation = value;
        }

        public double Lat;
        public double Lon;

        public double GetElevation(ELEVATION_TYPE type = ELEVATION_TYPE.ELLIPSOIDAL)
        {
            //System.Diagnostics.Debug.WriteLine($"Undulation Legacy {UNDULATION} - Undulation Grid: {Geodetic.Geoid.GetUndulation(Lat, Lon)}");

            switch (type)
            {
                case ELEVATION_TYPE.EGM96:
                    return Elevation;
                case ELEVATION_TYPE.ELLIPSOIDAL:
                    return Elevation + Geoid.GetUndulation(Lat, Lon);
                default:
                    return Elevation + Geoid.GetUndulation(Lat, Lon);
            }
            //return 0;
        }

        public static ElevationResponse ReturnInvalid(ElevationResponse.SOURCE _source)
        {
            ElevationResponse r = new ElevationResponse()
            {
                Source = _source,
                TileType = TILE_TYPE.Invalid
            };

            return r;
        }
    }
}
