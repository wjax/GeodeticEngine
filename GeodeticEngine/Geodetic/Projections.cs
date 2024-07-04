using System;
using System.Collections.Generic;
using System.Text;

namespace GeodeticEngine.Geodetic
{
    public static class Projections
    {
        #region const
        private const double equatorial_radius = 6378137.0;
        private const double inverse_flattening = 298.257223563;
        private const double a = equatorial_radius;
        private const double f = 1.0 / inverse_flattening;
        private const double b = a * (1 - f);   // polar radius

        private static double e = Math.Sqrt(1 - Math.Pow(b, 2) / Math.Pow(a, 2));
        private static double e0 = e / Math.Sqrt(1 - Math.Pow(e, 1));

        private const double drad = Math.PI / 180;
        private const double k0 = 0.9996;

        private const double esq = (1 - (b / a) * (b / a));
        private static double e0sq = e * e / (1 - Math.Pow(e, 2));

        //

        private const double sm_a = equatorial_radius;
        private const double sm_b = equatorial_radius * (1 - (f)); //Polar Radius
        //private const double flattening = 1 / inverse_flattening;

        private const double deg2rad = Math.PI / 180;
        private const double rad2deg = 180 / Math.PI;

        #endregion



        public struct UTMPosition
        {
            public string latZone;
            public int lonZone;
            public double easting;
            public double northing;
        }

        public struct LatLonPosition
        {
            public double lat;
            public double lon;
        }
        
        private static string getUtmLetterDesignator(double latitude)
        {
            if ((84 >= latitude) && (latitude >= 72))
                return "X";
            else if ((72 > latitude) && (latitude >= 64))
                return "W";
            else if ((64 > latitude) && (latitude >= 56))
                return "V";
            else if ((56 > latitude) && (latitude >= 48))
                return "U";
            else if ((48 > latitude) && (latitude >= 40))
                return "T";
            else if ((40 > latitude) && (latitude >= 32))
                return "S";
            else if ((32 > latitude) && (latitude >= 24))
                return "R";
            else if ((24 > latitude) && (latitude >= 16))
                return "Q";
            else if ((16 > latitude) && (latitude >= 8))
                return "P";
            else if ((8 > latitude) && (latitude >= 0))
                return "N";
            else if ((0 > latitude) && (latitude >= -8))
                return "M";
            else if ((-8 > latitude) && (latitude >= -16))
                return "L";
            else if ((-16 > latitude) && (latitude >= -24))
                return "K";
            else if ((-24 > latitude) && (latitude >= -32))
                return "J";
            else if ((-32 > latitude) && (latitude >= -40))
                return "H";
            else if ((-40 > latitude) && (latitude >= -48))
                return "G";
            else if ((-48 > latitude) && (latitude >= -56))
                return "F";
            else if ((-56 > latitude) && (latitude >= -64))
                return "E";
            else if ((-64 > latitude) && (latitude >= -72))
                return "D";
            else if ((-72 > latitude) && (latitude >= -80))
                return "C";
            else
                return "Z";
        }

