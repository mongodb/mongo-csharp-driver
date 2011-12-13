/* Copyright 2010-2011 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.DriverUnitTests.Jira.CSharp283
{
    [TestFixture]
    public class CSharp283Tests
    {
        private BsonArray bsonArray = new BsonArray { 1, 2, 3 };
        private BsonValue bsonValue = 1;
        private BsonValue[] bsonValueArray = new BsonValue[] { 1, 2, 3 };
        private List<BsonValue> bsonValueList = new List<BsonValue> { 1, 2, 3 };
        private IEnumerable<BsonValue> ienumerableBsonValue = new BsonValue[] { 1, 2, 3 };

        [Test]
        public void TestQueryAll()
        {
            var query1 = Query.All("name", bsonValue);
            var query2 = Query.All("name", bsonArray);
            var query3 = Query.All("name", bsonValueArray);
            var query4 = Query.All("name", bsonValueList);
            var query5 = Query.All("name", ienumerableBsonValue);

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
            var query1 = Query.In("name", bsonValue);
            var query2 = Query.In("name", bsonArray);
            var query3 = Query.In("name", bsonValueArray);
            var query4 = Query.In("name", bsonValueList);
            var query5 = Query.In("name", ienumerableBsonValue);

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
            var query1 = Query.NotIn("name", bsonValue);
            var query2 = Query.NotIn("name", bsonArray);
            var query3 = Query.NotIn("name", bsonValueArray);
            var query4 = Query.NotIn("name", bsonValueList);
            var query5 = Query.NotIn("name", ienumerableBsonValue);

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
            var query1 = Query.Not("name").All(bsonValue);
            var query2 = Query.Not("name").All(bsonArray);
            var query3 = Query.Not("name").All(bsonValueArray);
            var query4 = Query.Not("name").All(bsonValueList);
            var query5 = Query.Not("name").All(ienumerableBsonValue);

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
            var query1 = Query.Not("name").In(bsonValue);
            var query2 = Query.Not("name").In(bsonArray);
            var query3 = Query.Not("name").In(bsonValueArray);
            var query4 = Query.Not("name").In(bsonValueList);
            var query5 = Query.Not("name").In(ienumerableBsonValue);

            var expectedSingle = "{ 'name' : { '$not' : { '$in' : [1] } } }".Replace("'", "\"");
            var expectedMultiple = "{ 'name' : { '$not' : { '$in' : [1, 2, 3] } } }".Replace("'", "\"");

            Assert.AreEqual(expectedSingle, query1.ToJson());
            Assert.AreEqual(expectedMultiple, query2.ToJson());
            Assert.AreEqual(expectedMultiple, query3.ToJson());
            Assert.AreEqual(expectedMultiple, query4.ToJson());
            Assert.AreEqual(expectedMultiple, query5.ToJson());
        }

        [Test]
        public void TestQueryNotNin()
        {
            var query1 = Query.Not("name").NotIn(bsonValue);
            var query2 = Query.Not("name").NotIn(bsonArray);
            var query3 = Query.Not("name").NotIn(bsonValueArray);
            var query4 = Query.Not("name").NotIn(bsonValueList);
            var query5 = Query.Not("name").NotIn(ienumerableBsonValue);

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
            var update1 = Update.AddToSetEach("name", bsonValue);
            var update2 = Update.AddToSetEach("name", bsonArray);
            var update3 = Update.AddToSetEach("name", bsonValueArray);
            var update4 = Update.AddToSetEach("name", bsonValueList);
            var update5 = Update.AddToSetEach("name", ienumerableBsonValue);

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
            var update1 = Update.PullAll("name", bsonValue);
            var update2 = Update.PullAll("name", bsonArray);
            var update3 = Update.PullAll("name", bsonValueArray);
            var update4 = Update.PullAll("name", bsonValueList);
            var update5 = Update.PullAll("name", ienumerableBsonValue);

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
            var update1 = Update.PushAll("name", bsonValue);
            var update2 = Update.PushAll("name", bsonArray);
            var update3 = Update.PushAll("name", bsonValueArray);
            var update4 = Update.PushAll("name", bsonValueList);
            var update5 = Update.PushAll("name", ienumerableBsonValue);

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
