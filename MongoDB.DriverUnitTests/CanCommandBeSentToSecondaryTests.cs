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
    public class CanCommandBeSentToSecondaryTests
    {
        [Test]
        public void TestGetDelegate()
        {
            Func<BsonDocument, bool> defaultImplementation = CanCommandBeSentToSecondary.DefaultImplementation;
            Assert.AreEqual(defaultImplementation, CanCommandBeSentToSecondary.Delegate);
        }

        [Test]
        public void TestSetDelegate()
        {
            Func<BsonDocument, bool> func = doc => true;
            CanCommandBeSentToSecondary.Delegate = func;
            Assert.AreEqual(func, CanCommandBeSentToSecondary.Delegate);
            CanCommandBeSentToSecondary.Delegate = CanCommandBeSentToSecondary.DefaultImplementation; // reset Delegate
        }

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
            var result = CanCommandBeSentToSecondary.DefaultImplementation(doc);

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void TestCanSendInlineMapReduceToSecondary()
        {
            var doc = new BsonDocument
            {
                { "mapreduce", "col" },
                { "map", "emit()" },
                { "reduce", "return 1" },
                { "out", new BsonDocument("inline", 1) }
            };

            var result = CanCommandBeSentToSecondary.DefaultImplementation(doc);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestCannotSendNonInlineMapReduceToSecondary()
        {
            var doc = new BsonDocument            
            {
                { "mapreduce", "col" },
                { "map", "emit()" },
                { "reduce", "return 1" }
            };

            var result = CanCommandBeSentToSecondary.DefaultImplementation(doc);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestCannotSendNonInlineMapReduceToSecondary2()
        {
            var doc = new BsonDocument            
            {
                { "mapreduce", "col" },
                { "map", "emit()" },
                { "reduce", "return 1" },
                { "out", new BsonDocument("merge", "funny") }
            };

            var result = CanCommandBeSentToSecondary.DefaultImplementation(doc);

            Assert.IsFalse(result);
        }
    }
}