using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageLibrary.Creation;
using ImageLibrary.File;
using System.Drawing;
using ShibugakiViewer.Models.Utility;
using Boredbone.Utility.Tools;

namespace ShibugakiViewer.Models.ImageViewer
{
    class MetaImage : IDisposable
    {
        private Image Source { get; set; }

        public int Width => this.Source?.Width ?? 0;
        public int Height => this.Source?.Height ?? 0;

        public string Path { get; private set; }

        private bool isNotFound = false;

        public MetaImage()
        {
        }
        
        /// <summary>
        /// Load image from file
        /// </summary>
        /// <param name="path"></param>
        public void LoadImage(string fullPath)
        {
            this.Path = fullPath;
            try
            {
                var image = System.Drawing.Image.FromFile(fullPath);
                this.Source = image;
            }
            catch (FileNotFoundException)
            {
                this.isNotFound = true;
                this.Source = null;
            }
            catch
            {
                this.Source = null;
            }
        }

        public void LoadImage(Record record)
        {
            this.LoadImage(record.FullPath);

            if (this.isNotFound)
            {
                record.IsNotFound = true;
                return;
            }

            if (record.Width != this.Width || record.Height != this.Height)
            {
                record.Width = this.Width;
                record.Height = this.Height;

                ImageFileUtility.UpdateInformation(record, true, true);
            }
        }

        public ImageSource DecodeImage(double? zoomFactor, double? width, double? height)
        {
            if (this.Source == null)
            {
                return null;
            }
            try
            {

                if (zoomFactor == null || zoomFactor <= 0)
                {
                    if (width != null && width > 0)
                    {
                        zoomFactor = width / this.Source.Width;
                    }
                    else if (height != null && height > 0)
                    {
                        zoomFactor = height / this.Source.Height;
                    }
                    else
                    {
                        zoomFactor = 1;
                    }

                    if (zoomFactor > 1)
                    {
                        zoomFactor = 1;
                    }
                }

                var imageWidth = (int)(this.Source.Width * zoomFactor);
                var imageHeight = (int)(this.Source.Height * zoomFactor);

                using (var canvas = new Bitmap(imageWidth, imageHeight))
                using (var graphics = Graphics.FromImage(canvas))
                {
                    graphics.DrawImage(this.Source, 0, 0, imageWidth, imageHeight);
                    return canvas.ToWPFBitmap();
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            this.Source?.Dispose();
        }
    }
}
