using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
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

            if (result)
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
            this.FullPath = fullPath;

            if (fullPath.IsNullOrWhiteSpace())
            {
                return false;
            }


            try
            {
                using (var stream = File.OpenRead(fullPath))
                {

                    this.IsNotFound = false;

                    this.Information = new GraphicInformation(stream);

                    if (this.Information.IsMetaImage)
                    {
                        return false;
                    }

                    //描画領域サイズ
                    var frameWidth = (!frameSize.HasValue || frameSize.Value.Width < 1)
                        ? 16.0 : frameSize.Value.Width;
                    var frameHeight = (!frameSize.HasValue || frameSize.Value.Height < 1)
                        ? 16.0 : frameSize.Value.Height;

                    //画像サイズ
                    var imageWidth = this.Information.GraphicSize.Width;
                    var imageHeight = this.Information.GraphicSize.Height;

                    //デコードサイズ
                    var loadWidth = -1.0;
                    var loadHeight = -1.0;


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
                        }

                        this.Quality = ImageQuality.OriginalSize;

                    }

                    /*
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
                    */
                    var image = this.SetSourceToImage(
                        new LoadingOptions(loadWidth, loadHeight),
                        stream, this.Information, asThumbnail);

                    if (image == null)
                    {
                        this.IsNotFound = true;
                        return false;
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
            catch (InvalidOperationException)
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


        private ImageSource SetSourceToImage
            (in LoadingOptions options, Stream stream, in GraphicInformation information, bool asThumbnail)
        {
            if (information.BlankHeaderLength == 0)
            {
                return this.SetImage(options, stream, information, asThumbnail);
            }
            else
            {
                using (var ms = new WrappingStream(new MemoryStream((int)
                    (stream.Length - information.BlankHeaderLength))))
                {

                    stream.Position = information.BlankHeaderLength;

                    ms.Position = 0;
                    stream.CopyTo(ms);

                    return this.SetImage(options, ms, information, asThumbnail);
                }
            }
        }

        private ImageSource SetImage
            (in LoadingOptions options, Stream stream, in GraphicInformation information, bool asThumbnail)
        {
            stream.Position = 0;

            if (information.Type == GraphicFileType.Webp)
            {
                return this.SetImageAsWebp(options, stream, information, asThumbnail);
            }

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
                        return this.SetStreamSourceToImage(options, stream);
                    }

                    using (var ms = new WrappingStream(new MemoryStream()))
                    {
                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(source));
                        encoder.Save(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        return this.SetStreamSourceToImage(options, ms);
                    }
                }
                catch
                {

                }
                stream.Position = 0;
            }

            return this.SetStreamSourceToImage(options, stream);
        }


        private ImageSource SetImageAsWebp
            (in LoadingOptions options, Stream stream, in GraphicInformation information, bool asThumbnail)
        {
            try
            {
                var width = options.Width;
                var height = options.Height;
                if (width > 0 && height <= 0 && information.GraphicSize.Width > 1)
                {
                    height = width * information.GraphicSize.Height / information.GraphicSize.Width;
                }
                else if (height > 0 && width <= 0 && information.GraphicSize.Height > 1)
                {
                    width = height * information.GraphicSize.Width / information.GraphicSize.Height;
                }

                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                using var webp = new WebPWrapper.WebP();
                using var bmp = webp.Decode(data);
                var ss = ToBetterBitmapSource(bmp, new LoadingOptions(width, height));
                return ss;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }
        }
#if false
        private ImageSource SetImageAsWebp
            (LoadingOptions options, Stream stream, in GraphicInformation information, bool asThumbnail)
        {
            try
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                var bmp = new Imazen.WebP.SimpleDecoder().DecodeFromBytes(data, data.Length);
                using (var ms = new System.IO.MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    ms.Position = 0;
                    return SetStreamSourceToImage(options, ms);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }
#endif

        private readonly struct LoadingOptions
        {
            public double Width { get; }
            public double Height { get; }

            public LoadingOptions(double width, double height)
            {
                this.Width = width;
                this.Height = height;
            }
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);


        private static BitmapSource ToBetterBitmapSource(System.Drawing.Bitmap source, in LoadingOptions options)
        {
            var handle = source.GetHbitmap();
            BitmapSizeOptions opt
                = (options.Width > 0 && options.Height > 0) ? BitmapSizeOptions.FromWidthAndHeight((int)options.Width, (int)options.Height)
                : (options.Width > 0) ? BitmapSizeOptions.FromWidthAndHeight((int)options.Width, (int)options.Width)
                : (options.Height > 0) ? BitmapSizeOptions.FromWidthAndHeight((int)options.Height, (int)options.Height)
                : BitmapSizeOptions.FromEmptyOptions();
            try
            {
                var image = Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    opt);
                image.Freeze();
                return image;
            }
            finally
            {
                DeleteObject(handle);
            }
        }
        private ImageSource SetStreamSourceToImage(in LoadingOptions options, Stream stream)
        {
            /*
            try
            {
                using var bmp = new System.Drawing.Bitmap(FullPath);
                return ToBetterBitmapSource(bmp, options);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }*/
            long position = -1;
            if (stream.CanSeek)
            {
                position = stream.Position;
            }
            for (int i = 0; i < 2; i++)
            {
                if (stream.CanSeek && position >= 0)
                {
                    stream.Position = position;
                }

                var image = new BitmapImage();

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = (i == 0)
                    ? BitmapCreateOptions.None : BitmapCreateOptions.IgnoreColorProfile;

                if (options.Width > 0)
                {
                    image.DecodePixelWidth = (int)Math.Round(options.Width);
                }
                else if (options.Height > 0)
                {
                    image.DecodePixelHeight = (int)Math.Round(options.Height);
                }

                image.StreamSource = stream;

                try
                {
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }
            return null;
        }

        private byte[] GetThumbnail(Stream source, in GraphicInformation information)
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
