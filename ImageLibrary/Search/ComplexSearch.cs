using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using ImageLibrary.SearchProperty;

namespace ImageLibrary.Search
{
    /// <summary>
    /// AND,OR検索
    /// </summary>
    [DataContract]
    public class ComplexSearch : ISqlSearch
    {
        public INotifyCollectionChanged Children => InnerChildren;

        private ObservableCollection<ISqlSearch> InnerChildren
        {
            get { return _fieldInnerChildren; }
            set
            {
                if (_fieldInnerChildren != value)
                {
                    _fieldInnerChildren = value;
                    value?.ForEach(x => x.Parent = this);
                }
            }
        }
        private ObservableCollection<ISqlSearch> _fieldInnerChildren;
        
        public ComplexSearch Parent { get; set; }

        [DataMember]
        private List<ISqlSearch> SavedChildren
        {
            get { return this.InnerChildren.ToList(); }
            set
            {
                this.InnerChildren = new ObservableCollection<ISqlSearch>(value);
            }
        }

        [DataMember]
        public bool IsOr
        {
            get { return _fieldIsOr; }
            set
            {
                if (_fieldIsOr != value)
                {
                    _fieldIsOr = value;
                    RaisePropertyChanged(nameof(IsOr));
                }
            }
        }
        private bool _fieldIsOr;
        
        public bool IsUnit => false;

        public string ReferenceLabel { get; } = "";

        public ComplexSearch(bool or)
        {
            this.Initialize();

            this.IsOr = or;
        }
        private void Initialize()
        {
            this.InnerChildren = new ObservableCollection<ISqlSearch>();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.Initialize();
        }


        public ComplexSearch Add(ISqlSearch method)
        {
            this.InnerChildren.Add(method);
            method.Parent = this;
            return this;
        }

        public string ToSql()
        {
            var items = this.InnerChildren
                .Select(x => x.ToSql())
                .Where(x => x != null)
                .ToArray();

            if (items.Length <= 0)
            {
                return null;
            }

            return (this.IsOr)
                ? DatabaseFunction.Or(items)
                : DatabaseFunction.And(items);
        }

        public void Remove(ISqlSearch item)
        {
            if (this.InnerChildren.Contains(item))
            {
                this.InnerChildren.Remove(item);
            }
        }
        public void RemoveSelf()
        {
            this.Parent?.Remove(this);
        }


        public void CopyFrom(ComplexSearch source)
        {
            this.InnerChildren = new ObservableCollection<ISqlSearch>
                (source.InnerChildren.Select(x => x.Clone()));
            this.IsOr = source.IsOr;

        }

        public ISqlSearch Clone()
        {
            var clone = new ComplexSearch(this.IsOr);
            clone.CopyFrom(this);
            return clone;
        }

        public bool ValueEquals(ISqlSearch other)
        {
            if (this.IsUnit != other.IsUnit)
            {
                return false;
            }
            var ot = (ComplexSearch)other;

            return this.IsOr == ot.IsOr
                && this.InnerChildren.SequenceEqual(ot.InnerChildren, (x, y) => x.ValueEquals(y));

        }

        public override string ToString()
        {
            return ((this.IsOr) ? "OR" : "AND") + "," + this.InnerChildren.Count.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
