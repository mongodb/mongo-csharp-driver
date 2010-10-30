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
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests {
    [TestFixture]
    public class MongoCollectionTests {
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<BsonDocument> collection;

        [TestFixtureSetUp]
        public void Setup() {
            server = MongoServer.Create();
            server.Connect();
            database = server["onlinetests"];
            collection = database["testcollection"];
        }

        // TODO: more tests for MongoCollection

        [Test]
        public void TestCountZero() {
            collection.RemoveAll();
            var count = collection.Count();
            Assert.AreEqual(0, count);
        }

        [Test]
        public void TestCountOne() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument());
            var count = collection.Count();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestSetFields() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = collection.FindAll().SetFields("x").FirstOrDefault();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("_id", result.GetElement(0).Name);
            Assert.AreEqual("x", result.GetElement(1).Name);
        }

        [Test]
        public void TestSortAndLimit() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = collection.FindAll().SetSortOrder("x").SetLimit(3).Select(x => x["x"].AsInt32);
            Assert.AreEqual(3, result.Count());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
        }

        [Test]
        public void TestFind() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = collection.Find(Query.GT("x", 3));
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(4, result.Select(x => x["x"].AsInt32).FirstOrDefault());
        }
    }
}
