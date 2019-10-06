using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Media.Imaging;
using Boredbone.XamlTools;
using ImageLibrary.File;
using ShibugakiViewer.Models;
using ShibugakiViewer.Models.ImageViewer;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;
using XamlAnimatedGif;
using System.IO;
using System.Diagnostics;

namespace ShibugakiViewer.Views.Behaviors
{
    class ImageBehavior : Behavior<Image>, IDisposable
    {

        #region Record

        public Record Record
        {
            get { return (Record)GetValue(RecordProperty); }
            set { SetValue(RecordProperty, value); }
        }

        public static readonly DependencyProperty RecordProperty =
            DependencyProperty.Register(nameof(Record), typeof(Record), typeof(ImageBehavior),
            new PropertyMetadata(null, new PropertyChangedCallback(OnRecordChanged)));

        private static void OnRecordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var record = e.NewValue as Record;

            if (thisInstance != null && e.NewValue != e.OldValue)
            {
                thisInstance.RecordInner = record;

                var element = thisInstance.AssociatedObject;
                if (element == null)
                {
                    return;
                }

                var oldPath = ((Record)e.OldValue)?.FullPath;
                var newPath = record?.FullPath;

                if (newPath != null && newPath.Equals(oldPath))
                {
                    return;
                }


                var text = oldPath + "," + (newPath ?? "null");



                thisInstance.ChangeSource(null, null, false);


                if (record == null)
                {
                    thisInstance.CancelLoading(text);
                    return;
                }


                if (oldPath != record.FullPath)
                {
                    thisInstance.CancelLoading(text);
                    thisInstance.LoadMain(element, record, record.FullPath);
                }

            }
        }

        private Record RecordInner { get; set; }

        #endregion

        #region FilePath

        public string FilePath
        {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(ImageBehavior),
            new PropertyMetadata(null, new PropertyChangedCallback(OnFilePathChanged)));

        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as string;

