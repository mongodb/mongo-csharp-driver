/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Linq
{
    [TestFixture]
    public class ExplainTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoCollection _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [Test]
        public void TestExplainFromLinqQueryEqualsExplainFromCursor()
        {
            var linqExplain = _collection.AsQueryable<C>().Where(c => c.X == 2 && c.Y == 1).Take(1).Explain();
            var queryExplain = _collection.FindAs<C>(Query.And(Query.EQ("X", 2), Query.EQ("Y", 1))).SetLimit(1).Explain();

            // millis could be different, so we'll ignore that difference.
            RemoveKey(linqExplain, "millis");
            RemoveKey(queryExplain, "millis");
            RemoveKey(linqExplain, "executionTimeMillis");
            RemoveKey(queryExplain, "executionTimeMillis");
            RemoveKey(linqExplain, "executionTimeMillisEstimate");
            RemoveKey(queryExplain, "executionTimeMillisEstimate");

            Assert.AreEqual(linqExplain, queryExplain);
        }

        [Test]
        public void TestVerboseExplainFromLinqQueryEqualsVerboseExplainFromCursor()
        {
            var linqExplain = _collection.AsQueryable<C>().Where(c => c.X == 2 && c.Y == 1).Take(1).Explain(true);
            var queryExplain = _collection.FindAs<C>(Query.And(Query.EQ("X", 2), Query.EQ("Y", 1))).SetLimit(1).Explain(true);

            // millis could be different, so we'll ignore that difference.
            RemoveKey(linqExplain, "millis");
            RemoveKey(queryExplain, "millis");
            RemoveKey(linqExplain, "executionTimeMillis");
            RemoveKey(queryExplain, "executionTimeMillis");
            RemoveKey(linqExplain, "executionTimeMillisEstimate");
            RemoveKey(queryExplain, "executionTimeMillisEstimate");

            Assert.AreEqual(linqExplain, queryExplain);
        }

        [Test]
        public void TestDistinctQueryCannotBeExplained()
        {
            Assert.Throws<NotSupportedException>(()=> _collection.AsQueryable<C>().Select(c=>c.X).Distinct().Explain());
        }

        [Test]
        public void TestTakeZeroQueriesCannotBeExplained()
        {
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable<C>().Take(0).Explain());
        }

        private void RemoveKey(BsonValue value, string key)
        {
            if(value.IsBsonDocument)
            {
                value.AsBsonDocument.Remove(key);
                foreach (var element in value.AsBsonDocument)
                {
                    RemoveKey(element.Value, key);
                }
            }
            else if (value.IsBsonArray)
            {
                foreach(var item in value.AsBsonArray)
                {
                    RemoveKey(item, key);
                }
            }
        }
    }
}
