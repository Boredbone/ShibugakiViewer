using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.File;
using ImageLibrary.Search;

namespace ImageLibrary.SearchProperty
{

    public enum FileProperty : int
    {
        Id,
        DirectoryPath,
        DirectoryPathStartsWith,
        DirectoryPathContains,
        FullPath,
        FileName,
        FileNameContains,
        FileNameSequenceNumLeft,
        FileNameSequenceNumRight,
        DateTimeCreated,
        DateTimeModified,
        DateTimeRegistered,
        DateCreated,
        DateModified,
        DateRegistered,
        Width,
        Height,
        Size,
        ContainsTag,
        HasTag,
        Rating,
        Group,
        AspectRatio,
    }
    public static class FilePropertyManager
    {

        private static string isLabel;
        private static string isNotLabel;
        private static string containsLabel;
        private static string notContainsLabel;
        private static string andLabel;
        private static string orLabel;

        private static Dictionary<int, PropertySearch> searchDictionary
            = MakeSearchDictionary();

        private static Dictionary<int, SortDefinition> sortDictionary
            = MakeSortDictionary();

        private static Dictionary<int, string> Labels;

        /// <summary>
        /// 各プロパティによる検索方法の定義
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, PropertySearch> MakeSearchDictionary()
        {
            var dictionary = new Dictionary<int, PropertySearch>();

            dictionary[(int)FileProperty.Id]
                = new PropertySearch(nameof(Record.Id), true,
                o => DatabaseFunction.ToEqualsString(o));

            dictionary[(int)FileProperty.DirectoryPath]
                = new PropertySearch(DatabaseFunction.ToLower(nameof(Record.Directory)), true,
                o => DatabaseFunction.ToLowerEqualsString(o));

            dictionary[(int)FileProperty.DirectoryPathStartsWith]
                = new PropertySearch(DatabaseFunction.ToLower(nameof(Record.Directory)), false, o =>
                 {
                     var str = o.ToString();
                     var separator = System.IO.Path.DirectorySeparatorChar.ToString();
                     return (str.EndsWith(separator))
                         ? DatabaseFunction.Match(str) : DatabaseFunction.StartsWith(str + separator);
                 });
            dictionary[(int)FileProperty.DirectoryPathContains]
                = new PropertySearch(DatabaseFunction.ToLower(nameof(Record.Directory)), false,
                o => DatabaseFunction.Contains(o.ToString()));

            dictionary[(int)FileProperty.FullPath]
                = new PropertySearch(
                    DatabaseFunction.ToLower
                    (DatabaseFunction.Combine(nameof(Record.Directory), nameof(Record.FileName))),
                true, o => DatabaseFunction.ToLowerEqualsString(o));

            dictionary[(int)FileProperty.FileName] = new PropertySearch(true,
                (o, mode) =>
                {
                    var column = DatabaseFunction.ToLower(nameof(Record.FileName));

                    if (mode == CompareMode.Equal || mode == CompareMode.NotEqual)
                    {
                        var converted = DatabaseFunction.Match(o.ToString());

                        if (mode == CompareMode.Equal)
                        {
                            return $"({column} {converted})";
                        }
                        else
                        {
                            return $"({column} NOT {converted})";
                        }
                    }
                    else
                    {
                        var converted = DatabaseFunction.ToLowerEqualsString(o);
                        return $"({column} {mode.ToSymbol()} {converted})";
                    }
                });

            dictionary[(int)FileProperty.FileNameContains] = new PropertySearch(
                DatabaseFunction.ToLower(nameof(Record.FileName)),
                false, o => DatabaseFunction.Contains(o.ToString()));

            dictionary[(int)FileProperty.DateTimeCreated]
                = new PropertySearch(nameof(Record.DateCreated), true,
                o => DatabaseFunction.DateTimeOffsetReference((DateTimeOffset)o));
            dictionary[(int)FileProperty.DateTimeModified]
                = new PropertySearch(nameof(Record.DateModified), true,
                o => DatabaseFunction.DateTimeOffsetReference((DateTimeOffset)o));
            dictionary[(int)FileProperty.DateTimeRegistered]
                = new PropertySearch(nameof(Record.DateRegistered), true,
                o => DatabaseFunction.DateTimeOffsetReference((DateTimeOffset)o));

            dictionary[(int)FileProperty.DateCreated]
                = new PropertySearch(DatabaseFunction.GetDate(nameof(Record.DateCreated)), true,
                o => DatabaseFunction.DateOffsetReference((DateTimeOffset)o));
            dictionary[(int)FileProperty.DateModified]
                = new PropertySearch(DatabaseFunction.GetDate(nameof(Record.DateModified)), true,
                o => DatabaseFunction.DateOffsetReference((DateTimeOffset)o));
            dictionary[(int)FileProperty.DateRegistered]
                = new PropertySearch(DatabaseFunction.GetDate(nameof(Record.DateRegistered)), true,
                o => DatabaseFunction.DateOffsetReference((DateTimeOffset)o));

            dictionary[(int)FileProperty.Width]
                = new PropertySearch(nameof(Record.Width), true);
            dictionary[(int)FileProperty.Height]
                = new PropertySearch(nameof(Record.Height), true);
            dictionary[(int)FileProperty.Size]
                = new PropertySearch(nameof(Record.Size), true);
            dictionary[(int)FileProperty.ContainsTag]
                = new PropertySearch(nameof(Record.TagEntry), false,
                o => DatabaseFunction.Contains($",{(int)o},"));
            dictionary[(int)FileProperty.HasTag]
                = new PropertySearch(false, (o, c) =>
                    c.ContainsEqual()
                    ? $"length({nameof(Record.TagEntry)}) >= 2"
                    : $"length({nameof(Record.TagEntry)}) < 2");
            dictionary[(int)FileProperty.Rating]
                = new PropertySearch(nameof(Record.Rating), true,
                o => RateConvertingHelper.Reverse((int)o).ToString());

            dictionary[(int)FileProperty.Group]
                = new PropertySearch(nameof(Record.GroupKey), false,
                o => DatabaseFunction.ToEqualsString(o));

            dictionary[(int)FileProperty.AspectRatio]
                = new PropertySearch(
                    DatabaseFunction.Divide(nameof(Record.Width), nameof(Record.Height)), true);

            return dictionary;
        }

