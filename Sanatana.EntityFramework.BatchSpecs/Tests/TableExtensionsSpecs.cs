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

namespace Sanatana.EntityFramework.BatchSpecs
{
    public class TableExtensionsSpecs
    {
        [TestFixture]
        public class when_getting_ef_mapped_names : SpecsFor<SampleDbContext>
            , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_returns_column_name()
            {
                string columnName = SUT.GetColumnName<SampleEntity>(x => x.IntProperty);

                Assert.AreEqual(SampleDbContext.SAMPLE_ID_COLUMN_NAME, columnName);
            }

            [Test]
            public void then_it_returns_coplex_object_column_name()
            {
                string columnName = SUT.GetColumnName<ParentEntity>(x => x.Embedded.IsActive);
                Assert.AreEqual("Embedded_IsActive", columnName);
            }

            [Test]
            public void then_it_returns_coplex_object_renamed_column_name()
            {
                string columnName = SUT.GetColumnName<ParentEntity>(x => x.Embedded.Address);
                Assert.AreEqual(SampleDbContext.COMPLEX_TYPE_COLUMN_NAME, columnName);
            }
        }
    }
}
