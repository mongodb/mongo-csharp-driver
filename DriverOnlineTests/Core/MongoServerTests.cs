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

namespace MongoDB.DriverOnlineTests
{
    [TestFixture]
    public class MongoServerTests
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

        // TODO: more tests for MongoServer

        [Test]
        public void TestDatabaseExists()
        {
            _database.Drop();
            Assert.IsFalse(_server.DatabaseExists(_database.Name));
            _collection.Insert(new BsonDocument("x", 1));
            Assert.IsTrue(_server.DatabaseExists(_database.Name));
        }

        [Test]
        public void TestDropDatabase()
        {
            _collection.Insert(new BsonDocument());
            var databaseNames = _server.GetDatabaseNames();
            Assert.IsTrue(databaseNames.Contains(_database.Name));

            var result = _server.DropDatabase(_database.Name);
            databaseNames = _server.GetDatabaseNames();
            Assert.IsFalse(databaseNames.Contains(_database.Name));
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
        public void TestReconnect()
        {
            _server.Reconnect();
            Assert.AreEqual(MongoServerState.Connected, _server.State);
        }

        [Test]
        public void TestRunAdminCommandAs()
        {
            var result = (CommandResult)_server.RunAdminCommandAs(typeof(CommandResult), "ping");
            Assert.AreEqual(true, result.Ok);
        }

        [Test]
        public void TestRunAdminCommandAsGeneric()
        {
            var result = _server.RunAdminCommandAs<CommandResult>("ping");
            Assert.AreEqual(true, result.Ok);
        }

        [Test]
        public void TestVersion()
        {
            var versionZero = new Version(0, 0, 0, 0);
            Assert.AreNotEqual(versionZero, _server.BuildInfo.Version);
        }
    }
}
