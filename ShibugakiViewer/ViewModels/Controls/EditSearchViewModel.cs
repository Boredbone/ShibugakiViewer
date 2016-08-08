using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Database.Search;
using ImageLibrary.Tag;
using ImageLibrary;
using ImageLibrary.Search;
using ImageLibrary.File;
using System.Windows;
using ImageLibrary.Core;
using ShibugakiViewer.Models;
using ImageLibrary.SearchProperty;
using WpfTools;
using Boredbone.XamlTools.Extensions;

namespace ShibugakiViewer.ViewModels.Controls
{

    public class EditSearchViewModel : INotifyPropertyChanged, IDisposable
    {

        private CompositeDisposable unsubscribers;

        public List<KeyValuePair<string, PropertyContainer>> PropertyList { get; private set; }
        public ReactiveProperty<int> PropertyListSelectedIndex { get; private set; }
        public string PropertyComboBoxDefault { get; private set; }
        public ReactiveProperty<Visibility> PropertyComboBoxDefaultVisibility { get; private set; }

        public List<KeyValuePair<string, CompareMode>> CompareOperator { get; private set; }
        public ReactiveProperty<int> CompareOperatorSelectedIndex { get; private set; }
        public ReactiveProperty<Visibility> CompareListVisibility { get; private set; }

        public ObservableCollection<string> EqualitySelector { get; private set; }


        public ReactiveProperty<int> EqualitySelectedIndex { get; private set; }
        public ReactiveProperty<Visibility> EqualityListVisibility { get; private set; }

        public List<KeyValuePair<int, TagInformation>> RegisteredTags
        {
            get
            {
                return LibraryOwner.GetCurrent().Tags.GetAll().ToList();
            }
        }
        public ReactiveProperty<Visibility> TagListVisibility { get; private set; }
        public ReactiveProperty<int> TagListSelectedIndex { get; private set; }
        public string TagComboBoxDefault { get; private set; }
        public ReactiveProperty<Visibility> TagComboBoxDefaultVisibility { get; private set; }

        public ReactiveProperty<int?> NumericText { get; private set; }
        public ReactiveProperty<Visibility> NumericTextVisibility { get; private set; }

        public ObservableCollection<DirectoryInfo> DirectoryList { get; private set; }
        public ReactiveProperty<Visibility> DirectoryListVisibility { get; private set; }

        public ReactiveProperty<string> TextBoxContent { get; private set; }
        public ReactiveProperty<Visibility> TextBoxVisibility { get; private set; }

        public ReactiveProperty<DateTime> DateContent { get; private set; }
        public ReactiveProperty<Visibility> DateVisibility { get; private set; }

