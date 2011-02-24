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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests {
    [TestFixture]
    public class MongoDatabaseTests {
        private MongoServer server;
        private MongoDatabase database;

        [TestFixtureSetUp]
        public void Setup() {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            server.Connect();
            server.DropDatabase("onlinetests");
            database = server["onlinetests"];
        }

        // TODO: more tests for MongoDatabase

        [Test]
        public void TestCollectionExists() {
            var collectionName = "testcollectionexists";
            Assert.IsFalse(database.CollectionExists(collectionName));

            database[collectionName].Insert(new BsonDocument());
            Assert.IsTrue(database.CollectionExists(collectionName));
        }

        [Test]
        public void TestCreateCollection() {
            var collectionName = "testcreatecollection";
            Assert.IsFalse(database.CollectionExists(collectionName));

            database.CreateCollection(collectionName, CollectionOptions.Null);
            Assert.IsTrue(database.CollectionExists(collectionName));
        }

        [Test]
        public void TestDropCollection() {
            var collectionName = "testdropcollection";
            Assert.IsFalse(database.CollectionExists(collectionName));

            database[collectionName].Insert(new BsonDocument());
            Assert.IsTrue(database.CollectionExists(collectionName));

            database.DropCollection(collectionName);
            Assert.IsFalse(database.CollectionExists(collectionName));
        }

        [Test]
        public void TestFetchDBRef() {
            var collectionName = "testdbref";
            var collection = database.GetCollection(collectionName);
            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "P", "x" } };
            collection.Insert(document);

            var dbRef = new MongoDBRef(collectionName, document["_id"].AsObjectId);
            var fetched = database.FetchDBRef(dbRef);
            Assert.AreEqual(document, fetched);
            Assert.AreEqual(document.ToJson(), fetched.ToJson());

            var dbRefWithDatabaseName = new MongoDBRef(database.Name, collectionName, document["_id"].AsObjectId);
            fetched = server.FetchDBRef(dbRefWithDatabaseName);
            Assert.AreEqual(document, fetched);
            Assert.AreEqual(document.ToJson(), fetched.ToJson());
            Assert.Throws<ArgumentException>(() => { server.FetchDBRef(dbRef); });
        }

        [Test]
        public void TestGetCollection() {
            var collectionName = "testgetcollection";
            var collection = database.GetCollection(collectionName);
            Assert.AreSame(database, collection.Database);
            Assert.AreEqual(database.Name + "." + collectionName, collection.FullName);
            Assert.AreEqual(collectionName, collection.Name);
            Assert.AreEqual(database.Settings.SafeMode, collection.Settings.SafeMode);
        }

        [Test]
        public void TestGetCollectionNames() {
            server.DropDatabase("onlinetests");
            database["a"].Insert(new BsonDocument("a", 1));
            database["b"].Insert(new BsonDocument("b", 1));
            database["c"].Insert(new BsonDocument("c", 1));
            var collectionNames = database.GetCollectionNames();
            Assert.AreEqual(new[] { "a", "b", "c", "system.indexes" }, collectionNames);
        }

        [Test]
        public void TestRenameCollection() {
            var collectionName1 = "testrenamecollection1";
            var collectionName2 = "testrenamecollection2";
            Assert.IsFalse(database.CollectionExists(collectionName1));
            Assert.IsFalse(database.CollectionExists(collectionName2));

            database[collectionName1].Insert(new BsonDocument());
            Assert.IsTrue(database.CollectionExists(collectionName1));
            Assert.IsFalse(database.CollectionExists(collectionName2));

            database.RenameCollection(collectionName1, collectionName2);
            Assert.IsFalse(database.CollectionExists(collectionName1));
            Assert.IsTrue(database.CollectionExists(collectionName2));
        }
    }
}
