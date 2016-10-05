using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Boredbone.XamlTools.Extensions;
using Reactive.Bindings.Extensions;
using WpfTools;

namespace ShibugakiViewer.Views.Behaviors
{
    /// <summary>
    /// プレースホルダーを表示する添付ビヘイビア
    /// http://tnakamura.hatenablog.com/entry/20100929/textbox_placeholder
    /// </summary>
    public static class PlaceHolderBehavior
    {
        // プレースホルダーとして表示するテキスト
        public static readonly DependencyProperty PlaceHolderTextProperty = DependencyProperty.RegisterAttached(
            "PlaceHolderText",
            typeof(string),
            typeof(PlaceHolderBehavior),
            new PropertyMetadata(null, OnPlaceHolderChanged));

        private static void OnPlaceHolderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var placeHolder = e.NewValue as string;

                SubscribeEvents(textBox, x =>
                {
                    textBox.Background = x
                        ? (Brush)CreateVisualBrush(placeHolder)
                        : new SolidColorBrush(Colors.Transparent);
                });

                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Background = CreateVisualBrush(placeHolder);
                }
            }

        }

        private static void SubscribeEvents(TextBox textBox, Action<bool> action)
        {

            var disposables = new CompositeDisposable();

            var textChanged = Observable.FromEvent<TextChangedEventHandler, TextChangedEventArgs>
                (h => (s, ea) => h(ea),
                h => textBox.TextChanged += h,
                h => textBox.TextChanged -= h);

            var gotFocus = Observable.FromEvent<RoutedEventHandler, RoutedEventArgs>
                (h => (s, ea) => h(ea),
                h => textBox.GotFocus += h,
                h => textBox.GotFocus -= h);

            var lostFocus = Observable.FromEvent<RoutedEventHandler, RoutedEventArgs>
                (h => (s, ea) => h(ea),
                h => textBox.LostFocus += h,
                h => textBox.LostFocus -= h);

            textChanged.Select(_ => Unit.Default)
                .Merge(lostFocus.Select(_ => Unit.Default))
                .Select(_ => string.IsNullOrEmpty(textBox.Text))
                .Merge(gotFocus.Select(_ => false))
                .Subscribe(action)
                .AddTo(disposables);

            textBox.UnloadedAsObservable()
                .Subscribe(_ => disposables?.Dispose())
                .AddTo(disposables);
        }


        private static TextChangedEventHandler CreateTextChangedEventHandler(string placeHolder)
        {
            // TextChanged イベントをハンドルし、TextBox が未入力のときだけ
            // プレースホルダーを表示するようにする。
            return (sender, e) =>
            {
                // 背景に TextBlock を描画する VisualBrush を使って
                // プレースホルダーを実現
                var textBox = (TextBox)sender;
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Background = CreateVisualBrush(placeHolder);
                }
                else
                {
                    textBox.Background = new SolidColorBrush(Colors.Transparent);
                }
            };
        }



        private static VisualBrush CreateVisualBrush(string placeHolder)
        {
            var visual = new Label()
            {
                Content = placeHolder,
                Padding = new Thickness(5, 1, 1, 1),
                Foreground = new SolidColorBrush(Colors.Gray),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            return new VisualBrush(visual)
            {
                Stretch = Stretch.None,
                TileMode = TileMode.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Center,
            };
        }

        public static void SetPlaceHolderText(DependencyObject textBox, string placeHolder)
        {
            textBox.SetValue(PlaceHolderTextProperty, placeHolder);
        }

        public static string GetPlaceHolderText(DependencyObject textBox)
        {
            return textBox.GetValue(PlaceHolderTextProperty) as string;
        }




        public static FrameworkElement GetPlaceHolderControl(DependencyObject obj)
        {
            return (FrameworkElement)obj.GetValue(PlaceHolderControlProperty);
        }

        public static void SetPlaceHolderControl(DependencyObject obj, FrameworkElement value)
        {
            obj.SetValue(PlaceHolderControlProperty, value);
        }

        public static readonly DependencyProperty PlaceHolderControlProperty =
            DependencyProperty.RegisterAttached("PlaceHolderControl", typeof(FrameworkElement),
                typeof(PlaceHolderBehavior), new PropertyMetadata(null, OnPlaceHolderControlChanged));

        private static void OnPlaceHolderControlChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var control = e.NewValue as FrameworkElement;

                SubscribeEvents(textBox, x =>
                {
                    control.Visibility = VisibilityHelper.Set(x);
                });

                control.Visibility = VisibilityHelper.Set(string.IsNullOrEmpty(textBox.Text));

            }

        }


    }

    public static class ComboBoxPlaceHolderBehavior
    {

        public static FrameworkElement GetPlaceHolderControl(DependencyObject obj)
        {
            return (FrameworkElement)obj.GetValue(PlaceHolderControlProperty);
        }

        public static void SetPlaceHolderControl(DependencyObject obj, FrameworkElement value)
        {
            obj.SetValue(PlaceHolderControlProperty, value);
        }

        public static readonly DependencyProperty PlaceHolderControlProperty =
            DependencyProperty.RegisterAttached("PlaceHolderControl", typeof(FrameworkElement),
                typeof(ComboBoxPlaceHolderBehavior), new PropertyMetadata(null, OnPlaceHolderControlChanged));

        private static void OnPlaceHolderControlChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                var control = e.NewValue as FrameworkElement;

                var disposables = new CompositeDisposable();

                var selectionChanged = Observable.FromEvent<SelectionChangedEventHandler, SelectionChangedEventArgs>
                    (h => (s, ea) => h(ea),
                    h => comboBox.SelectionChanged += h,
                    h => comboBox.SelectionChanged -= h);

                selectionChanged
                    .Select(x => x.AddedItems.Count <= 0 || string.IsNullOrWhiteSpace(x.AddedItems[0].ToString()))
                    .Subscribe(x =>
                    {
                        control.Visibility = VisibilityHelper.Set(x);
                    })
                    .AddTo(disposables);

                comboBox.UnloadedAsObservable()
                    .Subscribe(_ => disposables?.Dispose())
                    .AddTo(disposables);

                control.Visibility = VisibilityHelper.Set(string.IsNullOrWhiteSpace(comboBox.SelectionBoxItem.ToString()));

            }
        }
    }
}
