using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Media.Imaging;
using ShibugakiViewer.Models.Utility;
using System.Windows.Interop;

namespace ShibugakiViewer.Views.Behaviors
{
    class IconToImageSourceBehavior : Behavior<Image>
    {
        #region Source

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(IconToImageSourceBehavior),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as IconToImageSourceBehavior;
            thisInstance?.SetIcon(true);
        }

        #endregion

        #region Size

        public int Size
        {
            get { return (int)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(int), typeof(IconToImageSourceBehavior),
            new PropertyMetadata(0, new PropertyChangedCallback(OnSizeChanged)));

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as IconToImageSourceBehavior;
            thisInstance?.SetIcon(true);
        }

        #endregion

        private bool initialized = false;

        private void SetIcon(bool force)
        {
            var uri = this.Source;
            var size = this.Size;

            if (uri == null || size <= 0 || this.AssociatedObject == null
                || (!force && this.initialized))
            {
                return;
            }

            var icon = IconHelper.CreateIcon(uri, size);

            if (icon != null)
            {
                this.AssociatedObject.Source = Imaging.CreateBitmapSourceFromHIcon
                    (icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                this.initialized = true;
            }
        }


        protected override void OnAttached()
        {
            base.OnAttached();
            this.SetIcon(false);
        }
    }
}
