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
    public class GroupQuery : IRecordQuery<Record>
    {
        public TypedTable<Record, string> Table { get; }
        public Library Library { get; }

        public GroupQuery(TypedTable<Record, string> table, Library library)
        {
            this.Table = table;
            this.Library = library;
        }

        /// <summary>
        /// グループに所属するファイルの数を問い合わせ
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public async Task<long> CountAsync(Record group)
        {
            using (var connection = await this.Table.Parent.ConnectAsync())
            {
                return await this.Table.CountAsync(connection,
                    this.GetFilterString(group));
            }
        }

        /// <summary>
        /// グループの検索条件を生成
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public string GetFilterString(Record group)
        {
            if (!group.IsGroup)
            {
                throw new ArgumentException();
            }
            return this.GetGroupFilterString(group.Id);
        }

        public string GetGroupFilterString(string groupId)
        {
            return DatabaseFunction.And(DatabaseFunction.IsFalse(nameof(Record.IsGroup)),
                DatabaseFunction.AreEqualWithEscape(nameof(Record.GroupKey), groupId));
        }


        /// <summary>
        /// グループに所属するファイルを取得
        /// </summary>
        /// <param name="group"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<Record[]> SearchAsync(Record group, long skip, long take, Record skipUntil = null)
        {
            return await this.Library.SearchMainAsync(
                this.GetFilterString(group),
                SortSetting.GetFullSql(group.GetSort()),
                skip, take);
        }
    }
}
