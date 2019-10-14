using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ImageLibrary.File;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;
using Boredbone.Utility.Extensions;
using System.Reactive.Linq;
using Reactive.Bindings;
using WpfTools;
using System.Diagnostics;
using System.Reactive;
using ShibugakiViewer.Views.Behaviors;
using System.Reactive.Subjects;
using Boredbone.XamlTools;
using System.IO;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// ScrollImageViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class ScrollImageViewer : UserControl, IDisposable
    {
        private CompositeDisposable dispsables = new CompositeDisposable();

        private const double normalZoomTime = 150.0;
        private const double autoZoomTime = 200.0;
        private const double stepZoomTime = 80.0;

        private const double scrollDelta = 50.0;

        private const double scrollAnimationTime = 150;
        private const double rotateAnimationTime = 400;

        private int tapCount = 0;
        private Vector firstTapPosition;

        private const double tapMoveLengthSquaredThreshold = 64 * 64;//[px*px]
        private const double shortTapTimeThreshold = 500;//[ms]
        private const double tapDifferentPositionLengthSquaredThreshold = 100 * 100;//[px*px]

        private Lazy<double> doubleTapTimeThreshold = new Lazy<double>(() =>
        {
            return System.Windows.Forms.SystemInformation.DoubleClickTime;
            //return 400;//[ms]
        });
        private const double edgeTapThreshold = 0.2;

        private const double maxZoomFactor = 8.0;


        #region AutoFit

        public bool AutoFit
        {
            get { return (bool)GetValue(AutoFitProperty); }
            set { SetValue(AutoFitProperty, value); }
        }
        public static readonly DependencyProperty AutoFitProperty =
            DependencyProperty.Register("AutoFit", typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(true, new PropertyChangedCallback(OnAutoFitChanged)));

        private static void OnAutoFitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = (bool)e.NewValue;

            if (value)
            {
                if (thisInstance.ScrollViewer.ScrollableHeight > 0 && thisInstance.ScrollViewer.ScrollableWidth > 0)
                {
                    var ho = thisInstance.scrollViewer.HorizontalOffset;
                    var vo = thisInstance.scrollViewer.VerticalOffset;
                    thisInstance.ChangeView(ho, vo, null);
                }
            }
        }

        #endregion

        #region Source

        public Record Source
        {
            get { return (Record)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Record), typeof(ScrollImageViewer),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as Record;
            var old = e.OldValue as Record;

            thisInstance.OnRecordChanged(value, old);
        }

        private void OnRecordChanged(Record record, Record oldRecord)
        {
            this.IsChanging = true;

            var oldPath = oldRecord?.FullPath;

            this.image.DataContext = record;

            if (!this.isImageLoaded)
            {
                this.ZoomFactor = this.originalScale;
            }

            if (record == null)
            {
                this.ImageHeight = 0;
                this.ImageWidth = 0;
            }
            else
            {
                this.ImageHeight = record.Height;
                this.ImageWidth = record.Width;
            }

            var newPath = record?.FullPath;
            if (newPath != oldPath)
            {
                int orientation = 0;
                bool horizontalMirror = false;
                bool verticalMirror = false;

                if (!this.isExifOrientationDisabled && record != null && newPath != null)
                {
                    var ext = record.Extension.ToLower();
                    if (ext == ".jpg" || ext == ".jpeg")
                    {
                        try
                        {
                            int num;
                            {
                                using var fs = new FileStream(newPath, FileMode.Open, FileAccess.Read);
                                num = ImageLibrary.Exif.ExifManager.GetOrientation(fs);
                                //System.Diagnostics.Debug.WriteLine($"exif orientation {num}");
                            }
                            switch (num)
                            {
                                case 2:
                                    horizontalMirror = true;
                                    break;
                                case 3:
                                    orientation = 180;
                                    break;
                                case 4:
                                    verticalMirror = true;
                                    break;
                                case 5:
                                    orientation = 90;
                                    verticalMirror = true;
                                    break;
                                case 6:
                                    orientation = 90;
                                    break;
                                case 7:
                                    orientation = 270;
                                    verticalMirror = true;
                                    break;
                                case 8:
                                    orientation = 270;
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }
                    }
                }
                this.rotateTransform.Angle = orientation;
                this.Orientation = orientation;
                this.CurrentOrientation = orientation;

                this.IsInHorizontalMirror = horizontalMirror;
                this.IsInVerticalMirror = verticalMirror;
                
                if (this.isImageLoaded)
                {
                    this.DoAutoScaling();
                }
                this.scaleInitializeFlag = true;
            }
            else
            {
                this.IsChanging = false;
            }
        }

        #endregion


        #region ViewWidth

        public double ViewWidth
        {
            get { return (double)GetValue(ViewWidthProperty); }
            set { SetValue(ViewWidthProperty, value); }
        }

        public static readonly DependencyProperty ViewWidthProperty =
            DependencyProperty.Register(nameof(ViewWidth), typeof(double),
                typeof(ScrollImageViewer), new PropertyMetadata(0.0));

        #endregion

        #region ViewHeight

        public double ViewHeight
        {
            get { return (double)GetValue(ViewHeightProperty); }
            set { SetValue(ViewHeightProperty, value); }
        }

        public static readonly DependencyProperty ViewHeightProperty =
            DependencyProperty.Register(nameof(ViewHeight), typeof(double),
                typeof(ScrollImageViewer), new PropertyMetadata(0.0));

        #endregion




        public double ImageHeight
        {
            get { return this.Source?.Height ?? 0; }
            set
            {
                if (_fieldImageHeight != value)
                {
                    _fieldImageHeight = value;
                    RaisePropertyChanged(nameof(ImageHeight));
                }
            }
        }
        private double _fieldImageHeight;


        public double ImageWidth
        {
            get { return this.Source?.Width ?? 0; }
            set
            {
                if (_fieldImageWidth != value)
                {
                    _fieldImageWidth = value;
                    RaisePropertyChanged(nameof(ImageWidth));
                }
            }
        }
        private double _fieldImageWidth;




        #region ZoomFactor

        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(ScrollImageViewer),
            new PropertyMetadata(1.0, new PropertyChangedCallback(OnZoomFactorChanged)));

        private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as double?;

            if (value == null)
            {
                return;
            }

            if (value.Value == 0)
            {
                Debug.WriteLine("Zero Zoom");
            }

            thisInstance.ActualZoomFactor = value.Value / thisInstance.originalScale;

            thisInstance.RefreshScale();
            if (!thisInstance.dispsables.IsDisposed)
            {
                thisInstance.MetaImageZoomFactorSubject.OnNext(value.Value);
            }

        }

        #endregion


        #region AnimatedZoomFactor

        public double AnimatedZoomFactor
        {
            get { return (double)GetValue(AnimatedZoomFactorProperty); }
            set { SetValue(AnimatedZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty AnimatedZoomFactorProperty =
            DependencyProperty.Register(nameof(AnimatedZoomFactor), typeof(double), typeof(ScrollImageViewer),
            new PropertyMetadata(1.0, new PropertyChangedCallback(OnAnimatedZoomFactorChanged)));

        private static void OnAnimatedZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as double?;

            if (value == null)
            {
                return;
            }
            var zoom = value.Value;
            thisInstance.OnAnimatedZoomFactorChanged(zoom);
        }

        private void OnAnimatedZoomFactorChanged(double zoom)
        {
            var zoomRate = zoom / this.baseZoomFactor;

            var offset = zoomRate * this.baseOffset + (zoomRate - 1.0) * this.baseCenter;

            this.ZoomFactor = zoom;

            this.CurrentOffset = (Point)offset;

            this.scrollViewer.ScrollToHorizontalOffset(offset.X);
            this.scrollViewer.ScrollToVerticalOffset(offset.Y);
        }


        #endregion

        #region DesiredZoomFactor

        public double DesiredZoomFactor
        {
            get { return (double)GetValue(DesiredZoomFactorProperty); }
            set { SetValue(DesiredZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty DesiredZoomFactorProperty =
            DependencyProperty.Register(nameof(DesiredZoomFactor), typeof(double), typeof(ScrollImageViewer),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnDesiredZoomFactorChanged)));

        private static void OnDesiredZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as double?;

            if (value == null || value.Value <= 0)
            {
                return;
            }
            var zoom = value.Value * thisInstance.originalScale;

            var p = thisInstance.GetCenter();
            /*
            var x = 0.0;
            if (thisInstance.scrollViewer.ActualWidth.IsValid())
            {
                x = thisInstance.scrollViewer.ActualWidth / 2.0;
            }
            var y = 0.0;
            if (thisInstance.scrollViewer.ActualHeight.IsValid())
            {
                y = thisInstance.scrollViewer.ActualHeight / 2.0;
            }*/

            thisInstance.ZoomImage(p.X, p.Y, zoom, stepZoomTime, false, false);
        }

        #endregion

        #region ActualZoomFactor

        public double ActualZoomFactor
        {
            get { return (double)GetValue(ActualZoomFactorProperty); }
            set { SetValue(ActualZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty ActualZoomFactorProperty =
            DependencyProperty.Register(nameof(ActualZoomFactor), typeof(double),
                typeof(ScrollImageViewer), new PropertyMetadata(1.0));

        #endregion



        #region MetaImageZoomFactorDp

        public double MetaImageZoomFactorDp
        {
            get { return (double)GetValue(MetaImageZoomFactorDpProperty); }
            set { SetValue(MetaImageZoomFactorDpProperty, value); }
        }

        public static readonly DependencyProperty MetaImageZoomFactorDpProperty =
            DependencyProperty.Register(nameof(MetaImageZoomFactorDp), typeof(double),
                typeof(ScrollImageViewer), new PropertyMetadata(1.0));

        #endregion




        #region CurrentOffset

        public Point CurrentOffset
        {
            get { return new Point(this.scrollViewer.HorizontalOffset, this.scrollViewer.VerticalOffset); }
            // (Point)GetValue(CurrentOffsetProperty); }
            set { SetValue(CurrentOffsetProperty, value); }
        }

        public static readonly DependencyProperty CurrentOffsetProperty =
            DependencyProperty.Register(nameof(CurrentOffset), typeof(Point), typeof(ScrollImageViewer),
            new PropertyMetadata(default(Point), new PropertyChangedCallback(OnCurrentOffsetChanged)));

        private static void OnCurrentOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var point = e.NewValue as Point?;

            if (point == null || thisInstance.scrollViewer == null)
            {
                return;
            }

            thisInstance.scrollViewer.ScrollToHorizontalOffset(point.Value.X);
            thisInstance.scrollViewer.ScrollToVerticalOffset(point.Value.Y);

        }

        #endregion


        #region ActualOffset

        public Point ActualOffset
        {
            get { return (Point)GetValue(ActualOffsetProperty); }
            set { SetValue(ActualOffsetProperty, value); }
        }

        public static readonly DependencyProperty ActualOffsetProperty =
            DependencyProperty.Register(nameof(ActualOffset), typeof(Point), typeof(ScrollImageViewer),
            new PropertyMetadata(default(Point)));

        #endregion

        #region CheckHorizontalScrollRequestFunction

        public Func<int> CheckHorizontalScrollRequestFunction
        {
            get { return (Func<int>)GetValue(CheckHorizontalScrollRequestFunctionProperty); }
            set { SetValue(CheckHorizontalScrollRequestFunctionProperty, value); }
        }

        public static readonly DependencyProperty CheckHorizontalScrollRequestFunctionProperty =
            DependencyProperty.Register(nameof(CheckHorizontalScrollRequestFunction),
                typeof(Func<int>), typeof(ScrollImageViewer),
            new PropertyMetadata(null, new PropertyChangedCallback(OnCheckHorizontalScrollRequestFunctionChanged)));

        private static void OnCheckHorizontalScrollRequestFunctionChanged
            (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as Func<int>;

        }

        #endregion

        #region CheckVerticalScrollRequestFunction

        public Func<int> CheckVerticalScrollRequestFunction
        {
            get { return (Func<int>)GetValue(CheckVerticalScrollRequestFunctionProperty); }
            set { SetValue(CheckVerticalScrollRequestFunctionProperty, value); }
        }

        public static readonly DependencyProperty CheckVerticalScrollRequestFunctionProperty =
            DependencyProperty.Register(nameof(CheckVerticalScrollRequestFunction),
                typeof(Func<int>), typeof(ScrollImageViewer),
            new PropertyMetadata(null, new PropertyChangedCallback(OnCheckVerticalScrollRequestFunctionChanged)));

        private static void OnCheckVerticalScrollRequestFunctionChanged
            (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as Func<int>;

        }

        #endregion

        #region IsScrollRequested

        public bool IsScrollRequested
        {
            get { return (bool)GetValue(IsScrollRequestedProperty); }
            set { SetValue(IsScrollRequestedProperty, value); }
        }

        public static readonly DependencyProperty IsScrollRequestedProperty =
            DependencyProperty.Register(nameof(IsScrollRequested), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsScrollRequestedChanged)));

        private static void OnIsScrollRequestedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue && value.Value)
            {
                thisInstance.IsScrollRequested = false;
                thisInstance.StartScrollAnimation();
            }
        }

        #endregion






        #region ImageLoadingTrigger

        public bool ImageLoadingTrigger
        {
            get { return (bool)GetValue(ImageLoadingTriggerProperty); }
            set { SetValue(ImageLoadingTriggerProperty, value); }
        }

        public static readonly DependencyProperty ImageLoadingTriggerProperty =
            DependencyProperty.Register(nameof(ImageLoadingTrigger), typeof(bool),
                typeof(ScrollImageViewer), new PropertyMetadata(false));


        #endregion


        #region ScaleToPhysicalPixel

        public bool ScaleToPhysicalPixel
        {
            get { return (bool)GetValue(ScaleToPhysicalPixelProperty); }
            set { SetValue(ScaleToPhysicalPixelProperty, value); }
        }

        public static readonly DependencyProperty ScaleToPhysicalPixelProperty =
            DependencyProperty.Register(nameof(ScaleToPhysicalPixel), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(true, new PropertyChangedCallback(OnScaleToPhysicalPixelChanged)));

        private static void OnScaleToPhysicalPixelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.RefreshDeviceScale();
            }
        }

        #endregion



        #region IsInHorizontalMirror

        public bool IsInHorizontalMirror
        {
            get { return (bool)GetValue(IsInHorizontalMirrorProperty); }
            set { SetValue(IsInHorizontalMirrorProperty, value); }
        }

        public static readonly DependencyProperty IsInHorizontalMirrorProperty =
            DependencyProperty.Register(nameof(IsInHorizontalMirror), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsInHorizontalMirrorChanged)));

        private static void OnIsInHorizontalMirrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            thisInstance.RefreshScale();
        }

        #endregion


        #region IsInVerticalMirror

        public bool IsInVerticalMirror
        {
            get { return (bool)GetValue(IsInVerticalMirrorProperty); }
            set { SetValue(IsInVerticalMirrorProperty, value); }
        }

        public static readonly DependencyProperty IsInVerticalMirrorProperty =
            DependencyProperty.Register(nameof(IsInVerticalMirror), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsInVerticalMirrorChanged)));

        private static void OnIsInVerticalMirrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            thisInstance.RefreshScale();
        }

        #endregion

        #region IsAutoScalingEnabled

        public bool IsAutoScalingEnabled
        {
            get { return (bool)GetValue(IsAutoScalingEnabledProperty); }
            set { SetValue(IsAutoScalingEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAutoScalingEnabledProperty =
            DependencyProperty.Register(nameof(IsAutoScalingEnabled), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsAutoScalingEnabledChanged)));

        private static void OnIsAutoScalingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue)
            {
                var p = thisInstance.GetCenter();
                thisInstance.StartAutoScaling(p.X, p.Y);
            }
        }

        #endregion


        #region ScrollBarVisibility

        public Visibility ScrollBarVisibility
        {
            get { return (Visibility)GetValue(ScrollBarVisibilityProperty); }
            set { SetValue(ScrollBarVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(ScrollBarVisibility), typeof(Visibility),
                typeof(ScrollImageViewer), new PropertyMetadata(Visibility.Visible));

        #endregion


        #region IsPointerMoving

        public bool IsPointerMoving
        {
            get { return (bool)GetValue(IsPointerMovingProperty); }
            set { SetValue(IsPointerMovingProperty, value); }
        }

        public static readonly DependencyProperty IsPointerMovingProperty =
            DependencyProperty.Register(nameof(IsPointerMoving), typeof(bool),
                typeof(ScrollImageViewer), new PropertyMetadata(false));

        #endregion





        #region TapCommand

        public ICommand TapCommand
        {
            get { return (ICommand)GetValue(TapCommandProperty); }
            set { SetValue(TapCommandProperty, value); }
        }

        public static readonly DependencyProperty TapCommandProperty =
            DependencyProperty.Register(nameof(TapCommand), typeof(ICommand),
                typeof(ScrollImageViewer), new PropertyMetadata(null));

        #endregion

        #region PointerMoveCommand

        public ICommand PointerMoveCommand
        {
            get { return (ICommand)GetValue(PointerMoveCommandProperty); }
            set { SetValue(PointerMoveCommandProperty, value); }
        }

        public static readonly DependencyProperty PointerMoveCommandProperty =
            DependencyProperty.Register(nameof(PointerMoveCommand), typeof(ICommand),
                typeof(ScrollImageViewer), new PropertyMetadata(null));

        #endregion



        #region Orientation

        public int Orientation
        {
            get { return (int)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(int), typeof(ScrollImageViewer),
            new PropertyMetadata(0, new PropertyChangedCallback(OnOrientationChanged)));

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as int?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.Rotate(value.Value);
            }
        }

        #endregion

        #region CurrentOrientation

        public double CurrentOrientation
        {
            get { return (double)GetValue(CurrentOrientationProperty); }
            set { SetValue(CurrentOrientationProperty, value); }
        }

        public static readonly DependencyProperty CurrentOrientationProperty =
            DependencyProperty.Register(nameof(CurrentOrientation), typeof(double), typeof(ScrollImageViewer),
            new PropertyMetadata(0.0, new PropertyChangedCallback(OnCurrentOrientationChanged)));

        private static void OnCurrentOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as double?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.rotateTransform.Angle = value.Value;
            }
        }

        #endregion

        #region IsChanging

        public bool IsChanging
        {
            get { return (bool)GetValue(IsChangingProperty); }
            set { SetValue(IsChangingProperty, value); }
        }

        public static readonly DependencyProperty IsChangingProperty =
            DependencyProperty.Register(nameof(IsChanging), typeof(bool),
                typeof(ScrollImageViewer), new PropertyMetadata(false));

        #endregion

        #region IsGifAnimationEnabled

        public bool IsGifAnimationEnabled
        {
            get { return (bool)GetValue(IsGifAnimationEnabledProperty); }
            set { SetValue(IsGifAnimationEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsGifAnimationEnabledProperty =
            DependencyProperty.Register(nameof(IsGifAnimationEnabled), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(true, new PropertyChangedCallback(OnIsGifAnimationEnabledChanged)));

        private static void OnIsGifAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value != null)
            {
                thisInstance.gifBehabior.IsGifAnimationEnabled = value.Value;
            }

        }

        #endregion

        #region IsExifOrientationDisabled

        public bool IsExifOrientationDisabled
        {
            get { return (bool)GetValue(IsExifOrientationDisabledProperty); }
            set { SetValue(IsExifOrientationDisabledProperty, value); }
        }

        public static readonly DependencyProperty IsExifOrientationDisabledProperty =
            DependencyProperty.Register(nameof(IsExifOrientationDisabled), typeof(bool), typeof(ScrollImageViewer),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsExifOrientationDisabledChanged)));

        private static void OnIsExifOrientationDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value != null)
            {
                thisInstance.isExifOrientationDisabled = value.Value;
            }

        }

        #endregion

        #region IsFill

        public bool IsFill
        {
            get { return (bool)GetValue(IsFillProperty); }
            set { SetValue(IsFillProperty, value); }
        }

        public static readonly DependencyProperty IsFillProperty =
            DependencyProperty.Register(nameof(IsFill), typeof(bool),
                typeof(ScrollImageViewer), new PropertyMetadata(false));

        #endregion

        #region IsZoomoutOnly

        public bool IsZoomoutOnly
        {
            get { return (bool)GetValue(IsZoomoutOnlyProperty); }
            set { SetValue(IsZoomoutOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsZoomoutOnlyProperty =
            DependencyProperty.Register(nameof(IsZoomoutOnly), typeof(bool),
                typeof(ScrollImageViewer), new PropertyMetadata(true));

        #endregion

        #region ScalingMode

        public int ScalingMode
        {
            get { return (int)GetValue(ScalingModeProperty); }
            set { SetValue(ScalingModeProperty, value); }
        }

        public static readonly DependencyProperty ScalingModeProperty =
            DependencyProperty.Register(nameof(ScalingMode), typeof(int), typeof(ScrollImageViewer),
            new PropertyMetadata(0, new PropertyChangedCallback(OnScalingModeChanged)));

        private static void OnScalingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as ScrollImageViewer;
            var value = e.NewValue as int?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.SetScalingMode(value.Value);
            }
        }

        #endregion





        public event Action<object, PointerTapEventArgs> PanelRightTapped;


        public ScrollViewer ScrollViewer { get { return this.scrollViewer; } }
        public Image Image { get { return this.image; } }

        private bool IsTurned => this.Orientation % 180 != 0;

        private Vector baseOffset;
        private Vector baseCenter;
        private double baseZoomFactor;

        private bool isImageLoaded = false;
        private bool scaleInitializeFlag = false;
        private bool isScrollAnimating = false;
        private bool ignoreNextScaleChange = false;
        private bool isWindowInitialized = false;
        private bool isExifOrientationDisabled = false;

        private Subject<double> MetaImageZoomFactorSubject { get; }
        public ReadOnlyReactiveProperty<double> MetaImageZoomFactor { get; }

        private double dpiScale = 1.0;
        private double originalScale = 1.0;


        public ScrollImageViewer()
        {
            InitializeComponent();

            this.Source = Record.Empty;

            this.inertiaBehavior.AddTo(this.dispsables);
            this.tapBehavior.AddTo(this.dispsables);
            this.imageBehabior.AddTo(this.dispsables);
            this.gifBehabior.AddTo(this.dispsables);

            this.SetScalingMode(0);

            var buffer = ((App)Application.Current).Core.ImageBuffer;
            buffer.Updated
                .ObserveOnUIDispatcher()
                .Where(x => x.Equals(this.Source?.FullPath))
                .Subscribe(x =>
                {
                    this.ImageLoadingTrigger = !this.ImageLoadingTrigger;
                })
                .AddTo(this.dispsables);

            var scrollChanged = Observable.FromEvent<ScrollChangedEventHandler, ScrollChangedEventArgs>
                (h => (sender, e) => h(e),
                h => this.scrollViewer.ScrollChanged += h,
                h => this.scrollViewer.ScrollChanged -= h)
                .Where(x =>
                {
                    if (this.ignoreNextScaleChange)
                    {
                        this.ignoreNextScaleChange = false;
                        return false;
                    }
                    return true;
                })
                .Select(_ => Unit.Default);

            var mouseMoving = Observable.FromEvent<MouseEventHandler, MouseEventArgs>
                (h => (sender, e) => h(e),
                h => this.scrollViewer.MouseMove += h,
                h => this.scrollViewer.MouseMove -= h)
                .Select(_ => Unit.Default);

            var scrollBarEvent = scrollChanged
                .Merge(mouseMoving)
                .Publish().RefCount();

            scrollBarEvent.Throttle(TimeSpan.FromMilliseconds(1000)).Select(_ => false)
                .Merge(scrollBarEvent.Select(_ => true))
                .ObserveOnUIDispatcher()
                .Subscribe(x =>
                {
                    this.IsPointerMoving = x;
                    this.ScrollBarVisibility = VisibilityHelper.Set(x);
                })
                .AddTo(this.dispsables);

            this.MetaImageZoomFactorSubject = new Subject<double>().AddTo(this.dispsables);

            this.MetaImageZoomFactorSubject
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOnUIDispatcher()
                .Subscribe(x => this.MetaImageZoomFactorDp = x)
                .AddTo(this.dispsables);

        }


        private void SetScalingMode(int mode)
        {
            //RenderOptions.SetBitmapScalingMode(this.image, BitmapScalingMode.Unspecified);//default
            if (mode == 1)
            {
                RenderOptions.SetBitmapScalingMode(this.image, BitmapScalingMode.Linear);
            }
            else
            {
                RenderOptions.SetBitmapScalingMode(this.image, BitmapScalingMode.Fant);
            }
        }

        /// <summary>
        /// ダブルクリックでアニメーション付き自動スケーリング
        /// </summary>
        /// <param name="e"></param>
        private void OnDoubleTapped(PointerTapEventArgs e)
        {
            if (this.scrollViewer != null)
            {
                Point p = e.GetPosition(this.scrollViewer);
                this.StartAutoScaling(p.X, p.Y);
            }
        }




        /// <summary>
        /// マウスボタンが離された時
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void tapBehavior_PointerTapped(object o, PointerTapEventArgs e)
        {

            var length = (e.EndPosition - e.StartPosition).LengthSquared;
            if (length < tapMoveLengthSquaredThreshold)
            {
                var h = e.EndPosition.X / e.SenderWidth;
                var v = e.EndPosition.Y / e.SenderHeight;

                if (e.Span < TimeSpan.FromMilliseconds(shortTapTimeThreshold))
                {
                    //ダウン中に動いてない && 長押しでない

                    if (e.Interval > TimeSpan.FromMilliseconds(doubleTapTimeThreshold.Value)
                        || (this.firstTapPosition - e.EndPosition).LengthSquared
                            > tapDifferentPositionLengthSquaredThreshold)
                    {
                        //前のタップから時間が開いた || タップ位置が離れた
                        this.tapCount = 1;
                        this.firstTapPosition = e.EndPosition;
                    }
                    else
                    {
                        this.tapCount++;
                    }

                    if (this.tapCount == 2 && h > edgeTapThreshold && h < (1.0 - edgeTapThreshold))
                    {
                        this.OnDoubleTapped(e);
                    }

                }

                this.TapCommand?.Execute(new ViewerTapEventArgs()
                {
                    VerticalRate = v,
                    HolizontalRate = h,
                    Count = this.tapCount,
                    Span = e.Span,
                    Interval = e.Interval,
                });
            }
        }


        /// <summary>
        /// 右クリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scrollViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
            => this.PanelRightTapped?.Invoke(sender, new PointerTapEventArgs(e));


        /// <summary>
        /// 画像サイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0.0 && e.NewSize.Height > 0.0
                && this.AutoFit
                && (this.scaleInitializeFlag || e.PreviousSize.Height <= 0 || e.PreviousSize.Width < 0))
            {
                this.IsChanging = true;
                this.isImageLoaded = true;
                this.DoAutoScaling();

                this.scaleInitializeFlag = false;
                this.IsChanging = false;
            }
        }

        /// <summary>
        /// 画面に合わせて画像をスケーリング
        /// </summary>
        /// <returns></returns>
        private bool DoAutoScaling()
        {
            this.ignoreNextScaleChange = true;

            var zoomed = FitImageToScrollView(true);
            //新しい画像に移ったときはやる、画質が変化しただけの時はやらない

            if (this.scaleInitializeFlag && !zoomed)
            {
                this.ZoomImage(null, null, this.originalScale, 0.0, false, this.IsFill);
            }

            this.scaleInitializeFlag = false;
            this.IsChanging = false;
            return zoomed;
        }

        /// <summary>
        /// マウスホイールで拡大率変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var elm = this.ScrollViewer;

            if (elm != null)
            {
                double newvalue = e.Delta;

                var scale = this.ZoomFactor;


                if (newvalue > 0)
                {
                    scale *= 1.2f;
                }
                if (newvalue < 0)
                {
                    scale /= 1.2f;
                }

                var p = e.GetPosition(elm);

                this.ZoomImage(p.X, p.Y, scale, stepZoomTime, false, false);

                e.Handled = true;
            }
        }


        private double GetFitScale()
        {
            return GetFitOrFillScale(false);
        }

        private double GetFillScale()
        {
            return GetFitOrFillScale(true);
        }

        private double GetFitOrFillScale(bool fill)
        {
            var view = this.ScrollViewer;
            var image = this.Image;

            var originalWidth = image.ActualWidth;
            var originalHeight = image.ActualHeight;
            //DesiredSizeだとワンテンポ遅れるのでNG

            var width = originalWidth <= 0 ? this.ImageWidth : originalWidth;
            var height = originalHeight <= 0 ? this.ImageHeight : originalHeight;

            if (this.IsTurned)
            {
                var buf = width;
                width = height;
                height = buf;
            }

            var horizontalRate = (this.ViewWidth - 1.0) / width;// view.ViewportWidth / width;
            var verticalRate = (this.ViewHeight - 1.0) / height;// view.ViewportHeight / height;

            if (fill)
            {
                return Math.Max(horizontalRate, verticalRate);
            }
            else
            {
                return Math.Min(horizontalRate, verticalRate);
            }
        }

        public void ZoomImage(double? centerX, double? centerY, double scale,
            double timeMilliSeconds, bool exponential, bool fixCenter)
        {
            if (scale < float.Epsilon)
            {
                scale = float.Epsilon;
            }
            if (scale > maxZoomFactor)
            {
                scale = maxZoomFactor;
            }

            var oldScale = this.ZoomFactor;

            if (this.HasAnimatedProperties)
            {
                var zoom = this.AnimatedZoomFactor;
                this.BeginAnimation(AnimatedZoomFactorProperty, null);
                this.AnimatedZoomFactor = zoom;
            }

            var view = this.ScrollViewer;
            var image = this.Image;

            double frameWidth = this.ViewWidth;// view.ViewportWidth;
            double frameHeight = this.ViewHeight;// view.ViewportHeight;

            double imageWidth = image.ActualWidth * this.ZoomFactor;
            double imageHeight = image.ActualHeight * this.ZoomFactor;

            
            if (this.IsTurned)
            {
                var tmp = imageWidth;
                imageWidth = imageHeight;
                imageHeight = tmp;
            }

            var zoomRate = scale / oldScale;


            var size = GetOriginalImageSize();


            var newImageWidth = size.Width * scale;
            var newImageHeight = size.Height * scale;

            double oldHorizontalOffset = view.HorizontalOffset;
            double oldVerticalOffset = view.VerticalOffset;


            if (imageWidth < frameWidth)
            {
                oldHorizontalOffset -= (frameWidth - imageWidth) / 2.0;
            }
            if (imageHeight < frameHeight)
            {
                oldVerticalOffset -= (frameHeight - imageHeight) / 2.0;
            }


            double horizontalOffset;
            double verticalOffset;

            double cX = (double)(centerX ?? (frameWidth / 2));
            double cY = (double)(centerY ?? (frameHeight / 2));
            //double cX = (double)(centerX ?? (size.Width / 2));
            //double cY = (double)(centerY ?? (size.Height / 2));


            //horizontalOffset = (2 - zoomRate) * oldHorizontalOffset - (1 - zoomRate) * cX;
            //verticalOffset = (2 - zoomRate) * oldVerticalOffset - (1 - zoomRate) * cY;

            horizontalOffset = zoomRate * oldHorizontalOffset + (zoomRate - 1) * cX;
            verticalOffset = zoomRate * oldVerticalOffset + (zoomRate - 1) * cY;


            if (fixCenter && this.Source != null)
            {
                imageWidth = this.Source?.Width * scale ?? 0.0;
                imageHeight = this.Source?.Height * scale ?? 0.0;

                horizontalOffset = -(frameWidth - imageWidth) / 2.0;
                verticalOffset = -(frameHeight - imageHeight) / 2.0;
            }


            this.baseCenter = new Vector(cX, cY);
            this.baseOffset = new Vector(oldHorizontalOffset, oldVerticalOffset);
            this.baseZoomFactor = oldScale;


            if (newImageWidth < frameWidth)
            {
                horizontalOffset = (frameWidth - newImageWidth) / 2;
            }
            else if (horizontalOffset < 0)
            {
                horizontalOffset = 0;
            }
            else if (horizontalOffset > newImageWidth - frameWidth)
            {
                horizontalOffset = (newImageWidth - frameWidth) / 2;
            }


            if (newImageHeight < frameHeight)
            {
                verticalOffset = (frameHeight - newImageHeight) / 2;
            }
            else if (verticalOffset < 0)
            {
                verticalOffset = 0;
            }
            else if (verticalOffset > newImageHeight - frameHeight)
            {
                verticalOffset = (newImageHeight - frameHeight) / 2;
            }


            //アニメーション無効化時
            if (timeMilliSeconds < 1)
            {
                this.ChangeView(Math.Round(horizontalOffset), Math.Round(verticalOffset), scale);
                return;
            }

            //アニメーションの設定

            if (oldScale <= 0)
            {
                oldScale = 0.01;
            }

            var zoomAnimation = new DoubleAnimation()
            {
                From = oldScale,
                To = scale,
                Duration = new Duration(TimeSpan.FromMilliseconds(timeMilliSeconds)),
            };

            zoomAnimation.Completed += (o, e) =>
            {
                this.AnimatedZoomFactor = scale;
            };


            this.BeginAnimation(AnimatedZoomFactorProperty, zoomAnimation);

        }


        private Size GetOriginalImageSize()
        {
            var image = this.Image;

            if (image == null)
            {
                return new Size(1, 1);
            }

            var width = this.ImageWidth;
            var height = this.ImageHeight;

            if (height < 1 || width < 1)
            {
                width = image.ActualWidth;
                height = image.ActualHeight;
            }

            if (this.IsTurned)
            {
                var buf = width;
                width = height;
                height = buf;
            }

            return new Size(width, height);
        }

        public bool FitImageToScrollView(bool disableAnimation)
        {
            if (!this.isImageLoaded)
            {
                return false;
            }
            var view = this.ScrollViewer;
            var image = this.Image;


            if (view != null)
            {
                var size = GetOriginalImageSize();
                if (size.Width > 0 && size.Height > 0)
                {
                    var oldScale = this.ZoomFactor;

                    var newScale = GetFitOrFillScale(this.IsFill);
                    var originalScale = this.originalScale;


                    if (this.IsZoomoutOnly && newScale > originalScale)
                    {
                        if (oldScale > 0.99 * originalScale && oldScale < 1.01 * originalScale)
                        {
                            return false;
                        }
                        newScale = originalScale;
                    }

                    var rate = oldScale / newScale;

                    this.ZoomImage(null, null, newScale, disableAnimation ? 0.0 : normalZoomTime, true, true);
                    return true;
                }
            }
            return false;
        }


        public void StartAutoScaling(double? x, double? y)
        {
            var view = this.ScrollViewer;
            var image = this.Image;
            var currentScale = this.ZoomFactor;
            var originalScale = this.originalScale;

            var newScale = originalScale;

            double th = 1.01;

            if (image != null)
            {
                var fitScale = GetFitScale();
                var fillScale = GetFillScale();

                if (fitScale > originalScale)//画面より小さい画像
                {
                    if (currentScale > fitScale * th)//画面からはみ出ている
                    {
                        newScale = fitScale;
                    }
                    else if (currentScale > originalScale * th)//Math.Sqrt(fitScale))//少し拡大されている
                    {
                        newScale = originalScale;
                    }
                    else if (currentScale > originalScale / th)//ほぼ原寸
                    {
                        newScale = fillScale;// fitScale;
                    }
                    else//縮小されている
                    {
                        newScale = originalScale;
                    }
                }
                else//画面より大きい画像
                {
                    if (fitScale / fillScale < th && fillScale / fitScale < th)//FillとFitがほぼ同じ
                    {
                        if (currentScale > originalScale * th)//拡大されている
                        {
                            newScale = originalScale;
                        }
                        else if (currentScale > fitScale * th)//Math.Sqrt(fitScale))//画面からはみ出ている
                        {
                            newScale = fillScale;
                        }
                        else if (currentScale > fitScale / th)//ほぼ画面サイズ
                        {
                            newScale = originalScale;
                        }
                        else//画面より小さい
                        {
                            newScale = fitScale;
                        }
                    }
                    else if (fillScale < originalScale)//縦も横も画面より大きい
                    {
                        if (currentScale > originalScale * th)//拡大されている
                        {
                            newScale = fillScale;
                        }
                        else if (currentScale > originalScale / th)//ほぼ原寸
                        {
                            newScale = fitScale;
                        }
                        else if (currentScale > fillScale / th)//Fill近く
                        {
                            newScale = fitScale;
                        }
                        else if (currentScale > fitScale / th)//ほぼ画面サイズ
                        {
                            newScale = originalScale;
                        }
                        else//画面より小さい
                        {
                            newScale = fitScale;
                        }
                    }
                    else//Fitより大きいがFillより小さい
                    {

                        if (currentScale > fillScale * th)//Fillより大きい
                        {
                            newScale = fillScale;
                        }
                        else if (currentScale > fillScale / th)//Fill近い
                        {
                            newScale = fitScale;
                        }
                        else if (currentScale > originalScale * th)//拡大されている
                        {
                            newScale = fillScale;
                        }
                        else if (currentScale > fitScale * th)//Fitより大きい
                        {
                            newScale = fitScale;//fillScale;// 
                        }
                        else if (currentScale > fitScale / th)//ほぼFit
                        {
                            newScale = originalScale;
                        }
                        else//画面より小さい
                        {
                            newScale = fitScale;
                        }
                    }
                }
                ZoomImage(x, y, newScale, autoZoomTime, true, false);
            }
        }




        private void scrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var oldSize = e.PreviousSize;
            var newSize = e.NewSize;

            this.ViewHeight = newSize.Height;
            this.ViewWidth = newSize.Width;

            if (oldSize.Width <= 0 || oldSize.Height <= 0)
            {
                this.FitImageToScrollView(true);
            }

            var sv = (ScrollViewer)sender;

            var newHorizontalOffset = sv.HorizontalOffset + oldSize.Width - newSize.Width;
            var newVerticalOffset = sv.VerticalOffset + oldSize.Height - newSize.Height;

            this.ChangeView(newHorizontalOffset, newVerticalOffset, null);
        }


        public void Raise()
        {
            RaisePropertyChanged("FixedHeight");
            RaisePropertyChanged("FixedWidth");
        }


        private void Rotate(int desiredAngle)
        {
            var currentAngle = this.rotateTransform.Angle;

            while (currentAngle < 0)
            {
                currentAngle += 360.0;
            }
            while (desiredAngle < 0)
            {
                desiredAngle += 360;
            }

            currentAngle = currentAngle % 360;
            desiredAngle = desiredAngle % 360;

            if (desiredAngle - currentAngle > 180)
            {
                desiredAngle -= 360;
            }
            if (desiredAngle - currentAngle < -180)
            {
                desiredAngle += 360;
            }

            this.rotateTransform.Angle = currentAngle;
            this.Orientation = desiredAngle;

            var animation = new DoubleAnimation()
            {
                From = currentAngle,
                To = this.Orientation,
                Duration = new Duration(TimeSpan.FromMilliseconds(rotateAnimationTime)),
            };

            animation.Completed += (o, e) =>
            {
                this.rotateTransform.Angle = this.Orientation;
            };


            this.BeginAnimation(CurrentOrientationProperty, animation);

        }

        private void ChangeSize(double width, double height)
        {
            var zoom = this.ZoomFactor;

            var oldHo = this.ScrollViewer.HorizontalOffset;
            var newHo = oldHo + zoom * (width - imageGrid.ActualWidth) / 2.0;

            var oldVo = this.ScrollViewer.VerticalOffset;
            var newVo = oldVo + zoom * (height - imageGrid.ActualHeight) / 2.0;

            this.ChangeView(newHo, newVo, null);
        }


        public void ChangeView(double? horizontalOffset, double? verticalOffset, double? zoomFactor)
        {
            if (horizontalOffset != null && horizontalOffset.Value.IsValid())
            {
                this.scrollViewer.ScrollToHorizontalOffset(horizontalOffset.Value);
            }
            if (verticalOffset != null && verticalOffset.Value.IsValid())
            {
                this.scrollViewer.ScrollToVerticalOffset(verticalOffset.Value);
            }

            if (zoomFactor != null && zoomFactor.Value.IsValid())
            {
                this.ZoomFactor = zoomFactor.Value;
            }


        }

        private void StartScrollAnimation()
        {
            if (this.isScrollAnimating)
            {
                return;
            }

            var xDelta = this.CheckHorizontalScrollRequestFunction();
            var yDelta = this.CheckVerticalScrollRequestFunction();

            if (xDelta == 0 && yDelta == 0)
            {
                return;
            }


            this.isScrollAnimating = true;

            var x = this.ActualOffset.X + xDelta * scrollDelta;
            var y = this.ActualOffset.Y + yDelta * scrollDelta;


            var animation = new PointAnimation()
            {
                From = this.ActualOffset,
                To = new Point(Math.Round(x), Math.Round(y)),
                Duration = new Duration(TimeSpan.FromMilliseconds(scrollAnimationTime)),
            };

            animation.Completed += (o, e) =>
            {
                this.IsScrollRequested = false;
                this.isScrollAnimating = false;
                this.StartScrollAnimation();
            };


            this.BeginAnimation(CurrentOffsetProperty, animation);
        }


        private void RefreshScale()
        {
            var zoom = this.ZoomFactor;
            if (zoom <= 0)
            {
                return;
            }

            this.scaleTransform.ScaleX = (this.IsInHorizontalMirror) ? -zoom : zoom;
            this.scaleTransform.ScaleY = (this.IsInVerticalMirror) ? -zoom : zoom;

        }

        private Point GetCenter()
        {
            var x = 0.0;
            if (this.scrollViewer.ActualWidth.IsValid())
            {
                x = this.scrollViewer.ActualWidth / 2.0;
            }
            var y = 0.0;
            if (this.scrollViewer.ActualHeight.IsValid())
            {
                y = this.scrollViewer.ActualHeight / 2.0;
            }
            return new Point(x, y);
        }

        private void scrollImageViewer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Orientation = 0;
            this.rotateTransform.Angle = 0;

            this.ZoomFactor = this.originalScale;
        }



        private void scrollImageViewer_Unloaded(object sender, RoutedEventArgs e)
        {
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            this.DataContext = null;
            this.dispsables.Dispose();
        }

        private void ImageBehavior_SourceChanged(OldNewPair<string> obj)
        {
            if (obj.OldItem == obj.NewItem && obj.NewItem != null)
            {
                this.scaleInitializeFlag = false;
            }
        }

        private void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.ActualOffset = new Point(e.HorizontalOffset, e.VerticalOffset);
        }

        private void scrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition((UIElement)sender);
            this.PointerMoveCommand?.Execute(p);
        }

        private void scrollViewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var scale = Math.Max(e.DeltaManipulation.Scale.X, e.DeltaManipulation.Scale.Y);

            if (scale != 0 && scale != 1)
            {
                this.ZoomImage(null, null, this.ZoomFactor * scale, 0, false, false);
            }
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var scale = Math.Max(e.DeltaManipulation.Scale.X, e.DeltaManipulation.Scale.Y);

            if (scale != 0)
            {
                this.ZoomImage(null, null, this.ZoomFactor * scale, 0, false, false);
            }

        }

        private void RefreshDeviceScale()
        {
            this.originalScale = (this.ScaleToPhysicalPixel && this.dpiScale > 0) ? 1.0 / this.dpiScale : 1.0;
        }

        private void scrollImageViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.isWindowInitialized)
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DpiChanged += (o, ea) =>
                    {
                        var scale = Math.Max(ea.NewDpi.DpiScaleX, ea.NewDpi.DpiScaleY);
                        if (scale != this.dpiScale)
                        {
                            this.dpiScale = scale;
                            this.RefreshDeviceScale();
                        }
                    };
                    this.isWindowInitialized = true;
                }
            }
        }
    }


    public class ViewerTapEventArgs : EventArgs
    {
        public double HolizontalRate { get; set; }
        public double VerticalRate { get; set; }
        public int Count { get; set; }
        public TimeSpan Span { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
