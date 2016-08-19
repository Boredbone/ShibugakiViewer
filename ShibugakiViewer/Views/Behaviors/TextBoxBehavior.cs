using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShibugakiViewer.Views.Behaviors
{
    class TextBoxBehavior
    {
        public static bool GetUpdateByReturn(DependencyObject obj)
        {
            return (bool)obj.GetValue(UpdateByReturnProperty);
        }

        public static void SetUpdateByReturn(DependencyObject obj, bool value)
        {
            obj.SetValue(UpdateByReturnProperty, value);
        }
        
        public static readonly DependencyProperty UpdateByReturnProperty =
            DependencyProperty.RegisterAttached("UpdateByReturn", typeof(bool),
                typeof(TextBoxBehavior), new PropertyMetadata(false, OnUpdateByReturnChanged));

        private static void OnUpdateByReturnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && (bool)e.NewValue)
            {
                textBox.LostFocus += (o, ea) =>
                {
                    textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                };

                textBox.PreviewKeyDown += (o, ea) =>
                {
                    if (ea.Key == Key.Enter)
                    {
                        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                    }
                };
            }
        }
    }
}
