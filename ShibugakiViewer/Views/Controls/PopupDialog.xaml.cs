using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfTools;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// PopupDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class PopupDialog : UserControl
    {
        #region IsOpen

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(PopupDialog),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsOpenChanged)));

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.rootGrid.Visibility = VisibilityHelper.Set(value.Value);
                if (!value.Value)
                {
                    var content = thisInstance.mainContent.Content as FrameworkElement;

                    thisInstance.DialogContent = null;
                    //thisInstance.mainContent.Content = null;

                    if (content != null)
                    {
                        content.IsEnabled = false;
                    }

                    thisInstance.ClosedCommand?.Execute(thisInstance);
                }
            }
        }

        #endregion

        #region HorizontalDialogAlignment

        public HorizontalAlignment HorizontalDialogAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalDialogAlignmentProperty); }
            set { SetValue(HorizontalDialogAlignmentProperty, value); }
        }

        public static readonly DependencyProperty HorizontalDialogAlignmentProperty =
            DependencyProperty.Register(nameof(HorizontalDialogAlignment),
                typeof(HorizontalAlignment), typeof(PopupDialog),
                new PropertyMetadata(HorizontalAlignment.Center,
                new PropertyChangedCallback(OnHorizontalDialogAlignmentChanged)));

        private static void OnHorizontalDialogAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as HorizontalAlignment?;

            if (value.HasValue)
            {
                thisInstance.positionCanvas.HorizontalAlignment = value.Value;
                thisInstance.mainContent.HorizontalAlignment = value.Value;
                thisInstance.DecidePosition();
            }
        }

        #endregion


        #region VerticalDialogAlignment

        public VerticalAlignment VerticalDialogAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalDialogAlignmentProperty); }
            set { SetValue(VerticalDialogAlignmentProperty, value); }
        }

        public static readonly DependencyProperty VerticalDialogAlignmentProperty =
            DependencyProperty.Register(nameof(VerticalDialogAlignment),
                typeof(VerticalAlignment), typeof(PopupDialog),
                new PropertyMetadata(VerticalAlignment.Center,
                new PropertyChangedCallback(OnVerticalDialogAlignmentChanged)));

        private static void OnVerticalDialogAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as VerticalAlignment?;

            if (value.HasValue)
            {
                thisInstance.positionCanvas.VerticalAlignment = value.Value;
                thisInstance.mainContent.VerticalAlignment = value.Value;
                thisInstance.DecidePosition();
            }
        }

        #endregion


        #region Position

        public Thickness Position
        {
            get { return (Thickness)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(nameof(Position), typeof(Thickness), typeof(PopupDialog),
            new PropertyMetadata(default(Thickness), new PropertyChangedCallback(OnPositionChanged)));

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            thisInstance.DecidePosition();
        }

        #endregion

        #region DialogContent

        public FrameworkElement DialogContent
        {
            get { return (FrameworkElement)GetValue(DialogContentProperty); }
            set { SetValue(DialogContentProperty, value); }
        }

        public static readonly DependencyProperty DialogContentProperty =
            DependencyProperty.Register(nameof(DialogContent), typeof(FrameworkElement), typeof(PopupDialog),
            new PropertyMetadata(null, new PropertyChangedCallback(OnDialogContentChanged)));

        private static void OnDialogContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as FrameworkElement;

            if (thisInstance != null)
            {
                if (value != null)
                {
                    value.IsEnabledChanged += (o, ea) =>
                    {
                        if (!(bool)ea.NewValue)
                        {
                            thisInstance.IsOpen = false;
                        }
                    };
                    thisInstance.mainContent.Content = value;
                    thisInstance.DecidePosition();
                }
                else
                {
                    thisInstance.mainContent.Content = null;
                }
            }
        }

        #endregion

        #region DockControl

        public FrameworkElement DockControl
        {
            get { return (FrameworkElement)GetValue(DockControlProperty); }
            set { SetValue(DockControlProperty, value); }
        }

        public static readonly DependencyProperty DockControlProperty =
            DependencyProperty.Register(nameof(DockControl), typeof(FrameworkElement), typeof(PopupDialog),
            new PropertyMetadata(null, new PropertyChangedCallback(OnDockControlChanged)));

        private static void OnDockControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as FrameworkElement;

        }

        #endregion


        #region ClosedCommand

        public ICommand ClosedCommand
        {
            get { return (ICommand)GetValue(ClosedCommandProperty); }
            set { SetValue(ClosedCommandProperty, value); }
        }

        public static readonly DependencyProperty ClosedCommandProperty =
            DependencyProperty.Register(nameof(ClosedCommand), typeof(ICommand), typeof(PopupDialog),
            new PropertyMetadata(null, new PropertyChangedCallback(OnClosedCommandChanged)));

        private static void OnClosedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as ICommand;

        }

        #endregion

        /*
        #region MaskBrush

        public Brush MaskBrush
        {
            get { return (Brush)GetValue(MaskBrushProperty); }
            set { SetValue(MaskBrushProperty, value); }
        }

        public static readonly DependencyProperty MaskBrushProperty =
            DependencyProperty.Register(nameof(MaskBrush), typeof(Brush), typeof(PopupDialog),
            new PropertyMetadata(new SolidColorBrush(Colors.Transparent),
                new PropertyChangedCallback(OnMaskBrushChanged)));

        private static void OnMaskBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as Brush;

            if (thisInstance != null && value != null)
            {
                thisInstance.rootGrid.Background = value;
            }
        }

        #endregion*/

        #region IsMasked

        public bool IsMasked
        {
            get { return (bool)GetValue(IsMaskedProperty); }
            set { SetValue(IsMaskedProperty, value); }
        }

        public static readonly DependencyProperty IsMaskedProperty =
            DependencyProperty.Register(nameof(IsMasked), typeof(bool), typeof(PopupDialog),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsMaskedChanged)));

        private static void OnIsMaskedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value != null)
            {
                thisInstance.SetMask(value.Value);
            }
        }

        #endregion




        /*
        #region IsLightMode

        public bool IsLightMode
        {
            get { return (bool)GetValue(IsLightModeProperty); }
            set { SetValue(IsLightModeProperty, value); }
        }

        public static readonly DependencyProperty IsLightModeProperty =
            DependencyProperty.Register(nameof(IsLightMode), typeof(bool), typeof(PopupDialog),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsLightModeChanged)));

        private static void OnIsLightModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PopupDialog;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.rootGrid.Background = value.Value
                    ? new SolidColorBrush(Colors.Transparent)
                    : new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
            }

        }

        #endregion
        */





        private Brush maskBrush;
        private Brush transparentBrush = new SolidColorBrush(Colors.Transparent);


        public PopupDialog()
        {
            InitializeComponent();
        }


        private void DecidePosition()
        {
            if (this.mainContent.ActualHeight <= 0 || this.mainContent.ActualWidth <= 0
                || !this.IsOpen)
            {
                return;
            }

            //if (this.mainContent.ActualHeight > this.rootGrid.ActualHeight)
            //{
            //    //this.mainContent.Height = this.rootGrid.ActualHeight - 4;
            //}
            //if (this.mainContent.ActualWidth > this.rootGrid.ActualWidth)
            //{
            //    //this.mainContent.Width = this.rootGrid.ActualWidth - 4;
            //}

            if (this.DockControl == null)
            {

                this.positionCanvas.HorizontalAlignment = this.HorizontalDialogAlignment;
                this.positionCanvas.VerticalAlignment = this.VerticalDialogAlignment;

                switch (this.HorizontalDialogAlignment)
                {
                    case HorizontalAlignment.Right:
                        Canvas.SetRight(this.mainContent, this.Position.Right);
                        Canvas.SetLeft(this.mainContent, double.NaN);
                        break;
                    case HorizontalAlignment.Left:
                        Canvas.SetRight(this.mainContent, double.NaN);
                        Canvas.SetLeft(this.mainContent, this.Position.Left);
                        break;
                    case HorizontalAlignment.Stretch:

                        var length = this.rootGrid.ActualWidth;

                        this.mainContent.Width = length - this.Position.Right - this.Position.Left;

                        Canvas.SetLeft(this.mainContent, double.NaN);
                        Canvas.SetRight(this.mainContent, -length / 2 + this.Position.Right);

                        break;
                    case HorizontalAlignment.Center:

                        Canvas.SetRight(this.mainContent, double.NaN);
                        Canvas.SetLeft(this.mainContent, -this.mainContent.ActualWidth / 2.0);
                        break;
                }

                switch (this.VerticalDialogAlignment)
                {
                    case VerticalAlignment.Top:
                        Canvas.SetTop(this.mainContent, this.Position.Top);
                        Canvas.SetBottom(this.mainContent, double.NaN);
                        break;
                    case VerticalAlignment.Bottom:
                        Canvas.SetTop(this.mainContent, double.NaN);
                        Canvas.SetBottom(this.mainContent, this.Position.Bottom);
                        break;
                    case VerticalAlignment.Stretch:

                        var length = this.rootGrid.ActualHeight;

                        this.mainContent.Height = length - this.Position.Top - this.Position.Bottom;

                        Canvas.SetBottom(this.mainContent, double.NaN);
                        Canvas.SetTop(this.mainContent, -length / 2 + this.Position.Top);
                        break;
                    case VerticalAlignment.Center:
                        Canvas.SetBottom(this.mainContent, double.NaN);
                        Canvas.SetTop(this.mainContent, -this.mainContent.ActualHeight / 2.0);
                        break;
                }
            }
            else
            {
                var pt = this.DockControl.PointToScreen(new Point(0.0, 0.0));

                var controlTop = pt.Y;
                var controlLeft = pt.X;
                var controlRight = controlLeft + this.DockControl.ActualWidth;
                var controlBottom = controlTop + this.DockControl.ActualHeight;

                //this.positionCanvas.HorizontalAlignment = HorizontalAlignment.Left;
                //this.positionCanvas.VerticalAlignment = VerticalAlignment.Top;

                Canvas.SetRight(this.mainContent, double.NaN);
                Canvas.SetBottom(this.mainContent, double.NaN);

                var y = 0.0;
                var x = 0.0;

                switch (this.HorizontalDialogAlignment)
                {
                    case HorizontalAlignment.Right:

                        if (!double.IsNaN(this.Position.Left))
                        {
                            x = controlRight + this.Position.Left;
                        }
                        else
                        {
                            x = controlRight - this.mainContent.ActualWidth - this.Position.Right;
                        }

                        break;
                    case HorizontalAlignment.Left:

                        if (!double.IsNaN(this.Position.Right))
                        {
                            x = controlLeft - this.mainContent.ActualWidth - this.Position.Right;
                        }
                        else
                        {
                            x = controlLeft + this.Position.Left;
                        }

                        break;
                    case HorizontalAlignment.Stretch:
                    case HorizontalAlignment.Center:

                        x = controlLeft + this.DockControl.ActualWidth / 2.0 - this.mainContent.ActualWidth / 2.0;
                        break;
                }

                switch (this.VerticalDialogAlignment)
                {
                    case VerticalAlignment.Top:

                        if (!double.IsNaN(this.Position.Bottom))
                        {
                            y = controlTop - this.mainContent.ActualHeight - this.Position.Bottom;
                        }
                        else
                        {
                            y = controlTop + this.Position.Top;
                        }

                        break;
                    case VerticalAlignment.Bottom:

                        if (!double.IsNaN(this.Position.Top))
                        {
                            y = controlBottom + this.Position.Top;
                        }
                        else
                        {
                            y = controlBottom - this.mainContent.ActualHeight - this.Position.Bottom;
                        }

                        break;
                    case VerticalAlignment.Stretch:
                    case VerticalAlignment.Center:
                        y = controlTop + this.DockControl.ActualHeight / 2.0 - this.mainContent.ActualHeight / 2.0;
                        break;
                }

                var canvasPosition = this.positionCanvas.PointToScreen(new Point(0.0, 0.0));

                var screenPosition = this.rootGrid.PointToScreen(new Point(0.0, 0.0));

                if (x + this.mainContent.ActualWidth > screenPosition.X + this.rootGrid.ActualWidth)
                {
                    x = screenPosition.X + this.rootGrid.ActualWidth - this.mainContent.ActualWidth;
                }
                if (x < screenPosition.X)
                {
                    x = screenPosition.X;
                }

                if (y + this.mainContent.ActualHeight > screenPosition.Y + this.rootGrid.ActualHeight)
                {
                    y = screenPosition.Y + this.rootGrid.ActualHeight - this.mainContent.ActualHeight;
                }
                if (y < screenPosition.Y)
                {
                    y = screenPosition.Y;
                }


                //var left = x - canvasPosition.X;
                //var top = y - canvasPosition.Y;



                Canvas.SetLeft(this.mainContent, Math.Round(x - canvasPosition.X));
                Canvas.SetTop(this.mainContent, Math.Round(y - canvasPosition.Y));


            }
        }

        private void rootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DecidePosition();
        }

        private void mainContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DecidePosition();
        }

        private void rootGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.IsOpen = false;
            e.Handled = true;
        }

        private void mainContent_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void SetMask(bool isEnabled)
        {
            if (this.maskBrush == null)
            {
                var color = Application.Current.Resources["PopupMaskColor"];
                this.maskBrush = color as Brush;
            }

            this.rootGrid.Background = (isEnabled && this.maskBrush != null)
                ? this.maskBrush : this.transparentBrush;
        }


        public void Show
            (FrameworkElement content, Thickness position,
            HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
            FrameworkElement dock = null, bool isMaskVisible = true)
        {
            this.IsMasked = isMaskVisible;
            this.DockControl = dock;
            this.HorizontalDialogAlignment = horizontalAlignment;
            this.VerticalDialogAlignment = verticalAlignment;
            this.Position = position;
            this.DialogContent = content;
            this.IsOpen = true;
        }
    }

    public interface IPopupDialogOwner
    {
        PopupDialog PopupDialog { get; }

        //void OpenPopup(FrameworkElement content, Thickness position,
        //    HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
        //    FrameworkElement dock = null, bool isMaskVisible = true);
        //
        //void ClosePopup();
    }
}
