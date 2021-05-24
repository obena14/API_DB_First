using Dapper;
using System;
using System.Data;

namespace DataAccess.CustomTypeHandlers
{
    public class GenericNullableTypeHandler<T>
        where T : class
    {
        public class NullableTimeSpanHandler
            : SqlMapper.TypeHandler<T>
        {
            protected NullableTimeSpanHandler() { }
          
            public override void SetValue(IDbDataParameter parameter, T value)
            {
                parameter.Value = (object)value ?? DBNull.Value;
            }
            public override T Parse(object value)
            {
                return value != null && value is T
                    ? value as T
                    : null;
            }

            public static readonly NullableTimeSpanHandler Default = new NullableTimeSpanHandler();
        }
    }
}
