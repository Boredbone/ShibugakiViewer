using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShibugakiViewer.Views.Behaviors
{

    public class HoverControlBehavior : Behavior<FrameworkElement>
    {
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(HoverControlBehavior),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));


        public Brush HoverBackground
        {
            get { return (Brush)GetValue(HoverBackgroundProperty); }
            set { SetValue(HoverBackgroundProperty, value); }
        }

        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.Register(nameof(HoverBackground), typeof(Brush), typeof(HoverControlBehavior),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));




        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            var parent = this.AssociatedObject.Parent as Panel;
            if (parent != null)
            {
                parent.Background = this.Background;
            }

            this.AssociatedObject.MouseEnter += this.Panel_PointerEntered;
            this.AssociatedObject.MouseLeave += this.Panel_PointerExited;
            this.AssociatedObject.LostMouseCapture += this.Panel_PointerCaptureLost;


        }


        private void Panel_PointerEntered(object sender, MouseEventArgs e)
        {
            var parent = this.AssociatedObject.Parent as Panel;
            if (parent != null)
            {
                parent.Background = this.HoverBackground;
            }
        }

        private void Panel_PointerExited(object sender, MouseEventArgs e)
        {
            var parent = this.AssociatedObject.Parent as Panel;
            if (parent != null)
            {
                parent.Background = this.Background;
            }
        }

        private void Panel_PointerCaptureLost(object sender, MouseEventArgs e)
        {
            var parent = this.AssociatedObject.Parent as Panel;
            if (parent != null)
            {
                parent.Background = this.Background;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.AssociatedObject.MouseEnter -= this.Panel_PointerEntered;
            this.AssociatedObject.MouseLeave -= this.Panel_PointerExited;
            this.AssociatedObject.LostMouseCapture -= this.Panel_PointerCaptureLost;
        }
        /*
        /// <summary>
        /// デタッチ時の解放処理
        /// </summary>
        protected override void OnDisposing()
        {
            base.OnDisposing();

            this.AssociatedObject.PointerEntered -= this.Panel_PointerEntered;
            this.AssociatedObject.PointerExited -= this.Panel_PointerExited;
            this.AssociatedObject.PointerCaptureLost -= this.Panel_PointerCaptureLost;

        }*/
    }
}
