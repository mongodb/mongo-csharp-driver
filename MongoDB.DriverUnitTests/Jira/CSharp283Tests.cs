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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp283
{
    [TestFixture]
    public class CSharp283Tests
    {
        private BsonArray _bsonArray = new BsonArray { 1, 2, 3 };
        private BsonValue _bsonValue = 1;
        private BsonValue[] _bsonValueArray = new BsonValue[] { 1, 2, 3 };
        private List<BsonValue> _bsonValueList = new List<BsonValue> { 1, 2, 3 };
        private IEnumerable<BsonValue> _ienumerableBsonValue = new BsonValue[] { 1, 2, 3 };

        [Test]
        public void TestQueryAll()
        {
            var query1 = Query.All("name", new BsonValue[] { _bsonValue });
            var query2 = Query.All("name", _bsonArray);
            var query3 = Query.All("name", _bsonValueArray);
            var query4 = Query.All("name", _bsonValueList);
            var query5 = Query.All("name", _ienumerableBsonValue);

            var expectedSingle = "{ 'name' : { '$all' : [1] } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$all' : [1, 2, 3] } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestQueryIn()
        {
            var query1 = Query.In("name", new BsonValue[] { _bsonValue });
            var query2 = Query.In("name", _bsonArray);
            var query3 = Query.In("name", _bsonValueArray);
            var query4 = Query.In("name", _bsonValueList);
            var query5 = Query.In("name", _ienumerableBsonValue);

            var expectedSingle = "{ 'name' : { '$in' : [1] } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$in' : [1, 2, 3] } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestQueryNin()
        {
            var query1 = Query.NotIn("name", new BsonValue[] { _bsonValue });
            var query2 = Query.NotIn("name", _bsonArray);
            var query3 = Query.NotIn("name", _bsonValueArray);
            var query4 = Query.NotIn("name", _bsonValueList);
            var query5 = Query.NotIn("name", _ienumerableBsonValue);

            var expectedSingle = "{ 'name' : { '$nin' : [1] } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$nin' : [1, 2, 3] } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestQueryNotAll()
        {
            var query1 = Query.Not(Query.All("name", new BsonValue[] { _bsonValue }));
            var query2 = Query.Not(Query.All("name", _bsonArray));
            var query3 = Query.Not(Query.All("name", _bsonValueArray));
            var query4 = Query.Not(Query.All("name", _bsonValueList));
            var query5 = Query.Not(Query.All("name", _ienumerableBsonValue));

            var expectedSingle = "{ 'name' : { '$not' : { '$all' : [1] } } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$not' : { '$all' : [1, 2, 3] } } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestQueryNotIn()
        {
            var query1 = Query.Not(Query.In("name", new BsonValue[] { _bsonValue }));
            var query2 = Query.Not(Query.In("name", _bsonArray));
            var query3 = Query.Not(Query.In("name", _bsonValueArray));
            var query4 = Query.Not(Query.In("name", _bsonValueList));
            var query5 = Query.Not(Query.In("name", _ienumerableBsonValue));

            var expectedSingle = "{ 'name' : { '$nin' : [1] } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$nin' : [1, 2, 3] } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestQueryNotNin()
        {
            var query1 = Query.Not(Query.NotIn("name", new BsonValue[] { _bsonValue }));
            var query2 = Query.Not(Query.NotIn("name", _bsonArray));
            var query3 = Query.Not(Query.NotIn("name", _bsonValueArray));
            var query4 = Query.Not(Query.NotIn("name", _bsonValueList));
            var query5 = Query.Not(Query.NotIn("name", _ienumerableBsonValue));

            var expectedSingle = "{ 'name' : { '$not' : { '$nin' : [1] } } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$not' : { '$nin' : [1, 2, 3] } } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestUpdateAddToSetEach()
        {
            var update1 = Update.AddToSetEach("name", _bsonValue);
            var update2 = Update.AddToSetEach("name", _bsonArray);
            var update3 = Update.AddToSetEach("name", _bsonValueArray);
            var update4 = Update.AddToSetEach("name", _bsonValueList);
            var update5 = Update.AddToSetEach("name", _ienumerableBsonValue);

            var expectedSingle = "{ '$addToSet' : { 'name' : { '$each' : [1] } } }".Replace("'", "\"");
            var expectedMultiple = "{ '$addToSet' : { 'name' : { '$each' : [1, 2, 3] } } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, update1.ToJson());
            Assert.AreEqual(expectedMultiple, update2.ToJson());
            Assert.AreEqual(expectedMultiple, update3.ToJson());
            Assert.AreEqual(expectedMultiple, update4.ToJson());
            Assert.AreEqual(expectedMultiple, update5.ToJson());
        }

        [Test]
        public void TestUpdatePullAll()
        {
            var update1 = Update.PullAll("name", _bsonValue);
            var update2 = Update.PullAll("name", _bsonArray);
            var update3 = Update.PullAll("name", _bsonValueArray);
            var update4 = Update.PullAll("name", _bsonValueList);
            var update5 = Update.PullAll("name", _ienumerableBsonValue);

            var expectedSingle = "{ '$pullAll' : { 'name' : [1] } }".Replace("'", "\"");
            var expectedMultiple = "{ '$pullAll' : { 'name' : [1, 2, 3] } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, update1.ToJson());
            Assert.AreEqual(expectedMultiple, update2.ToJson());
            Assert.AreEqual(expectedMultiple, update3.ToJson());
            Assert.AreEqual(expectedMultiple, update4.ToJson());
            Assert.AreEqual(expectedMultiple, update5.ToJson());
        }

        [Test]
        public void TestUpdatePushAll()
        {
            var update1 = Update.PushAll("name", _bsonValue);
            var update2 = Update.PushAll("name", _bsonArray);
            var update3 = Update.PushAll("name", _bsonValueArray);
            var update4 = Update.PushAll("name", _bsonValueList);
            var update5 = Update.PushAll("name", _ienumerableBsonValue);

            var expectedSingle = "{ '$pushAll' : { 'name' : [1] } }".Replace("'", "\"");
            var expectedMultiple = "{ '$pushAll' : { 'name' : [1, 2, 3] } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, update1.ToJson());
            Assert.AreEqual(expectedMultiple, update2.ToJson());
            Assert.AreEqual(expectedMultiple, update3.ToJson());
            Assert.AreEqual(expectedMultiple, update4.ToJson());
            Assert.AreEqual(expectedMultiple, update5.ToJson());
        }
    }
}
