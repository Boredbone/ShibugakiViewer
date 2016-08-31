//using SharpDX;
//using SharpDX.WIC;
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

        //public int LoadedWidth { get; set; }
        //public int LoadedHeight { get; set; }

        public string FullPath { get; private set; }

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
            (string fullPath, int thumbNailSize, int frameWidth,
            int frameHeight, bool cmsEnable)
        {
            return this.LoadImageMainAsync(null, fullPath, thumbNailSize, frameWidth, frameHeight, cmsEnable);
        }

        public async Task<bool> LoadImageAsync
            (Record file, int thumbNailSize, int frameWidth,
            int frameHeight, bool cmsEnable)
        {
            if (file == null)
            {
                return false;
            }

            var result = await this.LoadImageMainAsync
                (file, file.FullPath, thumbNailSize, frameWidth, frameHeight, cmsEnable);

            if (this.IsNotFound)
            {
                file.IsNotFound = true;
                Debug.WriteLine($"notfound:{file.Id}");
            }

            return result;

        }

        //private async Task<bool> LoadImageAsync
        //    (Record file, int thumbNailSize, int frameWidth,
        //    int frameHeight, bool cmsEnable)
        //{
        //    if (file == null)
        //    {
        //        return false;
        //    }
        //
        //    return await Task.Run(() =>
        //    {
        //        return this.LoadImageMain(file, thumbNailSize, frameWidth, frameHeight, cmsEnable);
        //    });
        //
        //}

        private async Task<bool> LoadImageMainAsync
            (Record file, string fullPath, int thumbNailSize, int frameWidth,
            int frameHeight, bool cmsEnable)
        {


            //return false;
            //return Task.Run(async () =>
            //{

            this.FullPath = fullPath;

            if (fullPath == null)
            {
                return false;
            }

            var asThumbNail = (thumbNailSize > 0);
            try
            {
                //Debug.WriteLine(fullPath);

                GraphicInformation information = null;

                if (false)
                {

                    using (var fileStream = new FileStream
                        (fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    {
                        var length = (fileStream.Length < int.MaxValue) ? (int)fileStream.Length : int.MaxValue;

                        byte[] buff = new byte[length];
                        await fileStream.ReadAsync(buff, 0, length);



                        using (var stream = new WrappingStream(new MemoryStream(buff)))
                        {
                            //fileStream.Position = 0;
                            //
                            //stream.Position = 0;
                            //await fileStream.CopyToAsync(stream, length, default(CancellationToken));
                            //stream.Position = 0;



                            this.IsNotFound = false;

                            information = new GraphicInformation(stream);

                            this.Information = information;

                            if (information.IsMetaImage)
                            {
                                return false;
                            }

                            byte[] fixedBuffer = null;

                            if (information.BlankHeaderLength > 0)
                            {
                                var fixedLength = buff.Length - information.BlankHeaderLength;
                                fixedBuffer = new byte[fixedLength];
                                for (int i = 0; i < fixedLength; i++)
                                {
                                    fixedBuffer[i] = buff[i + information.BlankHeaderLength];
                                }
                            }

                            //await Application.Current.Dispatcher.InvokeAsync(() =>
                            {


                                var image = new BitmapImage();

                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.CreateOptions = BitmapCreateOptions.None;

                                if (frameWidth > 0)
                                {
                                    image.DecodePixelWidth = frameWidth;
                                    this.Quality = ImageQuality.Resized;
                                }
                                else if (frameHeight > 0)
                                {
                                    image.DecodePixelHeight = frameHeight;
                                    this.Quality = ImageQuality.Resized;
                                }
                                else if (asThumbNail)
                                {
                                    image.DecodePixelWidth = thumbNailSize;
                                    //image.DecodePixelHeight = thumbNailSize;
                                    this.Quality = ImageQuality.LowQuality;
                                }
                                else
                                {
                                    this.Quality = ImageQuality.OriginalSize;
                                }

                                if (fixedBuffer == null)
                                {
                                    stream.Position = 0;

                                    image.StreamSource = stream;

                                    image.EndInit();
                                    if (image.CanFreeze)
                                    {
                                        image.Freeze();
                                    }
                                }
                                else
                                {
                                    using (var ms = new WrappingStream(new MemoryStream(fixedBuffer)))
                                    {
                                        image.StreamSource = ms;

                                        image.EndInit();
                                        if (image.CanFreeze)
                                        {
                                            image.Freeze();
                                        }
                                    }
                                }

                                this.Image = image;
                            }//);
                        }
                    }

                }
                else
                {
                    //Debug.WriteLine($"aa:{Thread.CurrentThread.ManagedThreadId}");
                    //await Task.Delay(1);
                    //Debug.WriteLine($"bb:{Thread.CurrentThread.ManagedThreadId}");
                    using (var stream = File.OpenRead(fullPath))
                    {
                        this.IsNotFound = false;

                        information = new GraphicInformation(stream);

                        this.Information = information;

                        if (information.IsMetaImage)
                        {
                            return false;
                        }

                        //Debug.WriteLine($"cc:{Thread.CurrentThread.ManagedThreadId}");


                        //await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var image = new BitmapImage();

                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.CreateOptions = BitmapCreateOptions.None;

                            if (frameWidth > 0)
                            {
                                image.DecodePixelWidth = frameWidth;
                                this.Quality = ImageQuality.Resized;
                            }
                            else if (frameHeight > 0)
                            {
                                image.DecodePixelHeight = frameHeight;
                                this.Quality = ImageQuality.Resized;
                            }
                            else if (asThumbNail)
                            {
                                image.DecodePixelWidth = thumbNailSize;
                                //image.DecodePixelHeight = thumbNailSize;
                                this.Quality = ImageQuality.LowQuality;
                            }
                            else
                            {
                                this.Quality = ImageQuality.OriginalSize;
                            }

                            if (information.BlankHeaderLength == 0)
                            {
                                stream.Position = 0;

                                image.StreamSource = stream;
                                //image.UriSource = new Uri(fullPath);

                                image.EndInit();
                                image.Freeze();
                            }
                            else
                            {
                                using (var ms = new WrappingStream(new MemoryStream((int)
                                    (stream.Length - information.BlankHeaderLength))))
                                {

                                    stream.Position = information.BlankHeaderLength;
                                    //stream.Seek(information.BlankHeaderLength, System.IO.SeekOrigin.Begin);
                                    ms.Position = 0;
                                    stream.CopyTo(ms);
                                    ms.Position = 0;

                                    image.StreamSource = ms;

                                    image.EndInit();
                                    image.Freeze();
                                }
                            }
                            
                            this.Image = image;
                            //Debug.WriteLine($"dd:{Thread.CurrentThread.ManagedThreadId}");
                        }//);
                    }

                }

                //リサイズされた画像サイズ
                //this.LoadedHeight = image.PixelHeight;
                //this.LoadedWidth = image.PixelWidth;

                if (file != null && information != null)
                {

                    if ((file.Width != information.GraphicSize.Width)
                        || (file.Height != information.GraphicSize.Height)
                        || (file.Size != information.FileSize))
                    {
                        if (information.GraphicSize.Height > 0)
                        {
                            file.Width = information.GraphicSize.Width;
                        }
                        if (information.GraphicSize.Height > 0)
                        {
                            file.Height = information.GraphicSize.Height;
                        }
                        if (information.FileSize > 0)
                        {
                            file.Size = information.FileSize;
                        }

                        ImageFileUtility.UpdateInformation(file, false, true);

                    }
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
            //});
        }

#if false
        private async Task<Windows.UI.Xaml.Media.Imaging.BitmapSource> LoadImageCmsAsync
            (StorageFile file)
        {
            if (file == null)
            {
                return null;
            }
            //this.currentFile = file;

            //// カラマネする場合
            // モニタプロファイルのStreamを作成
            // ※物理モニタがない環境だと例外を吐く
            IRandomAccessStream profileStream;
            try
            {
                var view = DisplayInformation.GetForCurrentView();
                profileStream = await view.GetColorProfileAsync();
            }
            catch
            {
                return null;
            }

            // Stream → Bytes
            var profileBytes = new byte[profileStream.Size];
            var reader = new DataReader(profileStream);
            await reader.LoadAsync((uint)profileStream.Size);
            reader.ReadBytes(profileBytes);

            // モニタプロファイルのColorContextを作成
            var factory = new ImagingFactory(); // 割とあちこちで使う
            var displayProfile = new ColorContext(factory);
            displayProfile.InitializeFromMemory(DataStream.Create(profileBytes, true, false));

            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                // デコーダーでファイルからフレームを取得
                var decoder = new BitmapDecoder(factory, stream.AsStream(), DecodeOptions.CacheOnDemand);
                if (decoder.FrameCount < 1)
                {
                    return null;
                }
                var frame = decoder.GetFrame(0);

                // 埋め込みプロファイル取得
                var srcColorContexts = frame.TryGetColorContexts(factory);
                var untaggedOrUnsupported = srcColorContexts == null || srcColorContexts.Length < 1;
                // プロファイルが読み込めなかった場合はsRGBとみなす
                var sourceProfile = !untaggedOrUnsupported ? srcColorContexts[0] : sRGBContext.Value;

                SharpDX.WIC.BitmapSource transformSource = frame;
                if (untaggedOrUnsupported)
                {
                    // プロファイルが読み込めなかった場合はsRGBを適用したいので、FormatConverterで32bppPBGRAへ変換
                    // 変換しなかった場合、色変換時にCMYK画像をsRGBとして扱ってしまうことでエラーが発生する
                    var converter = new FormatConverter(factory);
                    converter.Initialize(frame, PixelFormat.Format32bppPBGRA);
                    transformSource = converter;
                }
                // ColorTransformを通すことで色変換ができる
                var transform = new ColorTransform(factory);
                transform.Initialize(transformSource, sourceProfile, displayProfile, PixelFormat.Format32bppPBGRA);

                var stride = transform.Size.Width * 4;    // 横1行のバイト数
                var size = stride * transform.Size.Height;
                var bytes = new byte[size];
                transform.CopyPixels(bytes, stride); // Byte配列にピクセルデータをコピー

                // ピクセルデータをWriteableBitmapに書き込み
                var bitmap = new WriteableBitmap(transform.Size.Width, transform.Size.Height);
                using (var s = bitmap.PixelBuffer.AsStream())
                {
                    await s.WriteAsync(bytes, 0, size);
                }

                return bitmap;
            }

            ////// カラマネしない場合
            //var bitmapImage = new BitmapImage();
            //var fileStream = await file.OpenStreamForReadAsync();
            //await bitmapImage.SetSourceAsync(fileStream.AsRandomAccessStream());
            //this.Image1.Source = bitmapImage;

        }
#endif

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
