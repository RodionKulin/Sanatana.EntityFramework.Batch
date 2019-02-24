using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.Samples.Entities
{
    public class CompoundKeyDbGenerated
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CompoundNumberGenerated { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string CompoundStringGenerated { get; set; }
    }
}
