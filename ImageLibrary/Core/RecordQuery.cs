﻿using System;
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
            using (var connection = await this.Table.Parent.ConnectAsync())
            {
                return await this.Table.CountAsync(connection, this.GetFilterString(criteria));
            }
        }
        public long Count(SearchInformation criteria)
        {
            using (var connection = this.Table.Parent.Connect())
            {
                return this.Table.Count(connection, this.GetFilterString(criteria));
            }
        }


        /// <summary>
        /// 検索条件のSQL文字列を生成
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public IDatabaseExpression GetFilterString(SearchInformation criteria)
        {
            var search = criteria.GetWhereSql();
            return GetFilterString(search);
        }

        private IDatabaseExpression GetFilterString(IDatabaseExpression? sql)
        {
            var groupFilter = (this.Library.IsGroupingEnabled)
                ? DatabaseExpression.Or(
                    DatabaseExpression.IsTrue(nameof(Record.IsGroup)),
                    DatabaseExpression.IsNull(nameof(Record.GroupKey)))
                : DatabaseExpression.IsFalse(nameof(Record.IsGroup));

            var filter = (sql != null)
                ? DatabaseExpression.And(groupFilter, sql)
                : groupFilter;

            return filter;
        }


        /// <summary>
        /// 条件に一致するレコードをデータベースから取得
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<Record[]> SearchAsync(SearchInformation criteria, long skip, long take, Record? skipUntil)
        {
            //TODO データベースの状態が変わると、最初に数えたHit数と現在の検索結果が異なる可能性がある

            criteria.SetDateToNow();

            Record[] records;

            if (skipUntil != null)
            {
                var sort = criteria.GetSort().ToArray();

                var reference = await this.Library.GetSortReferenceAsync(criteria, skipUntil);

                records = await this.Library.SearchMainAsync
                    (DatabaseExpression.And(this.GetFilterString(criteria), SortSetting.GetSkipFilterSql(sort)),
                    SortSetting.GetFullSql(sort),
                    0, take, reference);
            }
            else
            {
                records = await this.Library.SearchMainAsync
                    (this.GetFilterString(criteria),
                    SortSetting.GetFullSql(criteria.GetSort()),
                    skip, take);
            }

            if ((criteria.ThumbnailFilePath == null || skip == 0) && records.Length > 0)
            {
                criteria.ThumbnailFilePath = records[0].FullPath;
            }
            else if (skip == 0 && take > 0 && records.Length <= 0)
            {
                criteria.ThumbnailFilePath = null;
            }

            return records;
        }
        public Record[] Search(SearchInformation criteria, long skip, long take, Record? skipUntil)
        {
            criteria.SetDateToNow();

            Record[] records;

            if (skipUntil != null)
            {
                var sort = criteria.GetSort().ToArray();

                var reference = this.Library.GetSortReference(criteria, skipUntil);

                records = this.Library.SearchMain
                    (DatabaseExpression.And(this.GetFilterString(criteria), SortSetting.GetSkipFilterSql(sort)),
                    SortSetting.GetFullSql(sort),
                    0, take, reference);
            }
            else
            {
                records = this.Library.SearchMain
                    (this.GetFilterString(criteria),
                    SortSetting.GetFullSql(criteria.GetSort()),
                    skip, take);
            }

            if ((criteria.ThumbnailFilePath == null || skip == 0) && records.Length > 0)
            {
                criteria.ThumbnailFilePath = records[0].FullPath;
            }
            else if (skip == 0 && take > 0 && records.Length <= 0)
            {
                criteria.ThumbnailFilePath = null;
            }

            return records;
        }
    }
}
