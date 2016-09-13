using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShibugakiViewer.Views.Behaviors
{
    class FocusBehavior
    {
        #region IsFocused

        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused",
                typeof(bool), typeof(FocusBehavior),
                new PropertyMetadata(false, new PropertyChangedCallback(OnIsFocusedChanged)));

        private static void OnIsFocusedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var value = e.NewValue as bool?;

            if (element != null && value != null)
            {
                if (value.Value)
                {
                    element.Focus();
                }
            }
        }

        #endregion




    }
}
