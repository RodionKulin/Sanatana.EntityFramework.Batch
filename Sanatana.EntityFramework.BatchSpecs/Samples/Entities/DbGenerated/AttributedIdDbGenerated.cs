using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.Samples.Entities
{
    public class AttributedIdDbGenerated
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid AttributedIdDbGeneratedId { get; set; }
        public int Counter { get; set; }
        public DateTime? CreationTime { get; set; }
    }
}
