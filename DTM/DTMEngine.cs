using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GeodeticEngine.DTM
{
    public class DTMEngine
    {
        static Dictionary<ElevationResponse.SOURCE, ElevationProviderBase> ElevProviders = new Dictionary<ElevationResponse.SOURCE, ElevationProviderBase>();

        static DTMEngine()
        {
            ElevProviders.Add(ElevationResponse.SOURCE.ASC, new SRTM.ASC());
            ElevProviders.Add(ElevationResponse.SOURCE.SRTM, new SRTM.SRTM());
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public static ElevationResponse GetElevation(double latitude, double longitude, bool interpolate = false)
        {
            ElevationResponse response;

            foreach (ElevationProviderBase p in ElevProviders.Values)
            {
                if ((response = p.GetElevation(latitude, longitude, interpolate)).TileType == ElevationResponse.TILE_TYPE.Valid)
                    return response;
            }

            return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.UNKNOWN);
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public static ElevationResponse GetElevation(double latitude, double longitude, ElevationResponse.SOURCE source, bool interpolate = false)
        {
            ElevationResponse response;

            if ((response = ElevProviders[source].GetElevation(latitude, longitude, interpolate)).TileType == ElevationResponse.TILE_TYPE.Valid)
                    return response;

            return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.UNKNOWN);
        }


    }
}
