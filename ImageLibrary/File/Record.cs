using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.Exif;
using ImageLibrary.Search;
using ImageLibrary.Tag;

namespace ImageLibrary.File
{
    /// <summary>
    /// データベースに保存されるファイルまたはグループの情報
    /// </summary>
    [DataContract]
    public class Record : INotifyPropertyChanged, IRecord<string>, ITrackable, ISearchCriteria
    {

        [RecordMember]
        public string Id
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
        private string _fieldId;


        [DataMember]
        public string FullPath
        {
            get
            {
                if (this.IsGroup)
                {
                    return this.GroupKey;
                }
                if (this._fieldFullPath == null)
                {
                    this._fieldFullPath = System.IO.Path.Combine
                        (this.Directory ?? "", this.FileName ?? "");
                }
                return _fieldFullPath;
            }
            private set
            {
                if (_fieldFullPath != value)
                {
                    _fieldFullPath = value;
                    RaisePropertyChanged(nameof(FullPath));
                }
            }
        }
        private string _fieldFullPath;


        [RecordMember]
        public string Directory
        {
            get { return _fieldDirectory; }
            private set
            {
                if (_fieldDirectory != value)
                {
                    _fieldDirectory = value;
                    RaisePropertyChanged(nameof(Directory));
                }
            }
        }
        private string _fieldDirectory;


        [RecordMember]
        public string FileName
        {
            get { return _fieldFileName; }
            private set
            {
                if (_fieldFileName != value)
                {
                    _fieldFileName = value;
                    RaisePropertyChanged(nameof(FileName));
                }
            }
        }
        private string _fieldFileName;



        [RecordMember]
        [DataMember]
        public DateTimeOffset DateModified
        {
            get { return _fieldDateModified; }
            set
            {
                if (_fieldDateModified != value)
                {
                    _fieldDateModified = value;
                    RaisePropertyChanged(nameof(DateModified));
                }
            }
        }
        private DateTimeOffset _fieldDateModified = UnixTime.DefaultDateTimeOffsetLocal;


        [RecordMember]
        [DataMember]
        public DateTimeOffset DateCreated
        {
            get { return _fieldDateCreated; }
            set
            {
                if (_fieldDateCreated != value)
                {
                    _fieldDateCreated = value;
                    RaisePropertyChanged(nameof(DateCreated));
                }
            }
        }
        private DateTimeOffset _fieldDateCreated = UnixTime.DefaultDateTimeOffsetLocal;


        [RecordMember]
        [DataMember]
        public DateTimeOffset DateRegistered
        {
            get { return _fieldDateRegistered; }
            set
            {
                if (_fieldDateRegistered != value)
                {
                    _fieldDateRegistered = value;
                    RaisePropertyChanged(nameof(DateRegistered));
                }
            }
        }
        private DateTimeOffset _fieldDateRegistered = UnixTime.DefaultDateTimeOffsetLocal;



        [RecordMember]
        [DataMember]
        public int Width
        {
            get { return _fieldWidth; }
            set
            {
                if (_fieldWidth != value)
                {
                    _fieldWidth = value;
                    RaisePropertyChanged(nameof(Width));
                }
            }
        }
        private int _fieldWidth;

        [RecordMember]
        [DataMember]
        public int Height
        {
            get { return _fieldHeight; }
            set
            {
                if (_fieldHeight != value)
                {
                    _fieldHeight = value;
                    RaisePropertyChanged(nameof(Height));
                }
            }
        }
        private int _fieldHeight;

        [RecordMember]
        [DataMember]
        public long Size
        {
            get { return _fieldSize; }
            set
            {
                if (_fieldSize != value)
                {
                    _fieldSize = value;
                    RaisePropertyChanged(nameof(Size));
                }
            }
        }
        private long _fieldSize;

        

        [RecordMember]
        public string TagEntry
        {
            get { return this.TagSet.ToEntry(); }
            private set
            {
                if (this._tagEntry != value)
                {
                    this._tagEntry = value;
                    this._fieldTagSet = null;
                }
            }
        }
        private string _tagEntry;

