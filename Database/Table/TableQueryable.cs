using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Dapper;
using Database.Search;

namespace Database.Table
{

    public interface ITableQueryable<T>
    {
    }

    public class TableQueryable<T> : ITableQueryable<T>
    {
        private readonly IDbConnection connection;
        private ITypedTable Table { get; set; }

        private string SelectSql { get; set; }
        private string WhereSql { get; set; }

        private long TakeCount { get; set; } = 0;
        private long SkipCount { get; set; } = 0;

        private readonly List<string> orders;

        private bool isDistinct = false;


        public TableQueryable(IDbConnection connection, ITypedTable table)
        {
            this.connection = connection;
            this.Table = table;
            this.orders = new List<string>();
        }


        public TableQueryable<TResult> Select<TResult>(params string[] sql)
        {
            if (sql == null || sql.Length <= 0)
            {
                throw new ArgumentException("Invalid SQL");
            }

            var table = new TableQueryable<TResult>(this.connection, this.Table);

            var text = (sql.Length == 1) ? sql[0] : string.Join(", ", sql);

            table.SelectSql = text;
            table.WhereSql = this.WhereSql;
            table.TakeCount = this.TakeCount;
            table.SkipCount = this.SkipCount;
            table.orders.Clear();
            table.orders.AddRange(this.orders);
            table.isDistinct = this.isDistinct;

            return table;
        }


        public TableQueryable<T> Take(long count)
        {
            this.TakeCount = count;
            return this;
        }

        public TableQueryable<T> Skip(long count)
        {
            this.SkipCount = count;
            return this;
        }


        public TableQueryable<T> OrderBy(params string[] sql)
        {
            this.orders.AddRange(sql);
            return this;
        }

        public TableQueryable<T> Where(IDatabaseExpression sql)
        {
            this.WhereSql = sql.GetSql();
            return this;
        }

        public TableQueryable<T> Distinct()
        {
            this.isDistinct = true;
            return this;
        }


        private string MakeSql()
        {
            var whereText = (string.IsNullOrWhiteSpace(this.WhereSql)) ? "" : $" WHERE {this.WhereSql}";
            var selectText = (string.IsNullOrWhiteSpace(this.SelectSql)) ? "*" : this.SelectSql;
            var limitText
                = (this.TakeCount <= 0) ? ""
                : (this.SkipCount <= 0) ? $" LIMIT {this.TakeCount}"
                : $" LIMIT {this.SkipCount}, {this.TakeCount}";

            var orderText
                = (this.orders.Count <= 0) ? ""
                : " ORDER BY " + this.orders.Join(", ");

            var distinct = this.isDistinct ? " DISTINCT" : "";

            return $"SELECT{distinct} {selectText} FROM {this.Table.Name}{whereText}{orderText}{limitText}";

        }

        public async Task<IEnumerable<T>> AsEnumerableAsync(object param = null)
        {
            return await this.connection.QueryAsync<T>(this.MakeSql(), param);
        }

        public IEnumerable<T> AsEnumerable(object param = null)
        {
            return this.connection.Query<T>(this.MakeSql(), param);
        }

        public T First(object param = null)
            => this.Take(1).AsEnumerable(param).First();

        public T FirstOrDefault(object param = null)
            => this.Take(1).AsEnumerable(param).FirstOrDefault();

        public T[] ToArray(object param = null)
            => this.AsEnumerable(param).ToArray();


        public async Task<T> FirstAsync(object param = null)
            => (await this.Take(1).AsEnumerableAsync(param)).First();

        public async Task<T> FirstOrDefaultAsync(object param = null)
            => (await this.Take(1).AsEnumerableAsync(param)).FirstOrDefault();

        public async Task<T[]> ToArrayAsync(object param = null)
            => (await this.AsEnumerableAsync(param)).ToArray();

    }
}
