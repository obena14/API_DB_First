using Dapper;
using DataAccess.Utils;
using DataAccess.Attributes;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DataAccess
{
    public abstract class RepositoryBase
        : IRepository
    {
        protected IDbConnection _dbConnection;
        protected ILog _logger;

        protected Type[] _acceptedExceptionTypes
            => new[]
            {
                typeof(SqlException),
                typeof(TimeoutException)
            };
        public RepositoryBase(IDbConnection dbConnection, ILog logger = null)
        {
            _dbConnection = dbConnection;
            _logger = logger
                ?? LogManager.GetLogger("LoggerRepository","DefaultLogger");
        }

        public RepositoryBase(string dbConnectionString, ILog logger = null)
            : this(new SqlConnection(dbConnectionString), logger)
        {
        }
        public IDbConnection DbConnection
            => _dbConnection;

        public virtual void UseDatabase(string databaseName)
        {
            try
            {
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_dbConnection.ConnectionString)
                {
                    InitialCatalog = databaseName
                };

                _dbConnection = new SqlConnection(sqlConnectionStringBuilder.ToString());
            }
            catch(Exception ex)
            {
                var thisTypeFullName = GetType().FullName;
                _logger.Error($"Exception occured in '{thisTypeFullName}.{nameof(UseDatabase)}'.", ex);
                throw ex;
            }
        }
    }
    public abstract class RepositoryBase<TEntity>
        : RepositoryBase
        where TEntity:RepositoryEntity
    {
        private const int _connectionWaitTimeoutInSeconds = 60;
        private const int _bulkCopyTimeoutInSeconds = 3600;

        protected virtual string _tableName { get; set; }
        protected virtual string _tableAlias { get; set; }
        protected virtual int _primaryKeyCount => 0;

        public RepositoryBase(IDbConnection dbConnection, ILog logger = null)
            :base(dbConnection,logger)
        {
            _tableName = _tableName
                ?? EntityHelper.GetTableName<TEntity>();
            _tableAlias = "x";

            SqlMapper.SetTypeMap(typeof(TEntity), new ColumnAttributeTypeMapper<TEntity>());
        }
        public RepositoryBase(string dbConnectionString, ILog logger = null)
            : this(new SqlConnection(dbConnectionString), logger)
        {
        }

        public virtual IList<TEntity> GetAll()
        {
            try
            {
                var query =
                    $@"SELECT *
                     FROM [{_tableName}]{_tableAlias}";
                var result = DbConnection.Query<TEntity>(query)
                    .AsList();
                return result;
            }
            catch(Exception ex)
            {
                var thisTypeFullName = GetType().FullName;
                _logger.Error($"Exception occured in '{thisTypeFullName}.{nameof(GetAll)}'.", ex);
                throw ex;
            }
        }

        public async virtual Task BulkInsertAsync(IList<TEntity> entities)
        {
            _logger.Debug($"Executing BulkInsert of type '{typeof(TEntity).Name}' to Database '{_dbConnection.Database}', Table '{_tableName}', with '{entities?.Count() ?? 0}' entities. ");
            if ((entities == null) || (!entities.Any()))
                return;

            var modelEntityType = typeof(TEntity);

            var modelEntityTypePropertyInfos =
                from x in modelEntityType.GetProperties()
                let columnAttribute =
                x.GetCustomAttributes(
                    typeof(ColumnAttribute),
                    inherit: false)
                .Select(y => y as ColumnAttribute)
                let primaryKeyAttribute =
                x.GetCustomAttributes(
                    typeof(PrimaryKeyAttribute),
                    inherit: false)
                where
                    !primaryKeyAttribute.Any()
                select new
                {
                    PropertyInfo = x,
                    ColumnAttribute = columnAttribute
                };

            var dataTable = new DataTable(_tableName);

            var dataColumnAndColumnMappings =
                from x in modelEntityTypePropertyInfos
                let propertyInfo = x.PropertyInfo
                let columnProperType = propertyInfo.PropertyType
                let columnName = x.ColumnAttribute
                    .FirstOrDefault()
                    .Name
                let type = columnProperType.IsGenericType
                    && columnProperType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                    : columnProperType
                select new
                {
                    DataColumn = new DataColumn(columnName, type),
                    ColumnMapping = new SqlBulkCopyColumnMapping(columnName, columnName)
                };

            var dataColumn = dataColumnAndColumnMappings
                .Select(x => x.DataColumn)
                .ToArray();
            var columnMappings = dataColumnAndColumnMappings
                .Select(x => x.ColumnMapping);

            dataTable.Columns
                .AddRange(dataColumn);

            foreach(var modelEntity in entities)
            {
                var values = modelEntityTypePropertyInfos
                    .Select(x => x.PropertyInfo
                    .GetValue(modelEntity, null))
                    .ToArray();

                dataTable.Rows.Add(values);
            }

            try
            {
                var connectoinWaitTimeSpan = TimeSpan.FromSeconds(_connectionWaitTimeoutInSeconds);
                var cancellationTokenSource = new CancellationTokenSource(connectoinWaitTimeSpan);

                var sqlConnection = _dbConnection as SqlConnection;

                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync(cancellationTokenSource.Token);

                using (var sqlTransaction = sqlConnection.BeginTransaction())
                using(var sqlBulkCopy = 
                    new SqlBulkCopy(
                        sqlConnection, 
                        SqlBulkCopyOptions.TableLock,
                        sqlTransaction)
                    { 
                        DestinationTableName = _tableName,
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
                _logger.Error($"Exception occured in '{thisTypeFullName}.{nameof(GetAll)}'.", ex);
                throw ex;
            }
        }

        protected void ValidatePrimaryKeys(string[] primaryKeyNames)
        {
            if (primaryKeyNames.Length < _primaryKeyCount)
                throw new InvalidOperationException("Model has insufficient primary key declaration. ");
            else if (primaryKeyNames.Length > _primaryKeyCount)
                throw new InvalidOperationException("Model has too many primary key declaration. ");
        }
    }
    public abstract class RepositoryBase<TEntity, TPrimaryKey>
        : RepositoryBase<TEntity>
        where TEntity : RepositoryEntity<TPrimaryKey>
    {
        protected override int _primaryKeyCount => 1;
        public RepositoryBase(IDbConnection dbConnection, ILog logger = null) 
        :base(dbConnection, logger)
        { }
        public RepositoryBase(string dbConnection, ILog logger = null)
            : this(new SqlConnection(dbConnection), logger)
        { }
        public virtual TEntity Get(TPrimaryKey id)
        {
            try
            {
                var entityDetails = EntityHelper.GetEntityDetails<TEntity>();

                var query =
                    $@"SELECT *
                     FROM [{_tableName}] {_tableAlias}
                     WHERE {_tableAlias}.[{entityDetails.PrimaryKeyColumnyName}] = {entityDetails.PrimaryKeyColumnyName.ToNormalizedParameterNameString()}";

                var parameters = new Dictionary<string, object>()
                {
                    {$"{entityDetails.PrimaryKeyColumnyName.ToNormalizedParameterNameString()}", id }
                };
                var result = _dbConnection.QuerySingleOrDefault<TEntity>(query, parameters);
                return result;
            }
            catch(Exception ex)
            {
                _logger.Error($"Exception occured in '{ typeof(RepositoryBase<TEntity, TPrimaryKey>).FullName}.{nameof(Get)}'. ", ex);
                throw ex;
            }
        }
        public virtual TEntity Insert(TEntity entity)
        {
            try
            {
                var entityDetails = EntityHelper.GetEntityDetails<TEntity>();
                var tableData = EntityHelper.GetTableColumnInfo<TEntity>(exludePrimaryKey: true);

                var columnNames = tableData.Select(x => $"[{x.ColumnName}]");
                var columnNameString = string.Join(",", columnNames);

                var parameterNames = tableData.Select(x => $"[{x.ParameterName}]");
                var parameterString = string.Join(",", parameterNames);

                var command =
                    $@"INSERT INTO [{_tableName}] ({columnNameString})
                       OUTPUT COALESCE(SCOPE_IDENTITY(), Inserted.[{entityDetails.PrimaryKeyColumnyName}])
                        VALUES({parameterString})";

                var parameterAndValues = entity.


            }
            catch(Exception ex)
            {
                _logger.Error($"Exception occured in '{ typeof(RepositoryBase<TEntity, TPrimaryKey>).FullName}.{nameof(Insert)}'. ", ex);
                throw ex;
            }
        }

    }
}

