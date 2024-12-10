using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeodeticEngine.DTM.GeoTIFF;

public class GeoTIFFDataCell : IDisposable
{
    private Dataset dataset;
    private double[] geoTransform;
    private int width;
    private int height;
    private Band band;

    internal static GeoTIFFDataCell LoadDataCell(string filepath)
    {
            GeoTIFFDataCell cell = null;
            try
            {
                cell = new GeoTIFFDataCell(filepath);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Bad GeoTIFF File: {e.Message}");
            }
            return cell;
        }

    private GeoTIFFDataCell(string filepath)
    {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("File not found.", filepath);

            dataset = Gdal.Open(filepath, Access.GA_ReadOnly);
            if (dataset == null)
                throw new InvalidOperationException("Failed to open GeoTIFF file.");

            geoTransform = new double[6];
            dataset.GetGeoTransform(geoTransform);

            width = dataset.RasterXSize;
            height = dataset.RasterYSize;

            band = dataset.GetRasterBand(1);
        }

    public ElevationResponse GetElevation(double lat, double lon, bool interpolate)
    {
            UTMPosition pos = Projections.ToUTM(lat, lon);
            // Convert geographic coordinates to pixel coordinates
            double dfGeoX = lon;
            double dfGeoY = lat;
            double[] adfGeoTransform = geoTransform;

            double dfPixel = (dfGeoX - adfGeoTransform[0]) / adfGeoTransform[1];
            double dfLine = (dfGeoY - adfGeoTransform[3]) / adfGeoTransform[5];

            int iPixel = (int)Math.Floor(dfPixel);
            int iLine = (int)Math.Floor(dfLine);

            if (iPixel < 0 || iPixel >= width || iLine < 0 || iLine >= height)
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.GEOTIFF);

            float elevationValue;

            if (interpolate)
            {
                // Bilinear interpolation
                double xFrac = dfPixel - iPixel;
                double yFrac = dfLine - iLine;

                float[] buffer = new float[4];
                band.ReadRaster(iPixel, iLine, 2, 2, buffer, 2, 2, 0, 0);

                float topLeft = buffer[0];
                float topRight = buffer[1];
                float bottomLeft = buffer[2];
                float bottomRight = buffer[3];

                float top = (float)(topLeft * (1 - xFrac) + topRight * xFrac);
                float bottom = (float)(bottomLeft * (1 - xFrac) + bottomRight * xFrac);

                elevationValue = (float)(top * (1 - yFrac) + bottom * yFrac);
            }
            else
            {
                float[] buffer = new float[1];
                band.ReadRaster(iPixel, iLine, 1, 1, buffer, 1, 1, 0, 0);
                elevationValue = buffer[0];
            }

            return new ElevationResponse()
            {
                Source = ElevationResponse.SOURCE.GEOTIFF,
                TileType = ElevationResponse.TILE_TYPE.Valid,
                Elevation = elevationValue,
                Lat = lat,
                Lon = lon
            };
        }

    public void Dispose()
    {
            band?.Dispose();
            dataset?.Dispose();
        }
}