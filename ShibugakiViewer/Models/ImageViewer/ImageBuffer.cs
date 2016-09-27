using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using System.Collections.Generic;
using System.Reactive.Linq;
using ImageLibrary.File;
using System.Windows;
using System.Windows.Threading;
using Boredbone.XamlTools;
using System.Reactive.Subjects;
using System.Reactive;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using Boredbone.Utility.Notification;

namespace ShibugakiViewer.Models.ImageViewer
{
    /// <summary>
    /// 読み込んだ画像を保持するバッファ
    /// </summary>
    public class ImageBuffer : DisposableBase
    {
        private ConcurrentDictionary<string, ImageBufferItem> images;
        private ConcurrentDictionary<string, ImageBufferItem> thumbNailImages;

        private ulong count = 0;

        private const int bufferSize = 16;
        private const int thumbNailbufferSize = 256;
        //private const int thumbnailThreshold = 128;
        //private const double resizeThreshold = 1.5;
        private const int failedLoadingLength = 1024;
        private readonly Size failedLoadingSize = new Size(failedLoadingLength, failedLoadingLength);


        private const int mainQueueCapacity = 8;
        private const int preloadQueueCapacity = 8;
        private const int lowQueueCapacity = 8;

        public int ThumbnailRequestCapacity { get; set; } = 64;
        public HashSet<string> MetaImageExtention { get; set; }

        private readonly Size lowQualitySize = new Size(128, 128);


        private Subject<string> UpdatedSubject { get; }
        public IObservable<string> Updated => this.UpdatedSubject.AsObservable();

        private readonly ConcurrentQueue<CommandPacket> currentFileLoadRequest
            = new ConcurrentQueue<CommandPacket>();
        private readonly ConcurrentQueue<CommandPacket> lowQualityPreLoadRequest
            = new ConcurrentQueue<CommandPacket>();
        private readonly ConcurrentQueue<CommandPacket> highQualityPreLoadRequest
            = new ConcurrentQueue<CommandPacket>();
        private readonly ConcurrentQueue<CommandPacket> thumbNailLoadRequest
            = new ConcurrentQueue<CommandPacket>();

        private readonly ConcurrentBag<ObservableCancellationTokenSource> cancellationSources
            = new ConcurrentBag<ObservableCancellationTokenSource>();

        private Subject<Unit> ActionQueueSubject { get; }

        private readonly Boredbone.Utility.AsyncLock asyncLock;

        private static IObserver<ImageSourceContainer> emptyObserver
            = Observer.Create<ImageSourceContainer>(_ => { });


        public ImageBuffer()
        {
            images = new ConcurrentDictionary<string, ImageBufferItem>();
            thumbNailImages = new ConcurrentDictionary<string, ImageBufferItem>();

            this.UpdatedSubject = new Subject<string>().AddTo(this.Disposables);

            this.asyncLock = new Boredbone.Utility.AsyncLock();

            this.ActionQueueSubject = new Subject<Unit>().AddTo(this.Disposables);

            var scheduler = new EventLoopScheduler();


            //読み込み要求を処理
            var subscription = this.ActionQueueSubject
                .ObserveOn(scheduler)
                .Subscribe(async _ =>
                {
                    CommandPacket packet;
                    while (this.currentFileLoadRequest.TryDequeue(mainQueueCapacity, out packet)
                        || this.lowQualityPreLoadRequest.TryDequeue(lowQueueCapacity, out packet)
                        || this.highQualityPreLoadRequest.TryDequeue(preloadQueueCapacity, out packet)
                        || this.thumbNailLoadRequest.TryDequeue(this.ThumbnailRequestCapacity, out packet))
                    {
                        await this.LoadImageAsync(packet).ConfigureAwait(false);
                    }
                });

            Disposable.Create(() =>
            {
                subscription.Dispose();
                scheduler.Dispose();
            }).AddTo(this.Disposables);

        }

        private async Task<ImageSourceContainer> GetImageAsync
            (Record file, ImageLoadingOptions option, bool hasPriority,
            ObservableCancellationTokenSource tokenSource)
        {
            return await this.GetImageMainAsync(file, file.FullPath, option, hasPriority, tokenSource);
        }
        private async Task<ImageSourceContainer> GetImageAsync
            (string path, ImageLoadingOptions option, bool hasPriority,
            ObservableCancellationTokenSource tokenSource)
        {
            return await this.GetImageMainAsync(null, path, option, hasPriority, tokenSource);
        }

