using System;
using System.Data;

namespace DataAccess.Dynamic
{
    public class DynamicTableColumnDefinition
    {
        public string ColumnName { get; set; }
        public string SqlDataTypeString { get; set; }
        public Type DataType { get; set; }
        public SqlDbType SqlDbType { get; set; }
    }
}
