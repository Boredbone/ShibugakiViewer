using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database.Table;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using ExifLib;

namespace ImageLibrary.Exif
{
    /// <summary>
    /// Exif情報の管理
    /// </summary>
    public class ExifManager : DisposableBase
    {

        private Subject<ExifVisibilityItem> AddedSubject { get; }
        public IObservable<ExifVisibilityItem> Added => this.AddedSubject.AsObservable();
        
        private ObservableCollection<ExifVisibilityItem> tagVisibilityList;
        public IReadOnlyList<ExifVisibilityItem> TagVisibilityList => this.tagVisibilityList;

        private readonly KeyValuePair<ushort, string>[] tags;

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

        private BehaviorSubject<bool> HasVisibleItemSubject { get; }
        public IObservable<bool> HasVisibleItem => this.HasVisibleItemSubject.AsObservable();
       


        public ExifManager()
        {
            this.AddedSubject = new Subject<ExifVisibilityItem>().AddTo(this.Disposables);
            this.HasVisibleItemSubject = new BehaviorSubject<bool>(false).AddTo(this.Disposables);

            var tagIds = Enum.GetValues(typeof(ExifTags)).Cast<ushort>().OrderBy(x => x).ToArray();

            this.tags = tagIds.Where(x => x > 0x1F && (x < 0x9C9B || x > 0x9C9F))
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
                .ToArray();
        }

        /// <summary>
        /// 辞書を初期化
        /// </summary>
        /// <param name="source"></param>
        public void SetSource(IEnumerable<ExifVisibilityItem> source)
        {
            if (source == null)
            {
                return;
            }

            var items = source
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

            this.tagVisibilityList = new ObservableCollection<ExifVisibilityItem>(this.tags.Select(x =>
                {
                    ExifVisibilityItem item = null;
                    if (!items.TryGetValue(x.Key, out item) || item == null)
                    {

                        item = new ExifVisibilityItem()
                        {
                            Id = x.Key,
                            IsEnabled = false,
                            Manager = this,
                        };
                        items[x.Key] = item;
                        this.AddedSubject.OnNext(item);
                    }

                    item.Name = x.Value;
                    return item;
                }));

        }
        

        /// <summary>
        /// 指定パスのファイルからExif情報を取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ExifInformation LoadExif(string path)
        {

            try
            {
                var props = new List<KeyValuePair<ExifVisibilityItem, string>>();

                using (var reader = new ExifReader(path))
                {
                    // Parse through all available fields and generate key-value labels
                    foreach(var item in this.tagVisibilityList)
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
                return new ExifInformation();
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

        /// <summary>
        /// 利用できるすべてのExif情報を可視化
        /// </summary>
        /// <param name="value"></param>
        public void EnableAll(bool value)
        {
            foreach(var item in this.tagVisibilityList)
            {
                item.IsEnabled = value;
            }
        }
    }
}
