using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;
using SerializerProtocol;

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


        public bool SetSort(IEnumerable<SortSetting> source, bool replaceDefaultSort)
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
            if (replaceDefaultSort)
            {
                LibraryOwner.GetCurrent().Searcher.SetDefaultSort(source);
            }
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

        private static ISqlSearch SearchNodeFromSerializable(SearchNodeSerializable obj)
        {
            if (obj.Property < 0)
            {
                var sc = new ComplexSearch(obj.IsOr);
                if (obj.Children != null)
                {
                    foreach (var item in obj.Children)
                    {
                        var ch = SearchNodeFromSerializable(item);
                        sc.Add(ch);
                    }
                }
                return sc;
            }
            else
            {
                var prop = (FileProperty)obj.Property;

                if (prop.IsDate())
                {
                    if (DateTimeOffset.TryParse(obj.Reference.ToString(), out var datetime))
                    {
                        return new UnitSearch()
                        {
                            Mode = (CompareMode)obj.Mode,
                            Reference = datetime,
                            Property = prop,
                        };
                    }
                }
                if(obj.Reference is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.String)
                    {
                        return new UnitSearch()
                        {
                            Mode = (CompareMode)obj.Mode,
                            Reference = je.ToString(),
                            Property = prop,
                        };
                    }
                    else if (je.ValueKind == JsonValueKind.Number)
                    {
                        if (prop.IsFloat())
                        {
                            if (je.TryGetDouble(out var num))
                            {
                                return new UnitSearch()
                                {
                                    Mode = (CompareMode)obj.Mode,
                                    Reference = num,
                                    Property = prop,
                                };
                            }
                        }
                        else
                        {
                            if (je.TryGetInt32(out var num))
                            {
                                return new UnitSearch()
                                {
                                    Mode = (CompareMode)obj.Mode,
                                    Reference = num,
                                    Property = prop,
                                };
                            }
                        }
                    }
                    else if (je.ValueKind == JsonValueKind.True)
                    {
                        return new UnitSearch()
                        {
                            Mode = (CompareMode)obj.Mode,
                            Reference = true,
                            Property = prop,
                        };
                    }
                    else if (je.ValueKind == JsonValueKind.False)
                    {
                        return new UnitSearch()
                        {
                            Mode = (CompareMode)obj.Mode,
                            Reference = false,
                            Property = prop,
                        };
                    }
                    else if (je.ValueKind == JsonValueKind.Null)
                    {
                        return new UnitSearch()
                        {
                            Mode = (CompareMode)obj.Mode,
                            Reference = null,
                            Property = prop,
                        };
                    }
                }
                //Debug.WriteLine(obj.Reference.GetType());
                return new UnitSearch()
                {
                    Mode = (CompareMode)obj.Mode,
                    Reference = obj.Reference.ToString(),
                    Property = prop,
                };
            }
        }
        public static SearchInformation FromSerializable(SearchInformationSerializable obj)
        {
            return new SearchInformation((ComplexSearch)SearchNodeFromSerializable(obj.Root))
            {
                SortSettings = obj.SortSettings.Select(x => SortSetting.FromSerializableObject(x)).ToList(),
                ThumbnailFilePath = System.Web.HttpUtility.UrlDecode(obj.ThumbnailId),
                Name = obj.Name,
            };
        }
        private static SearchNodeSerializable SearchNodeToSerializable(ISqlSearch obj)
        {
            if (obj is ComplexSearch cmp)
            {
                return new SearchNodeSerializable()
                {
                    IsOr = cmp.IsOr,
                    Children = cmp.Convert(SearchNodeToSerializable).ToList(),
                    Property=-1,
                };
            }
            else if (obj is UnitSearch unit)
            {
                return new SearchNodeSerializable()
                {
                    Mode = (int)unit.Mode,
                    Reference = unit.Reference,
                    Property = (int)unit.Property,
                };
            }
            return null;
        }
        public SearchInformationSerializable ToSerializable()
        {
            return new SearchInformationSerializable()
            {
                Root = SearchNodeToSerializable(this.Root),
                SortSettings = this.SortSettings.Select(x => x.ToSerializableObject()).ToList(),
                Name = this.Name,
                ThumbnailId = System.Web.HttpUtility.UrlEncode(this.ThumbnailFilePath),
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


        public long Count(Library library)
            => library.RecordQuery.Count(this);

        public Record[] Search(Library library, long skip, long take, Record skipUntil = null)
            => library.RecordQuery.Search(this, skip, take, skipUntil);

        public static SearchInformation GenerateEmpty() => new SearchInformation(new ComplexSearch(false));


        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
}
