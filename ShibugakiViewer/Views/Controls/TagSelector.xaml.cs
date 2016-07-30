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

        public Action<TagInformation> TagSelectedCallBack { get; set; }
        public Record Target { get; set; }

        private ObservableCollection<TagInformation> Tags { get; }
        private TagDictionary tagDictionary;

        private HashSet<TagInformation> generatedTags;

        public TagSelector()
        {
            InitializeComponent();

            this.generatedTags = new HashSet<TagInformation>();

            var core = ((App)Application.Current).Core;
            var library = core.Library;
            this.tagDictionary = library.Tags;

            this.Tags = new ObservableCollection<TagInformation>
                (this.tagDictionary.GetAll().Select(x => x.Value));

            this.list.ItemsSource = this.Tags;

            
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var selected = e.AddedItems.AsEnumerable().ToArray();
            //var old = this.list.SelectedItems.AsEnumerable().Where(x => !selected.Contains(x)).ToArray();
            //foreach(var item in old)
            //{
            //    this.list.SelectedItems.Remove(item);
            //}

        }

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void tagButton_Click(object sender, RoutedEventArgs e)
        {

            this.CommitTags();


            var tag = (sender as FrameworkElement)?.DataContext as TagInformation;
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
            var viewModel = (ClientWindowViewModel)this.DataContext;
            var scrollViewer = this.list.Descendants<ScrollViewer>().FirstOrDefault();

            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(viewModel.TagSelectorScrollOffset);
                scrollViewer.ScrollChanged += (o, ea)
                    => viewModel.TagSelectorScrollOffset = ea.VerticalOffset;
            }
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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var tag = new TagInformation();
            //this.tagDictionary.SetTag(tag);
            this.Tags.Insert(0, tag);
            this.generatedTags.Add(tag);

            this.list.SelectedItem = tag;
            this.list.ScrollIntoView(tag);

        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.CommitTags();
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
                this.list.SelectedItem = null;
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

            if(!key.IsNullOrWhiteSpace() || dc.Shortcut.Length == 1)
            {
                dc.Shortcut = key;
                this.tagDictionary.SetShortcut(dc);
            }
        }

        private void Border_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
