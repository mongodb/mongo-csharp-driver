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

namespace MongoDB.DriverOnlineTests
{
    [TestFixture]
    public class MongoServerTests
    {
        private MongoServer server;
        private MongoDatabase database;

        [TestFixtureSetUp]
        public void Setup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            server.Connect();
            server.DropDatabase("onlinetests");
            database = server["onlinetests"];
        }

        // TODO: more tests for MongoServer

        [Test]
        public void TestDatabaseExists()
        {
            var databaseName = "onlinetests-temp";
            var database = server[databaseName];
            database.Drop();
            Assert.IsFalse(server.DatabaseExists(databaseName));
            database["test"].Insert(new BsonDocument("x", 1));
            Assert.IsTrue(server.DatabaseExists(databaseName));
        }

        [Test]
        public void TestDropDatabase()
        {
            var databaseName = "onlinetests-temp";
            var database = server[databaseName];
            var test = database["test"];
            test.Insert(new BsonDocument());
            var databaseNames = server.GetDatabaseNames();
            Assert.IsTrue(databaseNames.Contains(databaseName));

            var result = server.DropDatabase(databaseName);
            databaseNames = server.GetDatabaseNames();
            Assert.IsFalse(databaseNames.Contains(databaseName));
        }

        [Test]
        public void TestPing()
        {
            server.Ping();
        }

        [Test]
        public void TestGetDatabaseNames()
        {
            var databaseNames = server.GetDatabaseNames();
        }

        [Test]
        public void TestReconnect()
        {
            server.Reconnect();
            Assert.AreEqual(MongoServerState.Connected, server.State);
        }

        [Test]
        public void TestRunAdminCommandAs()
        {
            var result = (CommandResult)server.RunAdminCommandAs(typeof(CommandResult), "ping");
            Assert.AreEqual(true, result.Ok);
        }

        [Test]
        public void TestRunAdminCommandAsGeneric()
        {
            var result = server.RunAdminCommandAs<CommandResult>("ping");
            Assert.AreEqual(true, result.Ok);
        }

        [Test]
        public void TestVersion()
        {
            var versionZero = new Version(0, 0, 0, 0);
            Assert.AreNotEqual(versionZero, server.BuildInfo.Version);
        }
    }
}
