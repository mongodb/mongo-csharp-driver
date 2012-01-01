/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.DriverOnlineTests
{
    [TestFixture]
    public class MongoDatabaseTests
    {
        private MongoServer _server;
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _server.Connect();
            _database = Configuration.TestDatabase;
            _database.Drop();
        }

        // TODO: more tests for MongoDatabase

        [Test]
        public void TestCollectionExists()
        {
            var collectionName = "testcollectionexists";
            Assert.IsFalse(_database.CollectionExists(collectionName));

            _database[collectionName].Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName));
        }

        [Test]
        public void TestCreateCollection()
        {
            var collectionName = "testcreatecollection";
            Assert.IsFalse(_database.CollectionExists(collectionName));

            _database.CreateCollection(collectionName);
            Assert.IsTrue(_database.CollectionExists(collectionName));
        }

        [Test]
        public void TestDropCollection()
        {
            var collectionName = "testdropcollection";
            Assert.IsFalse(_database.CollectionExists(collectionName));

            _database[collectionName].Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName));

            _database.DropCollection(collectionName);
            Assert.IsFalse(_database.CollectionExists(collectionName));
        }

        [Test]
        public void TestEvalNoArgs()
        {
            var code = "function() { return 1; }";
            var result = _database.Eval(code);
            Assert.AreEqual(1, result.ToInt32());
        }

        [Test]
        public void TestEvalNoArgsNoLock()
        {
            var code = "function() { return 1; }";
            var result = _database.Eval(EvalFlags.NoLock, code);
            Assert.AreEqual(1, result.ToInt32());
        }

        [Test]
        public void TestEvalWithArgs()
        {
            var code = "function(x, y) { return x / y; }";
            var result = _database.Eval(code, 6, 2);
            Assert.AreEqual(3, result.ToInt32());
        }

        [Test]
        public void TestEvalWithArgsNoLock()
        {
            var code = "function(x, y) { return x / y; }";
            var result = _database.Eval(EvalFlags.NoLock, code, 6, 2);
            Assert.AreEqual(3, result.ToInt32());
        }

        [Test]
        public void TestFetchDBRef()
        {
            var collectionName = "testdbref";
            var collection = _database.GetCollection(collectionName);
            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "P", "x" } };
            collection.Insert(document);

            var dbRef = new MongoDBRef(collectionName, document["_id"].AsObjectId);
            var fetched = _database.FetchDBRef(dbRef);
            Assert.AreEqual(document, fetched);
            Assert.AreEqual(document.ToJson(), fetched.ToJson());

            var dbRefWithDatabaseName = new MongoDBRef(_database.Name, collectionName, document["_id"].AsObjectId);
            fetched = _server.FetchDBRef(dbRefWithDatabaseName);
            Assert.AreEqual(document, fetched);
            Assert.AreEqual(document.ToJson(), fetched.ToJson());
            Assert.Throws<ArgumentException>(() => { _server.FetchDBRef(dbRef); });
        }

        [Test]
        public void TestGetCollection()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collection = _database.GetCollection(typeof(BsonDocument), collectionName);
            Assert.AreSame(_database, collection.Database);
            Assert.AreEqual(_database.Name + "." + collectionName, collection.FullName);
            Assert.AreEqual(collectionName, collection.Name);
            Assert.AreEqual(_database.Settings.SafeMode, collection.Settings.SafeMode);
        }

        [Test]
        public void TestGetCollectionGeneric()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collection = _database.GetCollection(collectionName);
            Assert.AreSame(_database, collection.Database);
            Assert.AreEqual(_database.Name + "." + collectionName, collection.FullName);
            Assert.AreEqual(collectionName, collection.Name);
            Assert.AreEqual(_database.Settings.SafeMode, collection.Settings.SafeMode);
        }

        [Test]
        public void TestGetCollectionNames()
        {
            _database.Drop();
            _database["a"].Insert(new BsonDocument("a", 1));
            _database["b"].Insert(new BsonDocument("b", 1));
            _database["c"].Insert(new BsonDocument("c", 1));
            var collectionNames = _database.GetCollectionNames();
            Assert.AreEqual(new[] { "a", "b", "c", "system.indexes" }, collectionNames);
        }

        [Test]
        public void TestGetProfilingInfo()
        {
            var collection = Configuration.TestCollection;
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new BsonDocument("x", 1));
            _database.SetProfilingLevel(ProfilingLevel.All);
            var count = collection.Count();
            _database.SetProfilingLevel(ProfilingLevel.None);
            var info = _database.GetProfilingInfo(Query.Null).SetSortOrder(SortBy.Descending("$natural")).SetLimit(1).First();
            Assert.IsTrue(info.Timestamp >= new DateTime(2011, 10, 6, 0, 0, 0, DateTimeKind.Utc));
            Assert.IsTrue(info.Duration >= TimeSpan.Zero);
        }

        [Test]
        public void TestRenameCollection()
        {
            var collectionName1 = "testrenamecollection1";
            var collectionName2 = "testrenamecollection2";
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsFalse(_database.CollectionExists(collectionName2));

            _database[collectionName1].Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName1));
            Assert.IsFalse(_database.CollectionExists(collectionName2));

            _database.RenameCollection(collectionName1, collectionName2);
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsTrue(_database.CollectionExists(collectionName2));
        }

        [Test]
        public void TestRenameCollectionDropTarget()
        {
            const string collectionName1 = "testrenamecollectiondroptarget1";
            const string collectionName2 = "testrenamecollectiondroptarget2";
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsFalse(_database.CollectionExists(collectionName2));

            _database[collectionName1].Insert(new BsonDocument());
            _database[collectionName2].Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName1));
            Assert.IsTrue(_database.CollectionExists(collectionName2));

            Assert.Throws<MongoCommandException>(() => _database.RenameCollection(collectionName1, collectionName2));
            _database.RenameCollection(collectionName1, collectionName2, true);
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsTrue(_database.CollectionExists(collectionName2));
        }

        [Test]
        public void TestSetProfilingLevel()
        {
            _database.SetProfilingLevel(ProfilingLevel.None, TimeSpan.FromMilliseconds(100));
            var result = _database.GetProfilingLevel();
            Assert.AreEqual(ProfilingLevel.None, result.Level);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), result.Slow);

            _database.SetProfilingLevel(ProfilingLevel.Slow);
            result = _database.GetProfilingLevel();
            Assert.AreEqual(ProfilingLevel.Slow, result.Level);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), result.Slow);

            _database.SetProfilingLevel(ProfilingLevel.Slow, TimeSpan.FromMilliseconds(200));
            result = _database.GetProfilingLevel();
            Assert.AreEqual(ProfilingLevel.Slow, result.Level);
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), result.Slow);

            _database.SetProfilingLevel(ProfilingLevel.Slow, TimeSpan.FromMilliseconds(100));
            result = _database.GetProfilingLevel();
            Assert.AreEqual(ProfilingLevel.Slow, result.Level);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), result.Slow);

            _database.SetProfilingLevel(ProfilingLevel.All);
            result = _database.GetProfilingLevel();
            Assert.AreEqual(ProfilingLevel.All, result.Level);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), result.Slow);

            _database.SetProfilingLevel(ProfilingLevel.None);
            result = _database.GetProfilingLevel();
            Assert.AreEqual(ProfilingLevel.None, result.Level);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), result.Slow);
        }

        [Test]
        public void TestUserMethods()
        {
            var collection = _database["system.users"];
            collection.RemoveAll();
            _database.AddUser(new MongoCredentials("username", "password"), true);
            Assert.AreEqual(1, collection.Count());

            var user = _database.FindUser("username");
            Assert.AreEqual("username", user.Username);
            Assert.AreEqual(MongoUtils.Hash("username:mongo:password"), user.PasswordHash);
            Assert.AreEqual(true, user.IsReadOnly);

            var users = _database.FindAllUsers();
            Assert.AreEqual(1, users.Length);
            Assert.AreEqual("username", users[0].Username);
            Assert.AreEqual(MongoUtils.Hash("username:mongo:password"), users[0].PasswordHash);
            Assert.AreEqual(true, users[0].IsReadOnly);

            _database.RemoveUser(user);
            Assert.AreEqual(0, collection.Count());
        }
    }
}
