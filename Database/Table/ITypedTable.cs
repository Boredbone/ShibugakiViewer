using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Database.Table
{
    public interface ITypedTable
    {
        string Name { get; }
        int Version { get; set; }
        DatabaseFront Parent { get; }
        Dictionary<string, string> TargetProperties { get; }

        bool CreateOrMigrate(IDbConnection connection, TableInformation[] tableInformations);
        void Drop(IDbConnection connection);
        string[] GetColumnInformations(IDbConnection connection);
        void RequestTransaction(Action<DatabaseFront.TransactionContext> action);
        Task RequestTransactionAsync(Action<DatabaseFront.TransactionContext> action);
    }

    public interface ITypedTable<T> : ITypedTable
    {
        Task UpdateAsync
            (T target, IDbConnection connection, IDbTransaction transaction,
            params string[] properties);

        Task AddRangeAsync(IEnumerable<T> items,
            DisposableThreadLocal<IDbConnection> connection, IDbTransaction transaction);


    }
}
