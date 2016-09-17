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
using Boredbone.XamlTools.Extensions;
using ImageLibrary.File;
using ImageLibrary.Tag;
using ShibugakiViewer.ViewModels;
using Boredbone.Utility.Extensions;
using System.Windows.Controls.Primitives;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// TagSelector.xaml の相互作用ロジック
    /// </summary>
    public partial class TagSelector : UserControl
    {


        public static string[] Alphabets { get; }
            = Enumerable.Range('A', 'Z' - 'A' + 1)
            .Select(x => Convert.ToChar(x).ToString()).Append(" ").ToArray();

        private readonly Dictionary<string,IDisposable> disposables;

        public Action<TagInformation> TagSelectedCallBack { get; set; }
        public Record Target { get; set; }

        private ObservableCollection<TagInformation> Tags { get; }
        private TagDictionary tagDictionary;

        private HashSet<TagInformation> generatedTags;

        public ClientWindowViewModel ViewModel
        {
            get
            {
                if (this._fieldViewModel == null)
                {
                    this._fieldViewModel = this.DataContext as ClientWindowViewModel;
                }
                return _fieldViewModel;
            }
        }
        private ClientWindowViewModel _fieldViewModel;

        private TagInformation lastSelectedTag = null;

        private bool skipDecision = false;
        private bool isSelectionInitialized = false;

        public TagSelector()
        {
            InitializeComponent();

            this.isSelectionInitialized = true;

            this.disposables = new Dictionary<string, IDisposable>();

            this.generatedTags = new HashSet<TagInformation>();

            var core = ((App)Application.Current).Core;
            var library = core.Library;
            this.tagDictionary = library.Tags;

            this.Tags = new ObservableCollection<TagInformation>();
                //(this.tagDictionary.GetAll().Select(x => x.Value));

            this.CloseEdit();

            this.list.ItemsSource = this.Tags;


            this.list.ItemContainerGenerator.StatusChanged += (sender, e) =>
            {
                if (!this.isSelectionInitialized
                    && this.list.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    //var tag = (this.ViewModel.TagSelectorLastSelected != null)
                    //    ? this.Tags.FirstOrDefault(x => x.Id == this.ViewModel.TagSelectorLastSelected.Id)
                    //    : this.Tags.FirstOrDefault();


                    if (this.SelectLastTag())// this.SelectItem(tag))
                    {
                        this.isSelectionInitialized = true;
                    }
                }
            };

        }

        private bool SelectLastTag()
        {
            var tag = (this.ViewModel.TagSelectorLastSelected != null)
                ? this.Tags.FirstOrDefault(x => x.Id == this.ViewModel.TagSelectorLastSelected.Id)
                : this.Tags.FirstOrDefault();


            return this.SelectItem(tag);
        }

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void tagButton_Click(object sender, RoutedEventArgs e)
        {
            this.DecideTag((sender as FrameworkElement)?.DataContext as TagInformation);
        }

        private void DecideTag(TagInformation tag)
        {
            //var tag = (sender as FrameworkElement)?.DataContext as TagInformation;

            this.lastSelectedTag = tag;

            this.CommitTags();

            if (tag != null && !tag.Name.IsNullOrWhiteSpace())
            {
                if (this.TagSelectedCallBack != null)
                {
                    this.TagSelectedCallBack.Invoke(tag);
                }
                else if (this.Target != null)
                {
                    this.Target.TagSet.Add(tag);
                }
            }
            this.IsEnabled = false;

        }

        private void list_Loaded(object sender, RoutedEventArgs e)
        {
            //var viewModel = (ClientWindowViewModel)this.DataContext;
            var scrollViewer = this.list.Descendants<ScrollViewer>().FirstOrDefault();

            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(this.ViewModel.TagSelectorScrollOffset);
                scrollViewer.ScrollChanged += (o, ea)
                    => this.ViewModel.TagSelectorScrollOffset = ea.VerticalOffset;
            }

            //this.list.SelectedIndex = 0;
            //
            //
            //if (viewModel.TagSelectorLaseSelected != null)
            //{
            //    this.list.SelectedItem = viewModel.TagSelectorLaseSelected;
            //}
            //else
            //{
            //    this.list.SelectedItem = this.Tags.FirstOrDefault();
            //}
        }

        private void CommitTags()
        {
            this.tagDictionary.AddTags(this.generatedTags);

            //var now = DateTimeOffset.Now;
            //foreach(var group in this.generatedTags.GroupBy(x => x.Name))
            //{
            //    if (group.Key.IsNullOrWhiteSpace())
            //    {
            //        continue;
            //    }
            //    var key=this.tagDictionary.SetTag(group.First());
            //    group.ForEach(x =>
            //    {
            //        x.Id = key;
            //        x.LastUsed = now;
            //    });
            //}
            this.generatedTags.Clear();


            //var viewModel = this.DataContext as ClientWindowViewModel;
            if (this.ViewModel != null)
            {
                var lastTag = lastSelectedTag ?? (this.list.SelectedItem as TagInformation);
                if (lastTag != null)
                {
                    this.ViewModel.TagSelectorLastSelected = lastTag;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var tag = new TagInformation()
            {
                IsOpen = true,
            };

            //this.tagDictionary.SetTag(tag);
            this.Tags.Insert(0, tag);
            this.generatedTags.Add(tag);

            this.SelectItem(tag);
            this.list.ScrollIntoView(tag);

        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.CommitTags();

            this.disposables.ForEach(x => x.Value.Dispose());
            this.disposables.Clear();
        }

        private void TagNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var binding = ((FrameworkElement)sender).GetBindingExpression(TextBox.TextProperty);
                if (binding != null)
                {
                    binding.UpdateSource();
                }
                //this.list.SelectedItem = null;
                this.skipDecision = true;
                e.Handled = true;
            }
        }


        private void TagNameTextBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((TextBox)sender).Focus();
        }

        private void shortcutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var dc = comboBox?.DataContext as TagInformation;
            var key = e.AddedItems.AsEnumerable().Select(x => x.ToString()).FirstOrDefault();

            if (comboBox == null || dc == null)
            {
                return;
            }

            if (!key.IsNullOrWhiteSpace() || dc.Shortcut.Length == 1)
            {
                dc.Shortcut = key;
                this.tagDictionary.SetShortcut(dc);
            }
        }

        private void Border_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.DataContext as TagInformation;
            this.ToggleEditing(tag);
        }

        //private void Border_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Apps)
        //    {
        //        var tag = (sender as FrameworkElement)?.DataContext as TagInformation;
        //        this.ToggleEditing(tag);
        //    }
        //}

        private void ToggleEditing(TagInformation tag)
        {
            if (tag != null && !tag.IsOpen)
            {
                this.EditTag(tag);
            }
            else
            {
                this.CloseEdit();
            }
        }

        private void EditTag(TagInformation tag)
        {
            this.CloseEdit();
            if (tag != null)
            {
                tag.IsOpen = true;
            }
        }

        private void CloseEdit()
        {
            this.Tags.ForEach(x => x.IsOpen = false);
        }

        //private void Border_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Apps)
        //    {
        //        var tag = (sender as FrameworkElement)?.DataContext as TagInformation;
        //        this.ToggleEditing(tag);
        //    }
        //
        //}

        private void UserControl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Apps)
            {
                this.ToggleEditing(this.list.SelectedItem as TagInformation);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                var focused = FocusManager.GetFocusedElement(this) ?? Keyboard.FocusedElement; 
                if (focused is TextBox)
                {
                    return;
                }
                if (this.skipDecision)
                {
                    this.skipDecision = false;
                    return;
                }
                this.DecideTag(this.list.SelectedItem as TagInformation);
                e.Handled = true;
            }
        }



        private bool SelectItem(TagInformation tag)
        {
            this.list.SelectedItem = tag;
            var item = (ListBoxItem)(this.list.ItemContainerGenerator.ContainerFromItem(this.list.SelectedItem));

            if (tag != null && item != null)
            {
                item.Focus();
                return true;
            }

            return false;
        }

        private void TagNameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var item = sender as UIElement;
            //item?.Focus();
        }
        

        private void LoadList(int mode)
        {
            this.CommitTags();

            this.Tags.Clear();

            var source = this.tagDictionary.GetAll();

            if (mode==1)
            {
                source = source.OrderByDescending(x => x.Value.LastUsed);
            }

            foreach(var t in source)
            {
                this.Tags.Add(t.Value);
            }
            
            this.CloseEdit();

            this.isSelectionInitialized = false;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = e.NewValue as ClientWindowViewModel;
            if (vm == null)
            {
                return;
            }

            vm.TagSelectorSortMode
                .Subscribe(x =>
                {
                    this.LoadList(x);
                })
                .AddTo(this.disposables,"SortMode");
        }
    }
}
