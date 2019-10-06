using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Views.Behaviors
{

    public class HoverGridBehavior : Behavior<Panel>, IDisposable
    {
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(HoverGridBehavior),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));


        public Brush HoverBackground
        {
            get { return (Brush)GetValue(HoverBackgroundProperty); }
            set { SetValue(HoverBackgroundProperty, value); }
        }

        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.Register(nameof(HoverBackground), typeof(Brush), typeof(HoverGridBehavior),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));



        private CompositeDisposable disposables = new CompositeDisposable();


        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Background = this.Background;

            Observable.FromEvent<MouseEventHandler, MouseEventArgs>
                (h => (sender, e) => h(e),
                h => this.AssociatedObject.MouseEnter += h,
                h => this.AssociatedObject.MouseEnter -= h)
                .Subscribe(_ => this.AssociatedObject.Background = this.HoverBackground)
                .AddTo(this.disposables);



            Observable.Merge(
                Observable.FromEvent<MouseEventHandler, MouseEventArgs>
                    (h => (sender, e) => h(e),
                    h => this.AssociatedObject.MouseLeave += h,
                    h => this.AssociatedObject.MouseLeave -= h),
                Observable.FromEvent<MouseEventHandler, MouseEventArgs>
                    (h => (sender, e) => h(e),
                    h => this.AssociatedObject.LostMouseCapture += h,
                    h => this.AssociatedObject.LostMouseCapture -= h))
                .Subscribe(_ => this.AssociatedObject.Background = this.Background)
                .AddTo(this.disposables);

        }


        public void Dispose()
        {
            this.disposables.Dispose();
        }
    }
}
