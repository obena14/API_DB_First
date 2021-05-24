using Dapper;

namespace DataAccess.CustomTypeHandlers
{
    public class CustomTypeHandlerRegistration
    {
        public static void RegisterHandlers()
        {
            SqlMapper.AddTypeHandler(NullableDateTimeHandler.Default);
            SqlMapper.AddTypeHandler(NullableTimeSpanHandler.Default);
        }
    }
}
