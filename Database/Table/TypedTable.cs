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
using Database.Search;

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
        public string Name { get; }
        public int Version { get; set; } = 1;
        public int BufferSize { get; set; } = 256;
        public int ArrayParameterMaxLength { get; set; } = 128;

        public bool IsIdAuto { get; set; }

        public event EventHandler<MigratingEventArgs> Migrating;

        public bool IsTextKey { get; private set; } = false;

        public Dictionary<string, Type> TargetProperties => this.properties.Value;

        private const string IdName = nameof(IRecord<TKey>.Id);


        private static readonly Dictionary<Type, SqliteTypeDefinition> TypeConversion
            = new Dictionary<Type, SqliteTypeDefinition>()
            {
                [typeof(int)] = new SqliteTypeDefinition("integer", false),
                [typeof(uint)] = new SqliteTypeDefinition("integer", false),
                [typeof(long)] = new SqliteTypeDefinition("integer", false),
                [typeof(ulong)] = new SqliteTypeDefinition("integer", false),
                [typeof(float)] = new SqliteTypeDefinition("real", false),
                [typeof(double)] = new SqliteTypeDefinition("real", false),
                [typeof(string)] = new SqliteTypeDefinition("text", true),
                [typeof(bool)] = new SqliteTypeDefinition("integer", false),
                [typeof(DateTime)] = new SqliteTypeDefinition("integer", false),
                [typeof(DateTimeOffset)] = new SqliteTypeDefinition("integer", false),
                [typeof(DateTime?)] = new SqliteTypeDefinition("integer", true),
                [typeof(DateTimeOffset?)] = new SqliteTypeDefinition("integer", true),
            };

        private readonly Dictionary<string, string> columnOptions;

        private readonly Lazy<Dictionary<string, Type>> properties;
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

            this.properties = new Lazy<Dictionary<string, Type>>(() =>
                typeof(TRecord)
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(property => property.CustomAttributes.Any
                        (atr => atr.AttributeType.Equals(typeof(RecordMemberAttribute))))
                    .ToDictionary(x => x.Name, x => x.PropertyType));

            var propertiesWithoutId = new Lazy<KeyValuePair<string, Type>[]>(() =>
                 this.properties.Value
                   .Where(x => !x.Key.Equals(IdName))
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

                var idType = TypeConversion[this.properties.Value[IdName]].TypeName;
                return $"{IdName} {idType} primary key,\n {columns}";
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

            var converters = eventArg.Converters;

            var convertDate = false;

            // Convert DateTime representation from text to unix time
            foreach (var column in existingColumns)
            {
                if (column.Type.ToLower().Contains("datetime"))
                {
                    converters[column.Name] = $"strftime('%s',{column.Name})";
                    convertDate = true;
                }
            }


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


            //// Removed properties
            //var removedProperties = existingColumns
            //    .Where(x => !this.properties.Value.ContainsKey(x.Name))
            //    .ToArray();


            // Remove columns
            if (convertDate || existingColumns.Any(x => !this.properties.Value.ContainsKey(x.Name)))
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

                        // Create new table
                        this.Create(connection, transaction);

                        var selector = this.properties.Value
                            .Where(x => !x.Key.Equals(IdName))
                            .OrderBy(x => x.Key)
                            .Select(x => converters.GetValueOrAlternative(x.Key, x.Key))
                            .Join(",\n ");

                        connection.Execute
                            ($"INSERT INTO {this.Name}({IdName},\n {this.targets.Value}) "
                            + $"SELECT {IdName},\n {selector} FROM {tmpName}",
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
                .Where(x => !x.Equals(IdName))
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
                        await this.AddMainAsync(buffer, connection, transaction, replace).ConfigureAwait(false);

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
                await this.AddRangeBufferedAsync(connection.Value, items, false).ConfigureAwait(false);
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
                await this.AddRangeBufferedAsync(connection.Value, items, true).ConfigureAwait(false);
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
                    ($"{command} INTO {this.Name} VALUES (@{IdName}, @{this.columnOrderedValues})",
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
                ($"UPDATE {this.Name} SET {text} WHERE {IdName} = @{IdName}",
                target, transaction).ConfigureAwait(false);
        }


        /// <summary>
        /// Remove a record
        /// </summary>
        /// <param name="item"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task RemoveAsync(TRecord item, DatabaseFront.TransactionContext context)
        {
            return this.RemoveAsync(item, context.Connection, context.Transaction);
        }

        /// <summary>
        /// Remove a record
        /// </summary>
        /// <param name="item"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task RemoveAsync(TRecord item, IDbConnection connection, IDbTransaction transaction)
        {
            return connection.ExecuteAsync
                ($"DELETE FROM {this.Name} WHERE {IdName} = @{IdName}",// LIMIT 1",
                item, transaction);
        }

        public Task RemoveAsync
            (object idContainer, string idPropertyName, IDbConnection connection, IDbTransaction transaction)
        {
            return connection.ExecuteAsync
                ($"DELETE FROM {this.Name} WHERE {IdName} = @{idPropertyName}", idContainer, transaction);
        }

        /// <summary>
        /// Remove records
        /// </summary>
        /// <param name="items"></param>
        /// <param name="context"></param>
        private void RemoveRange(IEnumerable<TKey> ids,
            DatabaseFront.ThreadSafeTransactionContext context)
        {
            this.RemoveRange(ids.Distinct(), context.Connection.Value, context.Transaction);
        }

        /// <summary>
        /// Remove records
        /// </summary>
        /// <param name="items"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        private void RemoveRange(IEnumerable<TKey> ids,
            IDbConnection connection, IDbTransaction transaction)
        {
            var param = new Tuple<TKey[]>(ids.ToArray());

            connection.Execute
                ($"DELETE FROM {this.Name} WHERE {IdName} IN @{nameof(param.Item1)}", param, transaction);
        }

        private Task RemoveRangeAsync(IEnumerable<TKey> ids,
            IDbConnection connection, IDbTransaction transaction)
        {
            var param = new Tuple<TKey[]>(ids.ToArray());

            return connection.ExecuteAsync
                ($"DELETE FROM {this.Name} WHERE {IdName} IN @{nameof(param.Item1)}", param, transaction);
        }

        /// <summary>
        /// Remove a lot of records
        /// </summary>
        /// <param name="items"></param>
        public void RemoveRangeBuffered(IDbConnection connection, IEnumerable<TKey> ids)
        {
            foreach (var buffer in ids.Distinct().Buffer(this.BufferSize))
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
        public void RemoveRangeBuffered(IDbConnection connection, IEnumerable<TRecord> items)
            => this.RemoveRangeBuffered(connection, items.Select(x => x.Id));


        public async Task RemoveRangeBufferedAsync(IDbConnection connection, IEnumerable<TKey> ids)
        {
            foreach (var buffer in ids.Distinct().Buffer(this.BufferSize))
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await this.RemoveRangeAsync(buffer, connection, transaction)
                            .ConfigureAwait(false);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }
        public Task RemoveRangeBufferedAsync(IDbConnection connection, IEnumerable<TRecord> items)
            => this.RemoveRangeBufferedAsync(connection, items.Select(x => x.Id));

        public void RemoveRangeBuffered(IEnumerable<TRecord> items)
        {
            using (var connection = this.Parent.ConnectAsThreadSafe())
            {
                this.RemoveRangeBuffered(connection.Value, items);
            }
        }

        /// <summary>
        /// Remove a lot of records
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ids"></param>
        public async Task RemoveRangeBufferedWithFilter
            (IDbConnection connection, IEnumerable<TKey> ids, IDatabaseExpression filter)
        {
            foreach (var buffer in ids.Distinct().Buffer(this.BufferSize))
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var param = new Tuple<TKey[]>(buffer.ToArray());

                        await connection.ExecuteAsync
                            ($"DELETE FROM {this.Name} WHERE (({IdName} IN @{nameof(param.Item1)}) AND {filter.GetSql()})",
                            param, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
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
                .ExecuteScalar<long>($@"SELECT COUNT({IdName}) FROM {this.Name}");
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public long Count(IDbConnection connection, IDatabaseExpression sql)
        {
            return connection
                .ExecuteScalar<long>($@"SELECT COUNT({IdName}) FROM {this.Name} WHERE {sql.GetSql()}");
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public Task<long> CountAsync(IDbConnection connection)
        {
            return connection
                .ExecuteScalarAsync<long>($@"SELECT COUNT({IdName}) FROM {this.Name}");
        }

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public Task<long> CountAsync(IDbConnection connection, IDatabaseExpression sql)
        {
            return connection
                .ExecuteScalarAsync<long>($@"SELECT COUNT({IdName}) FROM {this.Name} WHERE {sql.GetSql()}");
        }

        /// <summary>
        /// Count records with parameter
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public Task<long> CountAsync(IDbConnection connection, IDatabaseExpression sql, object parameter)
        {
            return connection
                .ExecuteScalarAsync<long>($@"SELECT COUNT({IdName}) FROM {this.Name} WHERE {sql.GetSql()}", parameter);
        }


        public Task<TResult> MaxAsync<TResult>
            (IDbConnection connection, string column, IDatabaseExpression whereSql, object parameter = null)
        {
            return connection
                .ExecuteScalarAsync<TResult>($@"SELECT MAX({column}) FROM {this.Name} WHERE {whereSql.GetSql()}", parameter);
        }
        public Task<TResult> MinAsync<TResult>
            (IDbConnection connection, string column, IDatabaseExpression whereSql, object parameter = null)
        {
            return connection
                .ExecuteScalarAsync<TResult>($@"SELECT MIN({column}) FROM {this.Name} WHERE {whereSql.GetSql()}", parameter);
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
        public Task<TRecord[]> GetAllAsync(IDbConnection connection)
            => this.AsQueryable(connection).ToArrayAsync();


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
                    ($@"SELECT * FROM {this.Name} WHERE {IdName} = @{nameof(IdContainer.Id)} LIMIT 1",
                    new IdContainer(key)))
                .FirstOrDefault();
        }
        public TRecord GetRecordFromKey(IDbConnection connection, TKey key)
        {
            return (connection
                .Query<TRecord>
                    ($@"SELECT * FROM {this.Name} WHERE {IdName} = @{nameof(IdContainer.Id)} LIMIT 1",
                    new IdContainer(key)))
                .FirstOrDefault();
        }

        public Task<IEnumerable<TRecord>> GetRecordsFromKeyAsync(IDbConnection connection, TKey[] ids)
        {
            var param = new Tuple<TKey[]>(ids);
            var sql = $"SELECT * FROM {this.Name} WHERE {IdName} IN @{nameof(param.Item1)}";
            return connection.QueryAsync<TRecord>(sql, param);
        }
        public IEnumerable<TRecord> GetRecordsFromKey(IDbConnection connection, TKey[] ids)
        {
            var param = new Tuple<TKey[]>(ids);
            var sql = $"SELECT * FROM {this.Name} WHERE {IdName} IN @{nameof(param.Item1)}";
            return connection.Query<TRecord>(sql, param);
        }

        public Task<T> GetColumnsFromKeyAsync<T>(IDbConnection connection, TKey key, params string[] columns)
        {
            var selectText = columns.Join(", ");
            return connection
                .ExecuteScalarAsync<T>
                    ($@"SELECT {selectText} FROM {this.Name} WHERE {IdName} = @{nameof(IdContainer.Id)} LIMIT 1",
                    new IdContainer(key));
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
                    ($@"SELECT {IdName} FROM {this.Name} WHERE {IdName} = @{nameof(IdContainer.Id)} LIMIT 1",
                    new IdContainer(key))
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
                        await connection.ExecuteAsync(sql, param, transaction).ConfigureAwait(false);

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
        public Task RequestTransactionAsync(Action<DatabaseFront.TransactionContext> action)
        {
            return Task.Run(() => this.Parent.RequestTransaction(action));
        }


        public Task<IEnumerable<T>> QueryAsync<T>
            (IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
        {
            return connection.QueryAsync<T>(sql, param, transaction);
        }
        public IEnumerable<T> Query<T>
            (IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null)
        {
            return connection.Query<T>(sql, param, transaction);
        }

        public async Task<object> GetDynamicParametersAsync
            (IDbConnection connection, string sql, TRecord param, IDbTransaction transaction = null)
        {
            var selector = $"SELECT {sql} FROM {this.Name} WHERE {IdName} = @{IdName} LIMIT 1";

            var dic = (await connection.QueryAsync(selector, param, transaction).ConfigureAwait(false))
                .FirstOrDefault();

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
        public object GetDynamicParameters
            (IDbConnection connection, string sql, TRecord param, IDbTransaction transaction = null)
        {
            var selector = $"SELECT {sql} FROM {this.Name} WHERE {IdName} = @{IdName} LIMIT 1";

            var dic = connection.Query(selector, param, transaction).FirstOrDefault();

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