        private bool isSVO;
        //public Visibility SVOVisibility
        //{
        //    get { return isSVO ? Visibility.Visible : Visibility.Collapsed; }
        //}
        public Visibility SOVVisibility
        {
            get { return !isSVO ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string BelowRefString { get; private set; }
        public string IsVString { get; private set; }
        public ReactiveProperty<Visibility> IsVStringVisibility { get; private set; }

        public ReactiveCommand OkCommand { get; }
        public ReactiveCommand CancelCommand { get; }
        public ReactiveProperty<bool> IsEditing { get; }

        private ApplicationCore settings;
        private Library library;

        public ISqlSearch SourceItem { get; private set; }



        public EditSearchViewModel(ISqlSearch source)
        {
            var core = ((App)Application.Current).Core;
            this.library = core.Library;
            this.settings = core;

            this.unsubscribers = new CompositeDisposable();


            this.PropertyList = FilePropertyManager.GetPropertyListToSearch()
                .Select(x => new KeyValuePair<string, PropertyContainer>(x.Key, new PropertyContainer(x.Value)))
                .ToList();

            if (!source.IsUnit)
            {
                this.PropertyList.Add
                    (new KeyValuePair<string, PropertyContainer>
                        (settings.GetResourceString("MatchAll"), new PropertyContainer(ComplexMode.And)));
                this.PropertyList.Add
                    (new KeyValuePair<string, PropertyContainer>
                        (settings.GetResourceString("MatchAny"), new PropertyContainer(ComplexMode.Or)));
            }

            this.PropertyComboBoxDefault = settings.GetResourceString("Property");
            this.TagComboBoxDefault = settings.GetResourceString("Tag");


            this.CompareOperator = new List<KeyValuePair<string, CompareMode>>();
            this.AddCompareSymbol(CompareMode.Great);
            this.AddCompareSymbol(CompareMode.GreatEqual);
            this.AddCompareSymbol(CompareMode.Equal);
            this.AddCompareSymbol(CompareMode.LessEqual);
            this.AddCompareSymbol(CompareMode.Less);
            this.AddCompareSymbol(CompareMode.NotEqual);

            this.EqualitySelector = new ObservableCollection<string>() { "", "" };
            

            this.isSVO = settings.IsSVOLanguage;
            this.BelowRefString = settings.GetResourceString("BelowRef");
            this.IsVString = settings.GetResourceString("IsV");

            this.SourceItem = source;

            var unit = source.IsUnit ? (UnitSearch)source : null;
            this.Init(unit);


            this.IsEditing = new ReactiveProperty<bool>(true).AddTo(this.unsubscribers);

            this.OkCommand = this.PropertyListSelectedIndex
                .Select(x => x >= 0)
                .ToReactiveCommand()
                .WithSubscribe(_ =>
                {
                    this.Commit();
                    this.IsEditing.Value = false;
                }, this.unsubscribers);
            this.CancelCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.IsEditing.Value = false, this.unsubscribers);


            var propertyObserver = this.PropertyListSelectedIndex
                .Select(x =>
                {
                    var container = this.GetProperty(x);
                    return new
                    {
                        Property = container.Property,
                        Enable = (container.Complex == ComplexMode.None
                            && container.IsValid)
                    };

                })
                .Publish().RefCount();

            this.CompareListVisibility = propertyObserver
                .Select(x => VisibilityHelper.Set(x.Enable && x.Property.IsComperable()))
                .ToReactiveProperty().AddTo(this.unsubscribers);

            this.EqualityListVisibility = propertyObserver
                .Select(x => VisibilityHelper.Set(x.Enable && !x.Property.IsComperable()))
                .ToReactiveProperty().AddTo(this.unsubscribers);

            this.NumericTextVisibility = propertyObserver
                .Select(x => VisibilityHelper.Set(x.Enable && x.Property.IsNumeric()))
                .ToReactiveProperty().AddTo(this.unsubscribers);

            this.DirectoryListVisibility = propertyObserver
                .Select(x => VisibilityHelper.Set(x.Enable && x.Property == FileProperty.DirectoryPathStartsWith))
                .ToReactiveProperty().AddTo(this.unsubscribers);

            this.TagListVisibility = propertyObserver
               .Select(x => VisibilityHelper.Set(x.Enable && x.Property == FileProperty.ContainsTag))
               .ToReactiveProperty().AddTo(this.unsubscribers);

            this.TextBoxVisibility = propertyObserver
               .Select(x => VisibilityHelper.Set(x.Enable && x.Property.IsText()))
               .ToReactiveProperty().AddTo(this.unsubscribers);

            this.DateVisibility = propertyObserver
               .Select(x => VisibilityHelper.Set(x.Enable && x.Property.IsDate()))
               .ToReactiveProperty().AddTo(this.unsubscribers);

            this.IsVStringVisibility = propertyObserver
               .Select(x => VisibilityHelper.Set(x.Enable && !this.isSVO))
               .ToReactiveProperty().AddTo(this.unsubscribers);

            this.PropertyComboBoxDefaultVisibility = this.PropertyListSelectedIndex
                .Select(x => VisibilityHelper.Set(x < 0))
                .ToReactiveProperty()
                .AddTo(this.unsubscribers);

            this.TagComboBoxDefaultVisibility
                = new[]
                {
                    this.TagListSelectedIndex.Select(x =>x < 0),
                    propertyObserver.Select(x => x.Enable && x.Property == FileProperty.ContainsTag)
                }
                .CombineLatestValuesAreAllTrue()
                .Select(x => VisibilityHelper.Set(x))
                .ToReactiveProperty()
                .AddTo(this.unsubscribers);


            propertyObserver.Subscribe(x =>
            {
                if (x.Enable)
                {
                    var currentIndex = this.EqualitySelectedIndex.Value;
                    this.EqualitySelector[this.equalitySelectorIndex(true)]
                        = x.Property.GetEqualityLabel(true);
                    this.EqualitySelector[this.equalitySelectorIndex(false)]
                        = x.Property.GetEqualityLabel(false);
                    this.EqualitySelectedIndex.Value = currentIndex;
                }
            }).AddTo(this.unsubscribers);


            this.PropertyListSelectedIndex.Value =
                unit == null
                ? -1
                : this.PropertyList.FindIndex(x => x.Value.Property == unit.Property);

        }

        private void AddCompareSymbol(CompareMode mode)
        {
            //this.CompareOperator.Add(new KeyValuePair<string, CompareMode>(mode.GetSymbol(), mode));
            this.CompareOperator.Add(new KeyValuePair<string, CompareMode>(mode.GetLabel(), mode));
        }

        private int equalitySelectorIndex(bool flag)
        {
            return flag ? 1 : 0;
        }

        private void Init(UnitSearch source)
        {

            this.PropertyListSelectedIndex = new ReactiveProperty<int>(-2).AddTo(this.unsubscribers);

            //this.PropertyListSelectedIndex = new ReactiveProperty<int>
            //    (source == null ? -1 : this.PropertyList.FindIndex(x => x.Value.Property == source.Property)).AddTo(this.unsubscribers);

            this.CompareOperatorSelectedIndex = new ReactiveProperty<int>
                (this.CompareOperator.FindIndex(x => x.Value == ((source?.Mode)??CompareMode.Equal))).AddTo(this.unsubscribers);

            this.EqualitySelectedIndex = new ReactiveProperty<int>
                (source == null ? 0 : this.equalitySelectorIndex(!source.Mode.ContainsEqual()));

            this.TextBoxContent = new ReactiveProperty<string>
                ((source == null || !source.Property.IsText()) ? "" : source.Reference.ToString()).AddTo(this.unsubscribers);

            this.DateContent = new ReactiveProperty<DateTime>
                ((source == null || !source.Property.IsDate())
                ? DateTime.Now : ((DateTimeOffset)source.Reference).Date).AddTo(this.unsubscribers);

            this.TagListSelectedIndex = new ReactiveProperty<int>
                ((source == null || source.Property != FileProperty.ContainsTag) ? -1
                : this.RegisteredTags.FindIndex(x => x.Key == (int)source.Reference)).AddTo(this.unsubscribers);

            this.NumericText = new ReactiveProperty<int?>(
                (source != null && source.Property.IsNumeric())
                ? ((int?)(source.Reference as long?) ?? (source.Reference as int?))
                : (int?)null)
                .AddTo(this.unsubscribers);


            if (source == null)
            {
                this.DirectoryList = new ObservableCollection<DirectoryInfo>();
                this.AddHierarchy(this.library.TreeRootNode, 0);
            }
            else
            {
                var newList = new List<DirectoryInfo>();

                var text = source.Reference as string;


                if (source.Property == FileProperty.DirectoryPathStartsWith
                    && text != null)
                {
                    var pathList = this.library.GetPathList(text);
                    pathList.ForEach(x => this.AddHierarchy(newList, x));

                    pathList.Skip(1).Zip(newList, (p, n) => new { Key = p.GetKey(), Node = n })
                        .ForEach(x => x.Node.Choice(x.Key));
                }
                else
                {

                    this.AddHierarchy(newList, this.library.TreeRootNode);
                }
                this.DirectoryList = new ObservableCollection<DirectoryInfo>(newList);
            }
        }

        private PropertyContainer GetProperty(int index)
        {
            if (index < 0 || index >= this.PropertyList.Count)
            {
                return new PropertyContainer();
            }
            return this.PropertyList[index].Value;
        }
        













        private void AddHierarchy(TreeNode<string> source, int index)
        {
            while (this.DirectoryList.Count > index)
            {
                this.DirectoryList.RemoveAt(index);
            }
            AddHierarchy(this.DirectoryList, source);
        }

        private void AddHierarchy(IList<DirectoryInfo> list, TreeNode<string> source)
        {
            if (source != null)
            {
                var items = source.GetChildren().ToList();
                if (items.Count >= 2 || (items.Count == 1 && !items.First().Key.Equals("\\")))
                {
                    var di = new DirectoryInfo(items);

                    di.SelectedIndex.Subscribe(x =>
                    {
                        if (this.DirectoryList == null)
                        {
                            return;
                        }
                        var hierarchy = this.DirectoryList.IndexOf(di);

                        if (hierarchy < 0)
                        {
                            return;
                        }

                        if (x < 0)
                        {
                            AddHierarchy(null, hierarchy + 1);
                            return;
                        }
                        if (x >= di.Children.Count)
                        {
                            return;
                        }
                        var item = di.Children[x];

                        AddHierarchy(item.Value, hierarchy + 1);
                    }).AddTo(this.unsubscribers);
                    
                    list.Add(di);

                }
            }
        }

















        public void Commit()
        {
            var container = this.GetProperty(this.PropertyListSelectedIndex.Value);
            if (container == null || !container.IsValid)
            {
                return;
            }

            ISqlSearch newSetting;

            if (container.Complex != ComplexMode.None)
            {
                newSetting = new ComplexSearch(container.Complex == ComplexMode.Or);
            }
            else
            {

                var unit = this.ToNewSetting(container.Property);
                if (unit == null)
                {
                    return;
                }
                if (this.SourceItem.IsUnit)
                {
                    var original = ((UnitSearch)this.SourceItem);
                    original.CopyFrom(unit);
                    original.RefreshReferenceLabel();
                    return;
                }

                newSetting = unit;
            }


            ((ComplexSearch)this.SourceItem).Add(newSetting);
        }


        private UnitSearch ToNewSetting(FileProperty property)
        {

            var reference = this.GetReference(property);
            if (reference == null)
            {
                return null;
            }

            var newSetting = new UnitSearch()
            {
                Property = property,
                Reference = reference,
            };

            if (property.IsComperable())
            {
                newSetting.Mode
                    = this.CompareOperator[this.CompareOperatorSelectedIndex.Value].Value;
                //newSetting.IsNot = false;
            }
            else
            {

                newSetting.Mode = (this.EqualitySelectedIndex.Value == this.equalitySelectorIndex(true))
                    ? CompareMode.NotEqual : CompareMode.Equal;

                // newSetting.IsNot = this.EqualitySelectedIndex.Value == this.equalitySelectorIndex(true);

            }

            return newSetting;
        }

        /// <summary>
        /// 検索閾値を取得
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private object GetReference(FileProperty property)
        {

            if (property == FileProperty.DirectoryPathStartsWith)
            {
                return this.DirectoryList
                    .Select(x => x.GetSelectedLabel())
                    .Join();
                //.ToList();
            }
            else if (property == FileProperty.ContainsTag)
            {
                if (this.TagListSelectedIndex.Value >= 0
                        && this.TagListSelectedIndex.Value < this.RegisteredTags.Count)
                {
                    var item = this.RegisteredTags[this.TagListSelectedIndex.Value];
                    return item.Key;
                }
            }

            if (property.IsNumeric())
            {
                //var str = Regex.Replace(this.NumericText.Value, @"[^\d]", "");
                var num = this.NumericText.Value ?? 0;

                if (property == FileProperty.Size)
                {
                    return (long)num;
                }
                else
                {
                    return num;
                    //int num;
                    //if (int.TryParse(str, out num))
                    //{
                    //    return num;
                    //}
                }
            }

            if (property.IsText())
            {
                return this.TextBoxContent.Value;
            }

            if (property.IsDate())
            {
                //case FileProperty.DateCreated:
                //case FileProperty.DateModified:
                var date = this.DateContent.Value;
                var fixedDate = new DateTimeOffset(date.Date, DateTimeOffset.Now.Offset);


                return fixedDate;
            }



            return null;
        }







        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            this.unsubscribers.Dispose();
        }
    }
    public class PropertyContainer
    {
        public FileProperty Property { get; private set; }
        public ComplexMode Complex { get; private set; }
        public bool IsValid { get; private set; }

        public PropertyContainer(FileProperty property)
        {
            this.Property = property;
            this.Complex = ComplexMode.None;
            this.IsValid = true;
        }
        public PropertyContainer(ComplexMode mode)
        {
            this.Complex = mode;
            this.IsValid = true;
        }
        public PropertyContainer()
        {
            this.IsValid = false;
        }
    }
    public enum ComplexMode
    {
        And, Or, None
    }
}
