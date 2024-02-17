using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;

namespace ShibugakiViewer.Models.ImageViewer
{
    internal class ImageResize
    {
        public static async Task<bool> Resize(string path, string destPath, int maxLength, bool asThumbnail)
        {
            if (path.IsNullOrWhiteSpace() || (asThumbnail && maxLength <= 0))
            {
                return false;
            }
            var ext = System.IO.Path.GetExtension(path).ToLower();
            if (ext == ".webp")
            {
                using var stream = System.IO.File.OpenRead(path);

                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                using var bmp = WebpWrapper.Decoder.Decode(data);
                return ResizeImage(bmp, destPath, maxLength, asThumbnail);
            }
            else if (ext == ".avif")
            {
                using var stream = System.IO.File.OpenRead(path);
                using var bmp = AvifDecoder.Decoder.Decode(stream, 0, 0);
                return ResizeImage(bmp, destPath, maxLength, asThumbnail);
            }
            else if (ext == ".wmf" || ext == ".emf" || ext == ".bhf")
            {
                //TODO
                return false;
            }

            using var image = Image.FromFile(path);
            return ResizeImage(image, destPath, maxLength, asThumbnail);
        }

        private static bool ResizeImage(System.Drawing.Image? image,
            string destPath, int maxLength, bool asThumbnail)
        {
            if (image is null || image.Width <= 0 || image.Height <= 0)
            {
                return false;
            }
            var imageLength = Math.Max(image.Width, image.Height);
            var scale = (maxLength > 1 && maxLength < imageLength) ? ((double)maxLength / imageLength) : 1;
            var resizeWidth = (int)(image.Width * scale);
            var resizeHeight = (int)(image.Height * scale);

            using var resizedBmp = new System.Drawing.Bitmap
                (resizeWidth, resizeHeight, PixelFormat.Format32bppArgb);
            using var g = System.Drawing.Graphics.FromImage(resizedBmp);

            if (asThumbnail)
            {
                g.Clear(System.Drawing.Color.White);
            }
            else
            {
                g.Clear(System.Drawing.Color.Transparent);
            }

            g.InterpolationMode
                = (asThumbnail) ? System.Drawing.Drawing2D.InterpolationMode.Bilinear
                : System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(image, 0, 0, resizeWidth, resizeHeight);

            if (asThumbnail)
            {
                var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(x => x.FormatID == ImageFormat.Jpeg.Guid);
                if (jpegEncoder != null)
                {
                    var encParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 60L);
                    var encParams = new EncoderParameters(1);
                    encParams.Param[0] = encParam;
                    resizedBmp.Save(destPath, jpegEncoder, encParams);
                }
                else
                {
                    resizedBmp.Save(destPath, ImageFormat.Jpeg);
                }
            }
            else
            {
                resizedBmp.Save(destPath, ImageFormat.Png);
            }
            return true;
        }
    }
}
