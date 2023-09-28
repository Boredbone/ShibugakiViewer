using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace WebpWrapper
{
    public static class Decoder
    {

        public static Bitmap Decode(Span<byte> source)
        {
            Bitmap? bmp = null;
            BitmapData? bmpData = null;
            try
            {
                unsafe
                {
                    fixed (byte* p = source)
                    {
                        IntPtr ptrData = new(p);
                        if (NativeMethods.WebPGetInfo(ptrData, (UIntPtr)source.Length,
                            out var width, out var height) == 0)
                        {
                            throw new Exception("WebPGetInfo failed");
                        }

                        bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                        bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly, bmp.PixelFormat);

                        var outputSize = bmpData.Stride * height;
                        if (NativeMethods.WebPDecodeBGRAInto(ptrData,
                            (UIntPtr)source.Length, bmpData.Scan0, outputSize, bmpData.Stride) == 0)
                        {
                            throw new Exception("WebPDecodeBGRAInto failed");
                        }

                        return bmp;
                    }
                }
            }
            finally
            {
                if (bmp != null && bmpData != null)
                {
                    bmp.UnlockBits(bmpData);
                }
            }
        }

        public static Bitmap Decode(Span<byte> source, WebPDecoderOptions options)
        {
            var config = new WebPDecoderConfig();
            if (NativeMethods.WebPInitDecoderConfig(ref config) == 0)
            {
                throw new Exception("WebPInitDecoderConfig failed");
            }

            unsafe
            {
                fixed (byte* p = source)
                {
                    int width = 0;
                    int height = 0;

                    var ptrData = new IntPtr(p);
                    if (options.use_scaling == 0)
                    {
                        var result = NativeMethods.WebPGetFeatures(ptrData, source.Length, ref config.input);
                        if (result != VP8StatusCode.VP8_STATUS_OK)
                        {
                            throw new Exception($"WebPGetFeatures {result}");
                        }

                        if (options.use_cropping == 1)
                        {
                            if (options.crop_left + options.crop_width > config.input.width
                                || options.crop_top + options.crop_height > config.input.height)
                            {
                                throw new Exception("Size over");
                            }
                            width = options.crop_width;
                            height = options.crop_height;
                        }
                    }
                    else
                    {
                        width = options.scaled_width;
                        height = options.scaled_height;
                    }

                    config.options = options;
                    return DecodeWithOption(ptrData, source.Length, width, height, ref config);
                }
            }
        }

        private static Bitmap DecodeWithOption
            (IntPtr ptrData, int length, int width, int height, ref WebPDecoderConfig config)
        {
            Bitmap? bmp = null;
            BitmapData? bmpData = null;

            try
            {
                bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.WriteOnly, bmp.PixelFormat);

                config.output.colorspace = WEBP_CSP_MODE.MODE_BGRA;
                config.output.u.RGBA.rgba = bmpData.Scan0;
                config.output.u.RGBA.stride = bmpData.Stride;
                config.output.u.RGBA.size = (UIntPtr)(bmp.Height * bmpData.Stride);
                config.output.height = bmp.Height;
                config.output.width = bmp.Width;
                config.output.is_external_memory = 1;

                {
                    var result = NativeMethods.WebPDecode(ptrData, (UIntPtr)length, ref config);
                    NativeMethods.WebPFreeDecBuffer(ref config.output);
                    if (result != VP8StatusCode.VP8_STATUS_OK)
                    {
                        throw new Exception($"WebPDecode {result}");
                    }

                    return bmp;
                }
            }
            finally
            {
                if (bmp != null && bmpData != null)
                {
                    bmp.UnlockBits(bmpData);
                }
            }
        }


        public static Bitmap GetThumbnail(Span<byte> source, int width, int height, bool lowQuality)
        {
            var config = new WebPDecoderConfig();
            if (NativeMethods.WebPInitDecoderConfig(ref config) == 0)
            {
                throw new Exception("WebPInitDecoderConfig failed");
            }

            config.options.bypass_filtering = lowQuality ? 1 : 0;
            config.options.no_fancy_upsampling = lowQuality ? 1 : 0;
            config.options.use_threads = 1;
            config.options.use_scaling = 1;
            config.options.scaled_width = width;
            config.options.scaled_height = height;

            unsafe
            {
                fixed (byte* p = source)
                {
                    return DecodeWithOption(new IntPtr(p), source.Length, width, height, ref config);
                }
            }
        }
    }
}
