using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace ImageLibrary.Exif
{
    /// <summary>
    /// 画像ファイルのExif情報
    /// </summary>
    public class ExifInformation
    {
        public ObservableCollection<KeyValuePair<ExifVisibilityItem, string>> Items { get; }

        public ExifInformation(IEnumerable<KeyValuePair<ExifVisibilityItem, string>> items)
        {
            this.Items = new ObservableCollection<KeyValuePair<ExifVisibilityItem, string>>(items);
        }
        public ExifInformation()
        {
            this.Items = new ObservableCollection<KeyValuePair<ExifVisibilityItem, string>>();
        }
    }
}
