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

using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class BsonDocumentTests
    {
        [Test]
        public void TestArray()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Colors"][0] == "Blue");
            var expected = "{ \"Colors.0\" : \"Blue\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEquals()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Name"] == "awesome");
            var expected = "{ \"Name\" : \"awesome\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Age"] > 20);
            var expected = "{ \"Age\" : { \"$gt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEquals()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Age"] >= 20);
            var expected = "{ \"Age\" : { \"$gte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Age"] < 20);
            var expected = "{ \"Age\" : { \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEquals()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Age"] <= 20);
            var expected = "{ \"Age\" : { \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEquals()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Name"] != "awesome");
            var expected = "{ \"Name\" : { \"$ne\" : \"awesome\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNestedDocumentEquals()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Address"]["City"] == "New York");
            var expected = "{ \"Address.City\" : \"New York\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNestedDocumentInAnArray()
        {
            var query = Query<BsonDocument>.Where(doc => doc["Children"][0]["Name"] == "Jack");
            var expected = "{ \"Children.0.Name\" : \"Jack\" }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}