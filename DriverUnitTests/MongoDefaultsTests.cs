using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoDefaultsTests
    {
        [TestCase("group", true)]
        [TestCase("aggregate", true)]
        [TestCase("geoSearch", true)]
        [TestCase("blah", false)]
        [TestCase("foo", false)]
        [TestCase("bar", false)]
        [TestCase("mapreduce", false)]
        public void TestCanSendCommandToSecondary(string command, bool expectedResult)
        {
            var doc = new BsonDocument(command, 1);
            var result = MongoDefaults.CanCommandBeSentToSecondary(doc);

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void TestCanSendInlineMapReduceToSecondary()
        {
            var doc = new BsonDocument("mapreduce",
                new BsonDocument
                {
                    { "map", "emit()" },
                    { "reduce", "return 1" },
                    { "out", new BsonDocument("inline", 1) }
                });

            var result = MongoDefaults.CanCommandBeSentToSecondary(doc);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestCannotSendNonInlineMapReduceToSecondary()
        {
            var doc = new BsonDocument("mapreduce",
                new BsonDocument
                {
                    { "map", "emit()" },
                    { "reduce", "return 1" }
                });

            var result = MongoDefaults.CanCommandBeSentToSecondary(doc);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestCannotSendNonInlineMapReduceToSecondary2()
        {
            var doc = new BsonDocument("mapreduce",
                new BsonDocument
                {
                    { "map", "emit()" },
                    { "reduce", "return 1" },
                    { "out", new BsonDocument("merge", "foo") }
                });

            var result = MongoDefaults.CanCommandBeSentToSecondary(doc);

            Assert.IsFalse(result);
        }
    }
}