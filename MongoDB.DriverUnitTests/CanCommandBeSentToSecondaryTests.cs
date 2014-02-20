/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

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

        [TestCase("aggregate", true)]
        [TestCase("collStats", true)]
        [TestCase("dbStats", true)]
        [TestCase("count", true)]
        [TestCase("distinct", true)]
        [TestCase("geoNear", true)]
        [TestCase("geoSearch", true)]
        [TestCase("geoWalk", true)]
        [TestCase("group", true)]
        [TestCase("text", true)]
        [TestCase("foo", false)]
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