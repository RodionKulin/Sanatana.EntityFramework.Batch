using Sanatana.EntityFramework.BatchSpecs.Samples;
using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using SpecsFor.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Sanatana.EntityFramework.BatchSpecs.TestTools.Providers
{
    public class SampleDbContextProvider : Behavior<INeedSampleDatabase>
    {
        //methods
        public override void SpecInit(INeedSampleDatabase instance)
        {
            instance.SampleDatabase = new SampleDbContext();

            instance.MockContainer.Configure(
                cfg => cfg.For<SampleDbContext>().Use(instance.SampleDatabase));
            instance.MockContainer.Configure(
                cfg => cfg.For<DbContext>().Use(instance.SampleDatabase));
        }

        public override void AfterSpec(INeedSampleDatabase instance)
        {
            instance.SampleDatabase.Dispose();
        }
    }
}
