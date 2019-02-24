using Sanatana.EntityFramework.BatchSpecs.Samples;
using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using SpecsFor.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.BatchSpecs.TestTools.Providers
{
    public class SampleDbCreator : Behavior<INeedSampleDatabase>
    {
        //fields
        private static bool _isInitialized;


        //methods
        public override void SpecInit(INeedSampleDatabase instance)
        {
            if (_isInitialized)
            {
                return;
            }

            AppDomain.CurrentDomain.SetData("DataDirectory",
                Directory.GetCurrentDirectory());

            var strategy = new MigrateDatabaseToLatestVersion<SampleDbContext, DbMigrationsConfiguration<SampleDbContext>>();
            Database.SetInitializer(strategy);

            using (var context = new SampleDbContext())
            {
                context.Database.Delete();

                context.Database.Initialize(force: true);
            }

            _isInitialized = true;
        }
    }
}
