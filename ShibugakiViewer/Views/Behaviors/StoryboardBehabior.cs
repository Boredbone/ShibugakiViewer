using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ShibugakiViewer.Views.Behaviors
{
    class StoryboardBehabior
    {

        #region CompletedCommand

        public static ICommand GetCompletedCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CompletedCommandProperty);
        }

        public static void SetCompletedCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CompletedCommandProperty, value);
        }

        public static readonly DependencyProperty CompletedCommandProperty =
            DependencyProperty.RegisterAttached("CompletedCommand",
                typeof(ICommand), typeof(StoryboardBehabior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnCompletedCommandChanged)));

        private static void OnCompletedCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as Storyboard;
            var value = e.NewValue as ICommand;

            if (element != null && value != null)
            {
                element.Completed += (o, ea) => value.Execute(ea);
            }
        }

        #endregion



    }
}
