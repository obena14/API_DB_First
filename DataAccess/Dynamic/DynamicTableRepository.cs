using Dapper;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Constants;
using System.Threading;

namespace DataAccess.Dynamic
{
    public class DynamicTableRepository
        : RepositoryBase,
        IDynamicTableRepository
    {
        private readonly double _connectionWaitTimeoutInSeconds = 30;
        private readonly int _bulkCopyTimeoutInSeconds = 3600;

        public DynamicTableEntityDefinition TableDefinition { get; private set; }

        public DynamicTableRepository(
            IDbConnection dbConnection,
            DynamicTableEntityDefinition tableDefinition,
            ILog logger = null)
            :base(dbConnection, logger)
        {
            TableDefinition = tableDefinition;
        }
        public DynamicTableRepository(
            string dbConnectionString,
            DynamicTableEntityDefinition tableDefinition,
            ILog logger = null)
            : base(dbConnectionString, logger)
        {
            TableDefinition = tableDefinition;
        }
        public async Task CreateTableAsync()
        {
            var columns = TableDefinition.ColumnDefinitions
                .Select(x =>
                {
                    return new
                    {
                        x.ColumnName,
                        x.DataType,
                        SqlDataType = x.SqlDataTypeString
                            ?? GetSqlDataTypeStringFromCsDataType(x.DataType)
                    };
                });
            var columnsFormatted = columns
                .Select(x => $"[{x.ColumnName}] {x.SqlDataType}");
            var columnsString = string.Join(",", columnsFormatted);

            var command =
                $@"CREATE TABLE [{TableDefinition.Schema}].[{TableDefinition.TableName}]({columnsString})";

            await _dbConnection.ExecuteAsync(command);
        }
        public async Task DropTableAsync()
        {
            var command =
                $@"DROP TABLE [{TableDefinition.Schema}].[{TableDefinition.TableName}]";

            await _dbConnection.ExecuteAsync(command);
        }
        public async Task DropTableIfExistsAsync()
        {
            try
            {
                await DropTableAsync();
            }
            catch(SqlException ex)
            when (ex.Number == (int)SqlExceptionType.DropTableError)
            {
                _logger.Debug($"Dropping SQL Table '{TableDefinition.TableName}' has not completed. Table may already not exist. ");
            }
        }
        public async Task BulkInsertAsync(IList<DynamicTableRows> records)
        {
            await Task.Run(() => BulkInsertAsync(records));
        }
        public DynamicTableRows CreateRow()
        {
            var result = new DynamicTableRows
            {
                RowData = TableDefinition.ColumnDefinitions
                    .Select(x => new DynamicTableCell
                    {
                        ColumnName = x.ColumnName
                    })
                    .ToList()
            };
            return result;
        }
        public async Task BulkInsert(IList<DynamicTableRows> records)
        {
            var tableName = TableDefinition.TableName;
            var dataTable = new DataTable(tableName);

            var columnNamesFromRecords = records
                .SelectMany(x => x.RowData)
                .Select(x => x.ColumnName)
                .Distinct()
                .ToList();
            var columnNamesFromDefinition = TableDefinition.ColumnDefinitions
                .Select(x => x.ColumnName)
                .ToList();
            var unexpectedColumnNames = columnNamesFromRecords
                .Except(
                    columnNamesFromDefinition,
                    StringComparer.CurrentCultureIgnoreCase);
            var unexpectedColumnNamesCount = unexpectedColumnNames?
                .Count()
                ?? 0;
            if (unexpectedColumnNamesCount > 0)
                throw new Exception($"Received Dynamic Table Rows have '{unexpectedColumnNamesCount}' unexpected columns");

            var dataColumns = TableDefinition.ColumnDefinitions
                .Select(x => new DataColumn(x.ColumnName))
                .ToArray();

            dataTable.Columns.AddRange(dataColumns);

            var dataRows = records
                .Select(x =>
                {
                    var newRow = dataTable.NewRow();
                    var dataRow = x.RowData
                        .ToDictionary(y => y.ColumnName);
                    var applicableColumnNames = dataRow
                        .Select(y => y.Key);
                    foreach (var columnName in applicableColumnNames)
                        newRow[columnName] = dataRow[columnName].Value;
                    return newRow;
                })
                .ToArray();
            foreach (var dataRow in dataRows)
                dataTable.Rows.Add(dataRow);
            var columnMappings = columnNamesFromDefinition
                    .Select(x => new SqlBulkCopyColumnMapping(x, x));

            try
            {
                var connectionWaitTimeSpan = TimeSpan.FromSeconds(_connectionWaitTimeoutInSeconds);
                var cancellationTokenSource = new CancellationTokenSource(connectionWaitTimeSpan);

                var sqlConnection = _dbConnection as SqlConnection;
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync(cancellationTokenSource.Token);

                using (var sqlTransaction = sqlConnection.BeginTransaction())
                using (var sqlBulkCopy =
                    new SqlBulkCopy(
                        sqlConnection,
                        SqlBulkCopyOptions.TableLock,
                        sqlTransaction)
                    { 
                        DestinationTableName = tableName,
                        BatchSize = 10000,
                        BulkCopyTimeout = _bulkCopyTimeoutInSeconds
                    })
                {
                    foreach (var columnMapping in columnMappings)
                        sqlBulkCopy.ColumnMappings
                            .Add(columnMapping);

                    sqlBulkCopy.WriteToServer(dataTable);
                    sqlTransaction.Commit();
                }
            }
            catch(Exception ex)
            {
                var thisTypeFullName = GetType().FullName;
                _logger.Error($"Exception occured in '{thisTypeFullName}.{nameof(BulkInsert)}'. ", ex);
                throw ex;
            }
        }
        private string GetSqlDataTypeStringFromCsDataType(Type type)
        {
            var result = string.Empty;
            switch (type.Name)
            {
                case "string":
                    result = "varchar(max)";
                    break;
                case "int":
                    result = "int";
                    break;
                case "decimal":
                    result = "decimal(28,20)";
                    break;
                default:
                    throw new InvalidOperationException("Unmapped or unsupported data type. ");
            }
            return result;
        }

        public async Task<int> DeleteManyAsync(
            string sqlTableName, 
            string columnName, 
            IList<object> values)
        {
            var isSqlTableNameAllLetters = sqlTableName
                 .All(x => char.IsLetter(x));
            var isColumnNameAllLetters = columnName
                 .All(x => char.IsLetter(x));

            if (!isSqlTableNameAllLetters)
                throw new Exception($"Not all characters are letters in '{sqlTableName}'. ");
            if (!isColumnNameAllLetters)
                throw new Exception($"Not all characters are letters in '{columnName}'. ");

            var query =
                $@"DELETE
                    FROM [{sqlTableName}]
                    WHERE [{columnName}] IN @ColumnNames";

            var parameters = new
            {
                ColumnNames = values
            };
            var result = await _dbConnection.ExecuteAsync(query, parameters);

            return result;
        }

      

      
    }
}
