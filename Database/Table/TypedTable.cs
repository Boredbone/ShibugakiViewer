using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Dapper;

namespace Database.Table
{
    /// <summary>
    /// Table to store the objects of the specified type
    /// </summary>
    /// <typeparam name="TRecord"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class TypedTable<TRecord, TKey> : ITypedTable<TRecord> where TRecord : IRecord<TKey>
    {

        public DatabaseFront Parent { get; }
        public string Name { get; set; }
        public int Version { get; set; } = 1;
        public int BufferSize { get; set; } = 256;
        public int ArrayParameterMaxLength { get; set; } = 128;

        public bool IsIdAuto { get; set; }

        public event EventHandler<MigratingEventArgs> Migrating;

        public bool IsTextKey { get; private set; } = false;

        public Dictionary<string, string> TargetProperties => this.properties.Value;


        private static readonly Dictionary<string, SqliteTypeDefinition> TypeConversion
            = new Dictionary<string, SqliteTypeDefinition>()
            {
                [typeof(int).ToString()] = new SqliteTypeDefinition("integer", false),
                [typeof(uint).ToString()] = new SqliteTypeDefinition("integer", false),
                [typeof(long).ToString()] = new SqliteTypeDefinition("integer", false),
                [typeof(ulong).ToString()] = new SqliteTypeDefinition("integer", false),
                [typeof(float).ToString()] = new SqliteTypeDefinition("real", false),
                [typeof(double).ToString()] = new SqliteTypeDefinition("real", false),
                [typeof(string).ToString()] = new SqliteTypeDefinition("text", true),
                [typeof(bool).ToString()] = new SqliteTypeDefinition("integer", false),
                [typeof(DateTime).ToString()] = new SqliteTypeDefinition("datetime2", false),
                [typeof(DateTimeOffset).ToString()] = new SqliteTypeDefinition("datetimeoffset", false),
                [typeof(DateTime?).ToString()] = new SqliteTypeDefinition("datetime2", true),
                [typeof(DateTimeOffset?).ToString()] = new SqliteTypeDefinition("datetimeoffset", true),
            };

        private readonly Dictionary<string, string> columnOptions;

        private readonly Lazy<Dictionary<string, string>> properties;
        private readonly Lazy<string> values;
        private readonly Lazy<string> targets;
        private readonly Lazy<string> tableSchema;
        private readonly Lazy<string> updateSchema;

        private string columnOrderedValues;


        public TypedTable(DatabaseFront database, string name)
        {
            this.Parent = database;
            this.Name = name;

            if (typeof(TKey) == typeof(string))
            {
                this.IsTextKey = true;
            }

            this.Parent.Add(this);

            this.columnOptions = new Dictionary<string, string>();

            this.properties = new Lazy<Dictionary<string, string>>(() =>
                typeof(TRecord)
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(property => property.CustomAttributes.Any
                        (atr => atr.AttributeType.Equals(typeof(RecordMemberAttribute))))
                    .ToDictionary(x => x.Name, x => x.PropertyType.ToString()));

            var propertiesWithoutId = new Lazy<KeyValuePair<string, string>[]>(() =>
                 this.properties.Value
                   .Where(x => !x.Key.Equals("Id"))
                   .OrderBy(x => x.Key)
                   .ToArray());

            this.values = new Lazy<string>(() =>
                propertiesWithoutId.Value
                    .Select(x => x.Key)
                    .Join(", @"));


            this.targets = new Lazy<string>(() =>
                propertiesWithoutId.Value
                    .Select(x => x.Key)
                    .Join(",\n "));

            this.tableSchema = new Lazy<string>(() =>
            {
                var columns =
                    propertiesWithoutId.Value
                        .Select(x =>
                        {
                            var schema = $"{x.Key} {TypeConversion[x.Value].ToString()}";
                            if (this.columnOptions.ContainsKey(x.Key))
                            {
                                return $"{schema} {this.columnOptions[x.Key]}";
                            }
                            return schema;
                        })
                        .Join(",\n ");

                var idType = TypeConversion[this.properties.Value["Id"]].TypeName;
                return $"Id {idType} primary key,\n {columns}";
            });

            this.updateSchema = new Lazy<string>(() =>
                propertiesWithoutId.Value
                    .Select(x => $"{x.Key} = @{x.Key}")
                    .Join(", "));
        }

        /// <summary>
        /// Delete Table
        /// </summary>
        /// <param name="connection"></param>
        public void Drop(IDbConnection connection)
        {
            try
            {
                connection.Execute($"DROP TABLE {this.Name}");
            }
            catch
            {
            }
        }



        /// <summary>
        /// Set option to the column used when initialization
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="option"></param>
        public void AddColumnOption(string propertyName, string option)
        {
            this.columnOptions[propertyName] = option;
        }

        /// <summary>
        /// Initialize the table
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tableInformations"></param>
        /// <returns></returns>
        public bool CreateOrMigrate(IDbConnection connection, TableInformation[] tableInformations)
        {
            var isExisting = false;
            TableColumnDefinition[] oldColumns = null;

            var isModified = false;

            try
            {
                oldColumns = this.GetColumns(connection);

                if (oldColumns != null && oldColumns.Length > 0)
                {
                    isExisting = true;
                }
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine(e);
            }

            if (!isExisting)
            {
                this.Drop(connection);
                this.Create(connection);
                this.columnOrderedValues = this.values.Value;
                isModified = true;
            }
            else
            {
                isModified = this.Migrate(connection, oldColumns, tableInformations);
            }

            return isModified;
        }

        /// <summary>
        /// Migrate the table
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="existingColumns"></param>
        /// <param name="tableInformations"></param>
        /// <returns></returns>
        private bool Migrate(IDbConnection connection, TableColumnDefinition[] existingColumns,
            TableInformation[] tableInformations)
        {

            var isModified = false;

            var eventArg = new MigratingEventArgs(tableInformations);
            this.Migrating?.Invoke(this, eventArg);


            if (existingColumns == null)
            {
                existingColumns = new TableColumnDefinition[0];
            }


            // New properties
            var addedProperties = this.properties.Value
                .ToDictionary(x => x.Key, x => x.Value);
            foreach (var column in existingColumns)
            {
                if (addedProperties.ContainsKey(column.Name))
                {
                    addedProperties.Remove(column.Name);
                }
            }

            // Removed properties
            var removerProperties = existingColumns
                .Where(x => !this.properties.Value.ContainsKey(x.Name))
                .ToArray();

            // Add columns
            if (addedProperties.Count > 0)
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var column in addedProperties)
                        {

                            var schema = $"{column.Key} {TypeConversion[column.Value].ToString()}";
                            if (this.columnOptions.ContainsKey(column.Key))
                            {
                                schema = $"{schema} {this.columnOptions[column.Key]}";
                            }

                            connection.Execute
                                ($"ALTER TABLE {this.Name} ADD COLUMN {schema}",
                                null, transaction);
                        }

                        transaction.Commit();
                        isModified = true;
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }

