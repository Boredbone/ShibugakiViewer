using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.IO;
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

                using (var reader = new ExifLib.ExifReader(path))
                {
                    // Parse through all available fields and generate key-value labels
                    foreach (var item in this.tagVisibilityList)
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
                                text = this.RenderTag(key, val);
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

        private string RenderTag(ushort id, object tagValue)
        {
            // Arrays don't render well without assistance.
            var array = tagValue as Array;
            if (array != null)
            {
                if (id >= (ushort)ExifTags.XPTitle && id <= (ushort)ExifTags.XPSubject)
                {
                    var data = array.OfType<byte>().ToArray();
                    var length = data.Length;
                    if (length >= 2 && data[length - 2] == 0 && data[length - 1] == 0)
                    {
                        data = data.Take(length - 2).ToArray();
                    }
                    try
                    {
                        return System.Text.Encoding.Unicode.GetString(data);
                    }
                    catch
                    {
                    }
                }
                // Hex rendering for really big byte arrays (ugly otherwise)
                if (array.Length > 20 && array.GetType().GetElementType() == typeof(byte))
                {
                    return "";// "0x" + string.Join("", array.Cast<byte>().Select(x => x.ToString("X2")).ToArray());
                }

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
            foreach (var item in this.tagVisibilityList)
            {
                item.IsEnabled = value;
            }
        }

        public static int GetOrientation(Stream stream)
        {
            if (stream == null || !stream.CanSeek || stream.Length < 34)
            {
                return -1;
            }
            try
            {
                bool isLittleEndian = false;
                using var reader = new BinaryReader(stream);

                {
                    var header = reader.ReadBytes(6);

                    if (header[0] != 0xFF || header[1] != 0xD8)
                    {
                        // Not JPEG
                        return -1;
                    }

                    // Get the next tag.
                    byte markerStart = header[2];
                    byte markerNumber = header[3];
                    int dataLength = (header[4] << 8) + header[5];

                    while (markerStart == 0xFF && markerNumber != 0xE1)
                    {
                        // Jump to the end of the data (note that the size field includes its own size)!
                        int offset = dataLength - 2;
                        long expectedPosition = stream.Position + offset;

                        if (expectedPosition > 0xffff)
                        {
                            return -1;
                        }

                        stream.Seek(offset, SeekOrigin.Current);

                        if (stream.Position != expectedPosition)
                        {
                            return -1;
                        }

                        var marker = reader.ReadBytes(4);

                        markerStart = marker[0];
                        markerNumber = marker[1];
                        dataLength = (marker[2] << 8) + marker[3];
                    }

                    // It's only success if we found the 0xFFE1 marker
                    if (markerStart != 0xFF || markerNumber != 0xE1)
                    {
                        return -1;
                    }
                }
                // We're now into the TIFF format
                var tiffHeaderStart = stream.Position + 6;
                {
                    var header = reader.ReadBytes(18);
                    if (header[0] != 'E' || header[1] != 'x' || header[2] != 'i' || header[3] != 'f'
                        || header[4] != 0 || header[5] != 0)
                    {
                        return -1;
                    }
                    isLittleEndian = (header[6] == 'I' && header[7] == 'I');

                    var isValid = ((isLittleEndian && header[9] == 0 && header[8] == 0x2A)
                        || (!isLittleEndian && header[8] == 0 && header[9] == 0x2A));
                    if (!isValid)
                    {
                        return -1;
                    }

                    var convertEndian = isLittleEndian != BitConverter.IsLittleEndian;

                    var ifdOffsetBin = header.AsSpan(10, 4);
                    if (convertEndian)
                    {
                        ifdOffsetBin.Reverse();
                    }
                    // Get the offset to the IFD (image file directory)
                    var ifdOffset = BitConverter.ToUInt32(ifdOffsetBin);

                    // Note that this offset is from the first byte of the TIFF header. Jump to the IFD.
                    if (ifdOffset > 8)
                    {
                        stream.Position = ifdOffset + tiffHeaderStart;
                    }

                    var tag = (int)ExifTags.Orientation;
                    var tagHigh = (tag >> 8) & 0xff;
                    var tagLow = tag & 0xff;

                    int entryCount = (isLittleEndian)
                        ? ((header[15] << 8) + header[14])
                        : ((header[14] << 8) + header[15]);

                    var currentTagHigh = (isLittleEndian) ? header[17] : header[16];
                    var currentTagLow = (isLittleEndian) ? header[16] : header[17];


                    bool isFound = false;
                    for (int currentEntry = 0; currentEntry < entryCount; currentEntry++)
                    {
                        if (currentTagHigh == tagHigh && currentTagLow == tagLow)
                        {
                            isFound = true;
                            break;
                        }
                        stream.Seek(10, SeekOrigin.Current);
                        var tagBin = reader.ReadBytes(2);

                        currentTagHigh = (isLittleEndian) ? tagBin[1] : tagBin[0];
                        currentTagLow = (isLittleEndian) ? tagBin[0] : tagBin[1];
                    }

                    if (!isFound)
                    {
                        return -1;
                    }

                    var dataBin = reader.ReadBytes(10);

                    var tiffDataTypeHigh = (isLittleEndian) ? dataBin[1] : dataBin[0];
                    if (tiffDataTypeHigh != 0)
                    {
                        return -1;
                    }
                    var tiffDataType = (isLittleEndian) ? dataBin[0] : dataBin[1];

                    int dataSize;

                    switch (tiffDataType)
                    {
                        case 1:
                        case 2:
                        case 7:
                        case 6:
                            dataSize = 1;
                            break;
                        case 3:
                        case 8:
                            dataSize = 2;
                            break;
                        case 4:
                        case 9:
                        case 11:
                            dataSize = 4;
                            break;
                        //case 5:
                        //case 10:
                        case 12:
                            dataSize = 8;
                            break;
                        default:
                            return -1;
                    }

                    if (dataSize <= 4 && tiffDataType != 11)
                    {
                        return (isLittleEndian) ? dataBin[6] : dataBin[5 + dataSize];
                    }

                    var offsetAddressBin = dataBin.AsSpan(6, 4);
                    if (convertEndian)
                    {
                        offsetAddressBin.Reverse();
                    }

                    var offsetAddress = BitConverter.ToUInt32(offsetAddressBin);
                    // Move to the TIFF offset and retrieve the data
                    stream.Seek(offsetAddress + tiffHeaderStart, SeekOrigin.Begin);

                    var numBin = reader.ReadBytes(dataSize).AsSpan();
                    if (convertEndian)
                    {
                        numBin.Reverse();
                    }
                    if (tiffDataType == 11)
                    {
                        return (int)BitConverter.ToSingle(numBin);
                    }
                    else if (tiffDataType == 12)
                    {
                        return (int)BitConverter.ToDouble(numBin);
                    }
                }
            }
            catch
            {
            }
            return -1;
        }
    }
}
