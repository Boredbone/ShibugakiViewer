using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Boredbone.Utility.Extensions;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// SortEditor.xaml の相互作用ロジック
    /// </summary>
    public partial class SortEditor : UserControl
    {

        #region ItemsSource

        public IEnumerable<SortSetting> ItemsSource
        {
            get { return (IEnumerable<SortSetting>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<SortSetting>), typeof(SortEditor),
            new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as SortEditor;
            var value = e.NewValue as IEnumerable<SortSetting>;

            if (thisInstance != null && value != null)
            {
                thisInstance.SortSettings.Clear();
                value.ForEach(x => thisInstance.SortSettings.Add(x));
            }

        }

        #endregion


        public ObservableCollection<SortSetting> SortSettings { get; }

        public static List<FileProperty> PropertyList { get; private set; }


        public SortEditor()
        {
            if (PropertyList == null)
            {
                PropertyList = FilePropertyManager.GetPropertyListToSort().Select(x => x.Value).ToList();
            }

            InitializeComponent();

            this.SortSettings = new ObservableCollection<SortSetting>();
            this.itemsList.ItemsSource = this.SortSettings;

        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            this.SortSettings.Add(new SortSetting() { Property = FileProperty.FileName });
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as SortSetting;
            if (item != null && this.SortSettings.Contains(item) && this.SortSettings.Count > 0)
            {
                this.SortSettings.Remove(item);
            }
        }

        /// <summary>
        /// Up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpButtonClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as SortSetting;
            if (item == null)
            {
                return;
            }
            var index = this.SortSettings.FindIndex(x => object.ReferenceEquals(x, item));

            if (index > 0)
            {
                this.SortSettings.Remove(item);
                this.SortSettings.Insert(index - 1, item);
            }
        }

        /// <summary>
        /// Down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownButtonClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as SortSetting;
            if (item == null)
            {
                return;
            }
            var index = this.SortSettings.FindIndex(x => object.ReferenceEquals(x, item));

            if (index >= 0 && index < this.SortSettings.Count - 1)
            {
                this.SortSettings.Remove(item);
                this.SortSettings.Insert(index + 1, item);
            }
        }
    }

}
