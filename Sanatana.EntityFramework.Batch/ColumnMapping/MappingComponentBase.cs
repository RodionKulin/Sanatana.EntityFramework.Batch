using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.ColumnMapping
{
    public abstract class MappingComponentBase<TEntity>
        where TEntity : class
    {
        //fields
        protected List<MappedProperty> _allEntityProperties;
        protected List<string> _includePropertyEfDefaultNames;
        protected List<string> _excludePropertyEfDefaultNames;
        protected MappedPropertyUtility _mappedPropertyUtility;
        protected bool _hasOtherConditions;


        //properties
        public bool ExcludeAllByDefault { get; set; }

        public IncludeDbGeneratedProperties IncludeGeneratedProperties { get; set; }


        //init
        internal MappingComponentBase(List<MappedProperty> allEntityProperties
            , MappedPropertyUtility mappedPropertyUtility)
        {
            _allEntityProperties = allEntityProperties;
            _mappedPropertyUtility = mappedPropertyUtility;

            _includePropertyEfDefaultNames = new List<string>();
            _excludePropertyEfDefaultNames = new List<string>();
        }


        //methods
        internal virtual List<MappedProperty> GetSelectedFlat()
        {
            List<MappedProperty> selected = _mappedPropertyUtility.FilterProperties(
                _allEntityProperties, _hasOtherConditions, _includePropertyEfDefaultNames
                , _excludePropertyEfDefaultNames, ExcludeAllByDefault, IncludeGeneratedProperties);

            selected = _mappedPropertyUtility.FlattenHierarchy(selected);
            selected = _mappedPropertyUtility.OrderFlatBySelectedProperties(selected, _includePropertyEfDefaultNames);
            return selected;
        }

        internal virtual List<MappedProperty> GetSelectedFlatWithValues(object entity)
        {
            List<MappedProperty> selected = _mappedPropertyUtility.FilterProperties(
                _allEntityProperties, _hasOtherConditions, _includePropertyEfDefaultNames
                , _excludePropertyEfDefaultNames, ExcludeAllByDefault, IncludeGeneratedProperties);

            _mappedPropertyUtility.GetValues(selected, entity);
            selected = _mappedPropertyUtility.FlattenHierarchy(selected);
            selected = _mappedPropertyUtility.OrderFlatBySelectedProperties(selected, _includePropertyEfDefaultNames);

            return selected;
        }

        internal List<string> GetSelectedPropertyNames()
        {
            return GetSelectedFlat()
                .Select(p => p.PropertyInfo.Name)
                .ToList();
        }
    }
}
