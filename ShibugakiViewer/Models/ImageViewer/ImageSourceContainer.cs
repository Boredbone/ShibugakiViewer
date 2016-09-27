using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;
using ImageLibrary.Creation;
using ImageLibrary.File;

namespace ShibugakiViewer.Models.ImageViewer
{
    /// <summary>
    /// ImageSourceと付随する情報を管理
    /// </summary>
    public class ImageSourceContainer : IDisposable
    {
        public ImageSource Image { get; private set; }

        public ImageQuality Quality { get; set; }
        public bool IsNotFound { get; set; }

        public GraphicInformation Information { get; private set; }
        
        public string FullPath { get; private set; }

        private const int maxSize = 4096;
        private const double resizeThreshold = 1.5;

        public ImageSourceContainer()
        {

        }


        public void Dispose()
        {
            var dispatcher = this.Image?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.BeginInvokeShutdown(DispatcherPriority.SystemIdle);
                Dispatcher.Run();
            }
            this.Image = null;
        }

        public bool HasImage()
        {
            return this.Image != null || this.IsNotFound;
        }

        public Task<bool> LoadImageAsync
            (string fullPath, Size? frameSize, bool asThumbnail, bool isFill, bool cmsEnable)
        {
            return this.LoadImageMainAsync(fullPath, frameSize, asThumbnail, isFill, cmsEnable);
        }

        public async Task<bool> LoadImageAsync
            (Record file, Size? frameSize, bool asThumbnail, bool isFill, bool cmsEnable)
        {
            if (file == null)
            {
                return false;
            }

            var result = await this.LoadImageMainAsync
                (file.FullPath, frameSize, asThumbnail, isFill, cmsEnable);

            if (result && this.Information != null)
            {

                if ((file.Width != this.Information.GraphicSize.Width)
                    || (file.Height != this.Information.GraphicSize.Height)
                    || (file.Size != this.Information.FileSize))
                {
                    if (this.Information.GraphicSize.Height > 0)
                    {
                        file.Width = this.Information.GraphicSize.Width;
                    }
                    if (this.Information.GraphicSize.Height > 0)
                    {
                        file.Height = this.Information.GraphicSize.Height;
                    }
                    if (this.Information.FileSize > 0)
                    {
                        file.Size = this.Information.FileSize;
                    }

                    ImageFileUtility.UpdateInformation(file, false, true);

                }
            }

            if (this.IsNotFound)
            {
                file.IsNotFound = true;
                Debug.WriteLine($"notfound:{file.Id}");
            }

            return result;

        }


#pragma warning disable 1998
        private async Task<bool> LoadImageMainAsync
            (string fullPath, Size? frameSize, bool asThumbnail, bool isFill, bool cmsEnable)
        {


            //return false;
            //return Task.Run(async () =>
            //{

            this.FullPath = fullPath;

            if (fullPath.IsNullOrWhiteSpace())
            {
                return false;
            }
            
            try
            {
                GraphicInformation information = null;
                
                using (var stream = File.OpenRead(fullPath))
                {
                    this.IsNotFound = false;

                    information = new GraphicInformation(stream);

                    this.Information = information;

                    if (information.IsMetaImage)
                    {
                        return false;
                    }
                    
                    //描画領域サイズ
                    var frameWidth = (!frameSize.HasValue || frameSize.Value.Width < 1)
                        ? 16.0 : frameSize.Value.Width;
                    var frameHeight = (!frameSize.HasValue || frameSize.Value.Height < 1)
                        ? 16.0 : frameSize.Value.Height;

                    //画像サイズ
                    var imageWidth = information.GraphicSize.Width;
                    var imageHeight = information.GraphicSize.Height;

                    //デコードサイズ
                    var loadWidth = -1.0;
                    var loadHeight = -1.0;

                    var rewrite = false;

                    if (frameSize.HasValue
                        && (imageWidth > frameWidth * resizeThreshold
                        || imageHeight > frameHeight * resizeThreshold
                        || asThumbnail))
                    {
                        //Resize

                        var verticalRate = imageHeight / frameHeight;
                        var horizontalRate = imageWidth / frameWidth;

                        if (isFill)
                        {
                            if (horizontalRate > verticalRate)
                            {
                                loadHeight = frameHeight;
                            }
                            else
                            {
                                loadWidth = frameWidth;
                            }
                        }
                        else
                        {
                            if (horizontalRate > verticalRate)
                            {
                                loadWidth = frameWidth;
                            }
                            else
                            {
                                loadHeight = frameHeight;
                            }
                        }

                        this.Quality = (asThumbnail)
                            ? ImageQuality.LowQuality : ImageQuality.Resized;
                        rewrite = true;
                    }
                    else
                    {
                        //Original size

                        if (imageHeight > maxSize || imageWidth > maxSize)
                        {
                            if (imageHeight > imageWidth)
                            {
                                loadHeight = maxSize;
                            }
                            else
                            {
                                loadWidth = maxSize;
                            }
                            rewrite = true;
                        }

                        this.Quality = ImageQuality.OriginalSize;

                    }
                    

                    var image = new BitmapImage();

                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.None;

                    if (loadWidth > 0)
                    {
                        image.DecodePixelWidth = (int)Math.Round(loadWidth);
                    }
                    else if (loadHeight > 0)
                    {
                        image.DecodePixelHeight = (int)Math.Round(loadHeight);
                    }


                    if (information.BlankHeaderLength == 0)
                    {
                        stream.Position = 0;

                        image.StreamSource = stream;

                        image.EndInit();
                        image.Freeze();
                    }
                    else
                    {
                        using (var ms = new WrappingStream(new MemoryStream((int)
                            (stream.Length - information.BlankHeaderLength))))
                        {

                            stream.Position = information.BlankHeaderLength;

                            ms.Position = 0;
                            stream.CopyTo(ms);
                            ms.Position = 0;

                            image.StreamSource = ms;

                            image.EndInit();
                            image.Freeze();
                        }
                    }

                    this.Image = image;
                }

                return true;

            }
            catch (FileNotFoundException)
            {
                this.IsNotFound = true;
                return false;

            }
            catch (DirectoryNotFoundException)
            {
                this.IsNotFound = true;
                return false;
            }
            catch (ArgumentException)
            {
                this.IsNotFound = true;
                return false;
            }
            catch (NotSupportedException)
            {
                //this.IsNotFound = true;
                return false;
            }
        }
#pragma warning restore 1998


    }


    /// <summary>
    /// 画像読み込み時の品質
    /// </summary>
    public enum ImageQuality
    {
        ThumbNail = 0,
        LowQuality = 1,
        Resized = 2,
        OriginalSize = 3,
    }

}
