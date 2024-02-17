using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using LibHeifSharp;
using System.Diagnostics;

namespace AvifDecoder
{
    public class Decoder
    {
        private static unsafe Bitmap? ConvertBitmap(HeifImage image)
        {
            Bitmap? bmp = null;
            BitmapData? bmpData = null;

            try
            {
                var width = image.Width;
                var height = image.Height;

                var dstWidth = width;
                //if ((width & 0xf) > 0)
                //{
                //    dstWidth += (0xf - (width & 0xf));
                //}

                bool hasAlpha = image.HasAlphaChannel;
                //int bitDepth = primaryImage.BitDepth;

                //var pixelFormat = hasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
                var pixelFormat = PixelFormat.Format32bppArgb;
                bmp = new Bitmap(dstWidth, height, pixelFormat);
                bmpData = bmp.LockBits(new Rectangle(0, 0, dstWidth, height),
                    ImageLockMode.WriteOnly, bmp.PixelFormat);

                var heifPlaneData = image.GetPlane(HeifChannel.Interleaved);
                byte* srcScan0 = (byte*)heifPlaneData.Scan0;
                byte* dstScan0 = (byte*)bmpData.Scan0;

                if (hasAlpha)
                {
                    if (image.IsPremultipliedAlpha)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int srcPos = y * heifPlaneData.Stride + x * 4;
                                int dstPos = y * bmpData.Stride + x * 4;

                                byte alpha = srcScan0[srcPos + 3];

                                if (alpha == 0)
                                {
                                    dstScan0[dstPos + 0] = 0;
                                    dstScan0[dstPos + 1] = 0;
                                    dstScan0[dstPos + 2] = 0;
                                    dstScan0[dstPos + 3] = 0;
                                }
                                else if (alpha == 0xff)
                                {
                                    dstScan0[dstPos + 0] = srcScan0[srcPos + 2];//B
                                    dstScan0[dstPos + 1] = srcScan0[srcPos + 1];//G
                                    dstScan0[dstPos + 2] = srcScan0[srcPos + 0];//R
                                    dstScan0[dstPos + 3] = 0xff;// srcScan0[pos + 0];//A
                                }
                                else
                                {
                                    dstScan0[dstPos + 0] = (byte)Math.Min(MathF.Round(srcScan0[srcPos + 2] * 255f / alpha), 255);
                                    dstScan0[dstPos + 1] = (byte)Math.Min(MathF.Round(srcScan0[srcPos + 1] * 255f / alpha), 255);
                                    dstScan0[dstPos + 2] = (byte)Math.Min(MathF.Round(srcScan0[srcPos + 0] * 255f / alpha), 255);
                                    dstScan0[dstPos + 3] = alpha;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int srcPos = y * heifPlaneData.Stride + x * 4;
                                int dstPos = y * bmpData.Stride + x * 4;

                                dstScan0[dstPos + 0] = srcScan0[srcPos + 2];//B
                                dstScan0[dstPos + 1] = srcScan0[srcPos + 1];//G
                                dstScan0[dstPos + 2] = srcScan0[srcPos + 0];//R
                                dstScan0[dstPos + 3] = srcScan0[srcPos + 3];//A

                            }
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcPos = y * heifPlaneData.Stride + x * 3;
                            int dstPos = y * bmpData.Stride + x * 4;

                            dstScan0[dstPos + 0] = srcScan0[srcPos + 2];
                            dstScan0[dstPos + 1] = srcScan0[srcPos + 1];
                            dstScan0[dstPos + 2] = srcScan0[srcPos + 0];
                            dstScan0[dstPos + 3] = 0xff;
                        }
                        for (int x = width; x < dstWidth; x++)
                        {
                            int dstPos = y * bmpData.Stride + x * 4;

                            dstScan0[dstPos + 0] = 0;
                            dstScan0[dstPos + 1] = 0;
                            dstScan0[dstPos + 2] = 0;
                            dstScan0[dstPos + 3] = 0xff;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (bmp != null && bmpData != null)
                {
                    bmp.UnlockBits(bmpData);
                }
            }
            return bmp;
        }

        public static Bitmap? Decode(Stream source, int resizeWidth, int resizeHeight)
        {
            try
            {
                var decodingOptions = new HeifDecodingOptions
                {
                    ConvertHdrToEightBit = true,
                    Strict = false,
                    DecoderId = null
                };

                using var context = new HeifContext(source);
                using var primaryImage = context.GetPrimaryImageHandle();

                bool hasAlpha = primaryImage.HasAlphaChannel;
                //int bitDepth = primaryImage.BitDepth;
                var chroma = hasAlpha ? HeifChroma.InterleavedRgba32 : HeifChroma.InterleavedRgb24;

                using var image = primaryImage.Decode(HeifColorspace.Rgb, chroma, decodingOptions);
                //var decodingWarnings = image.DecodingWarnings;
                //foreach (var item in decodingWarnings)
                //{
                //    Debug.WriteLine("Warning: " + item);
                //}
                var width = image.Width;
                var height = image.Height;

                if ((resizeWidth <= 0 && resizeHeight <= 0)
                    || (width <= 0 || height <= 0)
                    || (resizeWidth == width && resizeHeight == height))
                {
                    return ConvertBitmap(image);
                }
                else
                {
                    if (resizeWidth <= 0)
                    {
                        resizeWidth = width * resizeHeight / height;
                    }
                    else if (resizeHeight <= 0)
                    {
                        resizeHeight = height * resizeWidth / width;
                    }
                    //resizeWidth -= (resizeWidth & 15);
                    //resizeHeight -= (resizeHeight & 3);
                    resizeWidth -= (resizeWidth & 1);
                    resizeHeight -= (resizeHeight & 1);

                    //if ((resizeWidth & 1) > 0)
                    //{
                    //    ++resizeWidth;
                    //}
                    //if ((resizeHeight & 1) > 0)
                    //{
                    //    ++resizeHeight;
                    //}
                    using var resizedImage = image.ScaleImage(resizeWidth, resizeHeight);
                    return ConvertBitmap(resizedImage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }
    }
}
