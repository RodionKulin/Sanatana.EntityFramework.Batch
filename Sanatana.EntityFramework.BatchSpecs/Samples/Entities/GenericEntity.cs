using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities
{
    public class GenericEntity<T>
        where T :struct
    {
        public T EntityId { get; set; }
        public string Name { get; set; }
    }
}
