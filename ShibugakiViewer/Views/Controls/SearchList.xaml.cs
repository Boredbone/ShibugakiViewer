using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// SearchList.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchList : UserControl
    {

        public static ICommand GetStartCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(StartCommandProperty);
        }

        public static void SetStartCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(StartCommandProperty, value);
        }
        
        public static readonly DependencyProperty StartCommandProperty =
            DependencyProperty.RegisterAttached("StartCommand", typeof(ICommand), 
                typeof(SearchList), new PropertyMetadata(null));


        public static ICommand GetSelectCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(SelectCommandProperty);
        }

        public static void SetSelectCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(SelectCommandProperty, value);
        }
        
        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.RegisterAttached("SelectCommand", typeof(ICommand), 
                typeof(SearchList), new PropertyMetadata(null));


        public static IEnumerable GetItemsSource(DependencyObject obj)
        {
            return (IEnumerable)obj.GetValue(ItemsSourceProperty);
        }

        public static void SetItemsSource(DependencyObject obj, IEnumerable value)
        {
            obj.SetValue(ItemsSourceProperty, value);
        }
        
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable),
                typeof(SearchList), new PropertyMetadata(null));


        public static Visibility GetNameVisibility(DependencyObject obj)
        {
            return (Visibility)obj.GetValue(NameVisibilityProperty);
        }

        public static void SetNameVisibility(DependencyObject obj, Visibility value)
        {
            obj.SetValue(NameVisibilityProperty, value);
        }
        
        public static readonly DependencyProperty NameVisibilityProperty =
            DependencyProperty.RegisterAttached("NameVisibility", typeof(Visibility), 
                typeof(SearchList), new PropertyMetadata(Visibility.Collapsed));




        public SearchList()
        {
            InitializeComponent();
        }
    }
}
