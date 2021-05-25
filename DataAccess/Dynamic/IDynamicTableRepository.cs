using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Dynamic
{
    public interface IDynamicTableRepository
    {
        DynamicTableEntityDefinition TableDefinition { get; }
        Task CreateTableAsync();
        Task DropTableAsync();
        Task DropTableIfExistsAsync();
        Task BulkInsertAsync(IList<DynamicTableRows> records);
        DynamicTableRows CreateRow();
        Task<int> DeleteManyAsync(
            string sqlTableName,
            string columnName,
            IList<object> values);
    }
}
