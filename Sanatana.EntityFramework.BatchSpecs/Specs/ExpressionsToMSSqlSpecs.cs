using NUnit.Framework;
using Sanatana.EntityFramework.BatchSpecs.Samples;
using Sanatana.EntityFramework.BatchSpecs.TestTools.Interfaces;
using SpecsFor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sanatana.EntityFramework.Batch.Expressions;
using System.Text.RegularExpressions;
using System.Data.Entity;
using LinqKit;
using Sanatana.EntityFramework.BatchSpecs.Samples.Entities;

namespace Sanatana.EntityFramework.BatchSpecs
{
    [TestFixture]
    public class when_creating_mssql_queries : SpecsFor<SampleDbContext>
        , INeedSampleDatabase
    {
        public SampleDbContext SampleDatabase { get; set; }

        //compare field to field
        [Test]
        public void then_it_returns_sql_DoubleTest()
        {
            Expression expression = Expression((sp, tp) => sp.IntProperty == tp.IntProperty);

            string sql = expression.ToMSSqlString();
            string expected = $"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = [{ExpressionsToMSSql.ALIASES[1]}].[IntProperty]";
            Assert.AreEqual(expected, sql);
        }


        //static constant
        [Test]
        public void then_it_returns_sql_StaticFieldTest()
        {
            Expression expression = Expression(p => p.GuidNullableProperty == Guid.Empty);

            string sql = expression.ToMSSqlString();
            string expected = $"[{ExpressionsToMSSql.ALIASES[0]}].[GuidNullableProperty] = cast('{Guid.Empty}' as uniqueidentifier)";
            Assert.AreEqual(expected, sql);
        }



        //datetimes
        [Test]
        public void then_it_returns_sql_DateNullTest()
        {
            Expression expression = Expression(p => p.DateProperty == null);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[DateProperty] IS NULL", sql);
        }

        [Test]
        public void then_it_returns_sql_DateVariableTest()
        {
            DateTime datetime = new DateTime(2014, 1, 5, 15, 55, 45, 55);
            Expression expression = Expression(p => p.DateProperty == datetime);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[DateProperty] = convert(datetime2, '2014-1-5 15:55:45.0550000', 121)", sql);
        }

        [Test]
        public void then_it_returns_sql_DateStaticPropTest()
        {
            Expression expression = Expression(p => p.DateProperty == DateTime.UtcNow);

            string sql = expression.ToMSSqlString();
            string match = $@"\[{ExpressionsToMSSql.ALIASES[0]}\]" + @".\[DateProperty\] = convert\(datetime2, '\d{4}-\d{1,2}-\d{1,2} \d{2}:\d{2}:\d{2}.\d{7}', 121\)";
            bool isMatched = Regex.IsMatch(sql, match);
            Assert.IsTrue(isMatched);
        }

        [Test]
        public void then_it_returns_sql_DateObjectPropTest()
        {
            SampleEntity entity = new SampleEntity()
            {
                DateProperty = DateTime.Now
            };
            Expression expression = Expression(p => p.DateProperty == entity.DateProperty);

            string sql = expression.ToMSSqlString();
            string exprected = $"[{ExpressionsToMSSql.ALIASES[0]}].[DateProperty] = " + ExpressionsToMSSql.ConstantToMSSql(entity.DateProperty, typeof(DateTime));
            Assert.AreEqual(exprected, sql);
        }


        //int
        [Test]
        public void then_it_returns_sql_IntNullTest()
        {
            Expression expression = Expression(p => p.IntProperty == null);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] IS NULL", sql);
        }

        [Test]
        public void then_it_returns_sql_IntVariableTest()
        {
            int constantInt = 5;
            Expression expression = Expression(p => p.IntProperty == constantInt);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = 5", sql);
        }

        [Test]
        public void then_it_returns_sql_IntConstantTest()
        {
            Expression expression = Expression(p => p.IntProperty == 5);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = 5", sql);
        }


        //guid
        [Test]
        public void then_it_returns_sql_NullableGuidTest()
        {
            Guid? value = Guid.NewGuid();
            Expression expression = Expression(p => p.GuidNullableProperty == value);

            string sql = expression.ToMSSqlString();
            string expected = $"[{ExpressionsToMSSql.ALIASES[0]}].[GuidNullableProperty] = cast('{value}' as uniqueidentifier)";
            Assert.AreEqual(expected, sql);
        }


        //string
        [Test]
        public void then_it_returns_sql_StringVariableTest()
        {
            string varString = "string value";
            Expression expression = Expression(p => p.StringProperty == varString);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[StringProperty] = N'string value'", sql);
        }


        //complex property
        [Test]
        public void then_it_returns_sql_ComplexPropertyTest()
        {
            Expression<Func<ParentEntity, bool>> expression = x => x.Embedded.Address == "st. 1";
            string sql = expression.ToMSSqlString();

            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[Embedded_Address] = N'st. 1'", sql);
        }

        [Test]
        public void then_it_returns_sql_ComplexPropertyDbContextTest()
        {
            Expression<Func<ParentEntity, bool>> expression = x => x.Embedded.Address == "st. 1";
            DbContext context = new SampleDbContext();
            string sql = expression.ToMSSqlString(context);

            string expected = $"[{ExpressionsToMSSql.ALIASES[0]}].[{SampleDbContext.COMPLEX_TYPE_COLUMN_NAME}] = N'st. 1'";
            Assert.AreEqual(expected, sql);
        }



