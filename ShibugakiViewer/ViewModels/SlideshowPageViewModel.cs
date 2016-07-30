using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using ImageLibrary.Core;
using ImageLibrary.File;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels.SettingPages;

namespace ShibugakiViewer.ViewModels
{
    public class SlideshowPageViewModel : NotificationBase
    {
        private double FlipTimeMillisec = 3000;
        private const string visualStateNormal = "Normal";
        private const string visualStateAnimating = "Animating";

        public ReactiveProperty<bool> IsResizingAlways { get; }
        public ReactiveProperty<bool> IsResizeToFill { get; }
        public ReactiveProperty<bool> IsRandom { get; }

        public ReactiveProperty<string> VisualState { get; }

        //public event Action<string> GoToState;

        public double FrameWidth { get; set; }
        public double FrameHeight { get; set; }
        


        private Record _fieldCurrentImage;
        public Record CurrentImage
        {
            get { return _fieldCurrentImage; }
            set
            {
                if (_fieldCurrentImage != value)
                {
                    _fieldCurrentImage = value;
                    RaisePropertyChanged(nameof(CurrentImage));
                }
            }
        }

        private Record _fieldNextImage;
        public Record NextImage
        {
            get { return _fieldNextImage; }
            set
            {
                if (_fieldNextImage != value)
                {
                    _fieldNextImage = value;
                    RaisePropertyChanged(nameof(NextImage));
                }
            }
        }


        private KeyTime _fieldInterval;
        public KeyTime Interval
        {
            get { return _fieldInterval; }
            set
            {
                if (_fieldInterval != value)
                {
                    _fieldInterval = value;
                    RaisePropertyChanged(nameof(Interval));
                }
            }
        }

        public ReactiveProperty<bool> IsExecuting { get; }
        

        private IDisposable LoadedObserverDisposer { get; set; }
        private IDisposable timerSubscription;

        private List<int> History { get; }
        public ReactiveProperty<int> HistoryIndex { get; }
        public ReactiveProperty<int> Number { get; }
        public ReactiveProperty<int> Time { get; }
        //private int count;


        public Subject<int> UserOperationSubject { get; }
        
        public ReactiveCommand NextCommand { get; }
        public ReactiveCommand PrevCommand { get; }
        public ReactiveCommand BackCommand { get; }

        //public ReactiveCommand ChangeImageCommand { get; }

        private NumberProvider NumberProvider { get; }

        private double DeviceScale { get; set; }

        public SlideshowSettingPageViewModel SettingViewModel { get; }

        //private double _fieldIconWidth;
        //public double IconWidth
        //{
        //    get { return _fieldIconWidth; }
        //    set
        //    {
        //        if (_fieldIconWidth != value)
        //        {
        //            _fieldIconWidth = value;
        //            RaisePropertyChanged(nameof(IconWidth));
        //        }
        //    }
        //}

        private readonly Client client;
        private readonly Library library;
        private readonly ApplicationCore core;
        private readonly ClientWindowViewModel parent;

        


