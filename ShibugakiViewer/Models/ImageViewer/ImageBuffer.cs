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
using System.Reactive.Subjects;
using System.Reactive;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using Boredbone.Utility.Notification;
using Boredbone.Utility.LockingCollection;

namespace ShibugakiViewer.Models.ImageViewer
{
    /// <summary>
    /// 読み込んだ画像を保持するバッファ
    /// </summary>
    public class ImageBuffer : DisposableBase
    {
        private ImageDictionary images;
        private ImageDictionary thumbNailImages;

        private ulong count = 0;

        private const int bufferSize = 16;
        private const int thumbNailbufferSize = 256;
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

        private readonly LockingQueue<CommandPacket> currentFileLoadRequest
            = new LockingQueue<CommandPacket>();
        private readonly LockingQueue<CommandPacket> lowQualityPreLoadRequest
            = new LockingQueue<CommandPacket>();
        private readonly LockingQueue<CommandPacket> highQualityPreLoadRequest
            = new LockingQueue<CommandPacket>();
        private readonly LockingQueue<CommandPacket> thumbNailLoadRequest
            = new LockingQueue<CommandPacket>();
        

        private Subject<Unit> ActionQueueSubject { get; }


        private static IObserver<ImageSourceContainer> emptyObserver
            = Observer.Create<ImageSourceContainer>(_ => { });


        public ImageBuffer()
        {
            images = new ImageDictionary();
            thumbNailImages = new ImageDictionary();

            this.UpdatedSubject = new Subject<string>().AddTo(this.Disposables);

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


        public bool TryGetImage
            (string path, ImageQuality quality, out ImageSourceContainer image)
        {
            if (this.TryGetImageData(path, quality, out var result))
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
                return this.thumbNailImages.TryGetOrRemove(key, out result);
            }

            if (this.images.TryGetOrRemoveWithQuality(key, quality, out result))
            {
                return true;
            }
            

            if (quality == ImageQuality.LowQuality)
            {
                if (this.thumbNailImages.TryGetOrRemove(key, out result))
                {
                    return true;
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
        

        private void RequestLoadingToTask
            (Record file, string path, ImageLoadingOptions option, IObserver<ImageSourceContainer> observer,
            bool hasPriority, CancellationToken token)
        {
            if ((file != null || path != null) && !token.IsCancellationRequested)
            {
                this.RequestLoadingMain(file, path, option, observer, hasPriority, token);
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

            
            if (this.TryGetImage(filePath, option.Quality, out var image))
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

        /// <summary>
        /// 画像読み込みメイン処理
        /// 同じスケジューラの上でシーケンシャルに動作
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private async ValueTask<bool> LoadImageAsync(CommandPacket command)
        {
            var key = command.Path;
            
            if (this.TryGetImage(key, command.Option.Quality, out var result))
            {
                if (result != null)
                {
                    command.Observer.OnNext(result);
                    command.Observer.OnCompleted();
                    return true;
                }
            }

            var token = command.CancellationToken;

            if (token.IsCancellationRequested)
            {
                command.Observer.OnCompleted();
                return true;
            }


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
                    if (option.FrameWidth > 1 && option.FrameHeight > 1)
                    {
                        frameSize = new Size(option.FrameWidth, option.FrameHeight);
                    }
                    else if (option.Quality == ImageQuality.ThumbNail)
                    {
                        frameSize = this.lowQualitySize;
                    }
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

            try
            {
                if (file != null)
                {
                    image.LoadImage
                        (file, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                }
                else
                {
                    image.LoadImage
                        (key, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                }
            }
            catch (OutOfMemoryException e)
            {
                if (option.Quality == ImageQuality.ThumbNail)
                {
                    ClearBuffer();
                    command.Observer.OnError(e);
                    return true;
                }
                else
                {
                    failedFlag = true;
                }
            }
            catch (Exception e)
            {
                command.Observer.OnError(e);
                return true;
            }


            if (failedFlag)
            {
                if (token.IsCancellationRequested)
                {
                    command.Observer.OnCompleted();
                    return true;
                }


                this.ClearBuffer();
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

                var reloadOption = command.Option.Clone();

                try
                {
                    if (file != null)
                    {
                        image.LoadImage
                           (file, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                    }
                    else
                    {
                        image.LoadImage
                            (key, frameSize, asThumbnail, option.IsFill, option.CmsEnable);
                    }

                }
                catch (Exception e)
                {
                    command.Observer.OnError(e);
                    return true;
                }
            }

            if (image.HasImage())
            {
                if (option.Quality == ImageQuality.ThumbNail || image.Quality <= ImageQuality.LowQuality)
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
            return true;
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

        private void ClearQueue(LockingQueue<CommandPacket> queue)
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

            public string Path => this.path ?? this.File?.FullPath;

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


        private class ImageDictionary : LockingDictionary<string, ImageBufferItem>
        {
            public bool TryGetOrRemove(string key, out ImageBufferItem result)
            {
                lock (this.gate)
                {
                    if (this.dictionary.TryGetValue(key, out result))
                    {
                        if (result != null)
                        {
                            return true;
                        }
                        this.dictionary.Remove(key);
                    }
                    return false;
                }
            }

            public bool TryGetOrRemoveWithQuality(string key, ImageQuality quality, out ImageBufferItem result)
            {

                lock (this.gate)
                {
                    if (this.dictionary.TryGetValue(key, out result))
                    {
                        if ((result != null && result.Quality == ImageQuality.OriginalSize)
                            || quality == ImageQuality.Resized
                            || quality == ImageQuality.LowQuality)
                        {
                            if (result != null)
                            {
                                return true;
                            }
                            this.dictionary.Remove(key);
                        }
                    }
                    return false;
                }
            }
            public void ClearBuffer()
            {
                lock (this.gate)
                {
                    foreach (var e in this.dictionary)
                    {
                        e.Value.Dispose();
                    }
                    this.dictionary.Clear();
                }
            }

            public void AddOrExtrude(string key, ImageBufferItem value, int size)
            {

                lock (this.gate)
                {
                    if (this.dictionary.Count > size)
                    {
                        this.ReleaseOldImage();
                    }
                    
                    if (this.dictionary.TryGetValue(key, out var result))
                    {
                        if (result.Quality > value.Quality)
                        {
                            return;
                        }
                    }
                    this.dictionary[key] = value;
                }
            }

            private void ReleaseOldImage()
            {
                ulong min = ulong.MaxValue;
                string minKey = null;
                foreach (var val in this.dictionary)
                {
                    if (val.Value.LastLoadedCount < min)
                    {
                        minKey = val.Key;
                        min = val.Value.LastLoadedCount;
                    }
                }

                if (minKey != null)
                {
                    this.dictionary[minKey]?.Dispose();
                    this.dictionary.Remove(minKey);
                }
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
                dictionary.TryRemove(minKey, out var result);
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
