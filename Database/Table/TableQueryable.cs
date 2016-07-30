using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Dapper;

namespace Database.Table
{

    public interface ITableQueryable<T>
    {
    }

    public class TableQueryable<T> : ITableQueryable<T>
    {
        private IDbConnection connection;
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

        public TableQueryable<T> Where(string sql)
        {
            this.WhereSql = sql;
            return this;
        }

        public TableQueryable<T> Distinct()
        {
            this.isDistinct = true;
            return this;
        }


        private string MakeSql()
        {
            var whereText = (this.WhereSql == null) ? "" : $" WHERE {this.WhereSql}";
            var selectText = (this.SelectSql == null) ? "*" : this.SelectSql;
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

        public async Task<IEnumerable<T>> AsEnumerableAsync()
        {
            return await this.connection.QueryAsync<T>(this.MakeSql());
        }

        public IEnumerable<T> AsEnumerable()
        {
            return this.connection.Query<T>(this.MakeSql());
        }

        public T First()
            => this.Take(1).AsEnumerable().First();

        public T FirstOrDefault()
            => this.Take(1).AsEnumerable().FirstOrDefault();

        public T[] ToArray()
            => this.AsEnumerable().ToArray();


        public async Task<T> FirstAsync()
            => (await this.Take(1).AsEnumerableAsync()).First();

        public async Task<T> FirstOrDefaultAsync()
            => (await this.Take(1).AsEnumerableAsync()).FirstOrDefault();

        public async Task<T[]> ToArrayAsync()
            => (await this.AsEnumerableAsync()).ToArray();
        
    }
}
