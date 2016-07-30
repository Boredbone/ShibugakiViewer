using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ShibugakiViewer.Views.Behaviors
{

    public class FileDropAttachedBehavior
    {

        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }
        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(FileDropAttachedBehavior),
            new PropertyMetadata(null, OnCommandChanged));



        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element == null)
            {
                return;
            }

            var cmd = GetCommand(element);
            if (cmd != null)
            {
                element.AllowDrop = true;
                element.PreviewDragOver += element_PreviewDragOver;
                element.Drop += element_Drop;
            }
            else
            {
                element.AllowDrop = false;
                element.PreviewDragOver -= element_PreviewDragOver;
                element.Drop -= element_Drop;
            }
        }

        static void element_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        static void element_Drop(object sender, DragEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }

            var obj = e.Data.GetData(DataFormats.FileDrop);
            var cmd = GetCommand(element);

            if (obj != null && cmd.CanExecute(null))
            {
                cmd.Execute(obj);
            }
        }
    }
}
