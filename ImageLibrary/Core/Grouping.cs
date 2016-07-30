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
        //Tracker<Record, string> RecordTracker { get; }

        public Grouping
            (TypedTable<Record, string> table, Library library)//, Tracker<Record, string> recordTracker)
        {
            this.Table = table;
            this.Library = library;
            //this.RecordTracker = recordTracker;
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


            //var caseSql = $"CASE WHEN {nameof(Record.IsGroup)} == 0 THEN {nameof(Record.GroupKey)}"
            //    +$" ELSE {nameof(Record.Id)} END";

            var recordSql = $"SELECT DISTINCT {nameof(Record.GroupKey)} FROM {this.Table.Name}"
                + $" WHERE ({nameof(Record.IsGroup)} == 0 AND {nameof(Record.Id)} IN @Item1)";

            var groupSql = $"SELECT DISTINCT {nameof(Record.Id)} FROM {this.Table.Name}"
                + $" WHERE ({nameof(Record.IsGroup)} > 0 AND {nameof(Record.Id)} IN @Item1)";

            using (var connection = this.Table.Parent.Connect())
            {
                var relatedGroupsList = new List<string[]>();

                foreach (var ids in items.Buffer(128))
                {
                    var param = new Tuple<string[]>(ids.ToArray());

                    //渡されたコレクションから設定されているグループを列挙
                    var r = await this.Table.QueryAsync<string>(connection, recordSql, param);

                    //対象に含まれているグループを列挙
                    var g = await this.Table.QueryAsync<string>(connection, groupSql, param);

                    oldGroupsList.Add(r.ToArray());
                    relatedGroupsList.Add(g.ToArray());
                }

                //対象に含まれているグループ
                relatedGroups = relatedGroupsList.SelectMany(x => x).Distinct().ToArray();

                //グループメンバーを取得
                foreach (var g in relatedGroups)
                {
                    var filter = this.Library.GroupQuery.GetGroupFilterString(g);

                    var members = await this.Table
                        .AsQueryable(connection)
                        .Where(filter)
                        .Select<string>(nameof(Record.Id))
                        .Distinct()
                        .ToArrayAsync();

                    //var m = await this.Table.QueryAsync<string>(connection, sql);
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


            //var uniqueGroups = items
            //    .Select(x => x.IsGroup ? x.Id : x.GroupKey)
            //    .Where(x => x != null)
            //    .Distinct()
            //    .ToArray();

            string groupKey = null;
            Record group = null;
            //Record leader = null;

            if (uniqueGroups.Length == 1)
            {
                //関係するグループが一種類ならそれが新しいグループ
                groupKey = uniqueGroups[0];
            }
            //else
            //{
            //    //グループがないor複数なら新規グループを作る
            //    groupKey = this.GenerateNewGroupKey();
            //
            //    //group = Record.GenerateAsGroup(groupKey);
            //
            //    //group.SetName(this.GenerateGroupName());
            //}


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
                

                /*
                var newGroupSql = $"UPDATE {this.Table.Name} SET {nameof(Record.GroupKey)} = @Item1"
                    + $" WHERE ({nameof(Record.IsGroup)} == 0 AND {nameof(Record.Id)} IN @Item2)";

                foreach (var ids in items.Union(groupMembers).Buffer(128))
                {
                    var param = new Tuple<string, string[]>(groupKey, ids.ToArray());

                    using (var transaction = connection.Value.BeginTransaction())
                    {
                        try
                        {
                            await this.Table.ExecuteAsync
                                (connection.Value, newGroupSql, param, transaction);

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                        }
                    }
                }*/

                //if (group.GroupKey != null)
                //{
                //    leader = await this.Table.GetRecordFromKeyAsync(connection.Value, group.GroupKey);
                //}
                if (isNewGroupGenerated)
                {
                    var filter = this.Library.GroupQuery.GetGroupFilterString(groupKey);

                    var leader = await this.Table.AsQueryable(connection.Value)
                        .Where(filter)
                        .OrderBy(FileProperty.FileName.ToSort(false))
                        .FirstAsync();

                    //リーダーのタグを全て確認
                    //グループメンバーで同じものを持っていないレコードが一つもない場合はグループにもタグをつける
                    foreach (var tag in leader.TagSet.Read())
                    {
                        var f = DatabaseFunction.And
                            (filter, FileProperty.ContainsTag.ToSearch(tag, CompareMode.NotEqual));
                        var woTag = await this.Table.CountAsync(connection.Value, f);
                        if (woTag == 0)
                        {
                            group.TagSet.Add(tag);
                        }
                    }

                    //グループの評価はメンバーの中で最大のもの
                    var rating = await this.Table.MaxAsync<int>(connection.Value, nameof(Record.Rating), filter);
                    group.Rating = rating;

                    //リーダーを設定
                    group.SetGroupLeader(leader);
                }

                //データベースに保存
                using (var transaction = connection.Value.BeginTransaction())
                {
                    try
                    {
                        if (isNewGroupGenerated)
                        {
                            await this.Table.AddAsync(group, connection.Value, transaction);
                        }
                        else
                        {
                            await this.Table.UpdateAsync(group, connection.Value, transaction);
                        }

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

            ////リーダーを設定
            //var properties = new HashSet<string>();
            //group.PropertyChanged += (o, e) => properties.Add(e.PropertyName);
            //group.SetGroupLeader(leader);
            //
            //
            //using (var connection = this.Table.Parent.ConnectAsThreadSafe())
            //{
            //    using (var transaction = connection.Value.BeginTransaction())
            //    {
            //
            //        try
            //        {
            //            //日付を設定
            //
            //            //リーダー設定で更新されたプロパティを保存
            //
            //            //メンバーが0のグループは削除
            //
            //            transaction.Commit();
            //        }
            //        catch
            //        {
            //            transaction.Rollback();
            //        }
            //    }
            //}
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
            using (var transaction = connection.BeginTransaction())
            {

                try
                {
                    foreach (var group in groups)
                    {
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

                        var groupReference = DatabaseFunction.ToEqualsString(group);


                        //var gk = await this.Table.QueryAsync<string>(connection,
                        //    $"select GroupKey from {this.Table.Name}  WHERE {nameof(Record.Id)} == {groupReference}");
                        //
                        //var gka = gk?.ToArray();
                        //
                        //if (gka == null || gka.Length <= 0)
                        //{
                        //    Debug.WriteLine("not found");
                        //
                        //}else
                        //{
                        //    foreach(var g in gka)
                        //    {
                        //        Debug.WriteLine($"leader:{g}");
                        //
                        //    }
                        //}


                        //リーダーを探す
                        var findLeaderSqlBuilder = new StringBuilder();
                        

                        findLeaderSqlBuilder.Append($"SELECT {GroupInfo.LeaderSchema} FROM {this.Table.Name}");
                        findLeaderSqlBuilder.Append($" WHERE ({nameof(Record.Id)} ==");
                        findLeaderSqlBuilder.Append($" (SELECT {nameof(Record.GroupKey)} FROM {this.Table.Name}");
                        findLeaderSqlBuilder.Append($" WHERE {nameof(Record.Id)} == {groupReference})");
                        findLeaderSqlBuilder.Append($" AND {filter}) LIMIT 1");

                        var findLeaderSql = findLeaderSqlBuilder.ToString();

                        //Debug.WriteLine(findLeaderSql);

                        var leader = (await this.Table.QueryAsync<GroupInfo>(connection, findLeaderSql))
                            .FirstOrDefault();

                        //Debug.WriteLine(leader?.Id ?? "no leader");

                        //グループ内に見つからなければ新しいリーダーを設定
                        if (leader == null)
                        {
                            leader = await this.Table
                                .AsQueryable(connection)
                                .Where(filter)
                                .OrderBy(FileProperty.FileName.ToSort(false))
                                .Select<GroupInfo>(GroupInfo.LeaderSchema)
                                .FirstOrDefaultAsync();

                            //Debug.WriteLine(leader?.Id ?? "no leader");
                        }

                        if (leader == null)
                        {
                            continue;
                        }

                        //var leaderReference = DatabaseFunction.ToEqualsString(leader);


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

                        ////リーダーを設定
                        //var groupSqlBuilder = new StringBuilder();
                        //
                        //groupSqlBuilder.Append($@"UPDATE {this.Table.Name} SET");
                        //
                        //groupSqlBuilder.Append($@" {nameof(Record.GroupKey)} = @{nameof(GroupInfo.Id)},");
                        //groupSqlBuilder.Append($@" {nameof(Record.Width)} = @{nameof(GroupInfo.Width)},");
                        //groupSqlBuilder.Append($@" {nameof(Record.Height)} = @{nameof(GroupInfo.Height)},");
                        //groupSqlBuilder.Append($@" {nameof(Record.Size)} = @{nameof(GroupInfo.Size)},");
                        //groupSqlBuilder.Append($@" {nameof(Record.Directory)} = @{nameof(GroupInfo.Directory)}");
                        //
                        //groupSqlBuilder.Append
                        //    ($@" {nameof(Record.DateCreated)} = @{nameof(GroupInfo.DateCreated)},");
                        //groupSqlBuilder.Append
                        //    ($@" {nameof(Record.DateModified)} = @{nameof(GroupInfo.DateModified)},");
                        //groupSqlBuilder.Append
                        //    ($@" {nameof(Record.DateRegistered)} = @{nameof(GroupInfo.DateRegistered)}");
                        //
                        //groupSqlBuilder.Append($@" WHERE {nameof(Record.Id)} == @{nameof(GroupInfo.TargetGroupId)}");

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

                //groupSqlBuilder.Append($@"UPDATE {this.Table.Name} SET");
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
            var newGroupSql = $"UPDATE {this.Table.Name} SET {nameof(Record.GroupKey)} = @Item1"
                + $" WHERE (({nameof(Record.IsGroup)} == 0) AND ({nameof(Record.Id)} IN @Item2))";

            await this.Table.RequestWithBufferedArrayAsync(connection, ids, newGroupSql,
                x => new Tuple<string, string[]>(groupKey, x));

            /*
            foreach (var items in ids.Buffer(128))
            {
                var param = new Tuple<string, string[]>(groupKey, items.ToArray());
                

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await this.Table.ExecuteAsync
                            (connection, newGroupSql, param, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }*/
        }

        /*
        /// <summary>
        /// ファイルをグループ化
        /// </summary>
        /// <param name="source">グループ化するファイルのコレクション</param>
        public async Task<Record> GroupAsync(IEnumerable<Record> source)
        {
            var items = source.ToArray();
            //グループ取得
            var group = await GetGroupAsync(items);
            //group.IsLoaded = false;

            //全てのファイルに新しいグループを登録
            foreach (var item in items)
            {
                group.AddToGroup(item);
            }

            //共通のタグを設定
            var first = items.FirstOrDefault();
            if (first != null)
            {
                var buf = new HashSet<int>();

                foreach (var tag in first.TagSet.Read())
                {
                    if (items.All(x => x.TagSet.Contains(tag)))
                    {
                        buf.Add(tag);
                    }
                }

                foreach (var tag in buf)
                {
                    group.TagSet.Add(tag);
                }

            }

            //メンバーの評価の最大値
            var rating = items.Max(x => x.Rating);
            group.Rating = rating;


            //設定を保存

            this.Table.RequestTransaction(context =>
            {
                if (group.IsLoaded)
                {
                    this.Table.Update(group, context);
                }
                else
                {
                    this.Table.Add(group, context);
                    this.RecordTracker.Track(group);
                }
            });

            return group;
        }

        /// <summary>
        /// ファイルのコレクションから設定するべきグループを調べる
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private async Task<Record> GetGroupAsync(IEnumerable<Record> items)
        {
            //渡されたコレクションから設定されているグループを列挙
            var uniqueGroups = items
                .Select(x => x.IsGroup ? x.Id : x.GroupKey)
                .Where(x => x != null)
                .Distinct()
                .ToArray();


            if (uniqueGroups.Length == 1)
            {
                //設定されているグループが一種類ならそれが新しいグループ

                var key = uniqueGroups[0];

                var group = items.Where(x => x.Id.Equals(key)).FirstOrDefault();

                if (group == null)
                {
                    using (var connection = this.Table.Parent.Connect())
                    {
                        group = await this.Table.GetRecordFromKeyAsync(connection, key);
                        this.RecordTracker.Track(group);
                    }
                }

                return group;
            }
            else
            {
                string key;
                using (var connection = this.Table.Parent.Connect())
                {
                    //グループがないor複数なら新規グループを作る
                    key = this.GenerateNewGroupKey(connection);
                }

                var leader = items
                    .Where(x => !x.IsGroup)
                    .OrderBy(x => x.FileName)
                    .FirstOrDefault();

                if (leader != null)
                {
                    leader.IsLoaded = false;
                }
                var group = Record.GenerateAsGroup(key, leader);
                if (leader != null)
                {
                    leader.IsLoaded = true;
                }
                using (var connection = this.Table.Parent.Connect())
                {
                    group.SetName(this.GenerateGroupName(connection));
                }

                return group;
            }
        }
        */


        ///// <summary>
        ///// グループのレコードをデータベースから削除
        ///// </summary>
        ///// <param name="item"></param>
        ///// <returns></returns>
        //public async Task RemoveGroupAsync(Record item)
        //{
        //    if (!item.IsGroup)
        //    {
        //        throw new InvalidOperationException();
        //    }
        //
        //    await this.Table.RequestTransactionAsync(context =>
        //    {
        //        this.Table.Remove(item, context);
        //    });
        //}

        /// <summary>
        /// グループのIDを生成
        /// </summary>
        /// <returns></returns>
        private async Task<string> GenerateNewGroupKeyAsync(IDbConnection connection)
        {
            string key;

            //using (var connection = this.Table.Parent.Connect())
            {
                for (int i = 0; i < 99; i++)
                {
                    key = Guid.NewGuid().ToString();

                    var existingId = await this.Table
                        .GetColumnsFromKeyAsync<string>(connection, key, nameof(Record.Id));
                    //.AsQueryable(connection)
                    //.Where(DatabaseFunction.AreEqual(nameof(Record.Id),key))
                    //.Select<string>($"{nameof(Record.Id)}")
                    //.Take(1)
                    //.FirstOrDefault();

                    if (existingId == null)
                    {
                        return key;
                    }
                }
            }

            throw new Exception("Guid Generation Error");

            //return key;
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
                .Where($"{DatabaseFunction.ToLower(nameof(Record.FileName))} {DatabaseFunction.StartsWith(baseName)}")
                .Select<string>(nameof(Record.FileName))
                .OrderBy($"LENGTH({nameof(Record.FileName)}) DESC, {nameof(Record.FileName)} DESC")
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

            ////using (var connection = this.Table.Parent.Connect())
            //{
            //    for (int i = 0; i < 1024; i++)
            //    {
            //        var name = this.Library.Searcher.GroupName + i.ToString();
            //
            //        var existingName = this.Table
            //            .AsQueryable(connection)
            //            .Where(DatabaseFunction.Match(nameof(Record.FileName), name))
            //            .Select<string>(nameof(Record.FileName))
            //            .Take(1)
            //            .FirstOrDefault();
            //
            //        if (existingName == null)
            //        {
            //            return name;
            //        }
            //    }
            //}

            //return this.Library.Searcher.GroupName;
        }

    }
}
