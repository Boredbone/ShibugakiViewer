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

        public bool LoadImage
            (string fullPath, Size? frameSize, bool asThumbnail, bool isFill, bool cmsEnable)
        {
            return this.LoadImageMain(fullPath, frameSize, asThumbnail, isFill, cmsEnable);
        }

        public bool LoadImage
            (Record file, Size? frameSize, bool asThumbnail, bool isFill, bool cmsEnable)
        {
            if (file == null)
            {
                return false;
            }

            var result = this.LoadImageMain
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

        
        private bool LoadImageMain
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

                    //var rewrite = false;

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
                        //rewrite = true;
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
                            //rewrite = true;
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

                    this.SetSourceToImage(image, stream, information, asThumbnail);

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


        private void SetSourceToImage
            (BitmapImage image, Stream stream, GraphicInformation information, bool asThumbnail)
        {
            if (information.BlankHeaderLength == 0)
            {
                this.SetImage(image, stream, information, asThumbnail);
            }
            else
            {
                using (var ms = new WrappingStream(new MemoryStream((int)
                    (stream.Length - information.BlankHeaderLength))))
                {

                    stream.Position = information.BlankHeaderLength;

                    ms.Position = 0;
                    stream.CopyTo(ms);

                    this.SetImage(image, ms, information, asThumbnail);
                }
            }
        }

        private void SetImage
            (BitmapImage image, Stream stream, GraphicInformation information, bool asThumbnail)
        {
            stream.Position = 0;

            //var sw = new Stopwatch();
            //sw.Start();

            if (asThumbnail && information.Type == GraphicFileType.Jpeg)
            {
                try
                {
                    // Get JPEG thumbnail from header
                    var frame = BitmapFrame.Create
                        (stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var source = frame.Thumbnail;

                    if (source == null)
                    {
                        stream.Position = 0;
                        this.SetStreamSourceToImage(image, stream);

                        //sw.Stop();
                        //Debug.WriteLine($"{sw.ElapsedMilliseconds}, null {this.FullPath}");
                        return;
                    }

                    using (var ms = new MemoryStream())
                    {
                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(source));
                        encoder.Save(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        this.SetStreamSourceToImage(image, ms);

                        //sw.Stop();
                        //Debug.WriteLine($"{sw.ElapsedMilliseconds}, frame {this.FullPath}");
                        //if (information.Type != GraphicFileType.Jpeg)
                        //{
                        //    Debug.WriteLine($"frame {this.FullPath}");
                        //}
                        return;
                    }
                    /*
                    byte[] thumb = null;
                    thumb = this.GetThumbnail(stream, information);
                    if (thumb != null)
                    {
                        using (var ts = new MemoryStream(thumb))
                        {
                            this.SetStreamSourceToImage(image, ts);

                            sw.Stop();
                            Debug.WriteLine($"{sw.ElapsedMilliseconds}, thumbnail {this.FullPath}");
                            this.Quality = ImageQuality.ThumbNail;
                            return;
                        }
                    }*/
                }
                catch
                {

                }
                stream.Position = 0;
            }

            this.SetStreamSourceToImage(image, stream);
            //sw.Stop();
            //Debug.WriteLine($"{sw.ElapsedMilliseconds}, normal {this.FullPath}");
        }

        private void SetStreamSourceToImage(BitmapImage image, Stream stream)
        {
            image.StreamSource = stream;

            image.EndInit();
            image.Freeze();
        }

        private byte[] GetThumbnail(Stream source, GraphicInformation information)
        {
            if (information.Type != GraphicFileType.Jpeg)
            {
                return null;
            }

            // 画像オブジェクトの作成
            using (var orig = System.Drawing.Image.FromStream(source, false, false))
            {
                int[] pils = orig.PropertyIdList;
                int index = Array.IndexOf(pils, 0x501b); // サムネイル・データ

                if (index == -1)
                {
                    return null;
                }
                else
                {
                    // サムネイル・データの取得
                    var pi = orig.PropertyItems[index];
                    byte[] jpgBytes = pi.Value;

                    return jpgBytes;
                }
            }
        }
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
