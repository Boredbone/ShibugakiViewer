using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.Views.Controls;
using ShibugakiViewer.Views.Converters;

namespace ShibugakiViewer.Views.InformationPanes
{
    /// <summary>
    /// FileInformationPage.xaml の相互作用ロジック
    /// </summary>
    public partial class FileInformationPage : UserControl
    {

        #region Source

        public Record Source
        {
            get { return (Record)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Record), typeof(FileInformationPage),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as FileInformationPage;
            var value = e.NewValue as Record;

            var old = e.OldValue as Record;

            if (thisInstance == null)
            {
                return;
            }

            if (old != null && old.IsGroup && !old.FileName.Equals(thisInstance.groupNameBox.Text))
            {
                Debug.WriteLine($"Change Name {old.FileName} -> {thisInstance.groupNameBox.Text}");
                old.SetName(thisInstance.groupNameBox.Text);
            }

            thisInstance.rootGrid.DataContext = value;

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
                typeof(FileInformationPage), new PropertyMetadata(null));


        #endregion

        #region SelectedPath

        public string SelectedPath
        {
            get { return (string)GetValue(SelectedPathProperty); }
            set { SetValue(SelectedPathProperty, value); }
        }

        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register(nameof(SelectedPath), typeof(string),
                typeof(FileInformationPage), new PropertyMetadata(null));

        #endregion



        public FileInformationPage()
        {
            InitializeComponent();
            this.Source = Record.Empty;
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

            if (this.Source != null && tag != null)
            {
                this.Source.TagSet.Remove(tag.Id);
            }
        }

        private void pathButton_Click(object sender, RoutedEventArgs e)
        {
            var path = ((sender as FrameworkElement)?.DataContext as PathContainer)?.FullPath;
            this.SelectedPath = path;
            this.SelectedPath = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var clientViewModel = this.DataContext as ClientWindowViewModel;
            var control = sender as FrameworkElement;

            clientViewModel.ShowTagSelector(control);

            //var window = Window.GetWindow(this) as IPopupDialogOwner;
            ////var clientViewModel = this.DataContext as ClientWindowViewModel;
            //var control = sender as FrameworkElement;
            //
            //if (window != null && control!=null)
            //{
            //    var content = new TagSelector()
            //    {
            //        Target = this.Source,
            //    };
            //
            //    window.PopupDialog.Show(content,
            //        new Thickness(10.0, 10.0, double.NaN, double.NaN),
            //        HorizontalAlignment.Right, VerticalAlignment.Center, control);
            //}
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            SharePathOperation.CopyPath(this.Source?.FullPath);
            this.HideFileOperationDialog();
        }

        private void explorerButton_Click(object sender, RoutedEventArgs e)
        {
            SharePathOperation.OpenExplorer(this.Source?.FullPath);
            this.HideFileOperationDialog();
        }

        private void anotherAppButton_Click(object sender, RoutedEventArgs e)
        {
            SharePathOperation.OpenAnotheApp(this.Source?.FullPath);
            this.HideFileOperationDialog();
        }

        private void HideFileOperationDialog()
        {
            this.fileOperationDialog.IsEnabled = false;
        }
    }


}