        public SlideshowPageViewModel(ClientWindowViewModel parent)
        {
            this.parent = parent;
            this.core = parent.Core;
            this.client = parent.Client;
            this.library = parent.Library;
            
            
            this.DeviceScale = 1.0;

            this.SettingViewModel = new SlideshowSettingPageViewModel().AddTo(this.Disposables);

            this.HistoryIndex = new ReactiveProperty<int>(0).AddTo(this.Disposables);

            //this.IconWidth = 68;

            //this.Time = Observable.Interval(TimeSpan.FromMilliseconds(500))
            //    .Select(x => this.count++)
            //    .ObserveOnUIDispatcher()
            //    .ToReactiveProperty().AddTo(this.Disposables);

            this.UserOperationSubject = new Subject<int>().AddTo(this.Disposables);
            
            this.NextCommand = new ReactiveCommand().AddTo(this.Disposables);
            this.PrevCommand = new ReactiveCommand().AddTo(this.Disposables);

            this.VisualState = new ReactiveProperty<string>(visualStateNormal).AddTo(this.Disposables);
            

            this.IsExecuting = new ReactiveProperty<bool>(true).AddTo(this.Disposables);

            this.IsExecuting.Select(x => 0)
                .Merge(this.NextCommand.Select(x => 1))
                .Merge(this.PrevCommand.Select(x => -1))
                .Subscribe(x =>
                {
                    //this.client.ViewerIndex.Value += x;
                    if (this.UserOperationSubject.HasObservers)
                    {
                        this.UserOperationSubject.OnNext(x);
                    }
                }).AddTo(this.Disposables);

            //this.NextCommand.Select(x => 1).Subscribe(x =>
            //{
            //    UserOperationSubject.OnNext(x);
            //}).AddTo(this.Disposables);
            //this.PrevCommand.Select(x => -1).Subscribe(x =>
            //{
            //    UserOperationSubject.OnNext(x);
            //}).AddTo(this.Disposables);

            this.NumberProvider = new NumberProvider(8, (int)client.ViewerIndex.Value) { IsRandom = false, };
            this.Number = this.HistoryIndex.Skip(1).Select(x => this.NumberProvider.GetNumber(x))
                .ToReactiveProperty((int)client.ViewerIndex.Value).AddTo(this.Disposables);

            this.client.Length.Subscribe(x => this.NumberProvider.Length = (int)x).AddTo(this.Disposables);

            this.IsRandom = this.core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowRandom)
                .AddTo(this.Disposables);

            this.IsRandom
                .Subscribe(x => this.NumberProvider.IsRandom = x)
                .AddTo(this.Disposables);

            this.IsResizeToFill = this.core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowResizeToFill)
                .AddTo(this.Disposables);

