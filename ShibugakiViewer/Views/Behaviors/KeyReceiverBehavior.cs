using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models.Utility;

namespace ShibugakiViewer.Views.Behaviors
{
    public class KeyReceiverBehavior
    {

        public static CompositeDisposable GetDisposer(DependencyObject obj)
        {
            return (CompositeDisposable)obj.GetValue(DisposerProperty);
        }

        public static void SetDisposer(DependencyObject obj, CompositeDisposable value)
        {
            obj.SetValue(DisposerProperty, value);
        }
        
        public static readonly DependencyProperty DisposerProperty =
            DependencyProperty.RegisterAttached("Disposer", typeof(CompositeDisposable),
                typeof(KeyReceiverBehavior), new PropertyMetadata(null));




        public static KeyReceiver<object> GetReceiver(DependencyObject obj)
        {
            return (KeyReceiver<object>)obj.GetValue(ReceiverProperty);
        }

        public static void SetReceiver(DependencyObject obj, KeyReceiver<object> value)
        {
            obj.SetValue(ReceiverProperty, value);
        }
        
        public static readonly DependencyProperty ReceiverProperty =
            DependencyProperty.RegisterAttached("Receiver", typeof(KeyReceiver<object>),
                typeof(KeyReceiverBehavior), new PropertyMetadata(null,OnReceiverChanged));


        private static void OnReceiverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element == null)
            {
                return;
            }

            var disposables = GetDisposer(d);
            if (disposables == null)
            {
                disposables = new CompositeDisposable();
                SetDisposer(d, disposables);
            }
            disposables.Clear();

            var receiver = e.NewValue as KeyReceiver<object>;
            if (receiver == null)
            {
                return;
            }

            Observable.FromEvent<KeyEventHandler, KeyEventArgs>
                (h => (sender, ea) => h(ea), h => element.KeyDown += h, h => element.KeyDown -= h)
                .Subscribe(x =>
                {
                    var focused = FocusManager.GetFocusedElement(d);
                    if (receiver.Check(d, x.Key, x.SystemKey, focused, false))
                    {
                        x.Handled = true;
                    }
                })
                .AddTo(disposables);

            Observable.FromEvent<KeyEventHandler, KeyEventArgs>
                (h => (sender, ea) => h(ea), h => element.PreviewKeyDown += h, h => element.PreviewKeyDown -= h)
                .Subscribe(x =>
                {
                    var focused = FocusManager.GetFocusedElement(d);
                    if (receiver.Check(d, x.Key, x.SystemKey, focused, true))
                    {
                        x.Handled = true;
                    }
                })
                .AddTo(disposables);
        }
        
    }
}
