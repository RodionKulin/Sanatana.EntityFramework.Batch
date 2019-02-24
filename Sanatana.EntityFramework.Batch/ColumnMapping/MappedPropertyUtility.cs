using Sanatana.EntityFramework.Batch.Reflection;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.ColumnMapping
{
    public class MappedPropertyUtility
    {
        //fields
        protected static Dictionary<string, List<MappedProperty>> _entityPropertyCache;
        protected DbContext _context;
        protected Type _entityType;


        //init
        static MappedPropertyUtility()
        {
            _entityPropertyCache = new Dictionary<string, List<MappedProperty>>();
        }
        public MappedPropertyUtility(DbContext context, Type entityType)
        {
            _context = context;
            _entityType = entityType;
        }


        //Building property list
        public List<MappedProperty> GetAllEntityProperties()
        {
            if (_entityPropertyCache.ContainsKey(_entityType.FullName) == false)
            {
                List<MappedProperty> properties = GetPropertiesHierarchy(_entityType, _entityType, new List<string>());
                _entityPropertyCache.Add(_entityType.FullName, properties);
            }

            List<MappedProperty> cachedProperties = _entityPropertyCache[_entityType.FullName];
            return CopyProperties(cachedProperties);
        }
        
        protected List<MappedProperty> GetPropertiesHierarchy(Type rootType, Type propertyType, List<string> parentPropertyNames)
        {
            List<MappedProperty> list = new List<MappedProperty>();
           
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            PropertyInfo[] allEntityProperties = propertyType.GetProperties(bindingFlags);
            List<PropertyInfo> mappedProperties = allEntityProperties.Where(
                p =>
                {
                    Type underlined = Nullable.GetUnderlyingType(p.PropertyType);
                    Type property = underlined ?? p.PropertyType;
                    return property.IsValueType == true
                        || property.IsPrimitive
                        || property == typeof(string);
                  
                })
                .ToList();

            foreach (PropertyInfo supportedProp in mappedProperties)
            {
                List<string> namesHierarchy = parentPropertyNames.ToList();
                namesHierarchy.Add(supportedProp.Name);

                string efDefaultName = ReflectionUtility.ConcatenateEfPropertyName(namesHierarchy);
                string efMappedName = DbContextExtensions.GetColumnName(_context, rootType, efDefaultName);
                list.Add(new MappedProperty
                {
                    PropertyInfo = supportedProp,
                    EfDefaultName = efDefaultName,
                    EfMappedName = efMappedName
                });
            }

            List<PropertyInfo> complexProperties = allEntityProperties.Where(
                p => p.PropertyType != typeof(string)
                && p.PropertyType.IsClass)
                .ToList();

            //Exclude navigation properties
            //ICollection properties are excluded already as interfaces and not classes.
            //Here we exclude virtual properties that are not ICollection, by checking if they are virtual.
            complexProperties = complexProperties
                .Where(x => x.GetAccessors()[0].IsVirtual == false)
                .ToList();

            foreach (PropertyInfo complexProp in complexProperties)
            {
                List<string> namesHierarchy = parentPropertyNames.ToList();
                namesHierarchy.Add(complexProp.Name);

                List<MappedProperty> childProperties = GetPropertiesHierarchy(rootType, complexProp.PropertyType, namesHierarchy);
                if (childProperties.Count > 0)
                {
                    list.Add(new MappedProperty
                    {
                        PropertyInfo = complexProp,
                        ChildProperties = childProperties
                    });
                }
            }

            return list;
        }

        protected List<MappedProperty> CopyProperties(List<MappedProperty> properties)
        {
            List<MappedProperty> list = new List<MappedProperty>();

            foreach (MappedProperty prop in properties)
            {
                MappedProperty copy = prop.Copy();
                if (prop.IsComplexProperty)
                {
                    copy.ChildProperties = CopyProperties(prop.ChildProperties);
                }
                list.Add(copy);
            }

            return list;
        }


        //Property values
        public void GetValues(List<MappedProperty> properties, object entity)
        {
            foreach (MappedProperty property in properties)
            {
                property.Value = property.PropertyInfo.GetValue(entity);
                if (property.PropertyInfo.PropertyType.IsEnum)
                {
                    property.Value = (int)property.Value;
                }

                if (property.IsComplexProperty)
                {
                    GetValues(property.ChildProperties, property.Value);
                }
            }
        }


        //Filtering and ordering
        public List<MappedProperty> FilterProperties(List<MappedProperty> properties, bool hasOtherConditions
            , List<string> includeProperties, List<string> excludeProperties
            , bool excludeAllByDefault, IncludeDbGeneratedProperties includeDbGeneratedProperties)
        {
            List<MappedProperty> selectedProperties;

            if (includeProperties.Count > 0 || hasOtherConditions)
            {
                selectedProperties = properties.Where(
                    pr => pr.IsComplexProperty
                    || includeProperties.Contains(pr.EfDefaultName))
                   .ToList();
            }
            else if (excludeProperties.Count > 0)
            {
                selectedProperties = properties.Where(
                    pr => pr.IsComplexProperty
                    || !excludeProperties.Contains(pr.EfDefaultName))
                   .ToList();
            }
            else if (excludeAllByDefault)
            {
                selectedProperties = new List<MappedProperty>();

                if (includeDbGeneratedProperties == IncludeDbGeneratedProperties.IncludeByDefault)
                {
                    List<string> generatedProps = _context.GetDatabaseGeneratedOrComputedKeys(_entityType);
                    selectedProperties = properties
                        .Where(x => generatedProps.Contains(x.EfMappedName))
                        .ToList();
                }
            }
            else //include all by default
            {
                selectedProperties = properties;

                if(includeDbGeneratedProperties == IncludeDbGeneratedProperties.ExcludeByDefault)
                {
                    List<string> generatedProps = _context.GetDatabaseGeneratedOrComputedKeys(_entityType);
                    selectedProperties = selectedProperties
                        .Where(x => !generatedProps.Contains(x.EfMappedName))
                        .ToList();
                }
            }

            //exclude properties that are not part mapped to any column
            selectedProperties = selectedProperties.Where(
                pr => pr.IsComplexProperty 
                || pr.EfMappedName != null)
                .ToList();

            for (int i = 0; i < selectedProperties.Count; i++)
            {
                if (selectedProperties[i].IsComplexProperty)
                {
                    MappedProperty copy = selectedProperties[i].Copy();
                    copy.ChildProperties = FilterProperties(selectedProperties[i].ChildProperties, hasOtherConditions
                        , includeProperties, excludeProperties, excludeAllByDefault, includeDbGeneratedProperties);
                    selectedProperties[i] = copy;
                }
            }

            return selectedProperties;
        }

        public List<MappedProperty> FlattenHierarchy(List<MappedProperty> properties)
        {
            List<MappedProperty> selectedProperties = new List<MappedProperty>();

            foreach (MappedProperty item in properties)
            {
                if (item.IsComplexProperty)
                {
                    List<MappedProperty> children = FlattenHierarchy(item.ChildProperties);
                    selectedProperties.AddRange(children);
                }
                else
                {
                    selectedProperties.Add(item);
                }
            }

            return selectedProperties;
        }

        public List<MappedProperty> OrderFlatBySelectedProperties(List<MappedProperty> properties, List<string> includeProperties)
        {
            if(includeProperties.Count == 0)
            {
                return properties;
            }

            List<MappedProperty> result = new List<MappedProperty>();

            foreach (string includedPropName in includeProperties)
            {
                MappedProperty mappedProperty = properties.First(x => x.EfDefaultName == includedPropName);
                result.Add(mappedProperty);
            }

            return result;
        }
    }
}
