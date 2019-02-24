using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.Samples.Entities
{
    public class ManyToOneEntity
    {
        public int ManyToOneEntityId { get; set; }
        public int OneToManyEntityId { get; set; }
        public string Name { get; set; }


        public virtual OneToManyEntity OneToMany { get; set; }
    }
}
