using BitMiracle.LibTiff.Classic;
using GeodeticEngine.Geodetic;
using System;
using System.Collections.Generic;
using System.IO;
using static GeodeticEngine.Geodetic.Projections;

namespace GeodeticEngine.DTM.GeoTIFF
{
    public class GeoTIFFDataCell : IDisposable
    {
        private readonly Tiff tiff;
        private readonly double[] geoTransform;
        private readonly int width;
        private readonly int height;
        private readonly short bitsPerSample;
        private readonly short sampleFormat;
        private readonly bool isCompressed;
        private readonly int rowsPerStrip;
        private readonly int tileWidth;
        private readonly int tileHeight;
        private readonly Dictionary<(int, int), byte[]> tileBuffers;

        internal static GeoTIFFDataCell LoadDataCell(string filepath)
        {
            try
            {
                return new GeoTIFFDataCell(filepath);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Bad GeoTIFF File: {e.Message}");
                return null;
            }
        }

        private GeoTIFFDataCell(string filepath)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("File not found.", filepath);

            tiff = Tiff.Open(filepath, "r");
            if (tiff == null)
                throw new InvalidOperationException("Failed to open GeoTIFF file.");

            width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            bitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToShort();
            sampleFormat = tiff.GetField(TiffTag.SAMPLEFORMAT)?[0].ToShort() ?? 1; // Default to unsigned integer if not specified

            tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Check if the image is compressed
            FieldValue[] compressionField = tiff.GetField(TiffTag.COMPRESSION);
            isCompressed = compressionField != null && compressionField[0].ToInt() == (int)Compression.ADOBE_DEFLATE;

            // Get rows per strip, default to full image height if not specified
            FieldValue[] rowsPerStripField = tiff.GetField(TiffTag.ROWSPERSTRIP);
            rowsPerStrip = rowsPerStripField != null ? rowsPerStripField[0].ToInt() : height;

            geoTransform = new double[6];
            ReadGeoTransform();

            tileBuffers = new Dictionary<(int, int), byte[]>();
        }

        private void ReadGeoTransform()
        {
            FieldValue[] modelTiepointValue = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
            FieldValue[] modelPixelScaleValue = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);

            if (modelTiepointValue != null && modelPixelScaleValue != null)
            {
                double[] tiepoints = modelTiepointValue[1].ToDoubleArray();
                double[] pixelScale = modelPixelScaleValue[1].ToDoubleArray();

                geoTransform[0] = tiepoints[3]; // x origin (top left)
                geoTransform[1] = pixelScale[0]; // width of pixel
                geoTransform[2] = 0; // rotation (0 if image is north up)
                geoTransform[3] = tiepoints[4]; // y origin (top left)
                geoTransform[4] = 0; // rotation (0 if image is north up)
                geoTransform[5] = -pixelScale[1]; // height of pixel (negative as it's top to bottom)
            }
            else
            {
                throw new InvalidOperationException("GeoTIFF tags not found");
            }
        }

        public ElevationResponse GetElevation(double lat, double lon, bool interpolate)
        {
            UTMPosition pos = Projections.ToUTM(lat, lon);

            // Convert geographic coordinates to pixel coordinates
            double x = (pos.easting - geoTransform[0]) / geoTransform[1];
            double y = (pos.northing - geoTransform[3]) / geoTransform[5];

            int iPixel = (int)Math.Floor(x);
            int iLine = (int)Math.Floor(y);

            if (iPixel < 0 || iPixel >= width || iLine < 0 || iLine >= height)
                return ElevationResponse.ReturnInvalid(ElevationResponse.SOURCE.GEOTIFF);

            float elevationValue;

            if (interpolate)
            {
                // Bilinear interpolation
                double xFrac = x - iPixel;
                double yFrac = y - iLine;

                elevationValue = InterpolateBilinear(iPixel, iLine, xFrac, yFrac);
            }
            else
            {
                elevationValue = ReadElevationFromTile(iPixel, iLine);
            }

            return new ElevationResponse
            {
                Source = ElevationResponse.SOURCE.GEOTIFF,
                TileType = ElevationResponse.TILE_TYPE.Valid,
                Elevation = elevationValue,
                Lat = lat,
                Lon = lon
            };
        }

        private float InterpolateBilinear(int iPixel, int iLine, double xFrac, double yFrac)
        {
            float topLeft = ReadElevationFromTile(iPixel, iLine);
            float topRight = ReadElevationFromTile(Math.Min(iPixel + 1, width - 1), iLine);
            float bottomLeft = ReadElevationFromTile(iPixel, Math.Min(iLine + 1, height - 1));
            float bottomRight = ReadElevationFromTile(Math.Min(iPixel + 1, width - 1), Math.Min(iLine + 1, height - 1));

            float top = (float)(topLeft * (1 - xFrac) + topRight * xFrac);
            float bottom = (float)(bottomLeft * (1 - xFrac) + bottomRight * xFrac);

            return (float)(top * (1 - yFrac) + bottom * yFrac);
        }

        private float ReadElevationFromTile(int x, int y)
        {
            int tileX = x / tileWidth;
            int tileY = y / tileHeight;
            int xInTile = x % tileWidth;
            int yInTile = y % tileHeight;

            if (!tileBuffers.TryGetValue((tileX, tileY), out var tileBuffer))
            {
                tileBuffer = new byte[tileWidth * tileHeight * (bitsPerSample / 8)];
                int bytesRead = tiff.ReadTile(tileBuffer, 0, tileX * tileWidth, tileY * tileHeight, 0, 0);

                if (bytesRead < 0)
                {
                    throw new InvalidOperationException($"Failed to read tile at ({tileX}, {tileY})");
                }

                tileBuffers[(tileX, tileY)] = tileBuffer;
            }

            int offset = (yInTile * tileWidth + xInTile) * (bitsPerSample / 8);

            return ReadValue(tileBuffer.AsSpan(offset));
        }

        private float ReadValue(ReadOnlySpan<byte> buffer)
        {
            return bitsPerSample switch
            {
                16 => ReadInt16(buffer),
                32 => sampleFormat == 3 ? ReadFloat(buffer) : ReadInt32(buffer),
                _ => throw new NotSupportedException($"Unsupported bits per sample: {bitsPerSample}")
            };
        }

        private float ReadInt16(ReadOnlySpan<byte> buffer)
        {
            short value = BitConverter.ToInt16(buffer);
            return sampleFormat == 2 ? value : (ushort)value;
        }

        private float ReadInt32(ReadOnlySpan<byte> buffer)
        {
            return sampleFormat == 2 ? BitConverter.ToInt32(buffer) : BitConverter.ToUInt32(buffer);
        }

        private float ReadFloat(ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToSingle(buffer);
        }

        public void Dispose()
        {
            tiff?.Dispose();
        }
    }
}
