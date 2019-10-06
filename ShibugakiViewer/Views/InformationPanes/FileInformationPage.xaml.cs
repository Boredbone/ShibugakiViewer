using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.Views.Controls;
using ShibugakiViewer.Views.Converters;
using WpfTools;

namespace ShibugakiViewer.Views.InformationPanes
{
    /// <summary>
    /// FileInformationPage.xaml の相互作用ロジック
    /// </summary>
    public partial class FileInformationPage : UserControl, IDisposable
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
            thisInstance.RefreshVisibility();

        }

        #endregion

        #region IsMainClosed

        public bool IsMainClosed
        {
            get { return (bool)GetValue(IsMainClosedProperty); }
            set { SetValue(IsMainClosedProperty, value); }
        }

        public static readonly DependencyProperty IsMainClosedProperty =
            DependencyProperty.Register(nameof(IsMainClosed), typeof(bool), typeof(FileInformationPage),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsMainClosedChanged)));

        private static void OnIsMainClosedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as FileInformationPage;
            var value = e.NewValue as bool?;
            thisInstance?.RefreshVisibility();
        }

        #endregion

        #region FileCommonVisibility

        public Visibility FileCommonVisibility
        {
            get { return (Visibility)GetValue(FileCommonVisibilityProperty); }
            set { SetValue(FileCommonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FileCommonVisibilityProperty =
            DependencyProperty.Register(nameof(FileCommonVisibility), typeof(Visibility),
                typeof(FileInformationPage), new PropertyMetadata(Visibility.Visible));

        #endregion


        #region FileMainVisibility

        public Visibility FileMainVisibility
        {
            get { return (Visibility)GetValue(FileMainVisibilityProperty); }
            set { SetValue(FileMainVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FileMainVisibilityProperty =
            DependencyProperty.Register(nameof(FileMainVisibility), typeof(Visibility),
                typeof(FileInformationPage), new PropertyMetadata(Visibility.Visible));

        #endregion

        #region GroupVisibility

        public Visibility GroupVisibility
        {
            get { return (Visibility)GetValue(GroupVisibilityProperty); }
            set { SetValue(GroupVisibilityProperty, value); }
        }

        public static readonly DependencyProperty GroupVisibilityProperty =
            DependencyProperty.Register(nameof(GroupVisibility), typeof(Visibility),
                typeof(FileInformationPage), new PropertyMetadata(Visibility.Collapsed));

        #endregion

        #region CommonMainVisibility

        public Visibility CommonMainVisibility
        {
            get { return (Visibility)GetValue(CommonMainVisibilityProperty); }
            set { SetValue(CommonMainVisibilityProperty, value); }
        }

        public static readonly DependencyProperty CommonMainVisibilityProperty =
            DependencyProperty.Register(nameof(CommonMainVisibility), typeof(Visibility),
                typeof(FileInformationPage), new PropertyMetadata(Visibility.Visible));

        #endregion

        #region IsExifEnabled

        public bool IsExifEnabled
        {
            get { return (bool)GetValue(IsExifEnabledProperty); }
            set { SetValue(IsExifEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsExifEnabledProperty =
            DependencyProperty.Register(nameof(IsExifEnabled), typeof(bool), typeof(FileInformationPage),
            new PropertyMetadata(true, new PropertyChangedCallback(OnIsExifEnabledChanged)));

        private static void OnIsExifEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as FileInformationPage;
            var value = e.NewValue as bool?;

            if (thisInstance != null && value.HasValue)
            {
                thisInstance.expandButtonGrid.Visibility = VisibilityHelper.Set(value.Value);
                if (!value.Value)
                {
                    thisInstance.IsMainClosed = false;
                }
            }

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

        private CompositeDisposable disposables;


        public FileInformationPage()
        {
            InitializeComponent();
            this.Source = Record.Empty;

            this.disposables = new CompositeDisposable();

            var directoryBinding = this.directoryList
                    .GetBindingExpression(ItemsControl.ItemsSourceProperty);


            ((App)Application.Current).Core.Library.Loaded
                .Where(x => x.Action == ImageLibrary.Core.LibraryLoadAction.Activation
                    || x.Action == ImageLibrary.Core.LibraryLoadAction.UserOperation)
                .ObserveOnUIDispatcher()
                //.ObserveOnDispatcher()
                .Subscribe(_ => directoryBinding?.UpdateTarget())
                .AddTo(this.disposables);
        }

        public void Dispose()
        {
            this.disposables.Dispose();
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

        private async void FlatButton_Click(object sender, RoutedEventArgs e)
        {
            var clientViewModel = this.DataContext as ClientWindowViewModel;
            if (clientViewModel != null)
            {
                var result = await clientViewModel.Client.DeleteSelectedSingleFile(false);
            }
            this.HideFileOperationDialog();
        }

        private void RefreshVisibility()
        {
            var isGroup = this.Source?.IsGroup ?? false;
            var isOpen = !this.IsMainClosed;

            this.FileCommonVisibility = VisibilityHelper.Set(!isGroup);
            this.FileMainVisibility = VisibilityHelper.Set(!isGroup && isOpen);
            this.GroupVisibility = VisibilityHelper.Set(isGroup);
            this.CommonMainVisibility = VisibilityHelper.Set(isGroup || isOpen);
        }
    }


}
