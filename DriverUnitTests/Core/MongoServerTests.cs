﻿/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoServerTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;
        private bool _isMasterSlavePair;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;

            var adminDatabase = _server.GetDatabase("admin");
            var commandResult = adminDatabase.RunCommand("getCmdLineOpts");
            var argv = commandResult.Response["argv"].AsBsonArray;
            _isMasterSlavePair = argv.Contains("--master") || argv.Contains("--slave");
        }

        [Test]
        public void TestArbiters()
        {
            Assert.AreEqual(0, _server.Arbiters.Length);
        }

        [Test]
        public void TestBuildInfo()
        {
            var versionZero = new Version(0, 0, 0, 0);
            var buildInfo = _server.BuildInfo;
            Assert.IsTrue(buildInfo.Bits == 32 || buildInfo.Bits == 64);
            Assert.AreNotEqual(versionZero, buildInfo.Version);
        }

        [Test]
        public void TestCreateMongoServerSettings()
        {
            var settings = new MongoServerSettings
            {
                Server = new MongoServerAddress("localhost"),
                SafeMode = SafeMode.True
            };
            var server1 = MongoServer.Create(settings);
            var server2 = MongoServer.Create(settings);
            Assert.AreSame(server1, server2);
            Assert.AreEqual(settings, server1.Settings);
        }

        [Test]
        public void TestCreateNoArgs()
        {
            var server = MongoServer.Create(); // no args!
            Assert.IsNull(server.Settings.DefaultCredentials);
            Assert.AreEqual(MongoDefaults.GuidRepresentation, server.Settings.GuidRepresentation);
            Assert.AreEqual(SafeMode.False, server.Settings.SafeMode);
            Assert.AreEqual(false, server.Settings.SlaveOk);
            Assert.AreEqual(new MongoServerAddress("localhost"), server.Instance.Address);
        }

        [Test]
        public void TestDatabaseExists()
        {
            if (!_isMasterSlavePair)
            {
                _database.Drop();
                Assert.IsFalse(_server.DatabaseExists(_database.Name));
                _collection.Insert(new BsonDocument("x", 1));
                Assert.IsTrue(_server.DatabaseExists(_database.Name));
            }
        }

        [Test]
        public void TestDropDatabase()
        {
            if (!_isMasterSlavePair)
            {
                _collection.Insert(new BsonDocument());
                var databaseNames = _server.GetDatabaseNames();
                Assert.IsTrue(databaseNames.Contains(_database.Name));

                var result = _server.DropDatabase(_database.Name);
                databaseNames = _server.GetDatabaseNames();
                Assert.IsFalse(databaseNames.Contains(_database.Name));
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
            var server = MongoServer.Create("mongodb://newhostnamethathasnotbeenusedbefore");
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
            var settings = new MongoDatabaseSettings(_server, "test") { SlaveOk = true };
            var database1 = _server.GetDatabase(settings);
            var database2 = _server.GetDatabase(settings);
            Assert.AreSame(database1, database2);
            Assert.AreEqual("test", database1.Name);
            Assert.AreEqual(true, database1.Settings.SlaveOk);
        }

        [Test]
        public void TestGetDatabaseNames()
        {
            var databaseNames = _server.GetDatabaseNames();
        }

        [Test]
        public void TestInstance()
        {
            var instance = _server.Instance;
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.IsPrimary);
        }

        [Test]
        public void TestInstances()
        {
            var instances = _server.Instances;
            Assert.IsNotNull(instances);
            Assert.AreEqual(1, instances.Length);
            Assert.IsTrue(instances[0].IsPrimary);
        }

        [Test]
        public void TestIsDatabaseNameValid()
        {
            string message;
            Assert.Throws<ArgumentNullException>(() => { _server.IsDatabaseNameValid(null, out message); });
            Assert.IsFalse(_server.IsDatabaseNameValid("", out message));
            Assert.IsFalse(_server.IsDatabaseNameValid("/", out message));
            Assert.IsFalse(_server.IsDatabaseNameValid(new string('x', 128), out message));
        }

        [Test]
        public void TestPassives()
        {
            Assert.AreEqual(0, _server.Passives.Length);
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
        public void TestReconnect()
        {
            _server.Reconnect();
            Assert.AreEqual(MongoServerState.Connected, _server.State);
        }

        [Test]
        public void TestReplicaSetName()
        {
            Assert.IsNull(_server.ReplicaSetName);
        }

        [Test]
        public void TestRequestStart()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_database))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestRequestStartPrimary()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_database, _server.Primary))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestRequestStartPrimaryNested()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_database, _server.Primary))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
                using (_server.RequestStart(_database, _server.Primary))
                {
                    Assert.AreEqual(2, _server.RequestNestingLevel);
                }
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestRequestStartSlaveOk()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_database, true))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
            }
            Assert.AreEqual(0, _server.RequestNestingLevel);
        }

        [Test]
        public void TestRequestStartSlaveOkNested()
        {
            Assert.AreEqual(0, _server.RequestNestingLevel);
            using (_server.RequestStart(_database, false))
            {
                Assert.AreEqual(1, _server.RequestNestingLevel);
                using (_server.RequestStart(_database, true))
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
            Assert.AreEqual(0, _server.Secondaries.Length);
        }

        [Test]
        public void TestVersion()
        {
            var versionZero = new Version(0, 0, 0, 0);
            Assert.AreNotEqual(versionZero, _server.BuildInfo.Version);
        }
    }
}
