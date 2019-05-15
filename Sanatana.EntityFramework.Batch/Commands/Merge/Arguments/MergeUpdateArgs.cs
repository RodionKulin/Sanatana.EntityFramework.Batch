using Sanatana.EntityFramework.Batch.ColumnMapping;
using Sanatana.EntityFramework.Batch.Expressions;
using Sanatana.EntityFramework.Batch.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands.Merge
{
    public class MergeUpdateArgs<TEntity> : CommandArgsBase<TEntity>
        where TEntity : class
    {
        //properties
        internal List<Expression> Expressions { get; set; }


        //init
        internal MergeUpdateArgs(List<MappedProperty> allEntityProperties, MappedPropertyUtility mappedPropertyUtility)
            : base(allEntityProperties, mappedPropertyUtility)
        {
            Expressions = new List<Expression>();
        }

        
        //methods
        /// <summary>
        /// Expression to update columns of Target table. Example: (t) => t.IntProperty, (t, s) => s.OtherIntProperty * 2
        /// where t - target table, s - source table.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="targetProperty"></param>
        /// <param name="assignedValue"></param>
        /// <returns></returns>
        public MergeUpdateArgs<TEntity> Assign<TProp>(Expression<Func<TEntity, TProp>> targetProperty
            , Expression<Func<TEntity, TEntity, TProp>> assignedValue)
        {
            Expressions.Add(new AssignLambdaExpression()
            {
                Left = targetProperty,
                Right = assignedValue
            });
            _hasOtherConditions = true;
            return this;
        }

        /// <summary>
        /// Include property to the list of updated properties.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual MergeUpdateArgs<TEntity> IncludeProperty<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            _includePropertyEfDefaultNames.Add(propName);
            return this;
        }

        /// <summary>
        /// Exclude property from list of updated properties.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual MergeUpdateArgs<TEntity> ExcludeProperty<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            _excludePropertyEfDefaultNames.Add(propName);
            return this;
        }

        /// <summary>
        /// Set defaults when no property is selected.
        /// </summary>
        /// <param name="excludeAllByDefault"></param>
        public virtual MergeUpdateArgs<TEntity> SetExcludeAllByDefault(bool excludeAllByDefault)
        {
            ExcludeAllByDefault = excludeAllByDefault;
            return this;
        }

        /// <summary>
        /// Set defaults for database generated properties when no property is selected.
        /// </summary>
        /// <param name="excludeDbGeneratedByDefault"></param>
        /// <returns></returns>
        public virtual MergeUpdateArgs<TEntity> SetExcludeDbGeneratedByDefault(
            ExcludeOptions excludeDbGeneratedByDefault)
        {
            ExcludeDbGeneratedByDefault = excludeDbGeneratedByDefault;
            return this;
        }

    }
}
