using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database.Table;

namespace ImageLibrary.Tag
{

    public class TagInformation : INotifyPropertyChanged, IRecord<int>, ITrackable
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
        public string Name
        {
            get { return _fieldName; }
            set
            {
                if (_fieldName != value)
                {
                    _fieldName = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }
        private string _fieldName;

        [RecordMember]
        public string Shortcut
        {
            get { return _fieldShortcut; }
            set
            {
                if (_fieldShortcut != value)
                {
                    _fieldShortcut = value;
                    RaisePropertyChanged(nameof(Shortcut));
                }
            }
        }
        private string _fieldShortcut;

        [RecordMember]
        public bool IsIgnored
        {
            get { return _fieldIsIgnored; }
            set
            {
                if (_fieldIsIgnored != value)
                {
                    _fieldIsIgnored = value;
                    RaisePropertyChanged(nameof(IsIgnored));
                }
            }
        }
        private bool _fieldIsIgnored;

        [RecordMember]
        public DateTimeOffset LastUsed
        {
            get { return _fieldLastUsed; }
            set
            {
                if (_fieldLastUsed != value)
                {
                    _fieldLastUsed = value;
                    RaisePropertyChanged(nameof(LastUsed));
                }
            }
        }
        private DateTimeOffset _fieldLastUsed;

        public bool IsOpen
        {
            get { return _fieldIsOpen; }
            set
            {
                if (_fieldIsOpen != value)
                {
                    _fieldIsOpen = value;
                    RaisePropertyChanged(nameof(IsOpen));
                }
            }
        }
        private bool _fieldIsOpen;



        public bool IsLoaded { get; set; } = false;


        public TagInformation()
        {
            Name = "";
            Shortcut = "";
        }

        public void CopyFrom(TagInformation other)
        {
            this.Name = other.Name;
            this.Shortcut = other.Shortcut;
            this.IsIgnored = other.IsIgnored;
            this.LastUsed = other.LastUsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
