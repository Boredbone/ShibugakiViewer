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
                    this.IsEdited = true;
                    RaisePropertyChanged(nameof(Mode));
                }
            }
        }
        private CompareMode _fieldMode;

        [DataMember(EmitDefaultValue = false)]
        [Obsolete]
        public object? Reference
        {
            get { return _fieldReference; }
            set
            {
                if (_fieldReference != value)
                {
                    _fieldReference = value;
                    this.IsEdited = true;
                    //RaisePropertyChanged(nameof(Reference));
                }
            }
        }
        private object? _fieldReference;

        [DataMember(EmitDefaultValue = false)]
        public SearchReferences? SearchReference
        {
            get { return _fieldSearchReference; }
            set
            {
                if (_fieldSearchReference != value)
                {
                    _fieldSearchReference = value;
                    this.IsEdited = true;
                }
            }
        }
        private SearchReferences? _fieldSearchReference = null;



        [DataMember]
        public FileProperty Property
        {
            get { return _fieldProperty; }
            set
            {
                if (_fieldProperty != value)
                {
                    _fieldProperty = value;
                    this.IsEdited = true;
                    RaisePropertyChanged(nameof(Property));
                }
            }
        }
        private FileProperty _fieldProperty;

        public INotifyCollectionChanged? Children => null;

        public bool IsUnit => true;

        public ComplexSearch? Parent { get; set; }

        public string? ReferenceLabel
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
        private string? _fieldReferenceLabel;

        public bool IsEdited { get; private set; }


        public UnitSearch()
        {
        }

        public IDatabaseExpression? ToSql()
        {
            return (this.SearchReference != null)
                ? this.Property.ToSearch(this.SearchReference, this.Mode)
                : null;
        }


        public void RefreshReferenceLabel()
        {
            if (this.SearchReference != null)
            {
                if (this.Property == FileProperty.ContainsTag)
                {
                    this.ReferenceLabel = LibraryOwner.GetCurrent()
                        .Tags.GetTagValue(this.SearchReference.Num32)?.Name;
                }
                else if (this.Property.IsDate())
                {
                    if (this.Property.IsDateTime())
                    {
                        this.ReferenceLabel = this.SearchReference.DateTime.ToString("G");
                    }
                    else
                    {
                        this.ReferenceLabel = this.SearchReference.DateTime.ToString("d");
                    }
                }
                else if (this.Property == FileProperty.Size)
                {
                    this.ReferenceLabel = FileSizeConverter.ConvertAuto(this.SearchReference.Num64);
                }
                else
                {
                    this.ReferenceLabel = this.SearchReference.ToString();
                }
            }
        }

        public void RemoveSelf()
        {
            this.Parent?.Remove(this);
        }

        public void Migrate()
        {
#pragma warning disable CS0612
            if (this.Reference != null)
            {
                this.SearchReference = SearchReferences.ConvertFrom(this.Reference);
                this.Reference = null;
            }
#pragma warning restore CS0612
        }


        public void CopyFrom(UnitSearch source)
        {
            this.Property = source.Property;
            this.Mode = source.Mode;

#pragma warning disable CS0612
            this.Reference = source.Reference;
#pragma warning restore CS0612
            this.SearchReference = source.SearchReference;

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
                && this.ReferenceLabel != null
                && this.ReferenceLabel.Equals(ot.ReferenceLabel);
        }

        public void DownEdited()
        {
            this.IsEdited = false;
        }


        public override string ToString()
        {
            return this.Property.ToString() + "," + this.Mode.ToString() + "," + this.ReferenceLabel;
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
