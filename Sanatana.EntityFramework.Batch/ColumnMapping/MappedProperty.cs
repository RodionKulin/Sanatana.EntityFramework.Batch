using Sanatana.EntityFramework.Batch.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.ColumnMapping
{
    public class MappedProperty
    {
        //properties
        public List<MappedProperty> ChildProperties { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public string EfDefaultName { get; set; }
        public string EfMappedName { get; set; }
        public object Value { get; set; }



        //dependent properties
        public bool IsComplexProperty
        {
            get
            {
                return ChildProperties != null;
            }
        }



        //methods
        public virtual MappedProperty Copy()
        {
            return new MappedProperty()
            {
                PropertyInfo = PropertyInfo,
                EfDefaultName = EfDefaultName,
                EfMappedName = EfMappedName
            };
        }
    }
}
