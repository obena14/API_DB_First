using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Extensions
{
    public static class IDbConnectionExtensions
    {
        public static async Task<IList<string>> GetManyTablesStartingWithAsync(
            this IDbConnection dbConnection,
            string keyword)
        {
            const string query =
                @"SELECT 
                    t.TABLE_NAME [TableName]
                    FROM INFORMATION_SCHEMA.TABLES t
                  WHERE t.TABLE_NAME LIKE @SqlTableFormat";
            var parameters = new
            {
                SqlTableFormat = $"{keyword}%"
            };

            var result = await dbConnection.QueryAsync<string>(query, parameters);
            return result.AsList();
        }
    }
}
