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
    class ManipulationCommandBehavior
    {
        #region MouseDown
        public static ICommand GetMouseDownCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseDownCommandProperty);
        }

        public static void SetMouseDownCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseDownCommandProperty, value);
        }

        public static readonly DependencyProperty MouseDownCommandProperty =
            DependencyProperty.RegisterAttached("MouseDownCommand", typeof(ICommand),
                typeof(ManipulationCommandBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnMouseDownCommandChanged)));


        private static void OnMouseDownCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var command = e.NewValue as ICommand;

            if (element != null && command != null)
            {
                element.MouseDown += (o, ea) =>
                {
                    ea.Handled = true;
                    command.Execute(element.DataContext);
                };
            }
        }

        #endregion

        #region MouseUp

        public static ICommand GetMouseUpCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseUpCommandProperty);
        }

        public static void SetMouseUpCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseUpCommandProperty, value);
        }

        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand),
                typeof(ManipulationCommandBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnMouseUpCommandChanged)));


        private static void OnMouseUpCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var command = e.NewValue as ICommand;

            if (element != null && command != null)
            {
                element.MouseUp += (o, ea) =>
                {
                    ea.Handled = true;
                    command.Execute(element.DataContext);
                };
            }
        }

        #endregion

        #region MouseRightButtonUp

        public static ICommand GetMouseRightButtonUpCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseRightButtonUpCommandProperty);
        }

        public static void SetMouseRightButtonUpCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseRightButtonUpCommandProperty, value);
        }
        public static readonly DependencyProperty MouseRightButtonUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseRightButtonUpCommand", typeof(ICommand),
                typeof(ManipulationCommandBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnMouseRightButtonUpCommandChanged)));


        private static void OnMouseRightButtonUpCommandChanged
            (DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var command = e.NewValue as ICommand;

            if (element != null && command != null)
            {
                element.MouseRightButtonUp += (o, ea) =>
                {
                    ea.Handled = true;
                    command.Execute(element.DataContext);
                };
            }
        }

        #endregion


        #region MouseMoveAction

        public static Action<object, MouseEventArgs> GetMouseMoveAction(DependencyObject obj)
        {
            return (Action<object, MouseEventArgs>)obj.GetValue(MouseMoveActionProperty);
        }

        public static void SetMouseMoveAction(DependencyObject obj, Action<object, MouseEventArgs> value)
        {
            obj.SetValue(MouseMoveActionProperty, value);
        }

        public static readonly DependencyProperty MouseMoveActionProperty =
            DependencyProperty.RegisterAttached("MouseMoveAction",
                typeof(Action<object, MouseEventArgs>), typeof(ManipulationCommandBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnMouseMoveActionChanged)));

        private static void OnMouseMoveActionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var value = e.NewValue as Action<object, MouseEventArgs>;

            if (element != null && value != null)
            {
                element.MouseMove += (o, ea) => value?.Invoke(o, ea);
            }
        }

        #endregion

        #region WheelChangedAction

        public static Action<object, MouseWheelEventArgs> GetWheelChangedAction(DependencyObject obj)
        {
            return (Action<object, MouseWheelEventArgs>)obj.GetValue(WheelChangedActionProperty);
        }

        public static void SetWheelChangedAction(DependencyObject obj, Action<object, MouseWheelEventArgs> value)
        {
            obj.SetValue(WheelChangedActionProperty, value);
        }

        public static readonly DependencyProperty WheelChangedActionProperty =
            DependencyProperty.RegisterAttached("WheelChangedAction",
                typeof(Action<object, MouseWheelEventArgs>), typeof(ManipulationCommandBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnWheelChangedActionChanged)));

        private static void OnWheelChangedActionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var value = e.NewValue as Action<object, MouseWheelEventArgs>;

            if (element != null && value != null)
            {
                element.MouseWheel += (o, ea) => value?.Invoke(o, ea);
            }
        }

        #endregion




    }
}
