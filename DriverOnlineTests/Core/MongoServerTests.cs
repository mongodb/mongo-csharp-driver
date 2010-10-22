/* Copyright 2010 10gen Inc.
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

namespace MongoDB.DriverOnlineTests {
    [TestFixture]
    public class MongoServerTests {
        private MongoServer server;
        private MongoDatabase database;

        [TestFixtureSetUp]
        public void Setup() {
            server = MongoServer.Create();
            server.Connect();
            database = server["onlinetests"];
        }

        // TODO: more tests for MongoServer

        [Test]
        public void TestDropDatabase() {
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
        public void TestGetDatabaseNames() {
            var databaseNames = server.GetDatabaseNames();
        }

        [Test]
        [Explicit] // reconnecting is slow (but why? opening a socket should be faster)
        public void TestReconnect() {
            server.Reconnect();
            Assert.AreEqual(MongoServerState.Connected, server.State);
        }

        [Test]
        public void TestRenameCollection() {
            var collectionNames = database.GetCollectionNames();
            if (collectionNames.Contains("testrenamecollection1")) { database.DropCollection("testrenamecollection1"); }
            if (collectionNames.Contains("testrenamecollection2")) { database.DropCollection("testrenamecollection2"); }
            
            var collection1 = database["testrenamecollection1"];
            collection1.Insert(new BsonDocument("x", 1));
            collectionNames = database.GetCollectionNames();
            Assert.IsTrue(collectionNames.Contains("onlinetests.testrenamecollection1"));
            Assert.IsFalse(collectionNames.Contains("onlinetests.testrenamecollection2"));

            server.RenameCollection("onlinetests.testrenamecollection1", "onlinetests.testrenamecollection2");
            collectionNames = database.GetCollectionNames();
            Assert.IsFalse(collectionNames.Contains("onlinetests.testrenamecollection1"));
            Assert.IsTrue(collectionNames.Contains("onlinetests.testrenamecollection2"));

            database.DropCollection("testrenamecollection2");
        }
    }
}
