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
    public class LibraryQueryHelper: DisposableBase
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


        public async Task UpdateRatingAsync(IEnumerable<string> items, int value)
        {

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"UPDATE {this.Table.Name} SET {nameof(Record.Rating)}");
            sqlBuilder.Append($" = {value} WHERE {nameof(Record.Id)} IN @{nameof(Tuple<object>.Item1)}");

            var sql = sqlBuilder.ToString();

            await this.RequestWithBufferedArrayAsync(items, sql);

        }

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

        
        public async Task<HashSet<int>> GetCommonTagsAsync(string[] ids)
        {
            //var ids = items.ToArray();
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

        private async Task<long> CountItemsWithoutTag
            (IDbConnection connection, IEnumerable<string> items,int bufferLength,string filter)
        {

            foreach (var ids in items.Buffer(bufferLength))
            {
                var param = new Tuple<string[]>(ids.ToArray());

                var woTag = await this.Table.CountAsync(connection, filter,param);
                if (woTag > 0)
                {
                    return woTag;
                }
            }
            return 0;
        }
    }
}
