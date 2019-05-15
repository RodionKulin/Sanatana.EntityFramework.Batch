using NUnit.Framework;
using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using SpecsFor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Should;
using Sanatana.EntityFramework.Batch;
using Sanatana.EntityFramework.BatchSpecs.Samples.Entities;
using Sanatana.EntityFramework.BatchSpecs.Samples;
using System.Diagnostics;

namespace Sanatana.EntityFramework.BatchSpecs
{
    public class DbContextExtensionsSpecs
    {
        [TestFixture]
        public class when_getting_table_name : SpecsFor<SampleDbContext>
            , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_returns_renamed_table_name()
            {
                string tableName = SUT.GetTableName<SampleEntity>();
                
                string expectedName = $"[{SampleDbContext.SAMPLE_TABLE_SCHEMA}].[{SampleDbContext.SAMPLE_TABLE_NAME}]";
                Assert.AreEqual(expectedName, tableName);
            }

            [Test]
            public void then_it_returns_plural_default_name()
            {
                string tableName = SUT.GetTableName<ParentEntity>();

                string expectedName = $"[dbo].[ParentEntities]";
                Assert.AreEqual(expectedName, tableName);
            }

            [Test]
            public void then_it_returns_default_table_name()
            {
                string tableName = SUT.GetTableName<CustomKeyName>();

                string expectedName = $"[dbo].[{nameof(CustomKeyName)}s]";
                Assert.AreEqual(expectedName, tableName);
            }
        }

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
            public void then_it_returns_complex_object_column_name()
            {
                string columnName = SUT.GetColumnName<ParentEntity>(x => x.Embedded.IsActive);
                Assert.AreEqual("Embedded_IsActive", columnName);
            }

            [Test]
            public void then_it_returns_complex_object_renamed_column_name()
            {
                string columnName = SUT.GetColumnName<ParentEntity>(x => x.Embedded.Address);
                Assert.AreEqual(SampleDbContext.COMPLEX_TYPE_COLUMN_NAME, columnName);
            }
        }

        [TestFixture]
        public class when_getting_ef_entity_keys : SpecsFor<SampleDbContext>
            , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_returns_conventional_key_name()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetIdKeys<ParentEntity>();
                
                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(ParentEntity.ParentEntityId)
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }

            [Test]
            public void then_it_returns_conventional_key_name_id()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetIdKeys<SampleEntity>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(SampleEntity.Id)
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }
            
            [Test]
            public void then_it_returns_custom_key_name()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetIdKeys<CustomKeyName>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(CustomKeyName.CustomKey)
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }

            [Test]
            public void then_it_returns_compound_key_names()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetIdKeys<CompoundKeyEntity>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(CompoundKeyEntity.CompoundKeyNumber),
                    nameof(CompoundKeyEntity.CompoundKeyString)
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }
        }

        [TestFixture]
        public class when_getting_ef_database_generated_props : SpecsFor<SampleDbContext>
            , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_it_returns_default_identity_names()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetDatabaseGeneratedProperties<ParentEntity>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(ParentEntity.ParentEntityId)
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }

            [Test]
            public void then_it_returns_attributed_identity_names()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetDatabaseGeneratedProperties<AttributedIdDbGenerated>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(AttributedIdDbGenerated.AttributedIdDbGeneratedId)
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }
            
            [Test]
            public void then_it_returns_attributed_and_convention_names()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetDatabaseGeneratedProperties<ConventionKeyDbGenerated>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(ConventionKeyDbGenerated.Id),
                    nameof(ConventionKeyDbGenerated.SimpleProp),
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }

            [Test]
            public void then_it_returns_renamed_computed_names()
            {
                //Invoke
                List<string> actualKeyNames = SUT.GetDatabaseGeneratedProperties<RenamedColumnDbGenerated>();

                //Assert
                actualKeyNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(RenamedColumnDbGenerated.CustomId),
                    SampleDbContext.RENAMED_DB_GENERATED_COLUMN_NAME,
                };
                actualKeyNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }
        }


        [TestFixture]
        public class when_getting_ef_mapped_properties : SpecsFor<SampleDbContext>
            , INeedSampleDatabase
        {
            public SampleDbContext SampleDatabase { get; set; }

            [Test]
            public void then_excludes_all_unmapped_properties()
            {
                //Invoke
                List<string> actualMappedNames = SUT.GetAllMappedProperties<WithSomePropsUnmapped>();

                //Assert
                actualMappedNames.ShouldNotBeNull();
                List<string> expectedKeyNames = new List<string> {
                    nameof(WithSomePropsUnmapped.Id),
                    nameof(WithSomePropsUnmapped.MappedProp1),
                    nameof(WithSomePropsUnmapped.MappedProp2)
                };
                actualMappedNames.SequenceEqual(expectedKeyNames)
                    .ShouldBeTrue("Expected keys list do not match");
            }
        }
    }
}