            this.IsResizingAlways = this.core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowResizingAlways)
                .AddTo(this.Disposables);
            

            this.core.ObserveProperty(x => x.SlideshowAnimationTimeMillisec)
                .Subscribe(x => this.Interval = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(x < 0 ? 0 : x)))
                .AddTo(this.Disposables);

            this.core.ObserveProperty(x => x.SlideshowFlipTimeMillisec)
                .Subscribe(x => this.FlipTimeMillisec = x < 3000 ? 3000.0 : x).AddTo(this.Disposables);


            Disposable.Create(() => this.LoadedObserverDisposer?.Dispose()).AddTo(this.Disposables);
            Disposable.Create(() => this.timerSubscription?.Dispose()).AddTo(this.Disposables);
            //}
            //
            //
            //public void Initialize(string fileId)
            //{
            //    if (fileId != null)
            //    {
            //        var index = this.ImageList.FindIndex(x => x.FileId.Equals(fileId));
            //        if (this.ImageList.ContainsIndex(index))
            //        {
            //            this.HistoryIndex.Value = index;
            //        }
            //    }
            //
            //    this.ReceivedSelectionItem = fileId;

            this.client.ViewerDisplaying
                //.Skip(1)
                .Subscribe(x =>
                {
                    if (x != null)
                    {
                        //this.CurrentImage = x;
                        this.NextImage = x;
                    }
                    //if (this.VisualState.Value == visualStateAnimating)
                    //{
                    //    this.VisualState.Value = visualStateNormal;
                    //}
                    //else
                    //{
                    this.VisualState.Value = visualStateAnimating;
                    //}
                })
                .AddTo(this.Disposables);

            this.Number
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOnUIDispatcher()
                .Subscribe(x =>
                {
                    this.client.ViewerIndex.Value = x;
                    /*
                    if (this.ImageList.ContainsIndex(x))
                    {
                        this.AnimationUri.Value = null;

                        var image = this.ImageList[x];

                        this.LoadedObserverDisposer?.Dispose();
                        this.LoadedObserverDisposer = image.Loaded.Subscribe(y =>
                        {
                            if (y.Status == LoadStatus.NotFound)
                            {
                                this.NextCommand.Execute();
                            }
                            else
                            {
                                //this.AnimationUri.Value = null;
                                //image.SetAnimetion(false);


                                //if (this.IsResizeToFill.Value)
                                //{
                                //    if ((image.OriginalHeight >= this.FrameHeight// * this.DeviceScale
                                //        && image.OriginalWidth >= this.FrameWidth// * this.DeviceScale
                                //        || this.IsResizingAlways.Value))
                                //    {
                                //        image.Stretch = Stretch.UniformToFill;
                                //    }
                                //    else
                                //    {
                                //        image.Stretch = Stretch.None;
                                //    }
                                //}
                                //else
                                //{
                                //    if (image.OriginalHeight >= this.FrameHeight// * this.DeviceScale
                                //        || image.OriginalWidth >= this.FrameWidth// * this.DeviceScale
                                //        || this.IsResizingAlways.Value)
                                //    {
                                //        image.Stretch = Stretch.Uniform;
                                //    }
                                //    else
                                //    {
                                //        image.Stretch = Stretch.None;
                                //    }
                                //}


                                this.NextImage = image;

                                this.GoToState?.Invoke("Animating");
                                
                            }
                        });
                        this.LoadImage(image);
                        
                    }*/
                }).AddTo(this.Disposables);

            //this.ChangeImageCommand = new ReactiveCommand()
            //    .WithSubscribe(_ => this.ChangeImage(), this.Disposables);

            this.BackCommand = client.BackHistoryCount
                .Select(x => x > 0)
                .ToReactiveCommand()
                .WithSubscribe(_ => client.Back(), this.Disposables);

            this.StartMove();
        }

        public void ChangeImage()
        {
            if (this.VisualState.Value == visualStateNormal)
            {
                return;
            }
            var image = this.NextImage;

            //this.AnimationUri.Value = null;
            //image.SetAnimetion(false);

            this.CurrentImage = image;

            if (this.core.IsAnimatedGifEnabled)
            {
                //image.SetAnimetion(true);
                //this.AnimationUri.Value = image.Uri;
            }

            this.VisualState.Value = visualStateNormal;

            this.NextImage = null;
        }


        //private void LoadImage(Record image)
        //{
        //
        //    var mainOption = new ImageLoadingOptions()
        //    {
        //        FrameHeight = 0,
        //        FrameWidth = 0,
        //        Quality = ImageQuality.OriginalSize,
        //        CmsEnable = this.settings.IsCmsEnabled,
        //    };
        //    image.LoadImage(mainOption, true);
        //}

        private void StartMove()
        {
            this.timerSubscription?.Dispose();

            this.timerSubscription = Observable
                .Timer(TimeSpan.FromMilliseconds(FlipTimeMillisec))
                .Where(x => this.IsExecuting.Value)
                .Select(x => 1)
                .Merge(this.UserOperationSubject)
                .Take(1)
                .ObserveOnUIDispatcher()
                .Subscribe(x =>
                {
                    var index = this.HistoryIndex.Value + x;
                    if (index < 0)
                    {
                        index = 0;
                    }
                    this.HistoryIndex.Value = index;
                    StartMove();
                },
                () =>
                {
                    //throw new ArgumentException();
                });

            //this.count = 1;

        }

        //public void Unload()
        //{
        //    //this.AnimationUri.Value = null;
        //    //this.ImageList.ForEach(x => x.SetAnimetion(false));
        //    this.LoadedObserverDisposer?.Dispose();
        //}


        //public void ChangePageSize(double width, double height)
        //{
        //    this.FrameWidth = width;
        //    this.FrameHeight = height;
        //
        //
        //    if (width > 520)
        //    {
        //        this.IconWidth = 68;
        //    }
        //    else
        //    {
        //        this.IconWidth = 48;
        //    }
        //}
    }
}
