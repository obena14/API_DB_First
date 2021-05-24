using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.DbObjectStores
{
    public interface ISqlTableRepository
    {
        Task<IList<string>> GetManyTablesStartingWithAsync(string keyword);
    }
}
