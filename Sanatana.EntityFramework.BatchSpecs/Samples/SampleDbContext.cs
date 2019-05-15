using Sanatana.EntityFramework.BatchSpecs.Samples.Entities;
using System.Data.Entity;

namespace Sanatana.EntityFramework.BatchSpecs.Samples
{
    public class SampleDbContext : DbContext
    {
        public const string SAMPLE_TABLE_SCHEMA = "test";
        public const string SAMPLE_TABLE_NAME = "Sample Entities";
        public const string SAMPLE_ID_COLUMN_NAME = "CustomIntColumn";
        public const string COMPLEX_TYPE_COLUMN_NAME = "BuildingAddress";
        public const string RENAMED_DB_GENERATED_COLUMN_NAME = "IWasRenamed";

        public DbSet<SampleEntity> SampleEntities { get; set; }
        public DbSet<ParentEntity> ParentsEntity { get; set; }
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

            modelBuilder.Entity<SampleEntity>().Property(x => x.XmlProperty).HasColumnType("xml");
            modelBuilder.Entity<SampleEntity>().Property(x => x.IntProperty).HasColumnName(SAMPLE_ID_COLUMN_NAME);
            modelBuilder.Entity<SampleEntity>().ToTable(SAMPLE_TABLE_NAME, SAMPLE_TABLE_SCHEMA);

            modelBuilder.ComplexType<EmbeddedEntity>()
                .Property(x => x.Address)
                .HasColumnName(COMPLEX_TYPE_COLUMN_NAME);

            modelBuilder.Entity<OneToManyEntity>();

            modelBuilder.Entity<ManyToOneEntity>()
                .HasRequired(x => x.OneToMany)
                .WithMany(x => x.ManyToOnes)
                .HasForeignKey(x => x.OneToManyEntityId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<CustomKeyName>()
                .HasKey(x => x.CustomKey);
            modelBuilder.Entity<CompoundKeyEntity>()
                .HasKey(x => new { x.CompoundKeyNumber, x.CompoundKeyString });

            modelBuilder.Entity<AttributedIdDbGenerated>();
            modelBuilder.Entity<CompoundKeyDbGenerated>()
                .HasKey(x => new { x.CompoundNumberGenerated, x.CompoundStringGenerated });
            modelBuilder.Entity<ConventionKeyDbGenerated>();
            modelBuilder.Entity<RenamedColumnDbGenerated>()
                .HasKey(x => x.CustomId)
                .Property(x => x.HelloIAmAProp).HasColumnName(RENAMED_DB_GENERATED_COLUMN_NAME);

            modelBuilder.Entity<WithSomePropsUnmapped>().Ignore(x => x.NotMappedProp2);
        }
    }
}
