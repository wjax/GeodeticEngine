using GeodeticEngine.DTM;
using System;

namespace GeodeticEngine.Geodetic
{
    public class Calculator
    {
        private const double deg2rad = Math.PI / 180;
        private const double rad2deg = 180 / Math.PI;
        private const double radius_of_earth = 6378100.0;//# in meters
        private const int MIN_STEP = 4;

        public static (double latout, double lonout) MovePointByBearing(double Lat, double Lon, double bearing, double distance)
        {
            double lat1 = deg2rad * (Lat);
            double lon1 = deg2rad * (Lon);
            double brng = deg2rad * (bearing);
            double dr = distance / radius_of_earth;

            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(dr) +
                        Math.Cos(lat1) * Math.Sin(dr) * Math.Cos(brng));
            double lon2 = lon1 + Math.Atan2(Math.Sin(brng) * Math.Sin(dr) * Math.Cos(lat1),
                                Math.Cos(dr) - Math.Sin(lat1) * Math.Sin(lat2));

            double latout = rad2deg * (lat2);
            double lngout = rad2deg * (lon2);

            return (latout, lngout);
        }

        // Move a point a number of meters
        public static (double latout, double lonout) MovePointByMeters(double Lat, double Lon, double east, double north)
        {
            double bearing = Math.Atan2(east, north) * rad2deg;
            double distance = Math.Sqrt(Math.Pow(east, 2) + Math.Pow(north, 2));
            return MovePointByBearing(Lat, Lon, bearing, distance);
        }

        public static double GetBearingBAD(double Lat1, double Lon1, double Lat2, double Lon2, double offset = 0)
        {
            double latitude1 = deg2rad * (Lat1);
            double latitude2 = deg2rad * (Lat2);
            double longitudeDifference = deg2rad * (Lon2 - Lon1);

            double y = Math.Sin(longitudeDifference) * Math.Cos(latitude2);
            double x = Math.Cos(latitude1) * Math.Sin(latitude2) - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitudeDifference);

            double angle_raw = Math.Atan2(y, x) + offset;

            return (rad2deg * angle_raw + 360) % 360;
        }

        public static double GetBearing(double Lat1, double Lon1, double Lat2, double Lon2, double offset = 0)
        {
            double latitude1 = deg2rad * (Lat1);
            double latitude2 = deg2rad * (Lat2);
            double longitudeDifference = deg2rad * (Lon2 - Lon1);

            double y = Math.Sin(longitudeDifference) * Math.Cos(latitude2);
            double x = Math.Cos(latitude1) * Math.Sin(latitude2) - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitudeDifference);

            double angle_raw_deg = Math.Atan2(y, x) * rad2deg;

            return (angle_raw_deg + offset + 360) % 360;
        }
        public static double GetElevationAngle(double Lat1, double Lon1, double Alt1, double Lat2, double Lon2, double Alt2)
        {
            double distance = GetDistanceEquiRectangular(Lat1, Lon1, Lat2, Lon2);

            return Math.Atan2((Alt2 - Alt1), distance) * rad2deg;
        }

        public static double GetElevationAngle2(double Lat1, double Lon1, double Alt1, double Lat2, double Lon2, double Alt2)
        {
            double rLat1 = Lat1 * deg2rad;
            double rLat2 = Lat2 * deg2rad;
            double rLon1 = Lon1 * deg2rad;
            double rLon2 = Lon2 * deg2rad;

            double hAlt1 = radius_of_earth + Alt1;
            double hALt2 = radius_of_earth + Alt2;

            double r1 = hALt2 * Math.Cos(rLat2) * Math.Cos(rLon1 - rLon2);
            double r2 = hALt2 * Math.Cos(rLat2) * Math.Sin(rLon1 - rLon2);
            double r3 = hALt2 * Math.Sin(rLat2);

            double range = Math.Sqrt(Math.Pow(r1 - hAlt1 * Math.Cos(rLat1), 2) + Math.Pow(r2, 2) + Math.Pow(r3 - hAlt1 * Math.Sin(rLat1), 2));

            double elev = -Math.Asin(-(Math.Cos(rLat1) * r1 + r3 * Math.Sin(rLat1) - hAlt1) / range);

            return elev * rad2deg;
        }


        public static (double, double) GetBearingAndElevation(double Lat1, double Lon1, double Alt1, double Lat2, double Lon2, double Alt2)
        {
            return (GetBearing(Lat1, Lon1, Lat2, Lon2), GetElevationAngle(Lat1, Lon1, Alt1, Lat2, Lon2, Alt2));
        }

        public static double GetDistance(double Lat1, double Lon1, double Lat2, double Lon2)
        {
            double d = Lat1 * 0.017453292519943295;
            double num2 = Lon1 * 0.017453292519943295;
            double num3 = Lat2 * 0.017453292519943295;
            double num4 = Lon2 * 0.017453292519943295;
            double num5 = num4 - num2;
            double num6 = num3 - d;
            double num7 = Math.Pow(Math.Sin(num6 / 2.0), 2.0) + ((Math.Cos(d) * Math.Cos(num3)) * Math.Pow(Math.Sin(num5 / 2.0), 2.0));
            double num8 = 2.0 * Math.Atan2(Math.Sqrt(num7), Math.Sqrt(1.0 - num7));
            return (radius_of_earth * num8);// * 1000.0; // M
            // this should be radius_of_earth
        }

        public static double GetDistanceEquiRectangular(double Lat1, double Lon1, double Lat2, double Lon2)
        {
            double rLat1 = Lat1 * deg2rad;
            double rLat2 = Lat2 * deg2rad;
            double rLon1 = Lon1 * deg2rad;
            double rLon2 = Lon2 * deg2rad;

            var x = (rLon2 - rLon1) * Math.Cos((rLat1 + rLat2) / 2);
            var y = (rLat2 - rLat1);
            var d = Math.Sqrt(x * x + y * y) * radius_of_earth;

            return d;
        }

        // RPY in NED
        public static (double Lat, double Lon, double Alt) getIntersectionWithTerrain(double startLat, double startLon, double startAlt, double bearing, double pitch, int maxDistance, int stepSize = 50)
        {
            int distout = 0;
            double LatOut, LonOut, AltOut;
            double radPitch = -( pitch * deg2rad);
            double LatPrev = startLat;
            double LonPrev = startLon;
            double AltPrev = startAlt;

            ElevationResponse ElevationSurface;

            while (distout <= maxDistance)
            {
                // Get new point
                (LatOut, LonOut) = MovePointByBearing(LatPrev, LonPrev, bearing, stepSize);
                AltOut = AltPrev - stepSize * Math.Tan(radPitch);

                // Get TerrainElevation on this point
                ElevationSurface = DTMEngine.GetElevation(LatOut, LonOut, ElevationResponse.SOURCE.ASC);

                if (ElevationSurface.TileType != ElevationResponse.TILE_TYPE.Valid)
                    return (0,0,0);

                // Surface intersection
                if (ElevationSurface.GetElevation() > AltOut)
                {
                    if (stepSize < MIN_STEP)
                        return (LatOut, LonOut, AltOut);
                    else
                        return getIntersectionWithTerrain(LatPrev, LonPrev, AltPrev, bearing, pitch, stepSize , stepSize / 2);
                }

                LatPrev = LatOut;
                LonPrev = LonOut;
                AltPrev = AltOut;

                distout += stepSize;
            }

            return (0, 0, 0);
        }

        
    }
}
