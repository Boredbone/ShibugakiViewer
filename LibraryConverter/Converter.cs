using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using ShibugakiViewer.Models;

namespace LibraryConverter.Compat
{

    public class Converter
    {
        private SparkImageViewer.DataModel.ApplicationCoreData oldSettings;
        private SparkImageViewer.DataModel.ImageLibrary oldLibrary;

        public Dictionary<string, Record> Groups { get; private set; }
        public Dictionary<string, Record> Files { get; private set; }
        public Dictionary<int, TagInformation> Tags { get; private set; }

        public Converter()
        {

        }

        private async Task<bool> LoadAsync(string directory)
        {

            var settings = new SparkImageViewer.DataModel.ApplicationCoreData(directory);
            var library = new SparkImageViewer.DataModel.ImageLibrary(settings.Searcher, directory);

            var result = await settings.InitSettingsAsync();
            if (!result)
            {
                return false;
            }
            await library.LoadLibraryAsync();

            this.oldSettings = settings;
            this.oldLibrary = library;

            return true;
        }

        public async Task Start1(string directory, ApplicationSettings settings)
        {
            var succeeded = await this.LoadAsync(directory);
            if (!succeeded)
            {
                throw new Exception();
            }


            this.ConvertSettings(settings);
        }

        public async Task Start2(LibrarySettings libSettings,
            Library library, TypedTable<Record, string> records, Action<int> OnLoaded, Action<int> OnAdding)
        {
            this.ConvertLibrarySettings(libSettings);

            library.InitializeLibrarySettings(libSettings);

            this.ConvertLibraryData(library);

            var ignored = new HashSet<TagInformation>();

            var tagMax = this.Tags.Max(x => x.Key);
            for (int i = 1; i <= tagMax; i++)
            {
                TagInformation tag;
                if (!this.Tags.TryGetValue(i, out tag))
                {
                    tag = new TagInformation()
                    {
                        IsIgnored = false,
                        Name = i.ToString(),
                    };
                    ignored.Add(tag);
                }
                var newKey = library.Tags.SetTag(tag);
            }

            //this.Tags.OrderBy(x=>x.Key).ForEach(x => library.Tags.SetTag(x.Value));

            ignored.ForEach(x => x.IsIgnored = true);

            await Task.Delay(1000);

            await this.ConvertFileDatabase(library, records, OnLoaded, OnAdding);
        }

        public void ConvertSettings(ApplicationSettings target)
        {
            target.ThumbNailSize = 200;// (int)this.oldSettings.ThumbNailSize;
            target.IsFlipAnimationEnabled = this.oldSettings.IsFlipAnimationEnabled;
            target.IsViewerPageTopBarFixed = this.oldSettings.IsViewerPageTopBarFixed;
            target.IsViewerPageLeftBarFixed = this.oldSettings.IsViewerPageLeftBarFixed;
            target.LastSearchedFavorite = this.oldSettings.LastSearchedFavorite;
            target.IsFlipReversed = this.oldSettings.IsFlipReversed;
            target.IsOpenNavigationWithSingleTapEnabled = this.oldSettings.IsOpenNavigationWithSingleTapEnabled;
            target.IsCmsEnabled = this.oldSettings.IsCmsEnabled;
            target.IsGifAnimationDisabled = !this.oldSettings.IsAnimatedGifEnabled;
            target.UseExtendedMouseButtonsToSwitchImage = this.oldSettings.UseExtendedMouseButtonsToSwitchImage;
            target.RefreshLibraryOnLaunched = this.oldSettings.RefreshLibraryOnLaunched;
            target.IsLibraryRefreshStatusVisible = this.oldSettings.IsLibraryRefreshStatusVisible;
            target.CursorKeyBind = this.oldSettings.CursorKeyBind;
            target.IsProfessionalFolderSettingEnabled = this.oldSettings.IsProfessionalFolderSettingEnabled;
            target.SlideshowAnimationTimeMillisec = this.oldSettings.SlideshowAnimationTimeMillisec;
            target.SlideshowFlipTimeMillisec = this.oldSettings.SlideshowFlipTimeMillisec;
            target.IsSlideshowResizingAlways = false;// this.oldSettings.IsSlideshowResizingAlways;
            target.IsSlideshowResizeToFill = false;// this.oldSettings.IsSlideshowResizeToFill;
            target.IsSlideshowRandom = false;// this.oldSettings.IsSlideshowRandom;
            target.IsSlideshowFullScreen = this.oldSettings.IsSlideshowFullScreen;
            //target.IsAutoInformationPaneDisabled = false;
        }


