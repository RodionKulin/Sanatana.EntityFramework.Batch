using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Reflection
{
    public static class ReflectionUtility
    {
        /// <summary>
        /// Gets properties of T
        /// </summary>
        public static IEnumerable<PropertyInfo> GetProperties<T>(BindingFlags binding, PropertyReflectionOptions options = PropertyReflectionOptions.All)
        {
            PropertyInfo[] properties = typeof(T).GetProperties(binding);

            bool all = (options & PropertyReflectionOptions.All) != 0;
            bool ignoreIndexer = (options & PropertyReflectionOptions.IgnoreIndexer) != 0;
            bool ignoreEnumerable = (options & PropertyReflectionOptions.IgnoreEnumerable) != 0;

            foreach (PropertyInfo property in properties)
            {
                if (!all)
                {
                    if (ignoreIndexer && IsIndexer(property))
                    {
                        continue;
                    }

                    if (ignoreIndexer 
                        && !property.PropertyType.Equals(typeof(string)) 
                        && IsEnumerable(property))
                    {
                        continue;
                    }
                }

                yield return property;
            }
        }

        /// <summary>
        /// Check if property is indexer
        /// </summary>
        private static bool IsIndexer(PropertyInfo property)
        {
            ParameterInfo[] parameters = property.GetIndexParameters();

            if (parameters != null && parameters.Length > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if property implements IEnumerable
        /// </summary>
        private static bool IsEnumerable(PropertyInfo property)
        {
            return property.PropertyType.GetInterfaces().Any(
                x => x.Equals(typeof(System.Collections.IEnumerable)));
        }

        /// <summary>
        /// Get expression member same as EF default naming. 
        /// Important for complex properties when EF is doing concatenation of names by default.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="selectMemberLambda"></param>
        /// <returns></returns>
        public static string GetMemberName<TEntity, TProp>(Expression<Func<TEntity, TProp>> selectMemberLambda)
        {
            var memberAccess = selectMemberLambda.Body as MemberExpression;
            if (memberAccess == null)
            {
                throw new ArgumentException($"The parameter {nameof(selectMemberLambda)} must be a member accessing lambda such as x => x.Id"
                    , nameof(selectMemberLambda));
            }

            string name = GetExpressionPath(memberAccess);
            return name;
        }

        /// <summary>
        /// Get expression member same as EF default naming. 
        /// Important for complex properties when EF is doing concatenation of names by default.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="selectMemberLambda"></param>
        /// <returns></returns>
        public static string GetMemberName<TEntity>(Expression<Func<TEntity, object>> selectMemberLambda)
        {
            Expression expression = selectMemberLambda.Body;
            if (expression.NodeType == ExpressionType.Convert
                || expression.NodeType == ExpressionType.ConvertChecked)
            {
                var unary = expression as UnaryExpression;
                expression = unary.Operand;
            }

            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberAccess = expression as MemberExpression;
                string name = GetExpressionPath(memberAccess);
                return name;
            }

            throw new ArgumentException(
                $"The parameter {nameof(selectMemberLambda)} must be a member accessing lambda such as x => x.Id"
                , nameof(selectMemberLambda));
        }

        /// <summary>
        /// Get expression member same as EF default naming. 
        /// Important for complex properties when EF is doing concatenation of names by default.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="selectMemberLambda"></param>
        /// <returns></returns>
        public static string GetMemberName(MemberExpression expression)
        {
            string name = GetExpressionPath(expression);
            return name;
        }

        private static string GetExpressionPath(MemberExpression expression)
        {
            var stack = new List<string>();

            while (expression != null)
            {
                stack.Add(expression.Member.Name);
                expression = expression.Expression as MemberExpression;
            }

            stack.Reverse();
            return ConcatenateEfPropertyName(stack);
        }

        public static string ConcatenateEfPropertyName(List<string> hierarchyPropertyNames)
        {
            return string.Join("_", hierarchyPropertyNames);
        }

    }
}