        private async Task<ImageSourceContainer> GetImageMainAsync
            (Record file, string path, ImageLoadingOptions option, bool hasPriority,
            ObservableCancellationTokenSource tokenSource)
        {

            ImageSourceContainer image;
            if (this.TryGetImage(path, option.Quality, out image))
            {
                if (image != null && image.Image != null)
                {
                    if (file != null && (file.Width <= 0 || file.Height <= 0)
                        && image.Information != null)
                    {
                        file.Width = image.Information.GraphicSize.Width;
                        file.Height = image.Information.GraphicSize.Height;
                    }
                    return image;
                }
            }

            try
            {
                if (tokenSource.IsDisposed)
                {
                    return null;
                }

                this.cancellationSources.Add(tokenSource);

                var token = tokenSource.Token;

                return await Task.Run(async () =>
                {
                    var observer = new Subject<ImageSourceContainer>();
                    if (file != null)
                    {
                        this.RequestLoading(file, option, observer, hasPriority, token);
                    }
                    else
                    {
                        this.RequestLoading(path, option, observer, hasPriority, token);
                    }

                    image = null;

                    if (!tokenSource.IsDisposed)
                    {
                        try
                        {
                            image = await observer
                                .TakeUntil(tokenSource.Canceled.Select(_ => 0).LastOrDefaultAsync())
                                .Catch((Exception e) => Observable.Empty<ImageSourceContainer>())
                                .LastOrDefaultAsync();
                        }
                        catch
                        {
                        }
                    }

                    if (image != null || this.TryGetImage(path, option.Quality, out image))
                    {
                        return image;
                    }

                    return null;
                });
            }
            catch
            {
                return null;
            }
        }



        public bool TryGetImage
            (string path, ImageQuality quality, out ImageSourceContainer image)
        {
            ImageBufferItem result;
            if (this.TryGetImageData(path, quality, out result))
            {
                if (result != null)
                {
                    count++;
                    image = result.GetImage(count);
                    return true;
                }
            }
            image = null;
            return false;
        }


        private bool TryGetImageData
            (string path, ImageQuality quality, out ImageBufferItem result)
        {

            if (path == null)
            {
                result = null;
                return false;
            }

            var key = path;

            if (quality == ImageQuality.ThumbNail)
            {
                if (thumbNailImages.TryGetValue(key, out result))
                {
                    if (result != null)
                    {
                        return true;
                    }
                    thumbNailImages.TryRemove(key, out result);
                }
                return false;
            }

            if (images.TryGetValue(key, out result))
            {
                if (result.Quality == ImageQuality.OriginalSize
                    || quality == ImageQuality.Resized
                    || quality == ImageQuality.LowQuality)
                {
                    if (result != null)
                    {
                        return true;
                    }
                    images.TryRemove(key, out result);
                }
            }

            if (quality == ImageQuality.LowQuality)
            {
                if (thumbNailImages.TryGetValue(key, out result))
                {
                    if (result != null)
                    {
                        return true;
                    }
                    thumbNailImages.TryRemove(key, out result);
                }
            }

            result = null;
            return false;
        }


        public void RequestLoading
            (Record file, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, CancellationToken token)
            => this.RequestLoadingToTask(file, file?.FullPath, option, observer, hasPriority, token);


        public void RequestLoading
            (string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, CancellationToken token)
            => this.RequestLoadingToTask(null, path, option, observer, hasPriority, token);


        public void RequestLoading
            (Record file, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, ObservableCancellationTokenSource tokenSource)
            => this.RequestLoadingToTask(file, file.FullPath, option, observer, hasPriority, tokenSource);

        public void RequestLoading
            (string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, ObservableCancellationTokenSource tokenSource)
            => this.RequestLoadingToTask(null, path, option, observer, hasPriority, tokenSource);


        private void RequestLoadingToTask
            (Record file, string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, CancellationToken token)
        {
            //Task.Run(() =>
            this.RequestLoadingMain(file, path, option, observer, hasPriority, token);
        }

        private void RequestLoadingToTask
            (Record file, string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, ObservableCancellationTokenSource tokenSource)
        {
            if ((file != null || path != null) && !tokenSource.IsDisposed)
            {
                //Task.Run(() =>
                {
                    this.cancellationSources.Add(tokenSource);
                    var token = tokenSource.Token;
                    this.RequestLoadingMain(file, path, option, observer, hasPriority, token);
                }//);
            }
        }


