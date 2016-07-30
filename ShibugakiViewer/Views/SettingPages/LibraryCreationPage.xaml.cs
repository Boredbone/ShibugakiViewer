using System;
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
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels.SettingPages;

namespace ShibugakiViewer.Views.SettingPages
{
    /// <summary>
    /// LibraryCreationPage.xaml の相互作用ロジック
    /// </summary>
    public partial class LibraryCreationPage : UserControl, IDisposable
    {
        public LibraryCreationPage()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            (this.DataContext as IDisposable)?.Dispose();
        }
        

        //private void dataGrid_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (!this.initialized)
        //    {
        //        var tx = ((sender as DataGrid)?.ItemsSource as ListCollectionView)?.Groups.ToString();
        //    }
        //}

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    var collectionView = CollectionViewSource.GetDefaultView(this.dataGrid.Items);
        //    //((this.DataContext as LibraryCreationPageViewModel)?.LibraryUpdateHistory);
        //    //CollectionView collectionView = GetCollectionView();
        //
        //    // 一度キャッシュをクリアーしておく。
        //    //this.dataGrid.GroupStyle.Clear();
        //    collectionView.GroupDescriptions.Clear();
        //
        //    //this.dataGrid.GroupStyle.Add(new GroupStyle
        //    //{
        //    //    ContainerStyle = ((Style)Resources["groupItemStyle"])
        //    //});
        //    collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LibraryUpdateHistoryItem.Date)));
        //}

        //private void dataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    return;
        //    var collectionView = CollectionViewSource.GetDefaultView(this.dataGrid.Items);
        //    collectionView.GroupDescriptions.Clear();
        //    collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LibraryUpdateHistoryItem.Date)));
        //}
    }
}
