using NUnit.Framework;
using Sanatana.EntityFramework.Batch.Commands.Tests.Samples;
using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using SpecsFor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sanatana.EntityFramework.Batch;
using Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities;
using Sanatana.EntityFramework.Batch.Reflection;

namespace Sanatana.EntityFramework.BatchSpecs
{
    public class ReflectionUtilitySpecs
    {
        [TestFixture]
        public class when_getting_entity_member_names : SpecsFor<SampleDbContext>
            , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_returns_entity_member_name()
            {
                string propertyName = ReflectionUtility.GetMemberName<SampleEntity>(x => x.IntProperty);

                Assert.AreEqual(nameof(SampleEntity.IntProperty), propertyName);
            }
        }
    }
}