        public static UTMPosition ToUTM2(double latitude, double longitude)
        {
            double a = 6378137;
            double eccSquared = 0.00669438;

            int ZoneNumber;

            var LongTemp = longitude;
            var LatRad = deg2rad * latitude;
            var LongRad = deg2rad * LongTemp;

            if (LongTemp >= 8 && LongTemp <= 13 && latitude > 54.5 && latitude < 58)
            {
                ZoneNumber = 32;
            }
            else if (latitude >= 56.0 && latitude < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
            {
                ZoneNumber = 32;
            }
            else
            {
                ZoneNumber = (int)((LongTemp + 180) / 6) + 1;

                if (latitude >= 72.0 && latitude < 84.0)
                {
                    if (LongTemp >= 0.0 && LongTemp < 9.0)
                    {
                        ZoneNumber = 31;
                    }
                    else if (LongTemp >= 9.0 && LongTemp < 21.0)
                    {
                        ZoneNumber = 33;
                    }
                    else if (LongTemp >= 21.0 && LongTemp < 33.0)
                    {
                        ZoneNumber = 35;
                    }
                    else if (LongTemp >= 33.0 && LongTemp < 42.0)
                    {
                        ZoneNumber = 37;
                    }
                }
            }

            var LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3;  //+3 puts origin in middle of zone
            var LongOriginRad = deg2rad * (LongOrigin);

            var UTMZone = getUtmLetterDesignator(latitude);

            var eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            var N = a / Math.Sqrt(1 - eccSquared * Math.Sin(LatRad) * Math.Sin(LatRad));
            var T = Math.Tan(LatRad) * Math.Tan(LatRad);
            var C = eccPrimeSquared * Math.Cos(LatRad) * Math.Cos(LatRad);
            var A = Math.Cos(LatRad) * (LongRad - LongOriginRad);

            var M = a * ((1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256) * LatRad
                    - (3 * eccSquared / 8 + 3 * eccSquared * eccSquared / 32 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(2 * LatRad)
                    + (15 * eccSquared * eccSquared / 256 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(4 * LatRad)
                    - (35 * eccSquared * eccSquared * eccSquared / 3072) * Math.Sin(6 * LatRad));

            var UTMEasting = 0.9996 * N * (A + (1 - T + C) * A * A * A / 6
                    + (5 - 18 * T + T * T + 72 * C - 58 * eccPrimeSquared) * A * A * A * A * A / 120)
                    + 500000.0;

            var UTMNorthing = 0.9996 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                    + (61 - 58 * T + T * T + 600 * C - 330 * eccPrimeSquared) * A * A * A * A * A * A / 720));

            if (latitude < 0)
                UTMNorthing += 10000000.0;

            UTMPosition toReturn;
            toReturn.latZone = UTMZone;
            toReturn.lonZone = ZoneNumber;
            toReturn.easting = UTMEasting;
            toReturn.northing = UTMNorthing;

            return toReturn;
        }

        public static UTMPosition ToUTM(double lat, double lon)
        {
            string letter = "";
            double easting = 0;
            double northing = 0;
            int zone = (int)Math.Floor(lon / 6 + 31);
            if (lat < -72)
                letter = "C";
            else if (lat < -64)
                letter = "D";
            else if (lat < -56)
                letter = "E";
            else if (lat < -48)
                letter = "F";
            else if (lat < -40)
                letter = "G";
            else if (lat < -32)
                letter = "H";
            else if (lat < -24)
                letter = "J";
            else if (lat < -16)
                letter = "K";
            else if (lat < -8)
                letter = "L";
            else if (lat < 0)
                letter = "M";
            else if (lat < 8)
                letter = "N";
            else if (lat < 16)
                letter = "P";
            else if (lat < 24)
                letter = "Q";
            else if (lat < 32)
                letter = "R";
            else if (lat < 40)
                letter = "S";
            else if (lat < 48)
                letter = "T";
            else if (lat < 56)
                letter = "U";
            else if (lat < 64)
                letter = "V";
            else if (lat < 72)
                letter = "W";
            else
                letter = "X";

            

            double phi = lat * drad;                              // convert latitude to radians
            double lng = lon * drad;                             // convert longitude to radians
            double utmz = 1 + Math.Floor((lon + 180) / 6.0);            // longitude to utm zone
            double zcm = 3 + 6.0 * (utmz - 1) - 180;                     // central meridian of a zone
                                                                         // this gives us zone A-B for below 80S
            

            double N = a / Math.Sqrt(1 - Math.Pow(e * Math.Sin(phi), 2));
            double T = Math.Pow(Math.Tan(phi), 2);
            double C = e0sq * Math.Pow(Math.Cos(phi), 2);
            double A = (lon - zcm) * drad * Math.Cos(phi);

            double M = 0;
            // calculate M (USGS style)
            M = phi * (1 - esq * (1.0 / 4.0 + esq * (3.0 / 64.0 + 5.0 * esq / 256.0)));
            M = M - Math.Sin(2.0 * phi) * (esq * (3.0 / 8.0 + esq * (3.0 / 32.0 + 45.0 * esq / 1024.0)));
            M = M + Math.Sin(4.0 * phi) * (esq * esq * (15.0 / 256.0 + esq * 45.0 / 1024.0));
            M = M - Math.Sin(6.0 * phi) * (esq * esq * esq * (35.0 / 3072.0));
            M = M * a;//Arc length along standard meridian

            double M0 = 0;// if another point of origin is used than the equator

            // Calculate the UTM values...
            // first the easting
            var x = k0 * N * A * (1 + A * A * ((1 - T + C) / 6 + A * A * (5 - 18 * T + T * T + 72.0 * C - 58 * e0sq) / 120.0)); //Easting relative to CM
            x = x + 500000; // standard easting

            // Northing

            double y = k0 * (M - M0 + N * Math.Tan(phi) * (A * A * (1 / 2.0 + A * A * ((5 - T + 9 * C + 4 * C * C) / 24.0 + A * A * (61 - 58 * T + T * T + 600 * C - 330 * e0sq) / 720.0))));    // first from the equator
            double yg = y + 10000000;  //yg = y global, from S. Pole
            if (y < 0)
            {
                y = 10000000 + y;   // add in false northing if south of the equator
            }


            easting = Math.Round(10 * x) / 10.0;
            northing = Math.Round(10 * y) / 10.0;

            UTMPosition toReturn;
            toReturn.latZone = letter;
            toReturn.lonZone = zone;
            toReturn.easting = easting;
            toReturn.northing = northing;

            //if (lat <= -80 || lat >= 84) { withinCoordinateSystemBounds = false; }
            //else { withinCoordinateSystemBounds = true; }

            return toReturn;
        }

        public static LatLonPosition ToLatLong(double x, double y, double zone, string latZone)
        {
            //x easting
            //y northing
            bool southhemi = false;
            if (latZone == "A" || latZone == "B" || latZone == "C" || latZone == "D" || latZone == "E" || latZone == "F" || latZone == "G" || latZone == "H" || latZone == "J" ||
                   latZone == "K" || latZone == "L" || latZone == "M")
            {
                southhemi = true;
            }

            double cmeridian;

            x -=  500000.0;
            double UTMScaleFactor = 0.9996;
            x /= UTMScaleFactor;

            /* If in southern hemisphere, adjust y accordingly. */
            if (southhemi)
            {
                y -= 10000000.0;
            }

            y /= UTMScaleFactor;

            cmeridian = deg2rad * (-183.0 + (zone * 6.0));

            double phif, Nf, Nfpow, nuf2, ep2, tf, tf2, tf4, cf;
            double x1frac, x2frac, x3frac, x4frac, x5frac, x6frac, x7frac, x8frac;
            double x2poly, x3poly, x4poly, x5poly, x6poly, x7poly, x8poly;

            /* Get the value of phif, the footpoint latitude. */
            phif = FootpointLatitude(y);

            /* Precalculate ep2 */
            ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0))
                  / Math.Pow(sm_b, 2.0);

