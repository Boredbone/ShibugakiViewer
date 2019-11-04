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
using Boredbone.Utility.Extensions;
using ImageLibrary.File;
using ImageLibrary.Tag;
using ImageLibrary.Viewer;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.Views.Controls;

namespace ShibugakiViewer.Views.InformationPanes
{
    /// <summary>
    /// SelectedItemsPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectedItemsPage : UserControl
    {
        #region Source

        public SelectionManager Source
        {
            get { return (SelectionManager)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(SelectionManager), typeof(SelectedItemsPage),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as SelectedItemsPage;
            var value = e.NewValue as SelectionManager;

            thisInstance.list.ItemsSource = value?.Ids;
            thisInstance.tagList.ItemsSource = value?.CommonTags;

        }

        #endregion

        #region SelectedTag

        public TagInformation SelectedTag
        {
            get { return (TagInformation)GetValue(SelectedTagProperty); }
            set { SetValue(SelectedTagProperty, value); }
        }

        public static readonly DependencyProperty SelectedTagProperty =
            DependencyProperty.Register(nameof(SelectedTag), typeof(TagInformation),
                typeof(SelectedItemsPage), new PropertyMetadata(null));


        #endregion


        public SelectedItemsPage()
        {
            InitializeComponent();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var clientViewModel = this.DataContext as ClientWindowViewModel;
            var control = sender as FrameworkElement;

            clientViewModel.ShowTagSelector(control);
        }

        private void pathRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var path = (sender as FrameworkElement)?.DataContext as string;
            if (path != null)
            {
                this.Source.Remove(path);
            }
        }

        private void tagButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = ((sender as FrameworkElement)?.DataContext as TagInformation);
            this.SelectedTag = tag;
            this.SelectedTag = null;
        }

        private void tagRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = ((sender as FrameworkElement)?.DataContext as TagInformation);
            if (tag != null)
            {
                this.Source.RemoveTag(tag);
            }
        }
        
        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Core.CopySelectedItemsPath(this.Source);
            this.HideFileOperationDialog();
        }
        
        private void HideFileOperationDialog()
        {
            this.fileOperationDialog.IsEnabled = false;
        }

        private async void FlatButton_Click(object sender, RoutedEventArgs e)
        {
            var clientViewModel = this.DataContext as ClientWindowViewModel;
            if (clientViewModel != null)
            {
                await clientViewModel.Client.DeleteSelectedFiles(false);
            }
            this.HideFileOperationDialog();
        }
    }
}
