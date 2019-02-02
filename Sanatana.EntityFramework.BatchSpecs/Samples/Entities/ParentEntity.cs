using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities
{
    public class ParentEntity
    {
        public int ParentEntityId { get; set; }
        public DateTime CreatedTime { get; set; }
        public EmbeddedEntity Embedded { get; set; }
    }
}