        [DataMember]
        public TagManager TagSet
        {
            get
            {
                if (this._fieldTagSet == null)
                {
                    this._fieldTagSet = new TagManager(this._tagEntry);

                    this._fieldTagSet.CollectionChanged += (o, e) =>
                        RaisePropertyChanged(nameof(TagEntry));
                }
                return _fieldTagSet;
            }
        }
        private TagManager _fieldTagSet;


        [RecordMember]
        [DataMember]
        public int Rating
        {
            get { return this._rating; }
            set
            {
                if (_rating != value)
                {
                    this._rating = value;
                    RaisePropertyChanged(nameof(Rating));
                }
            }
        }
        private int _rating;


        [RecordMember]
        [DataMember]
        public bool IsNotFound
        {
            get { return _fieldIsNotFound; }
            set
            {
                if (_fieldIsNotFound != value)
                {
                    _fieldIsNotFound = value;
                    RaisePropertyChanged(nameof(IsNotFound));
                }
            }
        }
        private bool _fieldIsNotFound;




        #region Group



        [RecordMember]
        [DataMember]
        public string GroupKey
        {
            get { return _fieldGroupKey; }
            private set
            {
                if (_fieldGroupKey != value)
                {
                    _fieldGroupKey = value;
                    RaisePropertyChanged(nameof(GroupKey));
                }
            }
        }
        private string _fieldGroupKey;


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
            private set { this.SortSettings = SortHelper.FromEntry(value); }
        }

        

        [RecordMember]
        [DataMember]
        public int FlipDirection
        {
            get { return _fieldFlipDirection; }
            set
            {
                if (_fieldFlipDirection != value)
                {
                    _fieldFlipDirection = value;
                    RaisePropertyChanged(nameof(FlipDirection));
                }
            }
        }
        private int _fieldFlipDirection;



        [RecordMember]
        [DataMember]
        public bool IsGroup
        {
            get { return _fieldIsGroup; }
            private set
            {
                if (_fieldIsGroup != value)
                {
                    _fieldIsGroup = value;
                    RaisePropertyChanged(nameof(IsGroup));
                }
            }
        }
        private bool _fieldIsGroup;


        public long MemberCount
        {
            get
            {
                if (!this.IsGroup)
                {
                    return 0;
                }

                if (this._fieldMemberCount >= 0)
                {
                    return this._fieldMemberCount;
                }


                var context = SynchronizationContext.Current;
                Task.Run(async () =>
                {
                    this._fieldMemberCount
                        = await LibraryOwner.GetCurrent().GroupQuery.CountAsync(this);
                }).ContinueWith(t =>
                {
                    context?.Post(state =>
                    {
                        RaisePropertyChanged(nameof(this.MemberCount));
                    }, null);
                });

                return 0;
            }
        }
        private long _fieldMemberCount = -1;



        #endregion


        #region for sequence number sort

        [RecordMember]
        public string PreNameLong
        {
            get { return _fieldPreNameLong; }
            private set
            {
                if (_fieldPreNameLong != value)
                {
                    _fieldPreNameLong = value;
                    RaisePropertyChanged(nameof(PreNameLong));
                }
            }
        }
        private string _fieldPreNameLong;

        [RecordMember]
        public string PostNameShort
        {
            get { return _fieldPostNameShort; }
            private set
            {
                if (_fieldPostNameShort != value)
                {
                    _fieldPostNameShort = value;
                    RaisePropertyChanged(nameof(PostNameShort));
                }
            }
        }
        private string _fieldPostNameShort;

        [RecordMember]
        public int NameNumberRight
        {
            get { return _fieldNameNumberRight; }
            private set
            {
                if (_fieldNameNumberRight != value)
                {
                    _fieldNameNumberRight = value;
                    RaisePropertyChanged(nameof(NameNumberRight));
                }
            }
        }
        private int _fieldNameNumberRight;

        [RecordMember]
        public string PreNameShort
        {
            get { return _fieldPreNameShort; }
            private set
            {
                if (_fieldPreNameShort != value)
                {
                    _fieldPreNameShort = value;
                    RaisePropertyChanged(nameof(PreNameShort));
                }
            }
        }
        private string _fieldPreNameShort;