        public void ConvertLibrarySettings(LibrarySettings target)
        {

            target.SearchSettings.Clear();
            this.oldSettings.librarySettings.SearchSettings
                .ForEach(x => target.SearchSettings.Add(this.ConvertSearch(x)));


            target.FavoriteSearch.Clear();
            this.oldSettings.librarySettings.FavoriteSearch
                .Reverse<SparkImageViewer.FileSearch.SearchInformation>()
                .ForEach(x => target.FavoriteSearch.Add(this.ConvertSearch(x)));

            target.DefaultSort.Clear();
            this.oldSettings.librarySettings.DefaultSort
                .ForEach(x => target.DefaultSort.Add(this.ConvertSort(x)));

            target.DefaultGroupSort.Clear();
            this.oldSettings.librarySettings.DefaultGroupSort
                .ForEach(x => target.DefaultGroupSort.Add(this.ConvertSort(x)));

            target.IsGroupingEnabled = this.oldSettings.IsGroupingEnabled;
            target.RefreshLibraryCompletely = this.oldSettings.RefreshLibraryCompletely;

        }

        private void ConvertLibraryData(Library target)
        {
            target.Folders.TryAddItems(
            this.oldSettings.librarySettings.Folders
                .Where(x => !x.Value.Ignored)
                .Select(x => this.ConvertFolder(x.Value))
                .Where(x => x != null));

            var oldTags = this.oldSettings
                .Tags
                .GetAll()
                .OrderBy(x => x.Value.Shortcut.Length)
                .ToArray();

            var tags = new Dictionary<int, TagInformation>();

            var registeredShortcuts = new HashSet<string>();


            foreach (var tag in oldTags)
            {
                if (this.oldLibrary.FileDictionary.Any(x => x.Value.Tags.Contains(tag.Key))
                    || this.oldSettings.Searcher.GroupLeaderDictionary.Any(x=>x.Value.Tags.Contains(tag.Key)))
                {
                    var shortcut = tag.Value.Shortcut;

                    if (shortcut != null && shortcut.Length > 0)
                    {
                        var symbol = shortcut.Last().ToString().ToUpper();

                        if (!registeredShortcuts.Contains(symbol))
                        {
                            shortcut = symbol;
                            registeredShortcuts.Add(symbol);
                        }
                    }


                    tags.Add(tag.Key, new TagInformation()
                    {
                        Id = tag.Key,
                        IsIgnored = false,
                        Name = tag.Value.Name,
                        Shortcut = shortcut,
                    });
                }
            }

            //target.Tags.SetSource(tags);
            this.Tags = tags;

        }

        private async Task ConvertFileDatabase
            (Library library, TypedTable<Record, string> records, Action<int> OnLoaded, Action<int> OnAdding)
        {
            this.ConvertFiles();
            ConvertGroups(library);

            OnLoaded?.Invoke(this.Files.Count);
            var count = 0;

            foreach (var items in this.Files.Select(x => x.Value).Buffer(1024))
            {
                await records.ReplaceRangeBufferedAsync(items);
                count += items.Count;
                OnAdding?.Invoke(count);
            }

            //await records.ReplaceRangeBufferedAsync(this.Files.Select(x => x.Value));
            //return this.Files.Select(x => x.Value);
        }

        //public void ConvertDatabase()
        //{
        //    this.ConvertGroups();
        //    this.ConvertFiles();
        //}


