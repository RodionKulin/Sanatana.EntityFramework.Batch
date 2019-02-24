using Sanatana.EntityFramework.Batch.ColumnMapping;
using Sanatana.EntityFramework.Batch.Expressions;
using Sanatana.EntityFramework.Batch.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands.Merge
{
    public class MergeInsertPart<TEntity> : MappingComponentBase<TEntity>
        where TEntity : class
    {
        //properties
        internal Dictionary<string, string> Defaults { get; set; }


        //init
        internal MergeInsertPart(List<MappedProperty> allEntityProperties, MappedPropertyUtility mappedPropertyUtility)
            : base(allEntityProperties, mappedPropertyUtility)
        {
            Defaults = new Dictionary<string, string>();
        }



        //methods
        /// <summary>
        /// Include property to the list of inserted properties and extract value from entity.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual MergeInsertPart<TEntity> IncludeProperty<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            _includePropertyEfDefaultNames.Add(propName);
            return this;
        }

        /// <summary>
        /// Exclude property from list of inserted properties.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual MergeInsertPart<TEntity> ExcludeProperty<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            _excludePropertyEfDefaultNames.Add(propName);
            return this;
        }

        /// <summary>
        /// Include property to the list of inserted properties with predefined value.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public MergeInsertPart<TEntity> IncludeValue<TProp>(Expression<Func<TEntity, TProp>> property, TProp value)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            string sqlValue = ExpressionsToMSSql.ConstantToMSSql(value, typeof(TProp));
            Defaults[propName] = sqlValue;

            _hasOtherConditions = true;
            return this;
        }

        /// <summary>
        /// Include property to the list of inserted properties with empty value.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public MergeInsertPart<TEntity> IncludeDefaultValue<TProp>(Expression<Func<TEntity, TProp>> property)
        {
            string propName = ReflectionUtility.GetMemberName(property);
            TProp value = default(TProp);
            string sqlValue = ExpressionsToMSSql.ConstantToMSSql(value, typeof(TProp));
            Defaults[propName] = sqlValue;

            _hasOtherConditions = true;
            return this;
        }

    }
}
