using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.File;

namespace ImageLibrary.Search
{
    /// <summary>
    /// 検索設定
    /// </summary>
    [DataContract]
    public class SearchInformation : INotifyPropertyChanged, ISearchCriteria
    {
        [DataMember]
        public ComplexSearch Root { get; private set; }

        [DataMember]
        public DateTimeOffset DateLastUsed
        {
            get { return _fieldDateLastUsed; }
            set
            {
                if (_fieldDateLastUsed != value)
                {
                    _fieldDateLastUsed = value;
                    RaisePropertyChanged(nameof(DateLastUsed));
                }
            }
        }
        private DateTimeOffset _fieldDateLastUsed;


        [DataMember]
        private List<SortSetting> SortSettings
        {
            get { return _fieldSortSettings; }
            set
            {
                if (_fieldSortSettings != value)
                {
                    _fieldSortSettings = value;
                    RaisePropertyChanged(nameof(SortSettings));
                    RaisePropertyChanged(nameof(SortEntry));
                }
            }
        }
        private List<SortSetting> _fieldSortSettings = null;


        [RecordMember]
        public string SortEntry
        {
            get { return SortHelper.ToEntry(this.SortSettings); }
            private set
            {
                this.SortSettings = SortHelper.FromEntry(value);
            }
        }


        [DataMember]
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


        [DataMember]
        public string ThumbnailFilePath
        {
            get { return _fieldThumbnailFilePath; }
            set
            {
                if (_fieldThumbnailFilePath != value)
                {
                    _fieldThumbnailFilePath = value;
                    RaisePropertyChanged(nameof(ThumbnailFilePath));
                }
            }
        }
        private string _fieldThumbnailFilePath;

        



        public string Key { get; set; }


        public SearchInformation(ComplexSearch root)
        {
            this.Root = root;
            this.SortSettings = new List<SortSetting>();
        }

        public IDatabaseExpression GetWhereSql()
        {
            return this.Root.ToSql();
        }
        

        public bool SetSort(IEnumerable<SortSetting> source)
        {
            if (this.GetSort().SequenceEqual(source, (x, y) => x.Equals(y)))
            {
                if (this.SortSettings.IsNullOrEmpty())
                {
                    this.SortSettings = source.ToList();
                }
                return false;
            }
            this.SortSettings = source.ToList();
            LibraryOwner.GetCurrent().Searcher.SetDefaultSort(source);
            return true;
        }

        public IEnumerable<SortSetting> GetSort()
        {
            if (this.SortSettings.IsNullOrEmpty())
            {
                return LibraryOwner.GetCurrent().Searcher.GetDefaultSort();
            }
            return this.SortSettings.Select(x => x.Clone());
        }

        public SearchInformation Clone()
        {
            return new SearchInformation((ComplexSearch)this.Root.Clone())
            {
                DateLastUsed = this.DateLastUsed,
                SortSettings = new List<SortSetting>(this.SortSettings.Select(x => x.Clone())),
                ThumbnailFilePath = this.ThumbnailFilePath,
                Key = this.Key,
                Name = this.Name,
            };
        }

        public bool HasSameSearch(SearchInformation other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Root.ValueEquals(other.Root);
        }

        public bool HasSameSort(SearchInformation other)
        {
            if (other == null)
            {
                return false;
            }
            return this.SortSettings.SequenceEqual(other.SortSettings, (x, y) => x.ValueEquals(y));
        }

        public bool SettingEquals(SearchInformation other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            return this.HasSameSearch(other) && this.HasSameSort(other);
        }

        public bool CheckSimilarity(SearchInformation other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return !this.Root.IsEdited;
            }
            return this.HasSameSearch(other) && this.HasSameSort(other);
        }


        public bool ValueEquals(SearchInformation other)
        {
            if (this.Name != other.Name
                || this.DateLastUsed != other.DateLastUsed
                || this.ThumbnailFilePath != other.ThumbnailFilePath)
            {
                return false;
            }

            return this.SettingEquals(other);
        }

        public void SetDateToNow()
        {
            this.DateLastUsed = DateTimeOffset.Now;
        }

        public void SetNewKey()
        {
            this.Key = Guid.NewGuid().ToString();
        }


        public Task<long> CountAsync(Library library)
            => library.RecordQuery.CountAsync(this);

        public IDatabaseExpression GetFilterString(Library library)
            => library.RecordQuery.GetFilterString(this);

        public Task<Record[]> SearchAsync(Library library, long skip, long take, Record skipUntil = null)
            => library.RecordQuery.SearchAsync(this, skip, take, skipUntil);
        

        public static SearchInformation GenerateEmpty() => new SearchInformation(new ComplexSearch(false));


        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
}
