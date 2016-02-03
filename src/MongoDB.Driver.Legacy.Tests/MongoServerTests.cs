/* Copyright 2010-2015 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoServerTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;
        private bool _isMasterSlavePair;
        private bool _isReplicaSet;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _collection = LegacyTestConfiguration.Collection;
            _isReplicaSet = LegacyTestConfiguration.IsReplicaSet;

            var adminDatabase = _server.GetDatabase("admin");
            var commandResult = adminDatabase.RunCommand("getCmdLineOpts");
            var argv = commandResult.Response["argv"].AsBsonArray;
            _isMasterSlavePair = argv.Contains("--master") || argv.Contains("--slave");
        }

        [Test]
        public void TestArbiters()
        {
            if (_isReplicaSet)
            {
                var isMasterResult = _database.RunCommand("isMaster").Response;
                BsonValue arbiters;
                int arbiterCount = 0;
                if (isMasterResult.TryGetValue("arbiters", out arbiters))
                {
                    arbiterCount = arbiters.AsBsonArray.Count;
                }
                Assert.AreEqual(arbiterCount, _server.Arbiters.Length);
            }
        }

        [Test]
        public void TestBuildInfo()
        {
            var versionZero = new Version(0, 0, 0);
            var buildInfo = _server.BuildInfo;
            Assert.AreNotEqual(versionZero, buildInfo.Version);
        }

        [Test]
        public void TestCreateMongoServerSettings()
        {
            var settings = new MongoServerSettings
            {
                Server = new MongoServerAddress("localhost"),
            };
#pragma warning disable 618
            var server1 = MongoServer.Create(settings);
            var server2 = MongoServer.Create(settings);
#pragma warning restore 618
            Assert.AreSame(server1, server2);
            Assert.AreEqual(settings, server1.Settings);
        }

        [Test]
        public void TestDatabaseExists()
        {
            if (!_isMasterSlavePair)
            {
                var databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
                var database = _server.GetDatabase(databaseNamespace.DatabaseName);
                var collection = database.GetCollection("test");

                database.Drop();
                Assert.IsFalse(_server.DatabaseExists(database.Name));
                collection.Insert(new BsonDocument("x", 1));
                Assert.IsTrue(_server.DatabaseExists(database.Name));
            }
        }

        [Test]
        public void TestDropDatabase()
        {
            if (!_isMasterSlavePair)
            {
                var databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
                var database = _server.GetDatabase(databaseNamespace.DatabaseName);
                var collection = database.GetCollection("test");

                collection.Insert(new BsonDocument());
                var databaseNames = _server.GetDatabaseNames();
                Assert.IsTrue(databaseNames.Contains(database.Name));

                _server.DropDatabase(database.Name);
                databaseNames = _server.GetDatabaseNames();
                Assert.IsFalse(databaseNames.Contains(database.Name));
            }
        }

        [Test]
        public void TestFetchDBRef()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 2 } });
            var dbRef = new MongoDBRef(_database.Name, _collection.Name, 1);
            var document = _server.FetchDBRef(dbRef);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(1, document["_id"].AsInt32);
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestGetAllServers()
        {
            var snapshot1 = MongoServer.GetAllServers();
#pragma warning disable 618
            var server = MongoServer.Create("mongodb://newhostnamethathasnotbeenusedbefore");
#pragma warning restore
            var snapshot2 = MongoServer.GetAllServers();
            Assert.AreEqual(snapshot1.Length + 1, snapshot2.Length);
            Assert.IsFalse(snapshot1.Contains(server));
            Assert.IsTrue(snapshot2.Contains(server));
            MongoServer.UnregisterServer(server);
            var snapshot3 = MongoServer.GetAllServers();
            Assert.AreEqual(snapshot1.Length, snapshot3.Length);
            Assert.IsFalse(snapshot3.Contains(server));
        }

        [Test]
        public void TestGetDatabase()
        {
            var settings = new MongoDatabaseSettings { ReadPreference = ReadPreference.Primary };
            var database = _server.GetDatabase("test", settings);
            Assert.AreEqual("test", database.Name);
            Assert.AreEqual(ReadPreference.Primary, database.Settings.ReadPreference);
        }

        [Test]
        public void TestGetDatabaseNames()
        {
            var names = _server.GetDatabaseNames();

            CollectionAssert.IsOrdered(names);
        }

        [Test]
        public void TestInstance()
        {
            if (_server.Instances.Length == 1)
            {
                var instance = _server.Instance;
                Assert.IsNotNull(instance);
                Assert.IsTrue(instance.IsPrimary);
            }
        }

        [Test]
        public void TestInstances()
        {
            var instances = _server.Instances;
            Assert.IsNotNull(instances);
            Assert.GreaterOrEqual(instances.Length, 1);
        }

        [Test]
        public void TestIsDatabaseNameValid()
        {
            string message;
            Assert.Throws<ArgumentNullException>(() => { _server.IsDatabaseNameValid(null, out message); });
            Assert.IsFalse(_server.IsDatabaseNameValid("", out message));
            Assert.IsFalse(_server.IsDatabaseNameValid("/", out message));
            Assert.IsFalse(_server.IsDatabaseNameValid(new string('x', 128), out message));
            Assert.IsTrue(_server.IsDatabaseNameValid("$external", out message));
        }

        [Test]
        public void TestPing()
        {
            _server.Ping();
        }

        [Test]
        public void TestPrimary()
        {
            var instance = _server.Primary;
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.IsPrimary);
        }

        [Test]
        [Explicit]
        public void TestReconnect()
        {
            _server.Reconnect();
            Assert.IsTrue(_server.State == MongoServerState.Connected || _server.State == MongoServerState.ConnectedToSubset);
        }

        [Test]
        public void TestReplicaSetName()
        {
            if (_isReplicaSet)
            {
                Assert.IsNotNull(_server.ReplicaSetName);
            }
            else
            {
                Assert.IsNull(_server.ReplicaSetName);
            }
        }

        [Test]
        public void TestRequestStart()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart())
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestRequestStartPrimary()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_server.Primary))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestRequestStartPrimaryNested()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_server.Primary))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
                using (_server.RequestStart(_server.Primary))
                {
                    Assert.AreEqual(2, _server.RequestNestingLevel);
                }
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestSecondaries()
        {
            Assert.IsTrue(_server.Secondaries.Length < _server.Instances.Length);
        }

        [Test]
        public void TestVersion()
        {
            var versionZero = new Version(0, 0, 0);
            Assert.AreNotEqual(versionZero, _server.BuildInfo.Version);
        }
    }
}