        /// <summary>
        /// 各プロパティによるソート方法の定義
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, SortDefinition> MakeSortDictionary()
        {
            var dictionary = new Dictionary<int, SortDefinition>();

            dictionary[(int)FileProperty.Id] = new SortDefinition(nameof(Record.Id));

            dictionary[(int)FileProperty.DirectoryPath] = new SortDefinition
                (DatabaseFunction.ToLower(nameof(Record.Directory)));

            dictionary[(int)FileProperty.FullPath] = new SortDefinition
                (DatabaseFunction.ToLower
                (DatabaseFunction.Combine(nameof(Record.Directory), nameof(Record.FileName))));

            dictionary[(int)FileProperty.FileName] = new SortDefinition
                (DatabaseFunction.ToLower(nameof(Record.FileName)));


            dictionary[(int)FileProperty.FileNameSequenceNumLeft] = new SortDefinition(
                new[]
                {
                    DatabaseFunction.ToLower(nameof(Record.PreNameShort)),
                    nameof(Record.NameNumberLeft),
                    nameof(Record.NameLength),
                    DatabaseFunction.ToLower(nameof(Record.PostNameLong)),
                    DatabaseFunction.ToLower(nameof(Record.Extension))
                });
            dictionary[(int)FileProperty.FileNameSequenceNumRight] = new SortDefinition(
                new[]
                {
                    DatabaseFunction.ToLower(nameof(Record.PreNameLong)),
                    nameof(Record.NameNumberRight),
                    nameof(Record.NameLength),
                    DatabaseFunction.ToLower(nameof(Record.PostNameShort)),
                    DatabaseFunction.ToLower(nameof(Record.Extension))
                });

            dictionary[(int)FileProperty.DateTimeCreated] = new SortDefinition(nameof(Record.DateCreated));
            dictionary[(int)FileProperty.DateTimeModified] = new SortDefinition(nameof(Record.DateModified));
            dictionary[(int)FileProperty.DateTimeRegistered] = new SortDefinition(nameof(Record.DateRegistered));
            dictionary[(int)FileProperty.Width] = new SortDefinition(nameof(Record.Width));
            dictionary[(int)FileProperty.Height] = new SortDefinition(nameof(Record.Height));
            dictionary[(int)FileProperty.Size] = new SortDefinition(nameof(Record.Size));
            dictionary[(int)FileProperty.Rating] = new SortDefinition(nameof(Record.Rating));

            dictionary[(int)FileProperty.AspectRatio] = new SortDefinition
                (DatabaseFunction.Divide(nameof(Record.Width), nameof(Record.Height)));

            return dictionary;
        }

