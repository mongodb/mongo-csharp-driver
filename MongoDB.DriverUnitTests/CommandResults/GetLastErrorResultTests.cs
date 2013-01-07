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

namespace MongoDB.DriverUnitTests.CommandResults
{
    [TestFixture]
    public class GetLastErrorResultTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void TestInsert()
        {
            using (_database.RequestStart())
            {
                _collection.Insert(new BsonDocument());
                var result = _server.GetLastError();
                Assert.IsFalse(result.HasLastErrorMessage);
                Assert.IsFalse(result.UpdatedExisting);
                Assert.AreEqual(0, result.DocumentsAffected); // note: DocumentsAffected is only set after an Update?
            }
        }

        [Test]
        public void TestUpdate()
        {
            using (_database.RequestStart())
            {
                var id = ObjectId.GenerateNewId();
                var document = new BsonDocument
                {
                    { "_id", id },
                    { "x", 1 }
                };
                _collection.Insert(document);

                var query = Query.EQ("_id", id);
                var update = Update.Inc("x", 1);
                _collection.Update(query, update);
                var result = _server.GetLastError();
                Assert.IsFalse(result.HasLastErrorMessage);
                Assert.IsTrue(result.UpdatedExisting);
                Assert.AreEqual(1, result.DocumentsAffected);
            }
        }
    }
}
