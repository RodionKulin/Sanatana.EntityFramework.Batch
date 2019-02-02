using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Sanatana.EntityFramework.Batch
{
    internal static class DbQueryExtension
    {
        public static ObjectQuery<T> ToObjectQuery<T>(this DbQuery<T> query)
        {
            var internalQuery = query.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.Name == "_internalQuery")
                .Select(field => field.GetValue(query))
                .First();

            var objectQuery = internalQuery.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.Name == "_objectQuery")
                .Select(field => field.GetValue(internalQuery))
                .Cast<ObjectQuery<T>>()
                .First();

            return objectQuery;
        }

        public static SqlCommand ToSqlCommand<T>(this DbQuery<T> query)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = query.ToString();

            ObjectQuery<T> objectQuery = query.ToObjectQuery();

            foreach (ObjectParameter param in objectQuery.Parameters)
            {
                command.Parameters.AddWithValue(param.Name, param.Value);
            }

            return command;
        }

        public static string ToTraceString<T>(this DbQuery<T> query)
        {
            ObjectQuery<T> objectQuery = query.ToObjectQuery();

            return objectQuery.ToTraceStringWithParameters();
        }

        public static string ToTraceStringWithParameters<T>(this ObjectQuery<T> query)
        {
            string traceString = query.ToTraceString() + "\n";

            foreach (ObjectParameter parameter in query.Parameters)
            {
                traceString += parameter.Name + " [" + parameter.ParameterType.FullName + "] = " + parameter.Value + "\n";
            }

            return traceString;
        }
    }
}