        /// <summary>
        /// プロパティの名前
        /// </summary>
        private static Dictionary<string, FileProperty> EnumName
            = Enum.GetValues(typeof(FileProperty)).OfType<FileProperty>()
                .ToDictionary(x => x.ToString(), x => x);

        /// <summary>
        /// 列挙型から名前の取得
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string GetName(this FileProperty property)
            => EnumName.First(x => x.Value.Equals(property)).Key;

        /// <summary>
        /// 名前から列挙型の取得
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FileProperty FromName(string name) => EnumName[name];

        /// <summary>
        /// 比較検索が可能か
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsComperable(this FileProperty property)
        {
            PropertySearch result;
            if (searchDictionary.TryGetValue((int)property, out result))
            {
                return result.IsComparable;
            }
            return false;

            //if (!searchDictionary.ContainsKey((int)property))
            //{
            //    return false;
            //}
            //return searchDictionary[(int)property].IsComparable;
        }

        /// <summary>
        /// 検索可能なプロパティのリスト
        /// </summary>
        private static Lazy<KeyValuePair<string, FileProperty>[]> propertyListToSearch
            = new Lazy<KeyValuePair<string, FileProperty>[]>(() =>
            {
                return searchDictionary
                   .Where(x => IsSearchable(x.Key))
                   .Select(x => new KeyValuePair<string, FileProperty>(GetPropertyLabel(x.Key), (FileProperty)x.Key))
                   .OrderBy(x => x.Key)
                   .ToArray();
            });

        /// <summary>
        /// ソート可能なプロパティのリスト
        /// </summary>
        private static Lazy<KeyValuePair<string, FileProperty>[]> propertyListToSort
            = new Lazy<KeyValuePair<string, FileProperty>[]>(() =>
            {
                return sortDictionary
                   .Where(x => IsSortable(x.Key))
                   .Select(x => new KeyValuePair<string, FileProperty>(GetPropertyLabel(x.Key), (FileProperty)x.Key))
                   .OrderBy(x => x.Key)
                   .ToArray();
            });

        /// <summary>
        /// 検索可能なプロパティのリスト
        /// </summary>
        /// <returns></returns>
        public static KeyValuePair<string, FileProperty>[] GetPropertyListToSearch()
            => propertyListToSearch.Value;

        /// <summary>
        /// ソート可能なプロパティのリスト
        /// </summary>
        /// <returns></returns>
        public static KeyValuePair<string, FileProperty>[] GetPropertyListToSort()
            => propertyListToSort.Value;

        /// <summary>
        /// 検索用SQL
        /// </summary>
        /// <param name="property"></param>
        /// <param name="threshold"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static string ToSearch
            (this FileProperty property, object threshold, CompareMode mode)
        {
            return searchDictionary[(int)property].ToSql(mode, threshold);
        }

