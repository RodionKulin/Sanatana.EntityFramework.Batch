using Sanatana.EntityFramework.Batch.ColumnMapping;
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
    public class MergeCompareArgs<TEntity> : CommandArgsBase<TEntity>
        where TEntity : class
    {
        //properties
        internal List<Expression> Expressions { get; set; }


        //init
        internal MergeCompareArgs(List<MappedProperty> allEntityProperties, MappedPropertyUtility mappedPropertyUtility)
            : base(allEntityProperties, mappedPropertyUtility)
        {
            Expressions = new List<Expression>();
        }


        //methods
        /// <summary>
        /// Expression to compare properties from Target and Source tables. Example (t, s) => t.IntProperty == s.OtherIntProperty or (t, s) => t.IntProperty == 5
        /// where t - target table, s - source table.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual MergeCompareArgs<TEntity> Condition<TKey>(Expression<Func<TEntity, TEntity, TKey>> condition)
        {
            Expressions.Add(condition);
            _hasOtherConditions = true;
            return this;
        }

        /// <summary>
        /// Include property to the list of compared properties.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual MergeCompareArgs<TEntity> IncludeProperty<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            _includePropertyEfDefaultNames.Add(propName);
            return this;
        }

        /// <summary>
        /// Exclude property from list of compared properties.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual MergeCompareArgs<TEntity> ExcludeProperty<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            _excludePropertyEfDefaultNames.Add(propName);
            return this;
        }

        /// <summary>
        /// Set defaults when no property is selected.
        /// </summary>
        /// <param name="excludeAllByDefault"></param>
        public virtual MergeCompareArgs<TEntity> SetExcludeAllByDefault(bool excludeAllByDefault)
        {
            ExcludeAllByDefault = excludeAllByDefault;
            return this;
        }

        /// <summary>
        /// Set defaults for database generated properties when no property is selected..
        /// </summary>
        /// <param name="excludeDbGeneratedByDefault"></param>
        /// <returns></returns>
        public virtual MergeCompareArgs<TEntity> SetExcludeDbGeneratedByDefault(
            ExcludeOptions excludeDbGeneratedByDefault)
        {
            ExcludeDbGeneratedByDefault = excludeDbGeneratedByDefault;
            return this;
        }
    }
}
