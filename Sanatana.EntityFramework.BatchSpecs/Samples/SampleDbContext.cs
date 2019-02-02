using Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands.Tests.Samples
{
    public class SampleDbContext : DbContext
    {
        public const string CUSTOM_SCHEMA = "test";
        public const string SAMPLE_TABLE_NAME = "Sample Entities";
        public const string SAMPLE_ID_COLUMN_NAME = "CustomIntColumn";
        public const string COMPLEX_TYPE_COLUMN_NAME = "BuildingAddress";

        public DbSet<SampleEntity> SampleEntities { get; set; }
        public DbSet<ParentEntity> ParentEntities { get; set; }
        public DbSet<GenericDerivedEntity> GenericDerivedEntities { get; set; }
        public DbSet<OneToManyEntity> OneToManyEntities { get; set; }
        public DbSet<ManyToOneEntity> ManyToOneEntities { get; set; }



        //init
        public SampleDbContext()
            : base()
        {
            var initializer = new DropCreateDatabaseIfModelChanges<SampleDbContext>();
            System.Data.Entity.Database.SetInitializer<SampleDbContext>(initializer);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GenericDerivedEntity>()
                .HasKey(x => x.EntityId);

            modelBuilder.Entity<SampleEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<SampleEntity>().Property(x => x.XmlProperty).HasColumnType("xml");
            modelBuilder.Entity<SampleEntity>().Property(x => x.IntProperty).HasColumnName(SAMPLE_ID_COLUMN_NAME);
            modelBuilder.Entity<SampleEntity>().ToTable(SAMPLE_TABLE_NAME, CUSTOM_SCHEMA);
           
            modelBuilder.ComplexType<EmbeddedEntity>()
                .Property(x => x.Address)
                .HasColumnName(COMPLEX_TYPE_COLUMN_NAME);

            modelBuilder.Entity<OneToManyEntity>();

            modelBuilder.Entity<ManyToOneEntity>()
                .HasRequired(x => x.OneToMany)
                .WithMany(x => x.ManyToOnes)
                .HasForeignKey(x => x.OneToManyEntityId)
                .WillCascadeOnDelete(true);
        }
    } 
}
