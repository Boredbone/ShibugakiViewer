using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShibugakiViewer.Views.Behaviors
{
    public class FullScreenBehavior
    {
        #region IsFullScreen

        public static bool GetIsFullScreen(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFullScreenProperty);
        }

        public static void SetIsFullScreen(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFullScreenProperty, value);
        }

        public static readonly DependencyProperty IsFullScreenProperty =
            DependencyProperty.RegisterAttached("IsFullScreen",
                typeof(bool), typeof(FullScreenBehavior),
                new PropertyMetadata(false, new PropertyChangedCallback(OnIsFullScreenChanged)));

        private static void OnIsFullScreenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as Window;
            var value = e.NewValue as bool?;

            if (element != null && value.HasValue)
            {
                if (value.Value)
                {
                    element.WindowStyle = WindowStyle.None;
                    element.Topmost = true;
                    element.WindowState = WindowState.Maximized;
                    //element.Activate();
                }
                else
                {
                    element.WindowState = WindowState.Normal;
                    element.Topmost = false;
                    element.WindowStyle = WindowStyle.SingleBorderWindow;
                }
            }
        }

        #endregion


    }
}
