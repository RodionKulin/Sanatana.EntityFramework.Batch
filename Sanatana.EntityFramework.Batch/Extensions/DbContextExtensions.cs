using Sanatana.EntityFramework.Batch.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Get name of the table used by EF.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetTableName<T>(this DbContext context)
            where T : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            string sql = objectContext.CreateObjectSet<T>().ToTraceString();
            Regex regex = new Regex("FROM (?<table>.*) AS");
            Match match = regex.Match(sql);
            string table = match.Groups["table"].Value;
            return table;
        }

        /// <summary>
        /// Get name of the column used by EF or null if it is not found.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">DbContext</param>
        /// <param name="property">Property of entity that is used to get column name.</param>
        /// <returns></returns>
        public static string GetColumnName<T>(this DbContext context, Expression<Func<T, object>> property)
        {
            string efPropertyName = ReflectionUtility.GetMemberName(property);
            Type entityType = typeof(T);
            return GetColumnName(context, entityType, efPropertyName);
        }

        /// <summary>
        /// Get name of the column used by EF or null if it is not found.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">DbContext</param>
        /// <param name="property">Property of entity that is used to get column name.</param>
        /// <returns></returns>
        public static string GetColumnName(this DbContext context, Type entityType, string efPropertyName)
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            string entityName = entityType.Name;

            List<EdmProperty> entityProperties = objectContext.MetadataWorkspace
               .GetItems(DataSpace.SSpace)
               .Where(m => m.BuiltInTypeKind == BuiltInTypeKind.EntityType)
               .SelectMany(x => (x as EntityType).Properties)
               .Where(m => m.DeclaringType.Name == entityName)
               .ToList();

            EdmProperty selectedProperty = entityProperties.FirstOrDefault(
                x => (string)x.MetadataProperties.First(
                    m => m.Name == "PreferredName").Value == efPropertyName);

            if (selectedProperty == null)
            {
                return null;
            }

            return selectedProperty.Name;
        }


        /// <summary> 
        /// Create DataTable from entity list. 
        /// </summary> 
        public static DataTable ToDataTable<T>(this IEnumerable<T> source, List<string> propertyOrder = null)
        {
            DataTable table = new DataTable();

            BindingFlags binding = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;
            PropertyReflectionOptions options = PropertyReflectionOptions.IgnoreEnumerable |
                PropertyReflectionOptions.IgnoreIndexer;

            Type stringType = typeof(string);
            List<PropertyInfo> properties = ReflectionUtility.GetProperties<T>(binding, options)
                .Where(p => p.PropertyType == stringType || !p.PropertyType.IsClass)
                .ToList();

            if (propertyOrder != null)
            {
                properties = properties.Where(p => propertyOrder.Contains(p.Name)).ToList();
                properties = propertyOrder.Join(properties,
                             k => k,
                             m => m.Name,
                             (k, i) => i)
                             .ToList();
            }

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Type nestedNullableType = property.PropertyType.GetGenericArguments()[0];
                    table.Columns.Add(property.Name, nestedNullableType);
                }
                else if (property.PropertyType.IsEnum)
                {
                    table.Columns.Add(property.Name, typeof(int));
                }
                else
                {
                    table.Columns.Add(property.Name, property.PropertyType);
                }
            }


            foreach (T item in source)
            {
                List<object> rowValues = new List<object>();

                for (int i = 0; i < properties.Count; i++)
                {
                    object propValue = properties[i].GetValue(item, null);

                    if (propValue == null)
                        rowValues.Add(DBNull.Value);
                    else if (properties[i].PropertyType.IsEnum)
                        rowValues.Add((int)propValue);
                    else
                        rowValues.Add(propValue);
                }

                table.Rows.Add(rowValues.ToArray());
            }

            return table;
        }
    }
}
