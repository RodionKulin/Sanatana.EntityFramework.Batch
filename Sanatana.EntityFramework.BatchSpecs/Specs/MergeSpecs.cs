using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using NUnit.Framework;
using Sanatana.EntityFramework.Batch.Commands;
using Sanatana.EntityFramework.BatchSpecs.Samples;
using SpecsFor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Should;
using SpecsFor.ShouldExtensions;
using Sanatana.EntityFramework.Batch.Commands.Merge;
using Sanatana.EntityFramework.BatchSpecs.Samples.Entities;

namespace Sanatana.EntityFramework.BatchSpecs.Specs
{
    public class MergeSpecs
    {
        [TestFixture]
        public class when_merge_inserting_multiple_entities : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_merge_inserts_multiple_entities()
            {
                int insertCount = 15;
                var entities = new List<SampleEntity>();
                for (int i = 0; i < 15; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = Guid.NewGuid()
                    });
                }

                MergeCommand<SampleEntity> command = SUT.Merge<SampleEntity>(entities);
                command.Compare.IncludeProperty(x => x.Id);
                int changes = command.Execute(MergeType.Insert);

                changes.ShouldEqual(insertCount);
            }
        }

        [TestFixture]
        public class when_merge_inserting_multiple_entities_with_output : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_merge_inserts_multiple_entities_with_output_ids()
            {
                int insertCount = 15;
                var entities = new List<SampleEntity>();
                for (int i = 0; i < 15; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = Guid.NewGuid()
                    });
                }

                MergeCommand<SampleEntity> command = SUT.Merge<SampleEntity>(entities);
                command.Compare.IncludeProperty(x => x.Id);
                int changes = command.Execute(MergeType.Insert);

                changes.ShouldEqual(insertCount);
                entities.ForEach(
                    (entity) => entity.Id.ShouldNotEqual(0));
            }
        }

        [TestFixture]
        public class when_merge_updating_multiple_entities : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_updates_target_table()
            {
                var entities = new List<SampleEntity>();
                for (int i = 0; i < 15; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = Guid.NewGuid()
                    });
                }

                MergeCommand<SampleEntity> command = SUT.Merge<SampleEntity>(entities);
                command.Source
                    .IncludeProperty(x => x.Id);
                command.Compare
                    .IncludeProperty(x => x.Id)
                    .Condition((t, s) => t.IntProperty != 5);
                command.UpdateMatched
                    .Assign(t => t.StringProperty, (t, s) => "Updated");

                string sql = command.ConstructCommand(MergeType.Update);
                int changes = command.Execute(MergeType.Update);
            }
        }


        [TestFixture]
        public class when_merge_updating_with_different_type_source : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_updates_target_table()
            {
                var entities = new List<ParentEntity>();
                for (int i = 0; i < 15; i++)
                {
                    entities.Add(new ParentEntity
                    {
                        ParentEntityId = i,
                        CreatedTime = DateTime.UtcNow.AddDays(i),
                        Embedded = null
                    });
                }

                //MergeCommand<SampleEntity> command = SUT.Merge<SampleEntity>(entities);
                //command.Source
                //    .IncludeProperty(x => x.Id);
                //command.Compare
                //    .IncludeProperty(x => x.Id)
                //    .Condition((t, s) => t.IntProperty != 5);
                //command.UpdateMatched
                //    .Assign(t => t.StringProperty, (t, s) => "Updated");

                //string sql = command.ConstructCommand(MergeType.Update);
                //int changes = command.Execute(MergeType.Update);
            }
        }
    }
}
