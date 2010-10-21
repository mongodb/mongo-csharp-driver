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
    public class MongoDatabaseTests {
        private MongoServer server;
        private MongoDatabase database;

        [TestFixtureSetUp]
        public void Setup() {
            server = MongoServer.Create();
            server.Connect();
            database = server["onlinetests"];
        }

        // TODO: more tests for MongoDatabase

        [Test]
        public void TestDropCollection() {
            var collectionName = "testdropcollection";
            var collection = database[collectionName];
            collection.Insert(new BsonDocument());
            var collectionNames = database.GetCollectionNames();
            Assert.IsTrue(collectionNames.Contains(collection.FullName));

            database.DropCollection(collectionName);
            collectionNames = database.GetCollectionNames();
            Assert.IsFalse(collectionNames.Contains(collection.FullName));
       }

        [Test]
        public void TestGetCollectionNames() {
            var collectionNames = database.GetCollectionNames();
        }
    }
}
