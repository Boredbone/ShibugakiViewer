using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Database.Search;
using Database.Table;
using ImageLibrary.File;
using ImageLibrary.Search;

namespace ImageLibrary.Core
{
    public class RecordQuery : IRecordQuery<SearchInformation>
    {
        public TypedTable<Record, string> Table { get; }
        public Library Library { get; }

        public RecordQuery(TypedTable<Record, string> table, Library library)
        {
            this.Table = table;
            this.Library = library;
        }


        /// <summary>
        /// 条件を満たすレコードの数をデータベースに問い合わせ
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public async Task<long> CountAsync(SearchInformation criteria)
        {
            using (var connection = this.Table.Parent.Connect())
            {
                return await this.Table.CountAsync(connection,
                    this.GetFilterString(criteria));
            }
        }



        /// <summary>
        /// 検索条件のSQL文字列を生成
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public string GetFilterString(SearchInformation criteria)
        {
            //var groupFilter = (this.IsGroupingEnabled)
            //    ? DatabaseFunction.Or(
            //        DatabaseFunction.IsTrue(nameof(Record.IsGroup)),
            //        DatabaseFunction.IsNull(nameof(Record.GroupKey)))
            //    : DatabaseFunction.IsFalse(nameof(Record.IsGroup));

            var search = criteria.GetWhereSql();

            //var filter = (search != null)
            //    ? DatabaseFunction.And(groupFilter, search)
            //    : $"({groupFilter})";

            return GetFilterString(search);
        }

        private string GetFilterString(string sql)
        {
            var groupFilter = (this.Library.IsGroupingEnabled)
                ? DatabaseFunction.Or(
                    DatabaseFunction.IsTrue(nameof(Record.IsGroup)),
                    DatabaseFunction.IsNull(nameof(Record.GroupKey)))
                : DatabaseFunction.IsFalse(nameof(Record.IsGroup));

            var filter = (sql != null)
                ? DatabaseFunction.And(groupFilter, sql)
                : $"({groupFilter})";

            return filter;
        }


        /// <summary>
        /// 条件に一致するレコードをデータベースから取得
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<Record[]> SearchAsync(SearchInformation criteria, long skip, long take)
        {
            //TODO データベースの状態が変わると、最初に数えたHit数と現在の検索結果が異なる可能性がある

            criteria.SetDateToNow();

            var records = await this.Library.SearchMainAsync
                (this.GetFilterString(criteria),
                SortSetting.GetFullSql(criteria.GetSort()),
                skip, take);

            if ((criteria.ThumbnailFilePath == null || skip == 0) && records.Length > 0)
            {
                criteria.ThumbnailFilePath = records[0].FullPath;
            }

            return records;
        }


        //public async Task<long> FindIndexAsync(SearchInformation criteria, Record record)
        //{
        //    //return this.Library.FindIndexAsync
        //    //    (this.GetFilterString(criteria),
        //    //    SortSetting.GetFullSql(criteria.GetSort()),
        //    //    record);
        //
        //
        //    //var sort = this.GetSort(criteria.SortSettings, this.Searcher.DefaultSort);
        //
        //    var selectorParams = SortSetting.GetReferenceSelectorSql(criteria.GetSort());
        //    var selector = $"SELECT {selectorParams} FROM {this.Table.Name} WHERE Id = @Id LIMIT 1";
        //
        //    var filter = DatabaseFunction.And(this.GetFilterString(criteria),
        //        SortSetting.GetOrderFilterSql(criteria.GetSort(), record));
        //
        //    using (var connection = this.Table.Parent.Connect())
        //    {
        //        var reference = await this.Table.GetDynamicParametersAsync(connection, selector, record);
        //        
        //        return (await this.Table.CountAsync(connection, filter, reference)) - 1;
        //    }
        //}
    

        //public Task<string[]> GetAllIdsAsync(SearchInformation criteria)
        //    => this.Library.GetAllIdsAsync(this.GetFilterString(criteria));
    }
}