        [RecordMember]
        public string PostNameLong
        {
            get { return _fieldPostNameLong; }
            private set
            {
                if (_fieldPostNameLong != value)
                {
                    _fieldPostNameLong = value;
                    RaisePropertyChanged(nameof(PostNameLong));
                }
            }
        }
        private string _fieldPostNameLong;

        [RecordMember]
        public int NameNumberLeft
        {
            get { return _fieldNameNumberLeft; }
            private set
            {
                if (_fieldNameNumberLeft != value)
                {
                    _fieldNameNumberLeft = value;
                    RaisePropertyChanged(nameof(NameNumberLeft));
                }
            }
        }
        private int _fieldNameNumberLeft;

        [RecordMember]
        public string Extension
        {
            get { return _fieldExtension; }
            private set
            {
                if (_fieldExtension != value)
                {
                    _fieldExtension = value;
                    RaisePropertyChanged(nameof(Extension));
                }
            }
        }
        private string _fieldExtension;

        [RecordMember]
        public int NameLength
        {
            get { return _fieldNameLength; }
            private set
            {
                if (_fieldNameLength != value)
                {
                    _fieldNameLength = value;
                    RaisePropertyChanged(nameof(NameLength));
                }
            }
        }
        private int _fieldNameLength;


        #endregion

        public ExifInformation Exif
        {
            get { return this.IsGroup ? null : _fieldExif; }
            set
            {
                if (_fieldExif != value && !this.IsGroup)
                {
                    _fieldExif = value;
                    RaisePropertyChanged(nameof(Exif));
                }
            }
        }
        private ExifInformation _fieldExif;




        bool ITrackable.IsLoaded { get; set; } = false;

        public static Record Empty { get; } = new Record("");

        /// <summary>
        /// データベースからの取り出し用
        /// </summary>
        public Record()
        {

        }

        /// <summary>
        /// パスを指定してファイルデータを作成
        /// </summary>
        /// <param name="fullPath"></param>
        public Record(string fullPath)
        {
            this.Id = fullPath;
            this.FullPath = fullPath;

            try
            {
                this.SetName(System.IO.Path.GetFileName(fullPath));
            }
            catch (ArgumentException)
            {
                this.Id = "";
                this.FullPath = "";
                this.SetName("");
                this.Directory = "";
                return;
            }
            
            if (fullPath.IsNullOrEmpty())
            {
                this.Directory = "";
            }
            else
            {
                this.Directory = PathUtility.WithPostSeparator
                    (System.IO.Path.GetDirectoryName(fullPath));
            }

        }

        /// <summary>
        /// ファイル・グループの名前を変更
        /// </summary>
        /// <param name="name"></param>
        public void SetName(string name)
        {
            this.FileName = name;
            this._fieldFullPath = null;

            var sn = new SequenceNumber(this.FileName);
            this.PreNameLong = sn.PreNameLong;
            this.PostNameShort = sn.PostNameShort;
            this.NameNumberRight = sn.NameNumberRight;
            this.PreNameShort = sn.PreNameShort;
            this.PostNameLong = sn.PostNameLong;
            this.NameNumberLeft = sn.NameNumberLeft;
            this.Extension = sn.Extension;
            this.NameLength = sn.NameLength;
        }





        #region copy

        /// <summary>
        /// ファイル情報をコピー
        /// </summary>
        /// <param name="source"></param>
        private void CopyCoreInformation(Record source)
        {
            this.Id = source.Id;
            this.FullPath = source.FullPath;
            
            this.Directory = source.Directory;
            this.SetName(source.FileName);

            this.DateModified = source.DateModified;
            this.DateCreated = source.DateCreated;

            this.Width = source.Width;
            this.Height = source.Height;
            this.Size = source.Size;

        }

        /// <summary>
        /// アプリが追加したデータをコピー
        /// </summary>
        /// <param name="source"></param>
        public void CopyAdditionalInformation(Record source)
        {
            this.DateRegistered = source.DateRegistered;
            this.GroupKey = source.GroupKey;

            this.TagEntry = source.TagEntry;

            if (this.Rating == 0)
            {
                this.Rating = source.Rating;
            }

            this.SortEntry = source.SortEntry;
            this.FlipDirection = source.FlipDirection;
            this.IsGroup = source.IsGroup;
        }
        

