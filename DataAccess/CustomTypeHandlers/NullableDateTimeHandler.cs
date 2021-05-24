using System;
using Dapper;
using System.Data;

namespace DataAccess.CustomTypeHandlers
{
    public class NullableDateTimeHandler
        : SqlMapper.TypeHandler<DateTime?>
    {
        protected NullableDateTimeHandler()
        {
        }
        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.Value = value.HasValue
                ? (object)value.Value
                : DBNull.Value;
        }
        public override DateTime? Parse(object value)
        {
            if (value == null)
                return null;
            if (value is DateTime)
                return (DateTime)value;
            return Convert.ToDateTime(value);
        }
        public static readonly NullableDateTimeHandler Default = new NullableDateTimeHandler();
    }
}
