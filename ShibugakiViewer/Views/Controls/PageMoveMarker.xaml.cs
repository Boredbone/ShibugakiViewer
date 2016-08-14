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

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// PageMoveMarker.xaml の相互作用ロジック
    /// </summary>
    public partial class PageMoveMarker : UserControl
    {

        #region IsPointerMove

        public bool IsPointerMove
        {
            get { return (bool)GetValue(IsPointerMoveProperty); }
            set { SetValue(IsPointerMoveProperty, value); }
        }

        public static readonly DependencyProperty IsPointerMoveProperty =
            DependencyProperty.Register(nameof(IsPointerMove), typeof(bool), typeof(PageMoveMarker),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsPointerMoveChanged)));

        private static void OnIsPointerMoveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PageMoveMarker;
            thisInstance?.CheckColor();
        }

        #endregion



        #region IsPointerEnter

        public bool IsPointerEnter
        {
            get { return (bool)GetValue(IsPointerEnterProperty); }
            set { SetValue(IsPointerEnterProperty, value); }
        }

        public static readonly DependencyProperty IsPointerEnterProperty =
            DependencyProperty.Register(nameof(IsPointerEnter), typeof(bool), typeof(PageMoveMarker),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsPointerEnterChanged)));

        private static void OnIsPointerEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PageMoveMarker;
            thisInstance?.CheckColor();
        }

        #endregion

        #region IsPointerDown

        public bool IsPointerDown
        {
            get { return (bool)GetValue(IsPointerDownProperty); }
            set { SetValue(IsPointerDownProperty, value); }
        }

        public static readonly DependencyProperty IsPointerDownProperty =
            DependencyProperty.Register(nameof(IsPointerDown), typeof(bool), typeof(PageMoveMarker),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsPointerDownChanged)));

        private static void OnIsPointerDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PageMoveMarker;
            thisInstance?.CheckColor();
        }

        #endregion

        #region IsRight

        public bool IsRight
        {
            get { return (bool)GetValue(IsRightProperty); }
            set { SetValue(IsRightProperty, value); }
        }

        public static readonly DependencyProperty IsRightProperty =
            DependencyProperty.Register(nameof(IsRight), typeof(bool), typeof(PageMoveMarker),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsRightChanged)));

        private static void OnIsRightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as PageMoveMarker;
            var value = e.NewValue as bool?;

        }

        #endregion

        private Brush normalBrush;
        private Brush hoverBrush;
        private Brush pressedBrush;


        public PageMoveMarker()
        {
            InitializeComponent();

            this.normalBrush = (Brush)this.FindResource("ButtonBackground");
            this.hoverBrush = (Brush)this.FindResource("ButtonHoverBackground");
            this.pressedBrush = (Brush)this.FindResource("ButtonPressedBackground");
        }

        private void CheckColor()
        {
            if (this.IsPointerEnter || this.IsPointerDown)
            {
                this.border.Visibility = Visibility.Visible;
            }
            else
            {
                this.border.Visibility = Visibility.Collapsed;
            }

            if (this.IsPointerDown || this.IsPointerEnter || this.IsPointerMove)
            {
                if (this.IsRight)
                {
                    this.rightButton.Visibility = Visibility.Visible;
                }
                else
                {
                    this.leftButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                this.rightButton.Visibility = Visibility.Collapsed;
                this.leftButton.Visibility = Visibility.Collapsed;
            }

            var brush = this.IsPointerDown ? this.pressedBrush
                : this.IsPointerEnter ? this.hoverBrush
                : this.normalBrush;

            this.leftButton.Background = brush;
            this.rightButton.Background = brush;


        }
    }
}