        private void RequestLoadingMain
            (Record file, string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, CancellationToken token)
        {
            var filePath = file?.FullPath ?? path;

            if (filePath == null)
            {
                return;
            }


            if (observer == null)
            {
                observer = emptyObserver;
            }


            ImageSourceContainer image;
            if (this.TryGetImage(filePath, option.Quality, out image))
            {
                if (image != null && image.Image != null)
                {
                    if (file != null && (file.Width <= 0 || file.Height <= 0)
                        && image.Information != null)
                    {
                        file.Width = image.Information.GraphicSize.Width;
                        file.Height = image.Information.GraphicSize.Height;
                    }
                    observer.OnNext(image);
                    observer.OnCompleted();
                    return;
                }
            }

            if (this.MetaImageExtention != null
                && this.MetaImageExtention.Contains(System.IO.Path.GetExtension(filePath).ToLower()))
            {
                observer.OnCompleted();
                return;
            }

            var request = (file != null)
                ? new CommandPacket(file, option.Clone(), observer)
                : new CommandPacket(filePath, option.Clone(), observer);

            request.CancellationToken = token;


            if (option.Quality == ImageQuality.ThumbNail)
            {
                this.thumbNailLoadRequest.Enqueue(request);
            }
            else if (hasPriority)
            {
                this.currentFileLoadRequest.Enqueue(request);
            }
            else if (option.Quality == ImageQuality.LowQuality)
            {
                this.lowQualityPreLoadRequest.Enqueue(request);
            }
            else
            {
                this.highQualityPreLoadRequest.Enqueue(request);
            }
            if (this.ActionQueueSubject.HasObservers)
            {
                this.ActionQueueSubject.OnNext(Unit.Default);
            }
        }


