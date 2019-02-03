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
using Sanatana.EntityFramework.BatchSpecs.Samples;
using System.IO;
using Sanatana.EntityFramework.Batch.Scripts;
using Sanatana.EntityFramework.BatchSpecs.Resources.Scripts;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace Sanatana.EntityFramework.BatchSpecs
{
    public class ScriptableInitializeSpecs
    {
        [TestFixture]
        public class when_initializeing_dbcontext : SpecsFor<object>
        {
            [Test]
            public void then_it_executes_scripts_from_files()
            {
                AppDomain.CurrentDomain.SetData("DataDirectory",
                    Directory.GetCurrentDirectory());

                var scriptsDirectory = new DirectoryInfo("Resources/Scripts");
                var strategy = new ScriptInitializer<TestInitializerDbContext>(scriptsDirectory);
                System.Data.Entity.Database.SetInitializer<TestInitializerDbContext>(strategy);
                
                using (var context = new TestInitializerDbContext())
                {
                    context.Database.Initialize(force: true);
                }
            }
            
            [Test]
            public void then_it_executes_scripts_from_resources()
            {
                AppDomain.CurrentDomain.SetData("DataDirectory",
                    Directory.GetCurrentDirectory());

                var strategy = new ScriptInitializer<TestInitializerDbContext>(typeof(ScriptRes));
                System.Data.Entity.Database.SetInitializer<TestInitializerDbContext>(strategy);
                
                using (var context = new TestInitializerDbContext())
                {
                    context.Database.Initialize(force: true);
                }
            }
        }
    }
}