            /* Precalculate cos (phif) */
            cf = Math.Cos(phif);

            /* Precalculate nuf2 */
            nuf2 = ep2 * Math.Pow(cf, 2.0);

            /* Precalculate Nf and initialize Nfpow */
            Nf = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nuf2));
            Nfpow = Nf;

            /* Precalculate tf */
            tf = Math.Tan(phif);
            tf2 = tf * tf;
            tf4 = tf2 * tf2;

            /* Precalculate fractional coefficients for x**n in the equations
               below to simplify the expressions for latitude and longitude. */
            x1frac = 1.0 / (Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**2) */
            x2frac = tf / (2.0 * Nfpow);

            Nfpow *= Nf;   /* now equals Nf**3) */
            x3frac = 1.0 / (6.0 * Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**4) */
            x4frac = tf / (24.0 * Nfpow);

            Nfpow *= Nf;   /* now equals Nf**5) */
            x5frac = 1.0 / (120.0 * Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**6) */
            x6frac = tf / (720.0 * Nfpow);

            Nfpow *= Nf;   /* now equals Nf**7) */
            x7frac = 1.0 / (5040.0 * Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**8) */
            x8frac = tf / (40320.0 * Nfpow);

            /* Precalculate polynomial coefficients for x**n.
               -- x**1 does not have a polynomial coefficient. */
            x2poly = -1.0 - nuf2;

            x3poly = -1.0 - 2 * tf2 - nuf2;

            x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2
                - 3.0 * (nuf2 * nuf2) - 9.0 * tf2 * (nuf2 * nuf2);

            x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2;

            x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2
                + 162.0 * tf2 * nuf2;

            x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2);

            x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2);

            /* Calculate latitude */
            double nLat = phif + x2frac * x2poly * (x * x)
                + x4frac * x4poly * Math.Pow(x, 4.0)
                + x6frac * x6poly * Math.Pow(x, 6.0)
                + x8frac * x8poly * Math.Pow(x, 8.0);

            /* Calculate longitude */
            double nLong = cmeridian + x1frac * x
                + x3frac * x3poly * Math.Pow(x, 3.0)
                + x5frac * x5poly * Math.Pow(x, 5.0)
                + x7frac * x7poly * Math.Pow(x, 7.0);

            double dLat =  rad2deg * nLat;
            double dLong = rad2deg * nLong;
            //if (dLat > 90) { dLat = 90; }
            //if (dLat < -90) { dLat = -90; }
            //if (dLong > 180) { dLong = 180; }
            //if (dLong < -180) { dLong = -180; }

            //Coordinate c = new Coordinate(equatorialRadius, flattening, true);
            //CoordinatePart cLat = new CoordinatePart(dLat, CoordinateType.Lat);
            //CoordinatePart cLng = new CoordinatePart(dLong, CoordinateType.Long);

            //c.Latitude = cLat;
            //c.Longitude = cLng;

            LatLonPosition pos;
            pos.lat = dLat;
            pos.lon = dLong;

            return pos;
        }

        private static double FootpointLatitude(double y)
        {
            double y_, alpha_, beta_, gamma_, delta_, epsilon_, n;
            double result;

            /* Precalculate n (Eq. 10.18) */
            n = (sm_a - sm_b) / (sm_a + sm_b);

            /* Precalculate alpha_ (Eq. 10.22) */
            /* (Same as alpha in Eq. 10.17) */
            alpha_ = ((sm_a + sm_b) / 2.0) * (1 + (Math.Pow(n, 2.0) / 4) + (Math.Pow(n, 4.0) / 64));

            /* Precalculate y_ (Eq. 10.23) */
            y_ = y / alpha_;

            /* Precalculate beta_ (Eq. 10.22) */
            beta_ = (3.0 * n / 2.0) + (-27.0 * Math.Pow(n, 3.0) / 32.0)
                + (269.0 * Math.Pow(n, 5.0) / 512.0);

            /* Precalculate gamma_ (Eq. 10.22) */
            gamma_ = (21.0 * Math.Pow(n, 2.0) / 16.0)
                + (-55.0 * Math.Pow(n, 4.0) / 32.0);

            /* Precalculate delta_ (Eq. 10.22) */
            delta_ = (151.0 * Math.Pow(n, 3.0) / 96.0)
                + (-417.0 * Math.Pow(n, 5.0) / 128.0);

            /* Precalculate epsilon_ (Eq. 10.22) */
            epsilon_ = (1097.0 * Math.Pow(n, 4.0) / 512.0);

            /* Now calculate the sum of the series (Eq. 10.21) */
            result = y_ + (beta_ * Math.Sin(2.0 * y_))
                + (gamma_ * Math.Sin(4.0 * y_))
                + (delta_ * Math.Sin(6.0 * y_))
                + (epsilon_ * Math.Sin(8.0 * y_));

            return result;
        }
    }
}
