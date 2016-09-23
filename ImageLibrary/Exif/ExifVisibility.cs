using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database.Table;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using ExifLib;

namespace ImageLibrary.Exif
{
    public class ExifManager : DisposableBase
    {
        private Dictionary<int, ExifVisibilityItem> Items { get; set; }

        private Subject<ExifVisibilityItem> AddedSubject { get; }
        public IObservable<ExifVisibilityItem> Added => this.AddedSubject.AsObservable();

        //public IReadOnlyList<KeyValuePair<ushort, string>> Tags { get; }

        private readonly Lazy<IReadOnlyList<ExifVisibilityItem>> tagVisibilityList;
        public IReadOnlyList<ExifVisibilityItem> TagVisibilityList => this.tagVisibilityList.Value;

        internal int VisibleItemsCount
        {
            get { return _fieldVisibleItemsCount; }
            set
            {
                if (_fieldVisibleItemsCount != value)
                {
                    if (_fieldVisibleItemsCount > 0 && value <= 0)
                    {
                        this.HasVisibleItemSubject.OnNext(false);
                    }
                    else if (_fieldVisibleItemsCount <= 0 && value > 0)
                    {
                        this.HasVisibleItemSubject.OnNext(true);
                    }
                    if (value < 0)
                    {
                        _fieldVisibleItemsCount = 0;
                    }
                    else
                    {
                        _fieldVisibleItemsCount = value;
                    }
                }
            }
        }
        private int _fieldVisibleItemsCount;

        private Subject<bool> HasVisibleItemSubject { get; }
        public IObservable<bool> HasVisibleItem => this.HasVisibleItemSubject.AsObservable();
       


        public ExifManager()
        {
            this.AddedSubject = new Subject<ExifVisibilityItem>().AddTo(this.Disposables);
            this.Items = new Dictionary<int, ExifVisibilityItem>();

            var tagIds = Enum.GetValues(typeof(ExifTags)).Cast<ushort>().OrderBy(x => x).ToArray();

            var tags = tagIds.Where(x => x > 0x1F && (x < 0x9C9B || x > 0x9C9F))
                .Concat(tagIds.Where(x => x <= 0x1F))
                .Concat(tagIds.Where(x => x >= 0x9C9B && x <= 0x9C9F))
                .Distinct()
                .Select(x =>
                {
                    string name;
                    if (x == (ushort)ExifTags.PhotographicSensitivity)
                    {
                        name = nameof(ExifTags.PhotographicSensitivity);
                    }
                    else
                    {
                        name = Enum.GetName(typeof(ExifTags), x);
                    }
                    return new KeyValuePair<ushort, string>(x, name);
                })
                .ToList();

            this.tagVisibilityList = new Lazy<IReadOnlyList<ExifVisibilityItem>>
                (() => new ObservableCollection<ExifVisibilityItem>(tags.Select(x =>
                {
                    ExifVisibilityItem item = null;
                    if (!this.Items.TryGetValue(x.Key, out item) || item == null)
                    {
                        item = this.Add(x.Key);
                    }

                    item.Name = x.Value;
                    return item;
                })));

        }

        /// <summary>
        /// 辞書を初期化
        /// </summary>
        /// <param name="source"></param>
        public IReadOnlyList<ExifVisibilityItem> SetSource(IEnumerable<ExifVisibilityItem> source)
        {
            if (source == null)
            {
                this.Items.Clear();
            }
            else
            {
                this.Items = source
                    .Where(x => x != null)
                    .Select(x =>
                    {
                        x.Manager = this;
                        if (x.IsEnabled)
                        {
                            this.VisibleItemsCount++;
                        }
                        return x;
                    })
                    .ToDictionary(x => x.Id, x => x);
            }
            return this.TagVisibilityList;
        }

        private ExifVisibilityItem Add(ushort key)
        {
            var item = new ExifVisibilityItem()
            {
                Id = key,
                IsEnabled = false,
                Manager = this,
            };
            this.Items[key] = item;
            this.AddedSubject.OnNext(item);
            return item;
        }

        public bool IsVisible(int key)
        {
            ExifVisibilityItem result;
            if (this.Items.TryGetValue(key, out result))
            {
                if (result != null)
                {
                    return result.IsEnabled;
                }
            }
            return false;
        }


        public ExifInformation LoadExif(string path)
        {

            try
            {
                var props = new List<KeyValuePair<ExifVisibilityItem, string>>();

                using (var reader = new ExifReader(path))
                {
                    // Parse through all available fields and generate key-value labels
                    foreach(var item in this.tagVisibilityList.Value)
                    {
                        var key = (ushort)item.Id;
                        string text = null;

                        object val;
                        if (reader.GetTagValue(key, out val))
                        {
                            // Special case - some doubles are encoded as TIFF rationals. These
                            // items can be retrieved as 2 element arrays of {numerator, denominator}
                            if (val is double)
                            {
                                int[] rational;
                                if (reader.GetTagValue(key, out rational))
                                {
                                    text = string.Format($"{val} ({rational[0]}/{rational[1]})");
                                }
                            }

                            if (text == null)
                            {
                                text = this.RenderTag(val);
                            }

                            if (text != null)
                            {
                                props.Add(new KeyValuePair<ExifVisibilityItem, string>(item, text));
                            }
                        }
                    }

                    return new ExifInformation(props);
                }
            }
            catch
            {
                // Something didn't work!
                return null;
            }
        }
        private string RenderTag(object tagValue)
        {
            // Arrays don't render well without assistance.
            var array = tagValue as Array;
            if (array != null)
            {
                // Hex rendering for really big byte arrays (ugly otherwise)
                if (array.Length > 20 && array.GetType().GetElementType() == typeof(byte))
                    return "";// "0x" + string.Join("", array.Cast<byte>().Select(x => x.ToString("X2")).ToArray());

                return string.Join(", ", array.Cast<object>().Select(x => x.ToString()).ToArray());
            }

            return tagValue.ToString();
        }
    }

    public class ExifVisibilityItem : INotifyPropertyChanged, IRecord<int>, ITrackable
    {
        [RecordMember]
        public int Id
        {
            get { return _fieldId; }
            set
            {
                if (_fieldId != value)
                {
                    _fieldId = value;
                    RaisePropertyChanged(nameof(Id));
                }
            }
        }
        private int _fieldId;

        [RecordMember]
        public bool IsEnabled
        {
            get { return _fieldIsEnabled; }
            set
            {
                if (_fieldIsEnabled != value)
                {
                    _fieldIsEnabled = value;
                    if (this.Manager != null)
                    {
                        Manager.VisibleItemsCount += value ? 1 : -1;
                    }
                    RaisePropertyChanged(nameof(IsEnabled));
                }
            }
        }
        private bool _fieldIsEnabled;

        public string Name { get; set; }


        public bool IsLoaded { get; set; } = false;

        internal ExifManager Manager { get; set; }


        public ExifVisibilityItem()
        {
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

    public class ExifInformation
    {
        public ObservableCollection<KeyValuePair<ExifVisibilityItem, string>> Items { get; }

        public ExifInformation(IEnumerable<KeyValuePair<ExifVisibilityItem, string>> items)
        {
            this.Items = new ObservableCollection<KeyValuePair<ExifVisibilityItem, string>>(items);
        }
    }
}
