using System;
using System.Collections.Generic;
using DataAccess.Attributes;
using System.Linq;
using System.Reflection;

namespace DataAccess.Utils
{
    public static class EntityHelper
    {
        public static string GetTableName<TEntity>()
            where TEntity : RepositoryEntity
        {
            var tableAttribute = typeof(TEntity)
                .GetCustomAttributes(typeof(TableAttribute), true)
                .SingleOrDefault() as TableAttribute;

            return tableAttribute?.Name;
        }

        public static EntityDetails GetEntityDetails<TEntity>()
            where TEntity : RepositoryEntity
        {
            var properties = GetProperties<TEntity>()
                .Where(x => x.CustomAttributes
                .Select(y => y.AttributeType)
                .Contains(typeof(PrimaryKeyAttribute)));

            return new EntityDetails
            {
                PrimaryKeyName = properties.First().Name,
                PrimaryKeyColumnyName = properties.First().GetCustomAttribute<ColumnAttribute>().Name,
                PrimaryKeyNam2 = TryOrDefault(() => properties.ElementAt(1))?.Name,
                PrimaryKeColumnyName2 = TryOrDefault(() => properties.ElementAt(1)).GetCustomAttribute<ColumnAttribute>()?.Name
            };
        }
        public static List<TableColumnInfo> GetTableColumnInfo<TEntity>(bool exludePrimaryKey = false)
            where TEntity:RepositoryEntity
        {
            var properties = GetProperties<TEntity>();

            var tableDataTransportEntities = properties.Select(x => new TableColumnInfo
            {
                ColumnName = x.GetCustomAttributes(true)
                .Select(y => (y as ColumnAttribute)?.Name)
                .Where(y => y != null)
                .FirstOrDefault(),
                ParameterName = x.Name.ToNormalizedParameterNameString()
            }).Where(x => x?.ColumnName != null);

            if (exludePrimaryKey)
            {
                var entityDetails = GetEntityDetails<TEntity>();

                tableDataTransportEntities = tableDataTransportEntities
                    .Where(x => x.ColumnName != entityDetails?.PrimaryKeyColumnyName
                    && (entityDetails.PrimaryKeColumnyName2 != null
                    ? (x.ColumnName != entityDetails.PrimaryKeColumnyName2)
                    : true));
            }
            return tableDataTransportEntities.ToList();
        }
        public static string ToNormalizedParameterNameString(this string str)
        {
            var normalizedString = str.Replace(" ", "_");
            return $"@{normalizedString}";
        }
        private static PropertyInfo[] GetProperties<TEntity>() 
            where TEntity : RepositoryEntity
        {
            return typeof(TEntity).GetProperties();
        }
        public class TableColumnInfo
        {
            public string ParameterName { get; set; }
            public string ColumnName { get; set; }
        }
        public static T TryOrDefault<T>(Func<T> func, T defaultValue = default(T))
        {
            try
            {
                return func();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
