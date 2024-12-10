using System;
using System.Collections.Generic;
using System.Text;

namespace GeodeticEngine.DTM;

public interface ElevationProviderBase
{
    ElevationResponse GetElevation(double latitude, double longitude, bool interpolate = false);
}