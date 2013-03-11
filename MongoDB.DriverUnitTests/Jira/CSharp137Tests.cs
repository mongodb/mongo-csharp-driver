/* Copyright 2010-2013 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp137
{
    [TestFixture]
    public class CSharp137Tests
    {
        [Test]
        public void TestAndInNotIn()
        {
            var query = Query.And(
                Query.In("value", new BsonValue[] { 1, 2, 3, 4 }),
                Query.NotIn("value", new BsonValue[] { 11, 12, 13, 14 })
            );

            Assert.AreEqual(
                new BsonDocument
                {
                    { "value", new BsonDocument
                        {
                            { "$in", new BsonArray { 1, 2, 3, 4 } },
                            { "$nin", new BsonArray { 11, 12, 13, 14 } }
                        }
                    }
                },
                query.ToBsonDocument());
        }

        [Test]
        public void TestAndGtLt()
        {
            var query = Query.And(
                Query.NotIn("value", new BsonValue[] { 1, 2, 3 }),
                Query.EQ("OtherValue", 1),
                Query.GT("value", 6),
                Query.LT("value", 20)
            );

            Assert.AreEqual(
                new BsonDocument
                {
                    { "value", new BsonDocument
                        {
                            {"$nin", new BsonArray { 1, 2, 3 }},
                            {"$gt", 6},
                            {"$lt", 20}
                        }
                    },
                    { "OtherValue", 1 }
                },
                query.ToBsonDocument());
        }

        [Test]
        public void TestDuplicateEq()
        {
            // now that server supports $and this is actually syntactically valid
            var query = Query.And(
                Query.EQ("value", 6),
                Query.EQ("value", 20)
            );
            var expected = "{ '$and' : [{ 'value' : 6 }, { 'value' : 20 }] }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestEq1()
        {
            // now that server supports $and this is actually syntactically valid
            var query = Query.And(
                Query.EQ("value", 6),
                Query.LT("value", 20)
            );
            var expected = "{ '$and' : [{ 'value' : 6 }, { 'value' : { '$lt' : 20 } }] }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestEq2()
        {
            // now that server supports $and this is actually syntactically valid
            var query = Query.And(
                Query.GT("value", 6),
                Query.EQ("value", 20)
            );
            var expected = "{ '$and' : [{ 'value' : { '$gt' : 6 } }, { 'value' : 20 }] }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestDuplicateOperation()
        {
            // now that server supports $and this is actually syntactically valid
            var query = Query.And(
                Query.LTE("value", 6),
                Query.LTE("value", 20)
            );
            var expected = "{ '$and' : [{ 'value' : { '$lte' : 6 } }, { 'value' : { '$lte' : 20 } }] }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }
    }
}
