using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ShibugakiViewer.Views.Behaviors
{
    class PopupMenuBehavior
    {
        public static ContextMenu GetPopupMenu(DependencyObject obj)
        {
            return (ContextMenu)obj.GetValue(PopupMenuProperty);
        }

        public static void SetPopupMenu(DependencyObject obj, ContextMenu value)
        {
            obj.SetValue(PopupMenuProperty, value);
        }

        public static readonly DependencyProperty PopupMenuProperty =
            DependencyProperty.RegisterAttached("PopupMenu", typeof(ContextMenu),
                typeof(PopupMenuBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPopupMenuChanged)));


        private static void OnPopupMenuChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as ButtonBase;
            var menu = e.NewValue as ContextMenu;

            if (element != null && menu != null)
            {
                menu.PreviewMouseUp += (o, ea) =>
                {
                    var focused = FocusManager.GetFocusedElement(menu);
                    if (!(focused is TextBox))
                    {
                        menu.IsOpen = false;
                    }
                };

                element.Click += (o, ea) =>
                {
                    menu.PlacementTarget = element;
                    menu.IsOpen = !menu.IsOpen;
                };
            }
        }
    }
}
