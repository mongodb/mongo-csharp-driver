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
                { "DBRef", new BsonDocument
                    {
                        // starting with server version 2.5.2 the order of the fields must be exactly as below
                        { "$ref", "ref" },
                        { "$id", "id" },
                        { "$db", "db" }
                    }
                }
            };
            if (_server.BuildInfo.Version < new Version(2, 5, 2))
            {
                // starting with version 2.5.2 the server got stricter about dollars in element names
                // so the Code element below can only be added when testing against older servers
                document["Code"] = new BsonDocument
                {
                    { "$code", "code" },
                    { "$scope", "scope" }
                };
            }
            _collection.Insert(document);
        }

        [Test]
        public void TestCreateIndexOnNestedElement()
        {
            _collection.CreateIndex("a.b");
        }
    }
}