        /// <summary>
        /// ソート用SQL
        /// </summary>
        /// <param name="property"></param>
        /// <param name="byDescending"></param>
        /// <returns></returns>
        public static string ToSort(this FileProperty property, bool byDescending)
        {
            var direction = byDescending ? "DESC" : "ASC";
            return sortDictionary[(int)property].Columns.Select(x => $"{x} {direction}").Join(", ");
        }

        /// <summary>
        /// ソートする列
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string[] GetSortColumns(this FileProperty property)
        {
            return sortDictionary[(int)property].Columns;
        }



        /// <summary>
        /// プロパティの表示名をリソースから取得
        /// </summary>
        /// <param name="getString"></param>
        public static void InitializeLabels(Func<string, string> GetResource)
        {
            if (Labels != null)
            {
                return;
            }
            Labels = new Dictionary<int, string>()
            {
                {(int)FileProperty.Id, GetResource("FullPath")},
                {(int)FileProperty.DirectoryPath, GetResource("DirectoryPath")},
                {(int)FileProperty.DirectoryPathStartsWith, GetResource("Directory")},
                {(int)FileProperty.DirectoryPathContains, GetResource("DirectoryPathContains")},
                {(int)FileProperty.FullPath, GetResource("FullPath")},
                {(int)FileProperty.FileName, GetResource("FileName")},
                {(int)FileProperty.FileNameContains, GetResource("FileNameContains")},
                {(int)FileProperty.FileNameSequenceNumLeft, GetResource("FileNameSequenceNumLeft")},
                {(int)FileProperty.FileNameSequenceNumRight, GetResource("FileNameSequenceNumRight")},
                {(int)FileProperty.DateTimeCreated, GetResource("DateCreated")},
                {(int)FileProperty.DateTimeModified, GetResource("DateModified")},
                {(int)FileProperty.DateTimeRegistered, GetResource("DateRegistered")},
                {(int)FileProperty.DateCreated, GetResource("DateCreated")},
                {(int)FileProperty.DateModified, GetResource("DateModified")},
                {(int)FileProperty.DateRegistered, GetResource("DateRegistered")},
                {(int)FileProperty.Width, GetResource("Width")},
                {(int)FileProperty.Height, GetResource("Height")},
                {(int)FileProperty.Size, GetResource("FileSize")},
                {(int)FileProperty.ContainsTag, GetResource("Tag")},
                {(int)FileProperty.HasTag, GetResource("HasTag")},
                {(int)FileProperty.Rating, GetResource("Rating")},
                {(int)FileProperty.Group, GetResource("Group")},
                {(int)FileProperty.AspectRatio, GetResource("AspectRatio")},
            };

            isLabel = GetResource("Is");
            isNotLabel = GetResource("IsNot");
            containsLabel = GetResource("Contains");
            notContainsLabel = GetResource("DoNotContains");
            andLabel = GetResource("MatchAll");
            orLabel = GetResource("MatchAny");
        }

        /// <summary>
        /// プロパティの表示名
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string GetPropertyLabel(this FileProperty property)
        {
            return GetPropertyLabel((int)property);
            //string result;
            //if (Labels.TryGetValue(property, out result))
            //{
            //    return result;
            //}
            //return "";
        }

        private static string GetPropertyLabel(int property)
        {
            string result;
            if (Labels.TryGetValue(property, out result))
            {
                return result;
            }
            return "";
        }


        public static string GetEqualityLabel(this FileProperty property, bool isNot)
        {
            if (property.IsContainer())
            {
                return isNot ? notContainsLabel : containsLabel;
            }
            else
            {
                return isNot ? isNotLabel : isLabel;
            }

        }

        public static string GetMatchLabel(bool isOr)
        {
            return isOr ? orLabel : andLabel;
        }

