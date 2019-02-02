using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using Sanatana.EntityFramework.BatchSpecs.TestTools.Providers;
using NUnit.Framework;
using SpecsFor.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[SetUpFixture]
public class ServicesTestConfig : SpecsForConfiguration
{
    public ServicesTestConfig()
    {
        WhenTesting<INeedSampleDatabase>().EnrichWith<SampleDbCreator>();
        WhenTesting<INeedSampleDatabase>().EnrichWith<TransactionScopeWrapper>();
        WhenTesting<INeedSampleDatabase>().EnrichWith<SampleDbContextProvider>();
    }
}
