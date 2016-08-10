using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Database.Search;

namespace Database.Table
{

    public class DatabaseFront
    {
        public int Version { get; set; } = 1;

        private string connectionString;

        private readonly List<ITypedTable> tables;

        private readonly TypedTable<TableInformation, int> informationTable;

        private const string InformationTableName = "TableInformations";
        private const string DatabaseInformationName = "Database";



        public DatabaseFront(string dataSource)
        {
            this.tables = new List<ITypedTable>();

            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = dataSource,
                LegacyFormat = false,
                Version = 3,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Persist,//.Wal,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,
            };

            this.connectionString = builder.ToString();

            this.informationTable = new TypedTable<TableInformation, int>(this, InformationTableName)
            {
                IsIdAuto = true,
            };
            this.informationTable.AddColumnOption(nameof(TableInformation.TableName), "UNIQUE");
        }

        internal void Add(ITypedTable table)
        {
            this.tables.Add(table);
        }

        public IDbConnection Connect()
        {
            var connection = new SQLiteConnection(this.connectionString);

            connection.Open();

            return connection;
        }

        public DisposableThreadLocal<IDbConnection> ConnectAsThreadSafe()
            => DisposableThreadLocal.Create(() => this.Connect());


        /// <summary>
        /// Initialize Database
        /// </summary>
        /// <param name="connection"></param>
        public void Initialize(IDbConnection connection)
        {
            var obj = typeof(SqlMapper)
                .GetField("typeMap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            var typeMap = (Dictionary<Type, DbType>)obj;

            typeMap.Remove(typeof(DateTimeOffset));
            typeMap.Remove(typeof(DateTimeOffset?));

            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new NullableDateTimeOffsetHandler());


            this.informationTable.Version = this.Version;

            this.tables[0].CreateOrMigrate(connection, null);


            var oldInformations = this.informationTable.AsQueryable(connection).ToArray();
            var informations = oldInformations.OrderBy(x => x.Id).ToList();



            var isDatabaseModified = false;
            var now = DateTimeOffset.Now;

            if (informations.Count <= 0)
            {
                informations.Add(new TableInformation()
                {
                    Created = now,
                    Modified = now,
                    TableName = DatabaseInformationName,
                    Version = this.Version,
                });
                isDatabaseModified = true;
            }
            else if (!informations[0].TableName.Equals(DatabaseInformationName))
            {
                throw new ArgumentException("Parameter mismatch");
            }



            //var count = this.informationTable.Count(connection);
            //using (var transaction = connection.BeginTransaction())
            //{
            //    try
            //    {
            //        if (count <= 0)
            //        {
            //            this.informationTable.Add(new TableInformation()
            //            {
            //                Created = DateTimeOffset.Now,
            //                Modified= DateTimeOffset.Now,
            //                TableName = DatabaseInformationName,
            //                Version = this.Version,
            //            }, connection, transaction);
            //        }
            //        transaction.Commit();
            //    }
            //    catch
            //    {
            //        transaction.Rollback();
            //    }
            //}


            foreach (var table in this.tables.Skip(1))
            {
                if (!informations.Any(x => x.TableName.Equals(table.Name)))
                {
                    var info = new TableInformation()
                    {
                        TableName = table.Name,
                        Created = now,
                        Modified = now,
                        Version = table.Version,
                    };
                    informations.Add(info);
                    isDatabaseModified = true;
                }
            }

            foreach (var table in this.tables.Skip(1))
            {
                var info = informations.First(x => x.TableName.Equals(table.Name));

                var modified = table.CreateOrMigrate(connection, informations.ToArray());

                if (info.Version < table.Version)
                {
                    isDatabaseModified = true;
                }
                if (modified)
                {
                    isDatabaseModified = true;

                    info.Modified = now;
                    info.Version = table.Version;
                }
            }

            if (isDatabaseModified)
            {
                informations[0].Modified = now;
                informations[0].Version = this.Version;
            }

            this.RequestTransaction(async context =>
            {
                //await this.informationTable.ReplaceRangeAsync(informations, context.Connection,context.Transaction);
                foreach (var info in informations)
                {
                    if (!oldInformations.Any(x => x.Id == info.Id))
                    {
                        await this.informationTable.AddAsync(info, context);
                    }
                    else
                    {
                        await this.informationTable.UpdateAsync(info, context);
                    }
                }
            });
        }

        public void RequestTransaction(Action<TransactionContext> action)
        {

            using (var connection = this.Connect())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        action(new TransactionContext(connection, transaction));

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        public void RequestThreadSafeTransaction(Action<ThreadSafeTransactionContext> action)
        {

            using (var connection = this.ConnectAsThreadSafe())
            {
                using (var transaction = connection.Value.BeginTransaction())
                {
                    try
                    {
                        action(new ThreadSafeTransactionContext(connection, transaction));

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        public void InConnection(Action<IDbConnection> action)
        {
            using (var connection = this.Connect())
            {
                action(connection);
            }
        }


        private class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
        {
            public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            {
                parameter.Value = DatabaseFunction.DateTimeOffsetToString(value);
            }

            public override DateTimeOffset Parse(object value)
            {
                return DateTimeOffset.Parse(value.ToString());
            }
        }
        private class NullableDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset?>
        {
            public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
            {
                if (value == null)
                {
                    parameter.Value = null;
                }
                else
                {
                    parameter.Value = DatabaseFunction.DateTimeOffsetToString(value.Value);
                }
            }

            public override DateTimeOffset? Parse(object value)
            {
                if (value == null)
                {
                    return null;
                }
                DateTimeOffset result;
                DateTimeOffset.TryParse(value.ToString(), out result);
                return result;
                //return DateTimeOffset.Parse(value.ToString());
            }
        }

        public class TransactionContext
        {
            public IDbConnection Connection { get; }
            public IDbTransaction Transaction { get; }

            public TransactionContext(IDbConnection connection, IDbTransaction transaction)
            {
                this.Connection = connection;
                this.Transaction = transaction;
            }
        }

        public class ThreadSafeTransactionContext
        {
            public DisposableThreadLocal<IDbConnection> Connection { get; }
            public IDbTransaction Transaction { get; }

            public ThreadSafeTransactionContext
                (DisposableThreadLocal<IDbConnection> connection, IDbTransaction transaction)
            {
                this.Connection = connection;
                this.Transaction = transaction;
            }
        }
    }
}
