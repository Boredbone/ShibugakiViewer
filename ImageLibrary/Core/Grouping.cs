using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ImageLibrary.Core
{
    public class Grouping
    {

        public TypedTable<Record, string> Table { get; }
        public Library Library { get; }

        private readonly string recordSql;
        private readonly string groupSql;

        public Grouping
            (TypedTable<Record, string> table, Library library)
        {
            this.Table = table;
            this.Library = library;

            this.recordSql = $"SELECT DISTINCT {nameof(Record.GroupKey)} FROM {this.Table.Name}"
                + $" WHERE ({nameof(Record.IsGroup)} == 0 AND {nameof(Record.Id)} IN @{nameof(Tuple<string[]>.Item1)})";

            this.groupSql = $"SELECT DISTINCT {nameof(Record.Id)} FROM {this.Table.Name}"
                + $" WHERE ({nameof(Record.IsGroup)} > 0 AND {nameof(Record.Id)} IN @{nameof(Tuple<string[]>.Item1)})";
        }

        public async Task<IEnumerable<string>> GetGroupIds
            (IDbConnection connection, string[] items)
        {
            var list = new List<string>();

            foreach (var ids in items.Buffer(128))
            {
                var param = new Tuple<string[]>(ids.ToArray());

                //渡されたコレクションから設定されているグループを列挙
                var r = await this.Table.QueryAsync<string>(connection, this.recordSql, param);

                list.AddRange(r);
            }

            return list.Where(x => !x.IsNullOrWhiteSpace()).Distinct();
        }


        public async Task<string> GroupAsync(string[] items)
        {
            if (items == null || items.Length < 0)
            {
                throw new ArgumentException();
            }


            //以前に設定されていたグループ
            var oldGroupsList = new List<string[]>();

            //対象に含まれているグループのメンバー
            var groupMembersList = new List<string[]>();

            //対象に含まれているグループ
            string[] relatedGroups;


            using (var connection = await this.Table.Parent.ConnectAsync())
            {
                var relatedGroupsList = new List<string[]>();

                foreach (var ids in items.Buffer(128))
                {
                    var param = new Tuple<string[]>(ids.ToArray());

                    //渡されたコレクションから設定されているグループを列挙
                    var r = this.Table.Query<string>(connection, this.recordSql, param);

                    //対象に含まれているグループを列挙
                    var g = this.Table.Query<string>(connection, this.groupSql, param);

                    oldGroupsList.Add(r.ToArray());
                    relatedGroupsList.Add(g.ToArray());
                }

                //対象に含まれているグループ
                relatedGroups = relatedGroupsList.SelectMany(x => x).Distinct().ToArray();

                //グループメンバーを取得
                foreach (var g in relatedGroups)
                {
                    var filter = this.Library.GroupQuery.GetGroupFilterString(g);

                    var members = this.Table
                        .AsQueryable(connection)
                        .Where(filter)
                        .Select<string>(nameof(Record.Id))
                        .Distinct()
                        .ToArray();

                    groupMembersList.Add(members);
                }

            }

            //以前に設定されていたグループ
            var oldGroups = oldGroupsList.SelectMany(x => x).Distinct().ToArray();

            //対象に含まれているグループのメンバー
            var groupMembers = groupMembersList.SelectMany(x => x).Distinct().ToArray();

            //関係するすべてのグループ
            var uniqueGroups = oldGroups
                .Union(relatedGroups)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();



            string groupKey = null;
            Record group = null;

            if (uniqueGroups.Length == 1)
            {
                //関係するグループが一種類ならそれが新しいグループ
                groupKey = uniqueGroups[0];
            }


            using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            {
                var isNewGroupGenerated = false;

                group = (groupKey != null)
                    ? await this.Table.GetRecordFromKeyAsync(connection.Value, groupKey) : null;


                if (group == null || !group.IsGroup)
                {
                    //新しいグループを作る
                    groupKey = await this.GenerateNewGroupKeyAsync(connection.Value);
                    group = Record.GenerateAsGroup(groupKey);
                    group.SetName(this.GenerateGroupName(connection.Value));
                    isNewGroupGenerated = true;
                }


                //新しいグループを設定
                await this.SetGroupAsync(connection.Value, groupKey, items.Union(groupMembers));

                if (isNewGroupGenerated)
                {
                    var filter = this.Library.GroupQuery.GetGroupFilterString(groupKey);

                    var leader = this.Table.AsQueryable(connection.Value)
                        .Where(filter)
                        .OrderBy(FileProperty.FileName.ToSort(false))
                        .First();

                    //リーダーのタグを全て確認
                    //グループメンバーで同じものを持っていないレコードが一つもない場合はグループにもタグをつける
                    foreach (var tag in leader.TagSet.Read())
                    {
                        var f = DatabaseExpression.And
                            (filter, FileProperty.ContainsTag.ToSearch(tag, CompareMode.NotEqual));
                        var woTag = this.Table.Count(connection.Value, f);
                        if (woTag == 0)
                        {
                            group.TagSet.Add(tag);
                        }
                    }

                    //グループの評価はメンバーの中で最大のもの
                    var rating = await this.Table.MaxAsync<int>
                        (connection.Value, nameof(Record.Rating), filter);
                    group.Rating = rating;

                    //リーダーを設定
                    group.SetGroupLeader(leader);
                }

                //データベースに保存
                using (var transaction = connection.Value.BeginTransaction())
                {
                    try
                    {
                        await this.Table.ReplaceAsync(group, connection.Value, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }

                //グループの情報を更新
                await this.RefreshGroupPropertiesAsync
                    (connection.Value, uniqueGroups.Append(group.Id).Distinct().ToArray());
            }

            return groupKey;
        }

        public async Task RefreshGroupPropertiesAsync(params string[] groups)
        {
            using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            {
                await this.RefreshGroupPropertiesAsync(connection.Value, groups);
            }
        }
        public async Task RefreshGroupPropertiesAsync(IDbConnection connection, params string[] groups)
        {
            if (groups.Length <= 0)
            {
                return;
            }

            using (var transaction = connection.BeginTransaction())
            {

                try
                {
                    foreach (var group in groups)
                    {
                        if (group == null)
                        {
                            continue;
                        }

                        //IsGroup==0の場合はスキップ
                        var isGroup = await this.Table
                            .GetColumnsFromKeyAsync<int>(connection, group, nameof(Record.IsGroup));

                        if (isGroup == 0)
                        {
                            continue;
                        }

                        var filter = this.Library.GroupQuery.GetGroupFilterString(group);

                        //グループに所属するレコードの数
                        var count = await this.Table.CountAsync(connection, filter);

                        var idContainer = new Tuple<string>(group);

                        //メンバーが0のグループは削除
                        if (count <= 0)
                        {
                            await this.Table.RemoveAsync
                                (idContainer, nameof(idContainer.Item1), connection, transaction);
                            continue;
                        }

                        var groupReference = DatabaseReference.ToEqualsString(group);


                        //リーダーを探す
                        var findLeaderSqlBuilder = new StringBuilder();


                        findLeaderSqlBuilder.Append($"SELECT {GroupInfo.LeaderSchema} FROM {this.Table.Name}");
                        findLeaderSqlBuilder.Append($" WHERE ({nameof(Record.Id)} ==");
                        findLeaderSqlBuilder.Append($" (SELECT {nameof(Record.GroupKey)} FROM {this.Table.Name}");
                        findLeaderSqlBuilder.Append($" WHERE {nameof(Record.Id)} == {groupReference})");
                        findLeaderSqlBuilder.Append($" AND {filter}) LIMIT 1");

                        var findLeaderSql = findLeaderSqlBuilder.ToString();

                        var leader = (await this.Table.QueryAsync<GroupInfo>(connection, findLeaderSql))
                            .FirstOrDefault();


                        //グループ内に見つからなければ新しいリーダーを設定
                        if (leader == null)
                        {
                            leader = await this.Table
                                .AsQueryable(connection)
                                .Where(filter)
                                .OrderBy(FileProperty.FileName.ToSort(false))
                                .Select<GroupInfo>(GroupInfo.LeaderSchema)
                                .FirstOrDefaultAsync();
                        }

                        if (leader == null)
                        {
                            continue;
                        }


                        //日付を設定

                        var maxDateSqlBuilder = new StringBuilder();

                        maxDateSqlBuilder.Append
                            ($"SELECT MAX({nameof(Record.DateCreated)}) AS {nameof(GroupInfo.DateCreated)}");
                        maxDateSqlBuilder.Append
                            ($", MAX({nameof(Record.DateModified)}) AS {nameof(GroupInfo.DateModified)}");
                        maxDateSqlBuilder.Append
                            ($", MAX({nameof(Record.DateRegistered)}) AS {nameof(GroupInfo.DateRegistered)}");
                        maxDateSqlBuilder.Append
                            ($" FROM {this.Table.Name}");
                        maxDateSqlBuilder.Append($" WHERE {filter}");

                        var maxDateSql = maxDateSqlBuilder.ToString();

                        var maxDate = (await this.Table.QueryAsync<GroupInfo>(connection, maxDateSql)).FirstOrDefault();

                        var groupInfo = leader.CopyDateFrom(maxDate);

                        groupInfo.TargetGroupId = group;

                        var groupSql = groupInfo.GetUpdateSchema(this.Table.Name);

                        await this.Table.ExecuteAsync(connection, groupSql, groupInfo);

                    }
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    transaction.Rollback();
                }
            }
        }

        private class GroupInfo
        {
            public string Id { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public long Size { get; set; }
            public string Directory { get; set; }

            public DateTimeOffset DateModified { get; set; }
            public DateTimeOffset DateCreated { get; set; }
            public DateTimeOffset DateRegistered { get; set; }

            public string TargetGroupId { get; set; }

            public static string LeaderSchema
                => $"{nameof(Id)}, {nameof(Width)}, {nameof(Height)}, {nameof(Size)}, {nameof(Directory)}";

            public static string DateSchema
                => $"{nameof(DateModified)}, {nameof(DateCreated)}, {nameof(DateRegistered)}";

            private static Lazy<string> UpdateSchema = new Lazy<string>(GenerateUpdateSchema);

            private static string GenerateUpdateSchema()
            {
                var groupSqlBuilder = new StringBuilder();

                groupSqlBuilder.Append($@" SET");

                groupSqlBuilder.Append($@" {nameof(Record.GroupKey)} = @{nameof(GroupInfo.Id)},");
                groupSqlBuilder.Append($@" {nameof(Record.Width)} = @{nameof(GroupInfo.Width)},");
                groupSqlBuilder.Append($@" {nameof(Record.Height)} = @{nameof(GroupInfo.Height)},");
                groupSqlBuilder.Append($@" {nameof(Record.Size)} = @{nameof(GroupInfo.Size)},");
                groupSqlBuilder.Append($@" {nameof(Record.Directory)} = @{nameof(GroupInfo.Directory)},");

                groupSqlBuilder.Append
                    ($@" {nameof(Record.DateCreated)} = @{nameof(GroupInfo.DateCreated)},");
                groupSqlBuilder.Append
                    ($@" {nameof(Record.DateModified)} = @{nameof(GroupInfo.DateModified)},");
                groupSqlBuilder.Append
                    ($@" {nameof(Record.DateRegistered)} = @{nameof(GroupInfo.DateRegistered)}");

                groupSqlBuilder.Append($@" WHERE {nameof(Record.Id)} == @{nameof(GroupInfo.TargetGroupId)}");

                var groupSql = groupSqlBuilder.ToString();

                return groupSql;
            }

            public string GetUpdateSchema(string tableName)
            {
                return $"UPDATE {tableName}{UpdateSchema.Value}";
            }

            public GroupInfo CopyDateFrom(GroupInfo target)
            {
                this.DateModified = target.DateModified;
                this.DateCreated = target.DateCreated;
                this.DateRegistered = target.DateRegistered;
                return this;
            }
        }

        /// <summary>
        /// グループからレコードを退去
        /// </summary>
        /// <param name="group"></param>
        /// <param name="removedItems"></param>
        /// <returns></returns>
        public async Task RemoveFromGroupAsync(Record group, IEnumerable<string> removedItems)
        {
            using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            {
                await this.SetGroupAsync(connection.Value, null, removedItems.Select(x => x));
                await this.RefreshGroupPropertiesAsync(connection.Value, group.Id);
            }
        }


        /// <summary>
        /// 新しいグループを設定
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="groupKey"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        private async Task SetGroupAsync(IDbConnection connection, string groupKey, IEnumerable<string> ids)
        {
            var newGroupSql = $"UPDATE {this.Table.Name} SET {nameof(Record.GroupKey)} = @{nameof(Tuple<string, string[]>.Item1)}"
                + $" WHERE (({nameof(Record.IsGroup)} == 0) AND ({nameof(Record.Id)} IN @{nameof(Tuple<string, string[]>.Item2)}))";

            await this.Table.RequestWithBufferedArrayAsync(connection, ids, newGroupSql,
                x => new Tuple<string, string[]>(groupKey, x));
        }


        /// <summary>
        /// グループのIDを生成
        /// </summary>
        /// <returns></returns>
        private async Task<string> GenerateNewGroupKeyAsync(IDbConnection connection)
        {
            string key;

            for (int i = 0; i < 99; i++)
            {
                key = Guid.NewGuid().ToString();

                var existingId = await this.Table
                    .GetColumnsFromKeyAsync<string>(connection, key, nameof(Record.Id));

                if (existingId == null)
                {
                    return key;
                }
            }

            throw new Exception("Guid Generation Error");

        }

        private static Regex regexRight = new Regex(@"\d+", RegexOptions.RightToLeft);

        /// <summary>
        /// グループの名前を作成
        /// </summary>
        /// <returns></returns>
        private string GenerateGroupName(IDbConnection connection)
        {

            var baseName = this.Library.Searcher.GroupName;


            var existingName = this.Table
                .AsQueryable(connection)
                .Where(DatabaseExpression.Is(DatabaseFunction.ToLower(nameof(Record.FileName)), DatabaseReference.StartsWith(baseName)))
                .Select<string>(nameof(Record.FileName))
                .OrderBy($"{DatabaseFunction.Length(nameof(Record.FileName))} DESC, {nameof(Record.FileName)} DESC")
                .Take(1)
                .FirstOrDefault();

            var count = 0;

            if (existingName != null)
            {
                var succeeded = false;
                if (existingName.StartsWith(baseName))
                {
                    var str = existingName.Substring(baseName.Length);

                    int num;
                    if (int.TryParse(str, out num))
                    {
                        count = num;
                        succeeded = true;
                    }
                }

                if (!succeeded)
                {
                    int num;
                    var nameNumberRightString = regexRight.Match(existingName).ToString();
                    if (int.TryParse(nameNumberRightString, out num))
                    {
                        count = num;
                        succeeded = true;
                    }
                }
            }

            var name = baseName + (count + 1).ToString();

            return name;
        }

    }
}
