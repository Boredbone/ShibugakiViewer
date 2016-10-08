using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database.Table;

namespace ImageLibrary.Exif
{
    /// <summary>
    /// Exif情報を表示するかを設定
    /// </summary>
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
}
