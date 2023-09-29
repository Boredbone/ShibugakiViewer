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

        private static Dictionary<int, SearchSql> searchDictionary
            = MakeSearchDictionary();

        private static Dictionary<int, SortDefinition> sortDictionary
            = MakeSortDictionary();

        private static Dictionary<int, string> Labels;

        private static string GetStr(SearchReferences reference)
        {
            if (reference.Str != null)
            {
                return reference.Str;
            }
            throw new ArgumentNullException();
        }

        /// <summary>
        /// 各プロパティによる検索方法の定義
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, SearchSql> MakeSearchDictionary()
        {
            var dictionary = new Dictionary<int, SearchSql>();

            dictionary[(int)FileProperty.Id]
                = new SearchSql(nameof(Record.Id), true,
                o => DatabaseReference.ToEqualsString(GetStr(o)));

            dictionary[(int)FileProperty.DirectoryPath]
                = new SearchSql(DatabaseFunction.ToLower(nameof(Record.Directory)), true,
                o => DatabaseReference.ToLowerEqualsString(GetStr(o)));

            dictionary[(int)FileProperty.DirectoryPathStartsWith]
                = new SearchSql(DatabaseFunction.ToLower(nameof(Record.Directory)), false, o =>
                 {
                     var str = GetStr(o);
                     var separator = System.IO.Path.DirectorySeparatorChar.ToString();
                     return (str.EndsWith(separator))
                         ? DatabaseReference.Match(str) : DatabaseReference.StartsWith(str + separator);
                 });
            dictionary[(int)FileProperty.DirectoryPathContains]
                = new SearchSql(DatabaseFunction.ToLower(nameof(Record.Directory)), false,
                o => DatabaseReference.Contains(GetStr(o)));

            dictionary[(int)FileProperty.FullPath]
                = new SearchSql(
                    DatabaseFunction.ToLower
                    (DatabaseFunction.Combine(nameof(Record.Directory), nameof(Record.FileName))),
                true, o => DatabaseReference.ToLowerEqualsString(GetStr(o)));

            dictionary[(int)FileProperty.FileName] = new SearchSql(true,
                (o, mode) =>
                {
                    var column = DatabaseFunction.ToLower(nameof(Record.FileName));

                    if (mode == CompareMode.Equal || mode == CompareMode.NotEqual)
                    {
                        var converted = DatabaseReference.Match(GetStr(o));

                        if (mode == CompareMode.Equal)
                        {
                            return DatabaseExpression.Is(column, converted);
                        }
                        else
                        {
                            return DatabaseExpression.IsNot(column, converted);
                        }
                    }
                    else
                    {
                        var converted = DatabaseReference.ToLowerEqualsString(GetStr(o));
                        return DatabaseExpression.Compare(column, mode, converted);
                    }
                });

            dictionary[(int)FileProperty.FileNameContains] = new SearchSql(
                DatabaseFunction.ToLower(nameof(Record.FileName)),
                false, o => DatabaseReference.Contains(GetStr(o)));

            dictionary[(int)FileProperty.DateTimeCreated]
                = new SearchSql(nameof(Record.DateCreated), true,
                o => DatabaseReference.DateTimeOffsetReference(o.DateTime));
            dictionary[(int)FileProperty.DateTimeModified]
                = new SearchSql(nameof(Record.DateModified), true,
                o => DatabaseReference.DateTimeOffsetReference(o.DateTime));
            dictionary[(int)FileProperty.DateTimeRegistered]
                = new SearchSql(nameof(Record.DateRegistered), true,
                o => DatabaseReference.DateTimeOffsetReference(o.DateTime));

            dictionary[(int)FileProperty.DateCreated]
                = new SearchSql(DatabaseFunction.GetDate(nameof(Record.DateCreated)), true,
                o => DatabaseReference.DateOffsetReference(o.DateTime));
            dictionary[(int)FileProperty.DateModified]
                = new SearchSql(DatabaseFunction.GetDate(nameof(Record.DateModified)), true,
                o => DatabaseReference.DateOffsetReference(o.DateTime));
            dictionary[(int)FileProperty.DateRegistered]
                = new SearchSql(DatabaseFunction.GetDate(nameof(Record.DateRegistered)), true,
                o => DatabaseReference.DateOffsetReference(o.DateTime));

            dictionary[(int)FileProperty.Width]
                = new SearchSql(nameof(Record.Width), true);
            dictionary[(int)FileProperty.Height]
                = new SearchSql(nameof(Record.Height), true);
            dictionary[(int)FileProperty.Size]
                = new SearchSql(nameof(Record.Size), true);
            dictionary[(int)FileProperty.ContainsTag]
                = new SearchSql(nameof(Record.TagEntry), false,
                o => DatabaseReference.Contains($",{o.Num32},"));
            dictionary[(int)FileProperty.HasTag]
                = new SearchSql(false, (o, c) =>
                {
                    var column = DatabaseFunction.Length(nameof(Record.TagEntry));
                    var mode = c.ContainsEqual() ? CompareMode.GreatEqual : CompareMode.Less;
                    return DatabaseExpression.Compare(column, mode, new DatabaseReference("2"));
                });
            dictionary[(int)FileProperty.Rating]
                = new SearchSql(nameof(Record.Rating), true,
                o => new DatabaseReference(RateConvertingHelper.Reverse(o.Num32).ToString()));

            dictionary[(int)FileProperty.Group]
                = new SearchSql(nameof(Record.GroupKey), true,
                o => DatabaseReference.ToEqualsString(GetStr(o)));

            dictionary[(int)FileProperty.AspectRatio]
                = new SearchSql(
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
            if (searchDictionary.TryGetValue((int)property, out var result))
            {
                return result.IsComparable;
            }
            return false;
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
        public static IDatabaseExpression ToSearch
            (this FileProperty property, SearchReferences threshold, CompareMode mode)
        {
            return searchDictionary[(int)property].ToSql(mode, threshold);
        }
        public static IDatabaseExpression ToSearch
            (this FileProperty property, string threshold, CompareMode mode)
            => ToSearch(property, SearchReferences.From(threshold), mode);

        public static IDatabaseExpression ToSearch
            (this FileProperty property, int threshold, CompareMode mode)
            => ToSearch(property, SearchReferences.From(threshold), mode);
        

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
            => GetPropertyLabel((int)property);

        private static string GetPropertyLabel(int property)
            => Labels.TryGetValue(property, out var result) ? result : "";


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
        public static bool IsDateTime(this FileProperty property)
        {
            switch (property)
            {
                case FileProperty.DateTimeCreated:
                case FileProperty.DateTimeModified:
                case FileProperty.DateTimeRegistered:
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
