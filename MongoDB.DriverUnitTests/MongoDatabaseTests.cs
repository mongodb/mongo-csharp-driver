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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoDatabaseTests
    {
        private MongoServer _server;
        private MongoServerInstance _primary;
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _primary = Configuration.TestServer.Primary;
            _database = Configuration.TestDatabase;
            _database.Drop();
        }

        // TODO: more tests for MongoDatabase

        [Test]
        public void TestCollectionExists()
        {
            var collectionName = "testcollectionexists";
            Assert.IsFalse(_database.CollectionExists(collectionName));

            _database.GetCollection(collectionName).Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName));
        }

        [Test]
        public void TestConstructorArgumentChecking()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Throws<ArgumentNullException>(() => { new MongoDatabase(null, "name", settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoDatabase(_server, null, settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoDatabase(_server, "name", null); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new MongoDatabase(_server, "", settings); });
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

            _database.GetCollection(collectionName).Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName));

            _database.DropCollection(collectionName);
            Assert.IsFalse(_database.CollectionExists(collectionName));
        }

        [Test]
        public void TestEvalNoArgs()
        {
            var code = "function() { return 1; }";
#pragma warning disable 618
            var result = _database.Eval(code);
#pragma warning restore
            Assert.AreEqual(1, result.ToInt32());
        }

        [Test]
        public void TestEvalNoArgsNoLock()
        {
            var code = "function() { return 1; }";
#pragma warning disable 618
            var result = _database.Eval(EvalFlags.NoLock, code);
#pragma warning restore
            Assert.AreEqual(1, result.ToInt32());
        }

        [Test]
        public void TestEvalWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        failpoint.SetAlwaysOn();
                        var args = new EvalArgs
                        {
                            Code = "return 0;",
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<ExecutionTimeoutException>(() => _database.Eval(args));
                    }
                }
            }
        }

        [Test]
        public void TestEvalWithOneArg()
        {
            var code = "function(x) { return x + 1; }";
#pragma warning disable 618
            var result = _database.Eval(code, 1);
#pragma warning restore
            Assert.AreEqual(2, result.ToInt32());
        }

        [Test]
        public void TestEvalWithOneArgNoLock()
        {
            var code = "function(x) { return x + 1; }";
#pragma warning disable 618
            var result = _database.Eval(EvalFlags.NoLock, code, 1);
#pragma warning restore
            Assert.AreEqual(2, result.ToInt32());
        }

        [Test]
        public void TestEvalWithTwoArgs()
        {
            var code = "function(x, y) { return x / y; }";
#pragma warning disable 618
            var result = _database.Eval(code, 6, 2);
#pragma warning restore
            Assert.AreEqual(3, result.ToInt32());
        }

        [Test]
        public void TestEvalWithTwoArgsNoLock()
        {
            var code = "function(x, y) { return x / y; }";
#pragma warning disable 618
            var result = _database.Eval(EvalFlags.NoLock, code, 6, 2);
#pragma warning restore
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
            Assert.AreEqual(_database.Settings.WriteConcern, collection.Settings.WriteConcern);
        }

        [Test]
        public void TestGetCollectionGeneric()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collection = _database.GetCollection(collectionName);
            Assert.AreSame(_database, collection.Database);
            Assert.AreEqual(_database.Name + "." + collectionName, collection.FullName);
            Assert.AreEqual(collectionName, collection.Name);
            Assert.AreEqual(_database.Settings.WriteConcern, collection.Settings.WriteConcern);
        }

        [Test]
        public void TestGetCollectionNames()
        {
            _database.Drop();
            _database.GetCollection("a").Insert(new BsonDocument("a", 1));
            _database.GetCollection("b").Insert(new BsonDocument("b", 1));
            _database.GetCollection("c").Insert(new BsonDocument("c", 1));
            var collectionNames = _database.GetCollectionNames();
            Assert.AreEqual(new[] { "a", "b", "c", "system.indexes" }, collectionNames);
        }

        [Test]
        public void TestGetProfilingInfo()
        {
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
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
            }
        }

        [Test]
        public void TestIsCollectionNameValid()
        {
            string message;
            Assert.Throws<ArgumentNullException>(() => { _database.IsCollectionNameValid(null, out message); });
            Assert.IsFalse(_database.IsCollectionNameValid("", out message));
            Assert.IsFalse(_database.IsCollectionNameValid("a\0b", out message));
            Assert.IsFalse(_database.IsCollectionNameValid(new string('x', 128), out message));
        }

        [Test]
        public void TestRenameCollection()
        {
            var collectionName1 = "testrenamecollection1";
            var collectionName2 = "testrenamecollection2";
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsFalse(_database.CollectionExists(collectionName2));

            _database.GetCollection(collectionName1).Insert(new BsonDocument());
            Assert.IsTrue(_database.CollectionExists(collectionName1));
            Assert.IsFalse(_database.CollectionExists(collectionName2));

            _database.RenameCollection(collectionName1, collectionName2);
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsTrue(_database.CollectionExists(collectionName2));
        }

        [Test]
        public void TestRenameCollectionArgumentChecking()
        {
            Assert.Throws<ArgumentNullException>(() => { _database.RenameCollection(null, "new"); });
            Assert.Throws<ArgumentNullException>(() => { _database.RenameCollection("old", null); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _database.RenameCollection("old", ""); });
        }

        [Test]
        public void TestRenameCollectionDropTarget()
        {
            const string collectionName1 = "testrenamecollectiondroptarget1";
            const string collectionName2 = "testrenamecollectiondroptarget2";
            Assert.IsFalse(_database.CollectionExists(collectionName1));
            Assert.IsFalse(_database.CollectionExists(collectionName2));

            _database.GetCollection(collectionName1).Insert(new BsonDocument());
            _database.GetCollection(collectionName2).Insert(new BsonDocument());
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
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
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
            }
        }

        [Test]
        [TestCase("user1", "pass1", true)]
        [TestCase("user2", "pass2", false)]
        public void TestUserMethods(string username, string password, bool isReadOnly)
        {
            #pragma warning disable 618
            using (_database.RequestStart(ReadPreference.Primary))
            {
                bool usesCommands = _database.Server.RequestConnection.ServerInstance.Supports(FeatureId.UserManagementCommands);
                if (usesCommands)
                {
                    _database.RunCommand("dropAllUsersFromDatabase");
                }
                else
                {
                    var collection = _database.GetCollection("system.users");
                    collection.RemoveAll();
                }

                _database.AddUser(new MongoUser(username, new PasswordEvidence(password), isReadOnly));

                var user = _database.FindUser(username);
                Assert.IsNotNull(user);
                Assert.AreEqual(username, user.Username);
                if (!usesCommands)
                {
                    Assert.AreEqual(MongoUtils.Hash(string.Format("{0}:mongo:{1}", username, password)), user.PasswordHash);
                    Assert.AreEqual(isReadOnly, user.IsReadOnly);
                }

                var users = _database.FindAllUsers();
                Assert.AreEqual(1, users.Length);
                Assert.AreEqual(username, users[0].Username);
                if (!usesCommands)
                {
                    Assert.AreEqual(MongoUtils.Hash(string.Format("{0}:mongo:{1}", username, password)), users[0].PasswordHash);
                    Assert.AreEqual(isReadOnly, users[0].IsReadOnly);
                }

                _database.RemoveUser(user);
                user = _database.FindUser(username);
                Assert.IsNull(user);
                #pragma warning restore
            }
        }
    }
}