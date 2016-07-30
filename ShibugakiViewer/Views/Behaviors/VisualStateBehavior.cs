using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShibugakiViewer.Views.Behaviors
{
    class VisualStateBehavior
    {
        #region State

        public static string GetState(DependencyObject obj)
        {
            return (string)obj.GetValue(StateProperty);
        }

        public static void SetState(DependencyObject obj, string value)
        {
            obj.SetValue(StateProperty, value);
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached("State",
                typeof(string), typeof(VisualStateBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnStateChanged)));

        private static void OnStateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var value = e.NewValue as string;

            if (element != null)
            {
                VisualStateManager.GoToState(element, value, (e.OldValue != null));
            }
        }

        #endregion


    }
}
