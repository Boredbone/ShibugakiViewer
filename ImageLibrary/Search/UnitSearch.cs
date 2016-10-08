using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using Database.Search;
using ImageLibrary.Core;
using ImageLibrary.SearchProperty;

namespace ImageLibrary.Search
{

    [DataContract]
    public class UnitSearch : ISqlSearch
    {
        [DataMember]
        public CompareMode Mode
        {
            get { return _fieldMode; }
            set
            {
                if (_fieldMode != value)
                {
                    _fieldMode = value;
                    RaisePropertyChanged(nameof(Mode));
                }
            }
        }
        private CompareMode _fieldMode;

        [DataMember]
        public object Reference
        {
            get { return _fieldReference; }
            set
            {
                if (_fieldReference != value)
                {
                    _fieldReference = value;
                    RaisePropertyChanged(nameof(Reference));
                }
            }
        }
        private object _fieldReference;


        [DataMember]
        public FileProperty Property
        {
            get { return _fieldProperty; }
            set
            {
                if (_fieldProperty != value)
                {
                    _fieldProperty = value;
                    RaisePropertyChanged(nameof(Property));
                }
            }
        }
        private FileProperty _fieldProperty;

        public INotifyCollectionChanged Children => null;

        public bool IsUnit => true;

        public ComplexSearch Parent { get; set; }

        public string ReferenceLabel
        {
            get
            {
                if (this._fieldReferenceLabel == null)
                {
                    this.RefreshReferenceLabel();
                }
                return _fieldReferenceLabel;
            }
            private set
            {
                if (_fieldReferenceLabel != value && value != null)
                {
                    _fieldReferenceLabel = value;
                    RaisePropertyChanged(nameof(ReferenceLabel));
                }
            }
        }
        private string _fieldReferenceLabel;



        public UnitSearch()
        {
        }

        public string ToSql()
        {
            return this.Property.ToSearch(this.Reference, this.Mode);
        }


        public void RefreshReferenceLabel()
        {
            if (this.Reference != null)
            {
                if (this.Property == FileProperty.ContainsTag)
                {
                    this.ReferenceLabel = LibraryOwner.GetCurrent()
                        .Tags.GetTagValue((int)this.Reference)?.Name;
                }
                else if (this.Property.IsDate())
                {
                    if (this.Property == FileProperty.DateTimeCreated
                        || this.Property == FileProperty.DateTimeModified
                        || this.Property == FileProperty.DateTimeRegistered)
                    {
                        this.ReferenceLabel = ((DateTimeOffset)this.Reference).ToString("G");
                    }
                    else
                    {
                        this.ReferenceLabel = ((DateTimeOffset)this.Reference).ToString("d");
                    }
                }
                else if (this.Property == FileProperty.Size)
                {
                    this.ReferenceLabel = FileSizeConverter.ConvertAuto((long)this.Reference);
                }
                else
                {
                    this.ReferenceLabel = this.Reference.ToString();
                }

            }
        }

        public void RemoveSelf()
        {
            this.Parent?.Remove(this);
        }



        public void CopyFrom(UnitSearch source)
        {
            this.Property = source.Property;
            this.Mode = source.Mode;

            this.Reference = source.Reference;

            this._fieldReferenceLabel = null;
        }


        public ISqlSearch Clone()
        {
            var clone = new UnitSearch();
            clone.CopyFrom(this);
            return clone;
        }


        public bool ValueEquals(ISqlSearch other)
        {
            if (this.IsUnit != other.IsUnit)
            {
                return false;
            }
            var ot = (UnitSearch)other;

            return this.Property == ot.Property
                && this.Mode == ot.Mode
                && this.ReferenceLabel.Equals(ot.ReferenceLabel);
        }
        
        public override string ToString()
        {
            return this.Property.ToString() + "," + this.Mode.ToString() + "," + this.ReferenceLabel;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
