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
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp253
{
    [TestFixture]
    public class CSharp253Tests
    {
        public class C
        {
            public ObjectId Id { get; set; }
            public MongoDBRef DBRef { get; set; }
            public BsonNull BsonNull { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void TestInsertClass()
        {
            var c = new C
            {
                DBRef = new MongoDBRef("database", "collection", ObjectId.GenerateNewId()),
                BsonNull = null
            };
            _collection.Insert(c);
        }

        [Test]
        public void TestInsertDollar()
        {
            Assert.Throws<BsonSerializationException>(() => { _collection.Insert(new BsonDocument("$x", 1)); });
            Assert.Throws<BsonSerializationException>(() => { _collection.Insert(new BsonDocument("x", new BsonDocument("$x", 1))); });
        }

        [Test]
        public void TestInsertPeriod()
        {
            Assert.Throws<BsonSerializationException>(() => { _collection.Insert(new BsonDocument("a.b", 1)); });
            Assert.Throws<BsonSerializationException>(() => { _collection.Insert(new BsonDocument("a", new BsonDocument("b.c", 1))); });
        }

        [Test]
        public void TestLegacyDollar()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "BsonNull", new BsonDocument("_csharpnull", true) },
                { "Code", new BsonDocument
                    {
                        { "$code", "code" },
                        { "$scope", "scope" }
                    }
                },
                { "DBRef", new BsonDocument
                    {
                        { "$db", "db" },
                        { "$id", "id" },
                        { "$ref", "ref" }
                    }
                }
            };
            _collection.Insert(document);
        }

        [Test]
        public void TestCreateIndexOnNestedElement()
        {
            _collection.CreateIndex("a.b");
        }
    }
}