        private void ConvertGroups(Library library)
        {
            var items = this.oldSettings
                .Searcher
                .GroupLeaderDictionary;


            var groups = new List<Record>();

            foreach (var item in items)
            {
                Record leader;
                if (!this.Files.TryGetValue(item.Value.LeaderFilekey, out leader))
                {
                    leader = this.Files.Select(x => x.Value)
                        .FirstOrDefault(x => x.GroupKey != null && x.GroupKey.Equals(item.Key));
                    if (leader == null)
                    {
                        continue;
                    }
                }

                var group = Record.GenerateAsGroup(item.Key, leader);

                group.SetName(item.Value.displayName);
                //group.IsFlipReversed = item.Value.IsFlipReversed;
                //group.IsParticularFlipDirectionEnabled = item.Value.IsParticularFlipDirectionEnabled;

                group.FlipDirection
                    = (!item.Value.IsParticularFlipDirectionEnabled) ? 0
                    : (!item.Value.IsFlipReversed) ? 1
                    : 2;

                group.Rating = item.Value.Rating;

                if (item.Value.SortSettings != null)
                {
                    group.SetSort(item.Value.SortSettings.Select(y => this.ConvertSort(y)));
                }
                item.Value.Tags.ForEach(y => group.TagSet.Add(y));

                this.oldLibrary.FileDictionary
                    .Where(x => x.Value.GroupLeaderKey != null && x.Value.GroupLeaderKey.Equals(item.Key))
                    .Select(x =>
                    {
                        Record f;
                        if (this.Files.TryGetValue(x.Key, out f))
                        {
                            return f;
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .ForEach(x => group.AddToGroup(x));

                //this.Files.Select(x => x.Value)
                //    .Where(x => x.GroupKey != null && x.GroupKey.Equals(item.Key))
                //    .ForEach(x => group.AddToGroup(x));

                groups.Add(group);
            }

            groups.ForEach(x => this.Files.Add(x.Id, x));

        }

        private void ConvertFiles()
        {

            this.Files = this.oldLibrary.FileDictionary
                .ToDictionary(x => x.Key, x =>
                {
                    var file = new Record(x.Key)
                    {
                        DateCreated = x.Value.DateCreated,
                        DateModified = x.Value.DateModified,
                        DateRegistered = x.Value.DateRegistered,
                        Height = (int)x.Value.Height,
                        Rating = x.Value.Rating,
                        Size = (long)x.Value.Size,
                        Width = (int)x.Value.Width,
                    };


                    x.Value.Tags.ForEach(y => file.TagSet.Add(y));
                    //file.SetPath(x.Value.Path);

                    //if (x.Value.GroupLeaderKey != null)
                    //{
                    //    GroupFile group;
                    //    if (this.Groups.TryGetValue(x.Value.GroupLeaderKey, out group))
                    //    {
                    //        file.SetGroup(group);
                    //    }
                    //}

                    return file;
                });



            //foreach (var oldGroup in this.oldSettings.librarySettings.GroupLeaderDictionary)
            //{
            //    GroupFile group;
            //    if (this.Groups.TryGetValue(oldGroup.Key, out group))
            //    {
            //        FileInformation file;
            //        if (this.Files.TryGetValue(oldGroup.Value.LeaderFilekey, out file))
            //        {
            //            group.SetThumbnail(file);
            //        }
            //    }
            //}
        }


        private FolderInformation ConvertFolder(SparkImageViewer.DataModel.FolderInformation source)
        {
            if (!System.IO.Directory.Exists(source.DisplayName))
            {
                return null;
            }

            return new FolderInformation(source.DisplayName)
            {
                AutoRefreshEnable = source.AutoRefreshEnable,
                Ignored = source.Ignored,
                RefreshEnable = source.RefreshEnable,
                //RefreshTrigger = source.RefreshTrigger,
                IsTopDirectoryOnly = false,
                Mode = (source.RefreshMode == ThreeState.None) ? FolderCheckMode.None
                    : (source.RefreshMode == ThreeState.False) ? FolderCheckMode.Light
                    : FolderCheckMode.Detail,
                WatchChange = true,
            };
        }

        private SearchInformation ConvertSearch(SparkImageViewer.FileSearch.SearchInformation source)
        {
            var result = new SearchInformation(this.ConvertComplexSearch(source.Root))
            {
                DateLastUsed = source.DateLastUsed,
                Key = source.Key,
                Name = source.Name,
                ThumbnailFilePath = source.ThumbnailFilekey,
            };

            //result.SortSettings.Clear();
            //source.SortSettings.ForEach(x =>
            //{
            //    var sort = this.ConvertSort(x);
            //    result.SortSettings.Add(sort);
            //});

            result.SetSort(source.SortSettings.Select(x => this.ConvertSort(x)));

            return result;

        }

        private ComplexSearch ConvertComplexSearch(SparkImageViewer.DataModel.ComplexSearchSetting source)
        {
            var result = new ComplexSearch(source.IsOr);

            source.SavedChildren.ForEach(x =>
            {
                var child = x.IsUnit
                    ? (ISqlSearch)this.ConvertUnitSearch((SparkImageViewer.DataModel.UnitSearchSetting)x)
                    : (ISqlSearch)this.ConvertComplexSearch((SparkImageViewer.DataModel.ComplexSearchSetting)x);
                result.Add(child);
            });

            return result;
        }


        private UnitSearch ConvertUnitSearch(SparkImageViewer.DataModel.UnitSearchSetting source)
        {
            var property = source.Property.Convert();

            if (property.IsComperable())
            {
                return new UnitSearch()
                {
                    Mode = (CompareMode)source.Mode,
                    Property = property,
                    Reference = source.StringListReference?
                    .Select(x => (x == null) ? "" : x.Replace("\n", "")).Join()
                    ?? this.ConvertReference(source.SingleReference),
                };

            }
            else
            {
                return new UnitSearch()
                {
                    Mode = source.IsNot ? CompareMode.NotEqual : CompareMode.Equal,
                    Property = property,
                    Reference = source.StringListReference?
                    .Select(x => (x == null) ? "" : x.Replace("\n", "")).Join()
                    ?? this.ConvertReference(source.SingleReference),
                };

            }


        }

        private object ConvertReference(object source)
        {
            var ui32 = source as uint?;
            var i32 = source as int?;
            var ui64 = source as ulong?;
            var i64 = source as long?;

            return (ui32.HasValue) ? (int)ui32.Value
                : (i32.HasValue) ? i32.Value
                : (ui64.HasValue) ? (long)ui64.Value
                : (i64.HasValue) ? i64.Value
                : source;

        }

        private SortSetting ConvertSort(SparkImageViewer.FileSort.SortSetting source)
        {
            return new SortSetting()
            {
                IsDescending = source.IsDescending,
                Property = source.Property.Convert(),
            };
        }
    }
}
