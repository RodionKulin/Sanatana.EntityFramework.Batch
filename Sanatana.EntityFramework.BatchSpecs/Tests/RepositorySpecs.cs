using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using NUnit.Framework;
using Sanatana.EntityFramework.Batch.Commands;
using Sanatana.EntityFramework.Batch.Commands.Tests.Samples;
using SpecsFor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Should;
using SpecsFor.ShouldExtensions;
using Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities;
using Sanatana.EntityFramework.Batch.Commands.Merge;

namespace Sanatana.EntityFramework.BatchSpecs
{
    public class RepositorySpecs
    {
        [TestFixture]
        public class when_inserting_multiple_entities : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }
            
            [Test]
            public void then_it_inserts_multiple_entities()
            {
                int insertCount = 15;
                var entities = new List<SampleEntity>();                
                for (int i = 0; i < insertCount; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = Guid.NewGuid()
                    });
                }

                InsertCommand<SampleEntity> command = SUT.Insert<SampleEntity>();
                command.Insert.ExcludeProperty(x => x.Id);
                int changes = command.Execute(entities);

                changes.ShouldEqual(insertCount);
            }
        }
        
        [TestFixture]
        public class when_inserting_multiple_entities_with_output : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_inserts_multiple_entities_and_outputs_ids()
            {
                int insertCount = 15;
                var entities = new List<SampleEntity>();
                for (int i = 0; i < insertCount; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = Guid.NewGuid()
                    });
                }

                InsertCommand<SampleEntity> command = SUT.Insert<SampleEntity>();
                command.Insert.ExcludeProperty(x => x.Id);
                command.Output.IncludeProperty(x => x.Id);
                int changes = command.Execute(entities);

                changes.ShouldEqual(insertCount);
                entities.ForEach(
                    (entity) => entity.Id.ShouldNotEqual(0));
            }
        }

        [TestFixture]
        public class when_inserting_collection_navigation_property : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            private OneToManyEntity _parentEntity;
            public SampleDbContext SampleDatabase { get; set; }


            protected override void Given()
            {
                _parentEntity = new OneToManyEntity
                {
                    Name = "parent1"
                };
                SampleDatabase.Set<OneToManyEntity>().Add(_parentEntity);
                SampleDatabase.SaveChanges();
            }

            [Test]
            public void then_it_inserts_collection_navigation_property()
            {
                var entities = new List<ManyToOneEntity>
                {
                    new ManyToOneEntity
                    {
                        Name = "name1",
                        OneToManyEntityId = _parentEntity.OneToManyEntityId
                    },
                    new ManyToOneEntity
                    {
                        Name = "name2",
                        OneToManyEntityId = _parentEntity.OneToManyEntityId
                    }
                };

                InsertCommand<ManyToOneEntity> command = SUT.Insert<ManyToOneEntity>();
                command.Insert.ExcludeProperty(x => x.ManyToOneEntityId);
                int changes = command.Execute(entities);

                changes.ShouldEqual(entities.Count);
            }
        }
        
        [TestFixture]
        public class when_inserting_complex_property : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_inserts_complex_property()
            {
                var entities = new List<ParentEntity>
                {
                    new ParentEntity
                    {
                        CreatedTime = DateTime.Now,
                        Embedded = new EmbeddedEntity
                        {
                            Address = "address1",
                            IsActive = true
                        }
                    },
                    new ParentEntity
                    {
                        CreatedTime = DateTime.Now.AddDays(1),
                        Embedded = new EmbeddedEntity
                        {
                            Address = "address2",
                            IsActive = true
                        }
                    }
                };

                InsertCommand<ParentEntity> command = SUT.Insert<ParentEntity>();
                command.Insert.ExcludeProperty(x => x.ParentEntityId);
                int changes = command.Execute(entities);

                changes.ShouldEqual(entities.Count);
            }
        }
        
        [TestFixture]
        public class when_deleting_many : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            private Guid _commonGuidValue = Guid.NewGuid();
            private int _entitiesCount = 10;
            public SampleDbContext SampleDatabase { get; set; }

            protected override void Given()
            {
                var entities = new List<SampleEntity>();
                for (int i = 0; i < _entitiesCount; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = _commonGuidValue
                    });
                }

                InsertCommand<SampleEntity> command = SUT.Insert<SampleEntity>();
                command.Insert.ExcludeProperty(x => x.Id);
                int changes = command.Execute(entities);
            }

            [Test]
            public void then_it_deletes_multiple_entities()
            {
                int changes = SUT.DeleteMany<SampleEntity>(x => x.GuidProperty == _commonGuidValue);
                
                changes.ShouldEqual(_entitiesCount);
            }
        }

        [TestFixture]
        public class when_deleting_by_complex_property : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            private string _address = Guid.NewGuid().ToString();
            private int _entitiesCount = 10;
            public SampleDbContext SampleDatabase { get; set; }

            protected override void Given()
            {
                var entities = new List<ParentEntity>();
                for (int i = 0; i < _entitiesCount; i++)
                {
                    entities.Add(new ParentEntity
                    {
                        CreatedTime = DateTime.Now.AddMinutes(i),
                        Embedded = new EmbeddedEntity
                        {
                            Address = _address,
                            IsActive = true
                        }
                    });
                }

                InsertCommand<ParentEntity> command = SUT.Insert<ParentEntity>();
                command.Insert.ExcludeProperty(x => x.ParentEntityId);
                int changes = command.Execute(entities);
            }

            [Test]
            public void then_it_deletes_by_complex_property()
            {
                int changes = SUT.DeleteMany<ParentEntity>(x => x.Embedded.Address == _address);

                changes.ShouldEqual(_entitiesCount);
            }
        }
        
        [TestFixture]
        public class when_updating_many : SpecsFor<Repository>
           , INeedSampleDatabase
        {
            private Guid _commonGuidValue = Guid.NewGuid();
            private int _entitiesCount = 10;
            public SampleDbContext SampleDatabase { get; set; }

            protected override void Given()
            {
                var entities = new List<SampleEntity>();
                for (int i = 0; i < _entitiesCount; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = _commonGuidValue
                    });
                }

                InsertCommand<SampleEntity> command = SUT.Insert<SampleEntity>();
                command.Insert.ExcludeProperty(x => x.Id);
                int changes = command.Execute(entities);
            }

            [Test]
            public void then_it_updates_multiple_entities()
            {
                UpdateCommand<SampleEntity> updateOp = SUT.UpdateMany<SampleEntity>(
                    x => x.GuidProperty == _commonGuidValue);
                updateOp.Assign(x => x.DateProperty, x => DateTime.Now);
                int changes = updateOp.Execute();

                changes.ShouldEqual(_entitiesCount);
            }
        }

        [TestFixture]
        public class when_selecting_page : SpecsFor<Repository>
         , INeedSampleDatabase
        {
            private Guid _commonGuidValue = Guid.NewGuid();
            private int _entitiesCount = 15;
            public SampleDbContext SampleDatabase { get; set; }

            protected override void Given()
            {
                var entities = new List<SampleEntity>();
                for (int i = 0; i < _entitiesCount; i++)
                {
                    entities.Add(new SampleEntity
                    {
                        GuidNullableProperty = null,
                        DateProperty = DateTime.UtcNow,
                        GuidProperty = _commonGuidValue
                    });
                }

                InsertCommand<SampleEntity> command = SUT.Insert<SampleEntity>();
                command.Insert.ExcludeProperty(x => x.Id);
                int changes = command.Execute(entities);
            }

            [Test]
            public void then_it_selects_page_of_entities()
            {
                int pageSize = 10;

                RepositoryResult<SampleEntity> select = SUT.SelectPage<SampleEntity, DateTime?>(
                    1, pageSize, true
                    , x => x.GuidProperty == _commonGuidValue
                    , x => x.DateProperty
                    , true, false);

                select.Data.Count.ShouldEqual(pageSize);
                select.TotalRows.ShouldEqual(_entitiesCount);
            }
        }
        
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
                command.Insert.ExcludeProperty(x => x.Id);
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
                command.Insert.ExcludeProperty(x => x.Id);
                command.Output.IncludeProperty(x => x.Id);
                int changes = command.Execute(MergeType.Insert);

                changes.ShouldEqual(insertCount);
                entities.ForEach(
                    (entity) => entity.Id.ShouldNotEqual(0));
            }
        }

    }
}
