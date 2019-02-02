using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities
{
    public class OneToManyEntity
    {
        public int OneToManyEntityId { get; set; }
        public string Name { get; set; }


        public virtual ICollection<ManyToOneEntity> ManyToOnes { get; set; }
    }
}
