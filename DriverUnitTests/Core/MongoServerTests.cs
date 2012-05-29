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

        // TODO: more tests for MongoServer

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
        public void TestPing()
        {
            _server.Ping();
        }

        [Test]
        public void TestGetDatabaseNames()
        {
            var databaseNames = _server.GetDatabaseNames();
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
        public void TestReconnect()
        {
            _server.Reconnect();
            Assert.AreEqual(MongoServerState.Connected, _server.State);
        }

        [Test]
        public void TestRequestStart()
        {
            using (_server.RequestStart(_database))
            {
            }
        }

        [Test]
        public void TestRequestStartPrimary()
        {
            using (_server.RequestStart(_database, _server.Primary))
            {
            }
        }

        [Test]
        public void TestRequestStartPrimaryNested()
        {
            using (_server.RequestStart(_database, _server.Primary))
            {
                using (_server.RequestStart(_database, _server.Primary))
                {
                }
            }
        }

        [Test]
        public void TestRequestStartSlaveOk()
        {
            using (_server.RequestStart(_database, true))
            {
            }
        }

        [Test]
        public void TestRequestStartSlaveOkNested()
        {
            using (_server.RequestStart(_database, false))
            {
                using (_server.RequestStart(_database, true))
                {
                }
            }
        }

        [Test]
        public void TestVersion()
        {
            var versionZero = new Version(0, 0, 0, 0);
            Assert.AreNotEqual(versionZero, _server.BuildInfo.Version);
        }
    }
}
