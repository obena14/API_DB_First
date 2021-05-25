using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Dynamic
{
    public class DynamicTableEntityDefinition
    {
        public string TableName { get; set; }
        public string Schema { get; set; } = "dbo";
        public IList<DynamicTableColumnDefinition> ColumnDefinitions { get; set; }
    }
}
