using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class SelectWithCastExpressionTests
    {
        private class Foo
        {
            public short Short;
            public float Float;
            public enum FooEnum {}
            public FooEnum Enum;
            public float? NullableFloat;
        }

        [Test]
        public void TestShortToIntImplicitConversion()
        {
            var query = GetSelectQuery<Foo>(c => c.Short == 2);
            Assert.AreEqual("{ \"Short\" : 2 }", query.BuildQuery().ToJson());
        }

        [Test]
        public void TestFloatToDoubleImplicitConversion()
        {
            var query = GetSelectQuery<Foo>(c => c.Float == 2.0);
            Assert.AreEqual("{ \"Float\" : 2.0 }", query.BuildQuery().ToJson());
        }

        [Test]
        public void TestZeroToEnumImplicitConversion()
        {
            var query = GetSelectQuery<Foo>(c => c.Enum == 0);
            Assert.AreEqual("{ \"Enum\" : 0 }", query.BuildQuery().ToJson());
        }

        [Test]
        public void TestNullablesImplicitConversion()
        {
            var query = GetSelectQuery<Foo>(c => c.NullableFloat == 3.0);
            Assert.AreEqual("{ \"NullableFloat\" : 3.0 }", query.BuildQuery().ToJson());
        }

        private SelectQuery GetSelectQuery<T>(Expression<Func<T, bool>> predicate)
        {
            var query = Configuration.GetTestCollection<T>().AsQueryable<T>().Where(predicate);
            return (SelectQuery) MongoQueryTranslator.Translate(query);
        }
    }
}