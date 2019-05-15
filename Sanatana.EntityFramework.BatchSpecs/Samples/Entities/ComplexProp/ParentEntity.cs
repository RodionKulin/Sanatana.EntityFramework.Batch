using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.Samples.Entities
{
    public class ParentEntity
    {
        public int ParentEntityId { get; set; }
        public DateTime CreatedTime { get; set; }
        public EmbeddedEntity Embedded { get; set; }
    }
}
