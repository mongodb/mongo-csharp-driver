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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class CSharp281Tests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
            _collection.Drop();
        }

        [Test]
        public void TestPopFirst()
        {
            var document = new BsonDocument("x", new BsonArray { 1, 2, 3 });
            _collection.RemoveAll();
            _collection.Insert(document);

            var query = Query.EQ("_id", document["_id"]);
            var update = Update.PopFirst("x");
            _collection.Update(query, update);

            document = _collection.FindOne();
            var array = document["x"].AsBsonArray;
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(2, array[0].AsInt32);
            Assert.AreEqual(3, array[1].AsInt32);
        }

        [Test]
        public void TestPopLast()
        {
            var document = new BsonDocument("x", new BsonArray { 1, 2, 3 });
            _collection.RemoveAll();
            _collection.Insert(document);

            var query = Query.EQ("_id", document["_id"]);
            var update = Update.PopLast("x");
            _collection.Update(query, update);

            document = _collection.FindOne();
            var array = document["x"].AsBsonArray;
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(2, array[1].AsInt32);
        }
    }
}
