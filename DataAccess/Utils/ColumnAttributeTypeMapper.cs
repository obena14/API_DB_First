using Dapper;
using DataAccess.Attributes;
using System.Linq;

namespace DataAccess.Utils
{
/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
    public class ColumnAttributeTypeMapper<T>
        : FallbackTypeMapper
    {
        public ColumnAttributeTypeMapper()
            : base(
          new SqlMapper.ITypeMap[]
        {
            new CustomPropertyTypeMap(
                typeof(T),
                (type, columnName) => type.GetProperties()
                    .FirstOrDefault(prop => prop.GetCustomAttributes(inherit: false)
                    .OfType<ColumnAttribute>()
                    .Any(attr => attr.Name == columnName))),
            new DefaultTypeMap(typeof(T))
        })
        { }
    }
}

