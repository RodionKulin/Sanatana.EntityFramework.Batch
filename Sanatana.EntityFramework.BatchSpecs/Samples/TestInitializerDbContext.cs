using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.Samples
{
    public class TestInitializerDbContext : DbContext
    {
        public TestInitializerDbContext()
            : base()
        {
        }

    }
}