            // Remove columns
            if (removerProperties.Length > 0)
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Removing columns is not suppoted in SQLite
                        // Clone table

                        var tmpName = $"tmp_{this.Name}";

                        connection.Execute
                            ($"ALTER TABLE {this.Name} RENAME TO {tmpName}",
                            null, transaction);

                        this.Create(connection, transaction);

                        var selector = this.properties.Value
                            .Where(x => !x.Key.Equals("Id"))
                            .OrderBy(x => x.Key)
                            .Select(x => eventArg.Converters.ContainsKey(x.Key)
                                ? eventArg.Converters[x.Key] : x.Key)
                            .Join(",\n ");

                        connection.Execute
                            ($"INSERT INTO {this.Name}(Id,\n {this.targets.Value}) "
                            + $"SELECT Id,\n {selector} FROM {tmpName}",
                            null, transaction);

                        connection.Execute($"DROP TABLE {tmpName}",
                            null, transaction);

                        transaction.Commit();
                        isModified = true;
                    }
                    catch
                    {
                        transaction.Rollback();
                    }

                    try
                    {
                        connection.Execute($"VACUUM");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }

            }


            this.MakeInsertSchema(connection);

            return isModified;
        }

        /// <summary>
        /// Generate SQL for insert
        /// </summary>
        /// <param name="connection"></param>
        private void MakeInsertSchema(IDbConnection connection)
        {

            TableColumnDefinition[] columns = null;
            try
            {
                columns = this.GetColumns(connection);
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine(e);
            }

            if (columns == null || columns.Length <= 0)
            {
                this.columnOrderedValues = this.values.Value;
                return;
            }

            this.columnOrderedValues = columns
                .Select(x => x.Name)
                .Where(x => !x.Equals("Id"))
                .Join(", @");
        }

        /// <summary>
        /// Create the table
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        private void Create(IDbConnection connection, IDbTransaction transaction = null)
        {
            try
            {
                connection.Execute($"CREATE TABLE {this.Name} (\n {this.tableSchema.Value}\n)",
                    null, transaction);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                if (transaction != null)
                {
                    throw;
                }
            }
        }


        /// <summary>
        /// Execute SQL
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<int> ExecuteAsync
            (IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
        {
            return connection.ExecuteAsync(sql, param, transaction);
        }

        /// <summary>
        /// Execute SQL that returns scalar
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<T> ExecuteScalarAsync<T>
            (IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
        {
            return connection.ExecuteScalarAsync<T>(sql, param, transaction);
        }


        /// <summary>
        /// Add a record
        /// </summary>
        /// <param name="item"></param>
        /// <param name="context"></param>
        public Task AddAsync(TRecord item, DatabaseFront.TransactionContext context)
            => this.AddMainAsync(item, context.Connection, context.Transaction, false);

        /// <summary>
        /// Add a record
        /// </summary>
        /// <param name="item"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        public Task AddAsync(TRecord item, IDbConnection connection, IDbTransaction transaction)
            => this.AddMainAsync(item, connection, transaction, false);
        

        /// <summary>
        /// Add records
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        public Task AddRangeAsync(IEnumerable<TRecord> items,
            DisposableThreadLocal<IDbConnection> connection, IDbTransaction transaction)
            => this.AddMainAsync(items, connection.Value, transaction, false);

        /// <summary>
        /// Add a lot of records
        /// </summary>
        /// <param name="items"></param>
        public async Task AddRangeBufferedAsync(IDbConnection connection, IEnumerable<TRecord> items, bool replace)
        {

            foreach (var buffer in items.Buffer(this.BufferSize))
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await this.AddMainAsync(buffer, connection, transaction, replace);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
                Debug.WriteLine($"{buffer.Count()} Added");
            }

        }
        public async Task AddRangeBufferedAsync(IEnumerable<TRecord> items)
        {
            using (var connection = this.Parent.ConnectAsThreadSafe())
            {
                await this.AddRangeBufferedAsync(connection.Value, items, false);
            }
        }



        public Task ReplaceAsync(TRecord item, DatabaseFront.TransactionContext context)
            => this.AddMainAsync(item, context.Connection, context.Transaction, true);

        public Task ReplaceAsync(TRecord item, IDbConnection connection, IDbTransaction transaction)
            => this.AddMainAsync(item, connection, transaction, true);

        public Task ReplaceRangeAsync(IEnumerable<TRecord> items,
            DatabaseFront.ThreadSafeTransactionContext context)
            => this.ReplaceRangeAsync(items, context.Connection, context.Transaction);

        public Task ReplaceRangeAsync(IEnumerable<TRecord> items,
            DisposableThreadLocal<IDbConnection> connection, IDbTransaction transaction)
            => this.AddMainAsync(items, connection.Value, transaction, true);

        public Task ReplaceRangeAsync(IEnumerable<TRecord> items,
            IDbConnection connection, IDbTransaction transaction)
            => this.AddMainAsync(items, connection, transaction, true);

        public async Task ReplaceRangeBufferedAsync(IEnumerable<TRecord> items)
        {
            using (var connection = this.Parent.ConnectAsThreadSafe())
            {
                await this.AddRangeBufferedAsync(connection.Value, items, true);
            }
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="param"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        private Task AddMainAsync(object param, IDbConnection connection, IDbTransaction transaction, bool replace)
        {
            var command = replace ? "REPLACE" : "INSERT";
            if (this.IsIdAuto)
            {
                return connection.ExecuteAsync
                    ($"{command} INTO {this.Name}({this.targets.Value}) VALUES (@{this.values.Value})",
                    param, transaction);
            }
            else
            {
                return connection.ExecuteAsync
                    ($"{command} INTO {this.Name} VALUES (@Id, @{this.columnOrderedValues})",
                    param, transaction);
            }
        }




        /// <summary>
        /// Update
        /// </summary>
        /// <param name="target"></param>
        /// <param name="context"></param>
        /// <param name="properties"></param>
        public Task UpdateAsync
            (TRecord target, DatabaseFront.TransactionContext context, params string[] properties)
            => this.UpdateAsync(target, context.Connection, context.Transaction, properties);


        /// <summary>
        /// Update
        /// </summary>
        /// <param name="target"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="channnels"></param>
        public async Task UpdateAsync
            (TRecord target, IDbConnection connection, IDbTransaction transaction,
            params string[] channnels)
        {


            var text = (channnels == null || channnels.Length <= 0)
                ? this.updateSchema.Value
                : channnels
                    .Where(x => this.properties.Value.ContainsKey(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => $"{x} = @{x}")
                    .Join(", ");

            if (text.Length <= 0)
            {
                return;
            }
            
            await connection.ExecuteAsync
                ($"UPDATE {this.Name} SET {text} WHERE Id = @Id",
                target, transaction);
        }
        

        /// <summary>
        /// Remove a record
        /// </summary>
        /// <param name="item"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task RemoveAsync(TRecord item, DatabaseFront.TransactionContext context)
        {
            await this.RemoveAsync(item, context.Connection, context.Transaction);
        }

        /// <summary>
        /// Remove a record
        /// </summary>
        /// <param name="item"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task RemoveAsync(TRecord item, IDbConnection connection, IDbTransaction transaction)
        {
            await connection.ExecuteAsync
                ($"DELETE FROM {this.Name} WHERE Id = @Id",// LIMIT 1",
                item, transaction);
        }

        public async Task RemoveAsync
            (object idContainer, string idName, IDbConnection connection, IDbTransaction transaction)
        {
            await connection.ExecuteAsync
                ($"DELETE FROM {this.Name} WHERE Id = @{idName}", idContainer, transaction);
        }

        /// <summary>
        /// Remove records
        /// </summary>
        /// <param name="items"></param>
        /// <param name="context"></param>
        public void RemoveRange(IEnumerable<TRecord> items,
            DatabaseFront.ThreadSafeTransactionContext context)
        {
            this.RemoveRange(items, context.Connection.Value, context.Transaction);
        }

        /// <summary>
        /// Remove records
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        public void RemoveRange(IEnumerable<TRecord> items,
            IDbConnection connection, IDbTransaction transaction)
        {
            var array = items.ToArray();

            items.Distinct(x => x.Id).ForEach(item =>
            {
                connection.Execute
                    ($"DELETE FROM {this.Name} WHERE Id = @Id",
                    item, transaction);
            });
        }

        /// <summary>
        /// Remove a lot of records
        /// </summary>
        /// <param name="items"></param>
        public void RemoveRangeBuffered(IDbConnection connection, IEnumerable<TRecord> items)
        {
            foreach (var buffer in items.Buffer(this.BufferSize))
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        this.RemoveRange(buffer, connection, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }
        public void RemoveRangeBuffered(IEnumerable<TRecord> items)
        {
            using (var connection = this.Parent.ConnectAsThreadSafe())
            {
                this.RemoveRangeBuffered(connection.Value, items);
            }
        }

        /// <summary>
        /// Get informations of columns
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private TableColumnDefinition[] GetColumns(IDbConnection connection)
        {
            using (var cmd = new SQLiteCommand($"PRAGMA table_info({this.Name});"))
            {
                var table = new DataTable();

                cmd.Connection = (SQLiteConnection)connection;

                try
                {
                    var adp = new SQLiteDataAdapter(cmd);
                    adp.Fill(table);

                    return table.Rows
                        .OfType<DataRow>()
                        .Select(x => TableColumnDefinition.FromArray(x.ItemArray))
                        .ToArray();

                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Get informations of columns
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public string[] GetColumnInformations(IDbConnection connection)
        {
            return this.GetColumns(connection).Select(x => x.ToString()).ToArray();
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public long Count(IDbConnection connection)
        {
            return connection
                .ExecuteScalar<long>($@"SELECT COUNT(*) FROM {this.Name}");
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public long Count(IDbConnection connection, string sql)
        {
            return connection
                .ExecuteScalar<long>($@"SELECT COUNT(*) FROM {this.Name} WHERE {sql}");
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<long> CountAsync(IDbConnection connection)
        {
            return await connection
                .ExecuteScalarAsync<long>($@"SELECT COUNT(*) FROM {this.Name}");
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<long> CountAsync(IDbConnection connection, string sql)
        {
            return await connection
                .ExecuteScalarAsync<long>($@"SELECT COUNT(*) FROM {this.Name} WHERE {sql}");
        }

        /// <summary>
        /// Count records with parameter
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task<long> CountAsync(IDbConnection connection, string sql, object parameter)
        {
            return await connection
                .ExecuteScalarAsync<long>($@"SELECT COUNT(*) FROM {this.Name} WHERE {sql}", parameter);
        }


        public async Task<TResult> MaxAsync<TResult>
            (IDbConnection connection, string column, string whereSql, object parameter = null)
        {
            return await connection
                .ExecuteScalarAsync<TResult>($@"SELECT MAX({column}) FROM {this.Name} WHERE {whereSql}", parameter);
        }
        public async Task<TResult> MinAsync<TResult>
            (IDbConnection connection, string column, string whereSql, object parameter = null)
        {
            return await connection
                .ExecuteScalarAsync<TResult>($@"SELECT MIN({column}) FROM {this.Name} WHERE {whereSql}", parameter);
        }

        /// <summary>
        /// Get all records
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public TRecord[] GetAll(IDbConnection connection) => this.AsQueryable(connection).ToArray();

        /// <summary>
        /// Get all records
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<TRecord[]> GetAllAsync(IDbConnection connection)
            => await this.AsQueryable(connection).ToArrayAsync();
        

        /// <summary>
        /// Get a record from ID
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<TRecord> GetRecordFromKeyAsync(IDbConnection connection, TKey key)
        {
            return (await connection
                .QueryAsync<TRecord>
                    ($@"SELECT * FROM {this.Name} WHERE Id = @Id LIMIT 1", new IdContainer(key)))
                .FirstOrDefault();
        }
        
        public async Task<T> GetColumnsFromKeyAsync<T>(IDbConnection connection, TKey key, params string[] columns)
        {
            var selectText = columns.Join(", ");
            return await connection
                .ExecuteScalarAsync<T>
                    ($@"SELECT {selectText} FROM {this.Name} WHERE Id = @Id LIMIT 1", new IdContainer(key));
        }

        /// <summary>
        /// Check existence of ID
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(IDbConnection connection, TKey key)
        {
            return connection
                .Query<TRecord>
                    ($@"SELECT Id FROM {this.Name} WHERE Id = @Id LIMIT 1", new IdContainer(key))
                .FirstOrDefault() != null;
        }


        public async Task<bool> RequestWithBufferedArrayAsync
            (IDbConnection connection, IEnumerable<string> items,
            string sql, Func<string[], object> parameterGenerator)
        {
            var succeeded = true;

            foreach (var ids in items.Buffer(this.ArrayParameterMaxLength))
            {
                var param = parameterGenerator(ids.ToArray());

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await this.ExecuteAsync(connection, sql, param, transaction);

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        transaction.Rollback();
                        succeeded = false;
                    }
                }
            }
            return succeeded;
        }

        /// <summary>
        /// Start SQL construction
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public TableQueryable<TRecord> AsQueryable(IDbConnection connection)
        {
            return new TableQueryable<TRecord>(connection, this);
        }

        /// <summary>
        /// Start transaction
        /// </summary>
        /// <param name="action"></param>
        public void RequestTransaction(Action<DatabaseFront.TransactionContext> action)
            => this.Parent.RequestTransaction(action);

        /// <summary>
        /// Start transaction
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task RequestTransactionAsync(Action<DatabaseFront.TransactionContext> action)
        {
            await Task.Run(() => this.Parent.RequestTransaction(action));
        }
        

        public Task<IEnumerable<T>> QueryAsync<T>
            (IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
        {
            return connection.QueryAsync<T>(sql, param, transaction);
        }

        public async Task<object> GetDynamicParametersAsync
            (IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
        {
            var selector = $"SELECT {sql} FROM {this.Name} WHERE Id = @Id LIMIT 1";

            var dic = (await connection.QueryAsync(selector, param, transaction)).FirstOrDefault();

            if (dic == null)
            {
                return null;
            }

            var items = (IDictionary<string, object>)dic;

            var dbArgs = new DynamicParameters();
            foreach (var pair in items)
            {
                dbArgs.Add(pair.Key, pair.Value);
            }
            return dbArgs;
        }


        private struct IdContainer
        {
            public TKey Id { get; set; }

            public IdContainer(TKey key)
            {
                this.Id = key;
            }
        }

        /// <summary>
        /// Column type
        /// </summary>
        private class SqliteTypeDefinition
        {
            public string TypeName { get; private set; }
            public bool Nullable { get; private set; }

            public SqliteTypeDefinition(string typeName, bool nullable)
            {
                this.TypeName = typeName;
                this.Nullable = nullable;
            }

            public override string ToString()
            {
                return this.Nullable ? this.TypeName : ($"{this.TypeName} not null");
            }
        }

        /// <summary>
        /// Migration information
        /// </summary>
        public class MigratingEventArgs : EventArgs
        {
            public Dictionary<string, string> Converters { get; }
            public TableInformation[] TableInformations { get; }

            public MigratingEventArgs(TableInformation[] tableInformations)
            {
                this.Converters = new Dictionary<string, string>();
                this.TableInformations = tableInformations;
            }
        }
    }
}
