using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using Database.Table;

namespace ImageLibrary.Creation
{
    [DataContract]
    public class FolderInformation : INotifyPropertyChanged, IRecord<int>, ITrackable
    {

        [RecordMember]
        public int Id
        {
            get { return _fieldId; }
            private set
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
        [DataMember]
        public string Path
        {
            get { return _fieldPath; }
            set
            {
                if (_fieldPath != value)
                {
                    _fieldPath = value;
                    RaisePropertyChanged(nameof(Path));
                }
            }
        }
        private string _fieldPath;
        
        [RecordMember]
        [DataMember]
        public bool AutoRefreshEnable
        {
            get { return _fieldAutoRefreshEnable; }
            set
            {
                if (_fieldAutoRefreshEnable != value)
                {
                    _fieldAutoRefreshEnable = value;
                    RaisePropertyChanged(nameof(AutoRefreshEnable));
                }
            }
        }
        private bool _fieldAutoRefreshEnable;
        
        [RecordMember]
        [DataMember]
        public bool Ignored
        {
            get { return _fieldIgnored; }
            set
            {
                if (_fieldIgnored != value)
                {
                    _fieldIgnored = value;
                    RaisePropertyChanged(nameof(Ignored));
                }
            }
        }
        private bool _fieldIgnored;

        [RecordMember]
        [DataMember]
        public bool WatchChange
        {
            get { return _fieldWatchChange; }
            set
            {
                if (_fieldWatchChange != value)
                {
                    _fieldWatchChange = value;
                    RaisePropertyChanged(nameof(WatchChange));
                }
            }
        }
        private bool _fieldWatchChange;

        [RecordMember]
        [DataMember]
        public bool IsTopDirectoryOnly
        {
            get { return _fieldIsTopDirectoryOnly; }
            set
            {
                if (_fieldIsTopDirectoryOnly != value)
                {
                    _fieldIsTopDirectoryOnly = value;
                    RaisePropertyChanged(nameof(IsTopDirectoryOnly));
                }
            }
        }
        private bool _fieldIsTopDirectoryOnly;

        [RecordMember]
        public int ModeEntry
        {
            get { return _fieldModeEntry; }
            private set
            {
                if (_fieldModeEntry != value)
                {
                    _fieldModeEntry = value;
                    RaisePropertyChanged(nameof(ModeEntry));
                }
            }
        }
        private int _fieldModeEntry;

        [DataMember]
        public FolderCheckMode Mode
        {
            get { return (FolderCheckMode)this.ModeEntry; }
            set
            {
                var num = (int)value;
                if (this.ModeEntry != num)
                {
                    this.ModeEntry = num;
                    RaisePropertyChanged(nameof(Mode));
                }
            }
        }

        public int Count
        {
            get { return _fieldCount; }
            set
            {
                if (_fieldCount != value)
                {
                    _fieldCount = value;
                    RaisePropertyChanged(nameof(Count));
                }
            }
        }
        private int _fieldCount;


        public bool RefreshEnable { get; set; }

        public bool RefreshTrigger
        {
            get { return _fieldRefreshTrigger; }
            set
            {
                if (_fieldRefreshTrigger != value)
                {
                    _fieldRefreshTrigger = value;
                    RaisePropertyChanged(nameof(RefreshTrigger));
                }
            }
        }
        private bool _fieldRefreshTrigger;
        
        

        public bool IsLoaded { get; set; }
        

        public FolderInformation()
        {
        }

        public FolderInformation(string path)
        {
            this.Path = path;
            this.Ignored = false;

            this.RefreshEnable = true;
            this.AutoRefreshEnable = true;
            this.WatchChange = true;
        }

        public static IEnumerable<FolderInformation> GetSpecialFolders()
        {
            var folders = new[]
            {
                Environment.SpecialFolder.CommonPictures,
                Environment.SpecialFolder.MyPictures,
            };

            return folders.Select(x =>
            {
                var path = System.Environment.GetFolderPath(x);

                return new FolderInformation(path)
                {
                    AutoRefreshEnable = true,
                    Ignored = false,
                    RefreshEnable = true,
                    RefreshTrigger = false,
                };
            })
            .Where(x => x.Path != null && x.Path.Length > 0);

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum FolderCheckMode : int
    {
        None = 0,
        Light = 1,
        Detail = 2,
    }
}
