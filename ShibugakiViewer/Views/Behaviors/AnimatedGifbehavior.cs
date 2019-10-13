using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Media.Imaging;
using Boredbone.Utility.Notification;
using ImageLibrary.File;
using ShibugakiViewer.Models;
using ShibugakiViewer.Models.ImageViewer;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;
using XamlAnimatedGif;
using System.IO;
using Microsoft.Xaml.Behaviors;

namespace ShibugakiViewer.Views.Behaviors
{
    class AnimatedGifbehavior : Behavior<Image>, IDisposable
    {

        #region Record

        public Record Record
        {
            get { return (Record)GetValue(RecordProperty); }
            set { SetValue(RecordProperty, value); }
        }

        public static readonly DependencyProperty RecordProperty =
            DependencyProperty.Register(nameof(Record), typeof(Record), typeof(AnimatedGifbehavior),
            new PropertyMetadata(null, new PropertyChangedCallback(OnRecordChanged)));

        private static void OnRecordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as AnimatedGifbehavior;
            var record = e.NewValue as Record;

            if (thisInstance != null && e.NewValue != e.OldValue)
            {
                thisInstance.RecordSubject.OnNext(record);
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
                typeof(AnimatedGifbehavior), new PropertyMetadata(true));

        #endregion

        private Subject<Record> RecordSubject { get; }
        private CompositeDisposable Disposables { get; }

        private string previousPath = null;



        public AnimatedGifbehavior()
        {
            this.Disposables = new CompositeDisposable();

            this.RecordSubject = new Subject<Record>().AddTo(this.Disposables);

            this.RecordSubject
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOnUIDispatcher()
                .Merge(this.RecordSubject.Select(_ => (Record)null))
                .Subscribe(x => this.SetAnimation(x))
                .AddTo(this.Disposables);
        }



        private void SetAnimation(Record record)
        {
            var path = record?.FullPath;

            var pathChanged = (this.previousPath == null) ? path != null
                : this.previousPath.Equals(path);

            this.previousPath = path;


            this.StopGifAnimation();

            //Gifアニメのストリームを設定
            if (this.IsGifAnimationEnabled && pathChanged && path.HasText() && path.EndsWith(".gif"))
            {
                Stream stream = null;
                try
                {
                    stream = File.OpenRead(path);

                    var information = new GraphicInformation(stream);

                    if (information.Type == GraphicFileType.Gif)
                    {
                        stream.Position = information.BlankHeaderLength;
                        AnimationBehavior.SetSourceStream(this.AssociatedObject, stream);
                    }
                    else
                    {
                        stream.Dispose();
                    }
                    //AnimationBehavior.SetSourceUri(element, new Uri(path));
                }
                catch
                {
                    stream?.Dispose();
                }
            }
            //else
            //{
            //    //AnimationBehavior.SetSourceUri(element, null);
            //    //element.Source = source;
            //}
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

        }

        public void Dispose()
        {
            this.StopGifAnimation();
            this.Disposables.Dispose();
        }
    }
}
