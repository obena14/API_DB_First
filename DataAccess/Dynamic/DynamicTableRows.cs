using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Dynamic
{
    public class DynamicTableRows
    {
        public int? RowNo { get; set; }
        public IList<DynamicTableCell> RowData { get; set; }
    }

    public class DynamicTableCell
    {
        public string ColumnName { get; set; }
        public object Value { get; set; }
    }
}