        #endregion




        #region Group

        /// <summary>
        /// グループを作成
        /// </summary>
        /// <param name="uniqueKey"></param>
        /// <param name="leader"></param>
        /// <param name="library"></param>
        /// <returns></returns>
        public static Record GenerateAsGroup(string uniqueKey, Record leader)
        {
            var group = new Record()
            {
                Id = uniqueKey,
                IsGroup = true,
            };
            group.SetGroupLeader(leader);

            return group;
        }

        public static Record GenerateAsGroup(string uniqueKey)
            => GenerateAsGroup(uniqueKey, null);

        /// <summary>
        /// 指定Recordをグループの代表に設定
        /// </summary>
        /// <param name="leader"></param>
        /// <param name="library"></param>
        /// <returns></returns>
        public void SetGroupLeader(Record leader)
        {
            if (!this.IsGroup)
            {
                throw new InvalidOperationException();
            }

            this.GroupKey = leader?.Id;
            this.FullPath = null;

            if (leader == null)
            {
                this.Width = 0;
                this.Height = 0;
                this.Size = 0;
                this.Directory = "";
            }
            else
            {
                this.Width = leader.Width;
                this.Height = leader.Height;
                this.Size = leader.Size;
                this.Directory = leader.Directory;
            }
        }

        /// <summary>
        /// ファイルをグループに追加
        /// </summary>
        /// <param name="item"></param>
        public void AddToGroup(Record item)
        {
            if (!this.IsGroup)
            {
                throw new InvalidOperationException();
            }

            item.GroupKey = this.Id;
            this.RefreshGroupDate(item);
        }

        /// <summary>
        /// ファイルをグループから削除
        /// </summary>
        /// <param name="removedItems"></param>
        /// <param name="allItems"></param>
        /// <returns></returns>
        public async Task RemoveFromGroupAsync
            (IEnumerable<Record> removedItems, Library library)
        {
            await library.Grouping.RemoveFromGroupAsync(this, removedItems.Select(x => x.Id));
        }

        /// <summary>
        /// グループプロパティを更新
        /// </summary>
        /// <param name="item"></param>
        private void RefreshGroupDate(Record item)
        {
            if (!this.IsGroup)
            {
                throw new InvalidOperationException();
            }
            if (item != null)
            {
                if (this.DateModified < item.DateModified)
                {
                    this.DateModified = item.DateModified;
                }
                if (this.DateCreated < item.DateCreated)
                {
                    this.DateCreated = item.DateCreated;
                }
                if (this.DateRegistered < item.DateRegistered)
                {
                    this.DateRegistered = item.DateRegistered;
                }
            }
            else
            {
                this.DateModified = default(DateTimeOffset);
                this.DateCreated = default(DateTimeOffset);
                this.DateRegistered = default(DateTimeOffset);
            }
        }

        /// <summary>
        /// グループのソート設定
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public bool SetSort(IEnumerable<SortSetting> source)
        {
            if(this.GetSort().SequenceEqual(source, (x, y) => x.Equals(y)))
            {
                if (this.SortSettings.IsNullOrEmpty())
                {
                    this.SortSettings = source.ToList();
                }
                return false;
            }
            this.SortSettings = source.ToList();
            LibraryOwner.GetCurrent().Searcher.SetDefaultGroupSort(source);
            return true;
        }

        /// <summary>
        /// グループのソート設定
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SortSetting> GetSort()
        {
            if (this.SortSettings.IsNullOrEmpty())
            {
                return LibraryOwner.GetCurrent().Searcher.GetDefaultGroupSort();
            }
            return this.SortSettings.Select(x => x.Clone());
        }




        public Task<long> CountAsync(Library library)
            => library.GroupQuery.CountAsync(this);

        public string GetFilterString(Library library)
            => library.GroupQuery.GetFilterString(this);

        public Task<Record[]> SearchAsync(Library library, long skip, long take)
            => library.GroupQuery.SearchAsync(this, skip, take);
        


        #endregion

        public override string ToString()
        {
            return this.Id;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
