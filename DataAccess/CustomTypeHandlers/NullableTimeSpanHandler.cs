using System;
using Dapper;
using System.Data;

namespace DataAccess.CustomTypeHandlers
{
    public class NullableTimeSpanHandler
        : SqlMapper.TypeHandler<TimeSpan?>
    {
        protected NullableTimeSpanHandler() { }
        public override void SetValue(IDbDataParameter parameter, TimeSpan? value)
        {
            parameter.Value = value.HasValue
                ? (object)value.Value
                : DBNull.Value;
        }
        public override TimeSpan? Parse(object value)
        {
            if (value == null)
                return null;
            if (value is TimeSpan)
                return (TimeSpan)value;
            return TimeSpan.Parse(value.ToString());
        }
        public static readonly NullableTimeSpanHandler Default = new NullableTimeSpanHandler();
    }
}