            if (thisInstance != null && e.NewValue != e.OldValue)
            {
                thisInstance.FilePathInner = value;

                var element = thisInstance.AssociatedObject;
                if (element == null)
                {
                    return;
                }
                thisInstance.ChangeSource(null, null, false);

                thisInstance.CancelLoading((e.OldValue as string) + "," + (value ?? "null"));

                if (value == null)
                {
                    return;
                }

                thisInstance.LoadMain(element, null, value);

            }
        }

        private string FilePathInner { get; set; }

        #endregion

        #region DecodeWidth

        public double DecodeWidth
        {
            get { return (double)GetValue(DecodeWidthProperty); }
            set { SetValue(DecodeWidthProperty, value); }
        }

        public static readonly DependencyProperty DecodeWidthProperty =
            DependencyProperty.Register(nameof(DecodeWidth), typeof(double), typeof(ImageBehavior),
            new PropertyMetadata(128.0, new PropertyChangedCallback(OnDecodeWidthChanged)));

        private static void OnDecodeWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as double?;
            if (thisInstance != null && value.HasValue)
            {
                thisInstance.Width = value.Value;
            }
        }

        private double Width { get; set; } = 128.0;

        #endregion


        #region DecodeHeight

        public double DecodeHeight
        {
            get { return (double)GetValue(DecodeHeightProperty); }
            set { SetValue(DecodeHeightProperty, value); }
        }

        public static readonly DependencyProperty DecodeHeightProperty =
            DependencyProperty.Register(nameof(DecodeHeight), typeof(double), typeof(ImageBehavior),
            new PropertyMetadata(128.0, new PropertyChangedCallback(OnDecodeHeightChanged)));

        private static void OnDecodeHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as double?;
            if (thisInstance != null && value.HasValue)
            {
                thisInstance.Height = value.Value;
            }
        }

        private double Height { get; set; } = 128.0;

        #endregion

        #region IsCmsEnabled

        public bool IsCmsEnabled
        {
            get { return (bool)GetValue(IsCmsEnabledProperty); }
            set { SetValue(IsCmsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsCmsEnabledProperty =
            DependencyProperty.Register(nameof(IsCmsEnabled), typeof(bool), typeof(ImageBehavior),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsCmsEnabledChanged)));

        private static void OnIsCmsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as bool?;
            if (thisInstance != null && value.HasValue)
            {
                thisInstance.IsCmsEnabledInner = value.Value;
            }

        }

        private bool IsCmsEnabledInner { get; set; }

        #endregion

        #region IsFill

        public bool IsFill
        {
            get { return (bool)GetValue(IsFillProperty); }
            set { SetValue(IsFillProperty, value); }
        }

        public static readonly DependencyProperty IsFillProperty =
            DependencyProperty.Register(nameof(IsFill), typeof(bool), typeof(ImageBehavior),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsFillChanged)));

        private static void OnIsFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as bool?;
            if (thisInstance != null && value.HasValue)
            {
                thisInstance.isFill = value.Value;
            }
        }

        private bool isFill = false;

        #endregion





        #region HasPriority

        public bool HasPriority
        {
            get { return (bool)GetValue(HasPriorityProperty); }
            set { SetValue(HasPriorityProperty, value); }
        }

        public static readonly DependencyProperty HasPriorityProperty =
            DependencyProperty.Register(nameof(HasPriority), typeof(bool), typeof(ImageBehavior),
            new PropertyMetadata(false, new PropertyChangedCallback(OnHasPriorityChanged)));

        private static void OnHasPriorityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as bool?;
            if (thisInstance != null && value.HasValue)
            {
                thisInstance.HasPriorityInner = value.Value;
            }
        }
        private bool HasPriorityInner { get; set; }

        #endregion


        #region Quality

        public ImageQuality Quality
        {
            get { return (ImageQuality)GetValue(QualityProperty); }
            set { SetValue(QualityProperty, value); }
        }

        public static readonly DependencyProperty QualityProperty =
            DependencyProperty.Register(nameof(Quality), typeof(ImageQuality), typeof(ImageBehavior),
            new PropertyMetadata(ImageQuality.LowQuality, new PropertyChangedCallback(OnQualityChanged)));

        private static void OnQualityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as ImageQuality?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.ImageQualityInner = value.Value;
            }
        }
        private ImageQuality ImageQualityInner { get; set; } = ImageQuality.LowQuality;

        #endregion

        #region Trigger

        public bool Trigger
        {
            get { return (bool)GetValue(TriggerProperty); }
            set { SetValue(TriggerProperty, value); }
        }

        public static readonly DependencyProperty TriggerProperty =
            DependencyProperty.Register(nameof(Trigger), typeof(bool), typeof(ImageBehavior),
            new PropertyMetadata(false, new PropertyChangedCallback(OnTriggerChanged)));

        private static void OnTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue)
            {
                //if (!value.Value)
                //{
                //    return;
                //}

                var record = thisInstance.RecordInner;
                if (record != null)
                {
                    thisInstance.LoadMain(thisInstance.AssociatedObject, record, record.FullPath);
                }
                else
                {
                    var path = thisInstance.FilePathInner;
                    if (path != null)
                    {
                        thisInstance.LoadMain(thisInstance.AssociatedObject, null, path);
                    }
                }
            }

        }

        #endregion

        #region ZoomFactor

        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(ImageBehavior),
            new PropertyMetadata(1.0, new PropertyChangedCallback(OnZoomFactorChanged)));

        private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ImageBehavior;
            var value = e.NewValue as double?;


            if (thisInstance != null && value.HasValue)
            {
                thisInstance.DecodeMetaImage(thisInstance.Record, value.Value).FireAndForget();
            }
        }

        #endregion

        #region IsGifAnimationEnabled

        public bool IsGifAnimationEnabled
        {
            get { return (bool)GetValue(IsGifAnimationEnabledProperty); }
            set { SetValue(IsGifAnimationEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsGifAnimationEnabledProperty =
            DependencyProperty.Register(nameof(IsGifAnimationEnabled), typeof(bool),
                typeof(ImageBehavior), new PropertyMetadata(false));

        #endregion



        private CompositeDisposable disposables = new CompositeDisposable();




        private ApplicationCore core = ((App)System.Windows.Application.Current).Core;
        private ImageBuffer Buffer => this.core.ImageBuffer;



        private void CancelLoading(string oldPath)
        {
            this.disposables.Clear();
        }

        private void LoadMain(Image element, Record record, string path)
        {
            //this.disposables.Clear();

            var width = this.Width;
            var height = this.Height;
            var quality = this.ImageQualityInner;
            var cms = this.IsCmsEnabledInner;
            var priority = this.HasPriorityInner;
            var zoom = this.ZoomFactor;

            if (core.MetaImageExtention.Contains(System.IO.Path.GetExtension(path).ToLower()))
            {
                this.ChangeToMetaImage(record, path, zoom, width, height).FireAndForget();
                return;
            }


            var option = new ImageLoadingOptions
                (width, height, this.IsFill, quality, cms);

            var tokenSource = new CancellationTokenSource();

            var subject = new Subject<ImageSourceContainer>();



            subject
                .LastOrDefaultAsync()
                .Take(1)
                .Select(x =>
                {
                    if (x != null)
                    {
                        return x;
                    }

                    ImageSourceContainer image = null;
                    if (this.Buffer.TryGetImage(path, quality, out image))
                    {
                        return image;
                    }
                    return null;
                })
                .ObserveOnUIDispatcher()
                .Subscribe(image =>
                {
                    if (image != null)
                    {
                        var p = this.RecordInner?.FullPath ?? this.FilePathInner;

                        if (p == null)
                        {
                            this.ChangeSource(null, null, false);
                        }
                        else if (p.Equals(image?.FullPath))
                        {
                            this.ChangeSource(image.Image, image.FullPath,
                                image.Information != null
                                && image.Information.Type == GraphicFileType.Gif
                                && image.Quality >= ImageQuality.Resized);
                        }
                    }
                    else
                    {
                        //this.ChangeSource(null, null, false);
                    }

                    this.disposables.Clear();

                });


            Disposable.Create(() =>
            {
                tokenSource?.Cancel(true);

            }).AddTo(this.disposables);



            if (record != null)
            {
                //if (quality == ImageQuality.ThumbNail)
                //{
                //    this.Buffer.RequestLoading(record, option, subject, priority, tokenSource);
                //}
                //else
                //{
                this.Buffer.RequestLoading(record, option, subject, priority, tokenSource.Token);
                //}
            }
            else
            {
                this.Buffer.RequestLoading(path, option, subject, priority, tokenSource.Token);
            }



        }

        private async Task ChangeToMetaImage(Record record, string path,
            double? zoomFactor, double width, double height)
        {

            using (var meta = new MetaImage())
            {
                await Task.Run(() =>
                {
                    if (record != null)
                    {
                        meta.LoadImage(record);
                    }
                    else
                    {
                        meta.LoadImage(path);
                    }
                    //return meta.DecodeImage(null, width, height);
                });

                var image = await (zoomFactor.HasValue
                   ? meta.DecodeImageAsync(zoomFactor, null, null)
                   : meta.DecodeImageAsync(null, width, height));

                this.ChangeSource(image, meta.Path, false);
                return;
            }
        }

        private async Task DecodeMetaImage(Record record, double zoomFactor)
        {
            if (record == null)
            {
                return;
            }

            if (core.MetaImageExtention.Contains(System.IO.Path.GetExtension(record.FileName).ToLower()))
            {
                using (var meta = new MetaImage())
                {
                    await Task.Run(() =>
                    {
                        meta.LoadImage(record);
                    });

                    var image = await meta.DecodeImageAsync(zoomFactor, null, null);

                    this.ChangeSource(image, meta.Path, false);
                    return;
                }
            }
        }

        private string previousPath = null;
        public event Action<OldNewPair<string>> SourceChanged;

        /// <summary>
        /// 画像を変更
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <param name="isGifAnimation"></param>
        private void ChangeSource(ImageSource source, string path, bool isGifAnimation)
        {
            var pathChanged = (this.previousPath == null) ? path != null
                : this.previousPath.Equals(path);

            var element = this.AssociatedObject;
            if (element == null)
            {
                this.SourceChanged?.Invoke(new OldNewPair<string>(this.previousPath, null));
                this.previousPath = null;
                return;
            }
            this.SourceChanged?.Invoke(new OldNewPair<string>(this.previousPath, path));
            this.previousPath = path;


            this.StopGifAnimation();


            if (this.IsGifAnimationEnabled && isGifAnimation && pathChanged && path != null)
            {
                //Gifアニメのストリームを設定
                Stream stream = null;
                try
                {
                    stream = File.OpenRead(path);
                    AnimationBehavior.SetSourceStream(element, stream);
                }
                catch
                {
                    stream?.Dispose();
                }

            }
            else
            {
                //画像設定
                element.Source = source;

            }
        }

        private void Load()
        {
            var record = this.RecordInner;
            if (record != null)
            {
                this.LoadMain(this.AssociatedObject, record, record.FullPath);
            }
            else
            {
                var path = this.FilePathInner;
                if (path != null)
                {
                    this.LoadMain(this.AssociatedObject, null, path);
                }
            }
        }

        /// <summary>
        /// GIFアニメ用に開いていたStreamを破棄
        /// </summary>
        private void StopGifAnimation()
        {
            var element = this.AssociatedObject;
            if (element != null)
            {
                try
                {
                    //前にGifアニメを再生していたら破棄
                    var prevStream = AnimationBehavior.GetSourceStream(element);
                    if (prevStream != null)
                    {
                        AnimationBehavior.SetSourceStream(element, null);
                        prevStream.Dispose();
                    }
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.Load();
        }

        public void Dispose()
        {
            this.StopGifAnimation();
        }
    }
}