        private static bool IsSearchable(int property)
        {
            switch ((FileProperty)property)
            {
                case FileProperty.Id:
                case FileProperty.DirectoryPath:
                case FileProperty.FullPath:
                case FileProperty.FileNameSequenceNumLeft:
                case FileProperty.FileNameSequenceNumRight:
                case FileProperty.DateTimeCreated:
                case FileProperty.DateTimeModified:
                case FileProperty.DateTimeRegistered:
                case FileProperty.HasTag:
                case FileProperty.Group:
                    //case FileProperty.AspectRatio:
                    return false;
            }
            return true;
        }

        private static bool IsSortable(int property)
        {
            switch ((FileProperty)property)
            {
                case FileProperty.DirectoryPathStartsWith:
                case FileProperty.DirectoryPathContains:
                case FileProperty.FullPath:
                case FileProperty.FileNameContains:
                case FileProperty.DateCreated:
                case FileProperty.DateModified:
                case FileProperty.DateRegistered:
                case FileProperty.ContainsTag:
                case FileProperty.HasTag:
                case FileProperty.Group:
                    return false;
            }
            return true;
        }


        public static bool IsContainer(this FileProperty property)
        {
            switch (property)
            {
                case FileProperty.DirectoryPathContains:
                case FileProperty.FileNameContains:
                case FileProperty.ContainsTag:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsInteger(this FileProperty property)
        {
            switch (property)
            {
                case FileProperty.Width:
                case FileProperty.Height:
                case FileProperty.Rating:
                case FileProperty.Size:
                //case FileProperty.AspectRatio:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsFloat(this FileProperty property)
        {
            switch (property)
            {
                case FileProperty.AspectRatio:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsText(this FileProperty property)
        {
            switch (property)
            {
                case FileProperty.Id:
                case FileProperty.DirectoryPath:
                //case FileProperty.DirectoryPathStartsWith:
                case FileProperty.DirectoryPathContains:
                case FileProperty.FullPath:
                case FileProperty.FileName:
                case FileProperty.FileNameContains:
                case FileProperty.Group:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDate(this FileProperty property)
        {
            switch (property)
            {
                case FileProperty.DateTimeCreated:
                case FileProperty.DateTimeModified:
                case FileProperty.DateTimeRegistered:
                case FileProperty.DateCreated:
                case FileProperty.DateModified:
                case FileProperty.DateRegistered:
                    return true;
                default:
                    return false;
            }
        }


        private class SortDefinition
        {
            public string[] Columns { get; }

            public SortDefinition(string column)
            {
                this.Columns = new[] { column };
            }
            public SortDefinition(string[] columns)
            {
                this.Columns = columns;
            }
        }
    }

    /// <summary>
    /// レーティングの変換
    /// </summary>
    public static class RateConvertingHelper
    {

        private static int[] rateArray = new int[] { 0, 1, 25, 50, 75, 99 };
        public static int Steps { get { return rateArray.Length - 1; } }

        //Rating(評価,0->0,1->1,2->25,3->50,4->75,5->99)

        public static bool SetRate(this Record file, int value)
        {
            int index;
            if (value < 0)
            {
                index = rateArray.Length - 1;
            }
            else if (value >= rateArray.Length)
            {
                index = 0;
            }
            else
            {
                index = value;
            }
            var rate = rateArray[index];
            if (file.Rating != rate)
            {
                file.Rating = rate;
                return true;
            }
            return false;
        }
        public static int GetRate(this Record file)
        {
            var rate = file.Rating;
            return ToRating(rate);
        }

        public static int Reverse(int index)
        {
            return rateArray.ContainsIndex(index) ? rateArray[index] : 0;
        }

        public static int ToRating(int rate)
        {
            var index = rateArray.FindIndex(x => rate <= x);
            if (index < 0)
            {
                index = 0;
            }
            return index;
        }
    }
}
