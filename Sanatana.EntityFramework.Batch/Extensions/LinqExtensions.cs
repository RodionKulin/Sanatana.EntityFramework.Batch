using Sanatana.EntityFramework.Batch.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch
{
    public static class LinqExtensions
    {
        public static Expression<Func<T, U>> Or<T, U>(
            this IEnumerable<Expression<Func<T, U>>> list)
        {
            Expression<Func<T, U>> one = list.FirstOrDefault();
            IEnumerable<Expression<Func<T, U>>> others = list.Skip(1);

            var candidateExpr = Expression.Parameter(typeof(T), "candidate");
            var parameterReplacer = new ParameterReplacer(candidateExpr);

            foreach (Expression<Func<T, U>> another in others)
            {
                var left = parameterReplacer.Replace(one.Body);
                var right = parameterReplacer.Replace(another.Body);
                var body = Expression.Or(left, right);
                one = Expression.Lambda<Func<T, U>>(body, candidateExpr);
            }

            return one;
        }

        public static Expression<Func<T, U>> Or<T, U>(
            this Expression<Func<T, U>> one, Expression<Func<T, U>> another)
        {
            var candidateExpr = Expression.Parameter(typeof(T), "candidate");
            var parameterReplacer = new ParameterReplacer(candidateExpr);

            var left = parameterReplacer.Replace(one.Body);
            var right = parameterReplacer.Replace(another.Body);
            var body = Expression.Or(left, right);

            return Expression.Lambda<Func<T, U>>(body, candidateExpr);
        }

        public static Expression<Func<T, U, V>> Or<T, U, V>(
            this Expression<Func<T, U, V>> one, Expression<Func<T, U, V>> another)
        {
            var candidateExpr = Expression.Parameter(typeof(T), "candidate");
            var parameterReplacer = new ParameterReplacer(candidateExpr);

            var left = parameterReplacer.Replace(one.Body);
            var right = parameterReplacer.Replace(another.Body);
            var body = Expression.Or(left, right);

            return Expression.Lambda<Func<T, U, V>>(body, candidateExpr);
        }

        public static Expression<Func<T, U, V>> Or<T, U, V>(
            this IEnumerable<Expression<Func<T, U, V>>> list)
        {
            Expression<Func<T, U, V>> one = list.FirstOrDefault();
            IEnumerable<Expression<Func<T, U, V>>> others = list.Skip(1);

            var candidateExpr = Expression.Parameter(typeof(T), "candidate");
            var parameterReplacer = new ParameterReplacer(candidateExpr);

            foreach (Expression<Func<T, U, V>> another in others)
            {
                var left = parameterReplacer.Replace(one.Body);
                var right = parameterReplacer.Replace(another.Body);
                var body = Expression.Or(left, right);
                one = Expression.Lambda<Func<T, U, V>>(body, candidateExpr);
            }

            return one;
        }

        public static Expression<Func<T, U>> And<T, U>(this Expression<Func<T, U>> first, Expression<Func<T, U>> second)
        {
            Expression visited = new SwapVisitor(first.Parameters[0], second.Parameters[0]).Visit(first.Body);
            BinaryExpression binaryExpression = Expression.AndAlso(visited, second.Body);
            var lambda = Expression.Lambda<Func<T, U>>(binaryExpression, second.Parameters);
            return lambda;
        }
    }
}
