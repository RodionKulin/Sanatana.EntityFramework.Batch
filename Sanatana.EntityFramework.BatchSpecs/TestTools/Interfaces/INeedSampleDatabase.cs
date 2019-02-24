using Sanatana.EntityFramework.BatchSpecs.Samples;
using SpecsFor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces
{
    public interface INeedSampleDatabase : ISpecs
    {
        SampleDbContext SampleDatabase { get; set; }
    }
}
