using log4net;
using System;
using System.Collections.Generic;
using DataAccess.Extensions;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.DbObjectStores
{
    public class SqlTableRepository
        : RepositoryBase, ISqlTableRepository
    {
        public SqlTableRepository(IDbConnection dbConnection, ILog logger = null)
            :base(dbConnection, logger)
        {
        }
        public SqlTableRepository(string dbConnectionString, ILog logger = null)
           : base(dbConnectionString, logger)
        {
        }
        public async Task<IList<string>> GetManyTablesStartingWithAsync(string keyword)
        => await _dbConnection.GetManyTablesStartingWithAsync(keyword);
    }
}