        //contains methods
        [Test]
        public void then_it_returns_sql_DatetimeContainsTest()
        {
            DateTime datetime = new DateTime(2014, 1, 5, 15, 55, 45, 55);
            List<DateTime?> list = new List<DateTime?>() { datetime, datetime };
            Expression expression = Expression(p => list.Contains(p.DateProperty));

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[DateProperty] in (convert(datetime2, '2014-1-5 15:55:45.0550000', 121), convert(datetime2, '2014-1-5 15:55:45.0550000', 121))", sql);
        }

        [Test]
        public void then_it_returns_sql_IntContainsTest()
        {
            List<int?> list = new List<int?>() { 5, 6, 7 };
            Expression expression = Expression(p => list.Contains(p.IntProperty));

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] in (5, 6, 7)", sql);
        }

        [Test]
        public void then_it_returns_sql_GuidContainsTest()
        {
            List<Guid> list = new List<Guid>()
            {
                new Guid("{23D0F191-9075-49CB-8EED-A5BAF77E2985}"),
                new Guid("{54307A63-CE51-4551-85ED-DDCDDAEF229B}")
            };
            Expression expression = Expression(p => list.Contains(p.GuidProperty));

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[GuidProperty] in (cast('23d0f191-9075-49cb-8eed-a5baf77e2985' as uniqueidentifier), cast('54307a63-ce51-4551-85ed-ddcddaef229b' as uniqueidentifier))", sql);
        }

        [Test]
        public void then_it_returns_sql_DerivedEntityContainsTest()
        {
            List<int> list = new List<int>() { 5, 6, 7 };
            Expression<Func<GenericDerivedEntity, bool>> expression = x => list.Contains(x.EntityId);

            DbContext dbContext = new SampleDbContext();
            string sql = expression.ToMSSqlString(dbContext);
            string expected = $"[{ExpressionsToMSSql.ALIASES[0]}].[EntityId] in (5, 6, 7)";
            Assert.AreEqual(expected, sql);
        }


        //binary expressions
        [Test]
        public void then_it_returns_sql_LessTest()
        {
            Expression expression = Expression(p => p.IntProperty < 5);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] < 5", sql);
        }

        [Test]
        public void then_it_returns_sql_GreaterTest()
        {
            Expression expression = Expression(p => p.IntProperty > 5);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] > 5", sql);
        }

        [Test]
        public void then_it_returns_sql_LessOrEqualTest()
        {
            Expression expression = Expression(p => p.IntProperty <= 5);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] <= 5", sql);
        }

        [Test]
        public void then_it_returns_sql_GreaterEqualTest()
        {
            Expression expression = Expression(p => p.IntProperty >= 5);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] >= 5", sql);
        }


        //add
        [Test]
        public void then_it_returns_sql_AddTargetTest()
        {
            Expression expression = UpdateExpression(t => t.IntProperty, (t, s) => t.IntProperty + 5);

            string sql = expression.ToMSSqlString();
            string exprected = $"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = [{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] + 5";
            Assert.AreEqual(exprected, sql);
        }

        [Test]
        public void then_it_returns_sql_AddSourceTest()
        {
            Expression expression = UpdateExpression(t => t.IntProperty, (t, s) => s.IntProperty + 5 * t.IntProperty);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"[{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = [{ExpressionsToMSSql.ALIASES[1]}].[IntProperty] + 5 * [{ExpressionsToMSSql.ALIASES[0]}].[IntProperty]", sql);
        }


        //Or
        [Test]
        public void then_it_returns_sql_OrTest()
        {
            Expression expression = Expression(t => t.IntProperty > 5 || t.IntProperty == 0
                && t.IntProperty < 10);

            string sql = expression.ToMSSqlString();
            Assert.AreEqual($"([{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] > 5) OR (([{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = 0) AND ([{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] < 10))", sql);
        }


        //LinqKit
        [Test]
        public void then_it_returns_sql_LinqKitTest()
        {
            Expression<Func<SampleEntity, bool>> expression = t => t.IntProperty > 5;
            expression = expression.Or(t => t.IntProperty == 2);
            expression = expression.And(t => t.IntProperty < 10);

            string sql = expression.ToMSSqlString();
            string expected = $"(([{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] > 5) OR ([{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] = 2)) AND ([{ExpressionsToMSSql.ALIASES[0]}].[IntProperty] < 10)";
            Assert.AreEqual(expected, sql);
        }


        //helpers
        private Expression Expression(Expression<Func<SampleEntity, bool>> expression)
        {
            return expression;
        }

        private Expression Expression(Expression<Func<SampleEntity, SampleEntity, bool>> expression)
        {
            return expression;
        }

        private Expression UpdateExpression<T>(Expression<Func<SampleEntity, T>> targetProp
            , Expression<Func<SampleEntity, SampleEntity, T>> source)
        {
            return new AssignLambdaExpression()
            {
                Left = targetProp,
                Right = source
            };
        }
    }
}