        private async Task LoadImageAsync(CommandPacket command)
        {
            var key = command.Path;

            ImageSourceContainer result;
            if (this.TryGetImage(key, command.Option.Quality, out result))
            {
                if (result != null)
                {
                    command.Observer.OnNext(result);
                    command.Observer.OnCompleted();
                    return;
                }
            }

            if (command.CancellationToken.IsCancellationRequested)
            {
                command.Observer.OnCompleted();
                return;
            }


            using (var locked = await this.asyncLock.LockAsync())
            {
                var image = new ImageSourceContainer();

                bool failedFlag = false;

                var option = command.Option;
                var file = command.File;

                Size? frameSize = null;

                switch (option.Quality)
                {
                    case ImageQuality.ThumbNail:
                    case ImageQuality.Resized:
                        //リサイズ
                        frameSize = new Size(option.FrameWidth, option.FrameHeight);
                        break;
                    case ImageQuality.LowQuality:
                        //低画質読み込み
                        frameSize = this.lowQualitySize;
                        break;
                    case ImageQuality.OriginalSize:
                        //オリジナルサイズで読み込み
                        frameSize = null;
                        break;
                    default:
                        break;
                }

                var asThumbnail = option.Quality <= ImageQuality.LowQuality;


                /*
                int thumbNailSize = -1;
                int frameWidth = -1;
                int frameHeight = -1;


                if (option.Quality == ImageQuality.ThumbNail)
                {
                    thumbNailSize = (int)((option.FrameWidth > option.FrameHeight)
                        ? option.FrameWidth
                        : option.FrameHeight);
                    if (thumbNailSize < 1)
                    {
                        thumbNailSize = 16;
                    }
                }
                else if (file == null)
                {
                    if (option.Quality == ImageQuality.LowQuality)
                    {
                        thumbNailSize = thumbnailThreshold;
                    }
                    else if (option.Quality == ImageQuality.Resized)
                    {
                        frameWidth = (int)option.FrameWidth;
                    }
                }
                else if (option.Quality == ImageQuality.LowQuality
                    && (file.Width > thumbnailThreshold
                    || file.Height > thumbnailThreshold))
                {
                    thumbNailSize = thumbnailThreshold;
                }
                else if (option.Quality == ImageQuality.Resized
                    && option.FrameWidth > 10 && option.FrameHeight > 10
                    && (file.Width > option.FrameWidth * resizeThreshold
                    || file.Height > option.FrameHeight * resizeThreshold))
                {

                    var verticalRate = file.Height / option.FrameHeight;
                    var horizontalRate = file.Width / option.FrameWidth;

                    if (option.IsFill)
                    {
                        if (horizontalRate > verticalRate)
                        {
                            frameHeight = (int)option.FrameHeight;
                        }
                        else
                        {
                            frameWidth = (int)option.FrameWidth;
                        }
                    }
                    else
                    {
                        if (horizontalRate > verticalRate)
                        {
                            frameWidth = (int)option.FrameWidth;
                        }
                        else
                        {
                            frameHeight = (int)option.FrameHeight;
                        }
                    }
                }*/


                try
                {
                    if (file != null)
                    {
                        var t = await image.LoadImageAsync
                            (file, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                    }
                    else
                    {
                        await image.LoadImageAsync
                            (key, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                    }
                }
                catch (OutOfMemoryException e)
                {
                    if (option.Quality == ImageQuality.ThumbNail)
                    {
                        ClearBuffer();
                        command.Observer.OnError(e);
                        return;
                    }
                    else
                    {
                        failedFlag = true;
                    }
                }
                catch (Exception e)
                {
                    command.Observer.OnError(e);
                    return;
                }


                if (failedFlag)
                {
                    if (command.CancellationToken.IsCancellationRequested)
                    {
                        command.Observer.OnCompleted();
                        return;
                    }


                    ClearBuffer();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(300);

                    image = new ImageSourceContainer();

                    if (!frameSize.HasValue
                        || frameSize.Value.Width > failedLoadingLength
                        || frameSize.Value.Height > failedLoadingLength)
                    {
                        //サイズ小さめ
                        frameSize = failedLoadingSize;
                    }
                    /*
                    if (file == null)
                    {
                        frameWidth = (int)option.FrameWidth;
                    }
                    else if (thumbNailSize <= 0)
                    {
                        if (file.Width / option.FrameWidth
                            > file.Height / option.FrameHeight)
                        {
                            frameWidth = (int)option.FrameWidth;
                        }
                        else
                        {
                            frameHeight = (int)option.FrameHeight;
                        }
                    }*/

                    var reloadOption = command.Option.Clone();

                    try
                    {
                        if (file != null)
                        {
                            var t2 = await image.LoadImageAsync
                               (file, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                        }
                        else
                        {
                            await image.LoadImageAsync
                                (key, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                        }

                    }
                    catch (Exception e)
                    {
                        command.Observer.OnError(e);
                        return;
                    }
                }

                if (image.HasImage())// && !image.IsError())
                {
                    if (option.Quality == ImageQuality.ThumbNail || image.Quality == ImageQuality.LowQuality)
                    {
                        thumbNailImages.AddOrExtrude
                            (key, new ImageBufferItem(image, ++this.count), thumbNailbufferSize);
                    }
                    else
                    {
                        images.AddOrExtrude
                            (key, new ImageBufferItem(image, ++this.count), bufferSize);
                    }

                    this.UpdatedSubject.OnNext(key);
                    command.Observer.OnNext(image);
                }
                command.Observer.OnCompleted();
            }
        }


        public void ClearAll()
        {
            this.ClearRequests();
            this.ClearThumbNails();
            this.ClearBuffer();
        }

        private void ClearBuffer()
        {
            this.images.ClearBuffer();
            this.thumbNailImages.ClearBuffer();
        }


        public void ClearRequests()
        {
            this.ClearQueue(this.highQualityPreLoadRequest);
            this.ClearQueue(this.lowQualityPreLoadRequest);
            this.ClearQueue(this.currentFileLoadRequest);
        }

        public void ClearThumbNailRequests()
        {
            ObservableCancellationTokenSource result;
            while (this.cancellationSources.TryTake(out result))
            {
                if (result != null && !result.IsDisposed)
                {
                    result.Cancel();
                }
            }
            this.ClearQueue(this.thumbNailLoadRequest);
        }

        public void ClearThumbNails()
        {
            this.ClearThumbNailRequests();
            this.thumbNailImages.ClearBuffer();
        }


        private void ClearQueue(ConcurrentQueue<CommandPacket> queue)
        {
            CommandPacket item;
            while (queue.TryDequeue(out item))
            {
                item.Observer.OnCompleted();
            }
        }

        public override void Dispose()
        {
            this.ClearAll();
            base.Dispose();
        }


        /// <summary>
        /// 要求の情報を保持
        /// </summary>
        private class CommandPacket
        {
            public Record File { get; set; }
            public IObserver<ImageSourceContainer> Observer { get; set; }
            public ImageLoadingOptions Option { get; set; }
            public CancellationToken CancellationToken { get; set; }

            public string Path
                => (this.path != null) ? this.path : this.File?.FullPath;

            private string path;


            public CommandPacket(Record file, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer)
            {
                this.File = file;
                this.path = null;
                this.Observer = observer;
                this.Option = option;
            }

            public CommandPacket(string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer)
            {
                this.File = null;
                this.path = path;
                this.Observer = observer;
                this.Option = option;
            }
        }

    }

    /// <summary>
    /// 画像バッファの拡張メソッド
    /// </summary>
    static class Extensions
    {
        /// <summary>
        /// バッファ内の画像をすべて解放してからDictionaryを空にする
        /// </summary>
        /// <param name="dictionary"></param>
        public static void ClearBuffer(this ConcurrentDictionary<string, ImageBufferItem> dictionary)
        {
            foreach (var e in dictionary)
            {
                e.Value.Dispose();
            }
            dictionary.Clear();
        }

        /// <summary>
        /// 一番昔に読み込まれた画像をバッファから削除
        /// </summary>
        /// <param name="dictionary"></param>
        public static void ReleaseOldImage(this ConcurrentDictionary<string, ImageBufferItem> dictionary)
        {
            ulong min = ulong.MaxValue;
            string minKey = null;
            foreach (var val in dictionary)
            {
                if (val.Value.LastLoadedCount < min)
                {
                    minKey = val.Key;
                    min = val.Value.LastLoadedCount;
                }
            }

            if (minKey != null)
            {
                ImageBufferItem result;
                dictionary.TryRemove(minKey, out result);
                result?.Dispose();
            }
        }

        /// <summary>
        /// データを追加し、バッファサイズが大きくなったときは古いものを削除
        /// </summary>
        /// <param name="dictionary"></param>
        public static void AddOrExtrude
            (this ConcurrentDictionary<string, ImageBufferItem> dictionary,
            string key, ImageBufferItem value, int size)
        {

            if (dictionary.Count > size)
            {
                dictionary.ReleaseOldImage();
            }
            dictionary.AddOrUpdate(key, value,
                (oldkey, oldvalue) => (oldvalue.Quality <= value.Quality) ? value : oldvalue);
            //dictionary.AddOrReplace(key, value);
        }

    }


    /// <summary>
    /// 読み込み要求の設定情報
    /// </summary>
    public class ImageLoadingOptions
    {
        public double FrameWidth { get; }
        public double FrameHeight { get; }
        public bool IsFill { get; }
        public ImageQuality Quality { get; }
        public bool CmsEnable { get; }

        public ImageLoadingOptions(double width, double height, bool fill, ImageQuality quality, bool cms)
        {
            this.FrameWidth = width;
            this.FrameHeight = height;
            this.IsFill = fill;
            this.Quality = quality;
            this.CmsEnable = cms;
        }


        public ImageLoadingOptions Clone()
        {
            return new ImageLoadingOptions(
                this.FrameWidth,
                this.FrameHeight,
                this.IsFill,
                this.Quality,
                this.CmsEnable);
        }
    }


    /// <summary>
    /// 画像情報と最後に読み込まれた時期を保持
    /// </summary>
    class ImageBufferItem : IDisposable
    {
        private ImageSourceContainer image;
        public ulong LastLoadedCount { get; private set; }

        public ImageBufferItem(ImageSourceContainer source, ulong initialCount)
        {
            this.image = source;
            this.LastLoadedCount = initialCount;
        }


        public ImageQuality Quality
        {
            get { return image.Quality; }
            set { image.Quality = value; }
        }


        public ImageSourceContainer GetImage(ulong count)
        {
            LastLoadedCount = count;
            return image;
        }
        public void Dispose()
        {
            image.Dispose();
        }
    }


    internal static class QueueExtensions
    {
        public static bool TryDequeue<T>(this ConcurrentQueue<T> queue, int capacity, out T result)
        {
            T buf;
            while (queue.Count > capacity)
            {
                queue.TryDequeue(out buf);
            }
            return queue.TryDequeue(out result);
        }
    }

}
