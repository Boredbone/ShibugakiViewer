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
        public static async Task<bool> Resize(string path, string destPath, int maxLength)
        {
            if (path.IsNullOrWhiteSpace() || maxLength <= 0)
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
                return await ResizeImage(bmp, destPath, maxLength);
            }
            else if(ext == ".wmf" || ext == ".emf" || ext == ".bhf")
            {
                //TODO
                return false;
            }

            using var image = Image.FromFile(path);
            return await ResizeImage(image, destPath, maxLength);
        }
        private static async Task<bool> ResizeImage(System.Drawing.Image image, string destPath, int maxLength)
        {
            if (image.Width <= 0 || image.Height <= 0)
            {
                return false;
            }
            var imageLength = Math.Max(image.Width, image.Height);
            var scale = (maxLength < imageLength) ? ((double)maxLength / imageLength) : 1;
            var resizeWidth = (int)(image.Width * scale);
            var resizeHeight = (int)(image.Height * scale);

            using var resizedBmp = new System.Drawing.Bitmap(resizeWidth, resizeHeight);
            using var g = System.Drawing.Graphics.FromImage(resizedBmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            g.DrawImage(image, 0, 0, resizeWidth, resizeHeight);

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
            return true;
        }
    }
}
