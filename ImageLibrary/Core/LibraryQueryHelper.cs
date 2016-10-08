using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Database.Search;
using Database.Table;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Reactive.Bindings.Extensions;

namespace ImageLibrary.Core
{
    public class LibraryQueryHelper : DisposableBase
    {
        public TypedTable<Record, string> Table { get; }
        public Library Library { get; }

        private Subject<DatabaseUpdatedEventArgs> UpdatedSubject { get; }
        public IObservable<DatabaseUpdatedEventArgs> Updated => this.UpdatedSubject.AsObservable();


        public LibraryQueryHelper
            (TypedTable<Record, string> table, Library library)
        {
            this.Table = table;
            this.Library = library;
            this.UpdatedSubject = new Subject<DatabaseUpdatedEventArgs>().AddTo(this.Disposables);
        }

        /// <summary>
        /// 複数アイテムに同じ評価を設定
        /// </summary>
        /// <param name="items"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task UpdateRatingAsync(IEnumerable<string> items, int value)
        {

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"UPDATE {this.Table.Name} SET {nameof(Record.Rating)}");
            sqlBuilder.Append($" = {value} WHERE {nameof(Record.Id)} IN @{nameof(Tuple<object>.Item1)}");

            var sql = sqlBuilder.ToString();

            await this.RequestWithBufferedArrayAsync(items, sql);

        }

        /// <summary>
        /// 複数アイテムからタグを削除
        /// </summary>
        /// <param name="items"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task RemoveTagAsync(IEnumerable<string> items, TagInformation tag)
        {
            var key = tag.Id;

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"UPDATE {this.Table.Name} SET {nameof(Record.TagEntry)}");
            sqlBuilder.Append($" = REPLACE({nameof(Record.TagEntry)}, ',{key},', ',')");
            sqlBuilder.Append($" WHERE {nameof(Record.Id)} IN @{nameof(Tuple<object>.Item1)}");

            var sql = sqlBuilder.ToString();

            await this.RequestWithBufferedArrayAsync(items, sql);

        }

        /// <summary>
        /// 複数アイテムにタグを設定
        /// </summary>
        /// <param name="items"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task AddTagAsync(IEnumerable<string> items, TagInformation tag)
        {
            tag.LastUsed = DateTimeOffset.Now;
            var key = tag.Id;

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"UPDATE {this.Table.Name} SET {nameof(Record.TagEntry)}");
            sqlBuilder.Append($" = {nameof(Record.TagEntry)} || '{key},'");
            sqlBuilder.Append($" WHERE (({nameof(Record.Id)} IN @{nameof(Tuple<object>.Item1)}) AND");
            sqlBuilder.Append($" (NOT {nameof(Record.TagEntry)} GLOB '*,{key},*'))");

            var sql = sqlBuilder.ToString();

            await this.RequestWithBufferedArrayAsync(items, sql);

        }

        /// <summary>
        /// 複数アイテムに対する操作
        /// </summary>
        /// <param name="items"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        private async Task RequestWithBufferedArrayAsync(IEnumerable<string> items, string sql)
        {

            var succeeded = false;
            using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            {
                succeeded = await this.Table.RequestWithBufferedArrayAsync
                    (connection.Value, items, sql, x => new Tuple<string[]>(x));
            }

            if (succeeded)
            {
                this.UpdatedSubject.OnNext(new DatabaseUpdatedEventArgs()
                {
                    Sender = this,
                    Action = DatabaseAction.Update,
                });
            }
        }


        /// <summary>
        /// 共通のタグを取得
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<HashSet<int>> GetCommonTagsAsync(string[] ids)
        {
            var filter = DatabaseFunction.In(nameof(Record.Id), $"@{nameof(Tuple<object>.Item1)}");

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"SELECT {nameof(Record.TagEntry)} FROM {this.Table.Name}");
            sqlBuilder.Append($" WHERE {filter} LIMIT 1");

            var tags = new HashSet<int>();

            using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            {
                var tagEntry = await this.Table.ExecuteScalarAsync<string>
                    (connection.Value, sqlBuilder.ToString(), new Tuple<string[]>(ids.Take(64).ToArray()));


                var tagSet = new TagManager(tagEntry);


                foreach (var tag in tagSet.Read())
                {
                    var f = DatabaseFunction.And
                        (filter, FileProperty.ContainsTag.ToSearch(tag, CompareMode.NotEqual));
                    var woTag = await this.CountItemsWithoutTag(connection.Value, ids, 128, f);
                    if (woTag == 0)
                    {
                        tags.Add(tag);
                    }
                }
            }

            return tags;
        }


        /// <summary>
        /// 指定タグを含まないアイテムをカウント
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="items"></param>
        /// <param name="bufferLength"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private async Task<long> CountItemsWithoutTag
            (IDbConnection connection, IEnumerable<string> items, int bufferLength, string filter)
        {

            foreach (var ids in items.Buffer(bufferLength))
            {
                var param = new Tuple<string[]>(ids.ToArray());

                var woTag = await this.Table.CountAsync(connection, filter, param);
                if (woTag > 0)
                {
                    return woTag;
                }
            }
            return 0;
        }

        /// <summary>
        /// 共通の評価を取得
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<int> GetCommonRatingAsync(string[] ids)
        {
            var filter = DatabaseFunction.In(nameof(Record.Id), $"@{nameof(Tuple<object>.Item1)}");

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"SELECT DISTINCT {nameof(Record.Rating)} FROM {this.Table.Name}");
            sqlBuilder.Append($" WHERE {filter} LIMIT 2");

            var values = new HashSet<int>();

            using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            {
                foreach (var items in ids.Buffer(64))
                {
                    var results = await this.Table.QueryAsync<int>
                        (connection.Value, sqlBuilder.ToString(), new Tuple<string[]>(items.ToArray()));

                    results.ForEach(x => values.Add(x));

                    if (values.Count > 1)
                    {
                        return -1;
                    }
                }
            }

            return (values.Count == 1) ? values.First() : -1;
        }

    }
}
