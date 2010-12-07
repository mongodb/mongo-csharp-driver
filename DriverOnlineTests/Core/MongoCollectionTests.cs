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
        public void TestCountWithQuery() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 2));
            var query = Query.EQ("x", 1);
            var count = collection.Count(query);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestCreateIndex() {
            collection.DropAllIndexes();
            var indexes = collection.GetIndexes().ToArray();
            Assert.AreEqual(1, indexes.Length);
            Assert.AreEqual("_id_", indexes[0]["name"].AsString);

            collection.DropAllIndexes();
            collection.CreateIndex("x");
            indexes = collection.GetIndexes().ToArray();
            Assert.AreEqual(2, indexes.Length);
            Assert.AreEqual("_id_", indexes[0]["name"].AsString);
            Assert.AreEqual("x_1", indexes[1]["name"].AsString);

            collection.DropAllIndexes();
            collection.CreateIndex(IndexKeys.Ascending("x").Descending("y"), IndexOptions.SetUnique(true));
            indexes = collection.GetIndexes().ToArray();
            Assert.AreEqual(2, indexes.Length);
            Assert.AreEqual("_id_", indexes[0]["name"].AsString);
            Assert.AreEqual("x_1_y_-1", indexes[1]["name"].AsString);
            Assert.AreEqual(true, indexes[1]["unique"].ToBoolean());
        }

        [Test]
        public void TestDistinct() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 2));
            collection.Insert(new BsonDocument("x", 3));
            collection.Insert(new BsonDocument("x", 3));
            var values = new HashSet<BsonValue>(collection.Distinct("x"));
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(true, values.Contains(1));
            Assert.AreEqual(true, values.Contains(2));
            Assert.AreEqual(true, values.Contains(3));
            Assert.AreEqual(false, values.Contains(4));
        }

        [Test]
        public void TestDistinctWithQuery() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 2));
            collection.Insert(new BsonDocument("x", 3));
            collection.Insert(new BsonDocument("x", 3));
            var query = Query.LTE("x", 2);
            var values = new HashSet<BsonValue>(collection.Distinct("x", query));
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual(true, values.Contains(1));
            Assert.AreEqual(true, values.Contains(2));
            Assert.AreEqual(false, values.Contains(3));
            Assert.AreEqual(false, values.Contains(4));
        }

        [Test]
        public void TestDropAllIndexes() {
            collection.DropAllIndexes();
        }

        [Test]
        public void TestDropIndex() {
            collection.DropAllIndexes();
            Assert.AreEqual(1, collection.GetIndexes().Count());
            Assert.Throws<MongoCommandException>(() => collection.DropIndex("x"));

            collection.CreateIndex("x");
            Assert.AreEqual(2, collection.GetIndexes().Count());
            collection.DropIndex("x");
            Assert.AreEqual(1, collection.GetIndexes().Count());
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

        [Test]
        public void TestFindOne() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = collection.FindOne();
            Assert.AreEqual(1, result["x"].AsInt32);
            Assert.AreEqual(2, result["y"].AsInt32);
        }

        [Test]
        public void TestFindOneById() {
            collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            collection.Insert(new BsonDocument { { "_id", id }, { "x", 1 }, { "y", 2 } });
            var result = collection.FindOneById(id);
            Assert.AreEqual(1, result["x"].AsInt32);
            Assert.AreEqual(2, result["y"].AsInt32);
        }

        [Test]
        public void TestGetIndexes() {
            collection.DropAllIndexes();
            var indexes = collection.GetIndexes().ToArray();
            Assert.AreEqual(1, indexes.Length);
            Assert.AreEqual("_id_", indexes[0]["name"].AsString);
        }

        [Test]
        public void TestGroup() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 2));
            collection.Insert(new BsonDocument("x", 3));
            collection.Insert(new BsonDocument("x", 3));
            collection.Insert(new BsonDocument("x", 3));
            var initial = new BsonDocument("count", 0);
            var reduce = "function(doc, prev) { prev.count += 1 }";
            var results = collection.Group(Query.Null, "x", initial, reduce, null).ToArray();
            Assert.AreEqual(3, results.Length);
            Assert.AreEqual(1, results[0]["x"].ToInt32());
            Assert.AreEqual(2, results[0]["count"].ToInt32());
            Assert.AreEqual(2, results[1]["x"].ToInt32());
            Assert.AreEqual(1, results[1]["count"].ToInt32());
            Assert.AreEqual(3, results[2]["x"].ToInt32());
            Assert.AreEqual(3, results[2]["count"].ToInt32());
        }

        [Test]
        public void TestIndexExists() {
            collection.DropAllIndexes();
            Assert.AreEqual(false, collection.IndexExists("x"));

            collection.CreateIndex("x");
            Assert.AreEqual(true, collection.IndexExists("x"));

            collection.CreateIndex(IndexKeys.Ascending("y"));
            Assert.AreEqual(true, collection.IndexExists(IndexKeys.Ascending("y")));
        }

        [Test]
        public void TestMapReduce() {
            // this is Example 1 on p. 87 of MongoDB: The Definitive Guide
            // by Kristina Chodorow and Michael Dirolf

            collection.RemoveAll();
            collection.Insert(new BsonDocument { { "A", 1 }, { "B", 2 } });
            collection.Insert(new BsonDocument { { "B", 1 }, { "C", 2 } });
            collection.Insert(new BsonDocument { { "X", 1 }, { "B", 2 } });

            var map =
                "function() {\n" +
                "    for (var key in this) {\n" +
                "        emit(key, {count : 1});\n" +
                "    }\n" +
                "}\n";

            var reduce =
                "function(key, emits) {\n" +
                "    total = 0;\n" +
                "    for (var i in emits) {\n" +
                "        total += emits[i].count;\n" +
                "    }\n" +
                "    return {count : total};\n" +
                "}\n";

            using (database.RequestStart()) {
                var result = collection.MapReduce(map, reduce);
                Assert.IsTrue(result.CommandResult.Ok);
                Assert.IsTrue(result.Duration >= TimeSpan.Zero);
                Assert.AreEqual(9, result.EmitCount);
                Assert.AreEqual(5, result.OutputCount);
                Assert.AreEqual(3, result.InputCount);
                Assert.IsNotNullOrEmpty(result.ResultCollectionName);

                var expectedCounts = new Dictionary<string, int> {
                    { "A", 1 },
                    { "B", 3 },
                    { "C", 1 },
                    { "X", 1 },
                    { "_id", 3 }
                };
                foreach (var document in result.GetResults<BsonDocument>()) {
                    var key = document["_id"].AsString;
                    var count = document["value"].AsBsonDocument["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }
            }
        }

        [Test]
        public void TestSetFields() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = collection.FindAll().SetFields("x").FirstOrDefault();
            Assert.AreEqual(2, result.ElementCount);
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
        public void TestGeoNear()
        {
            collection.RemoveAll();
            collection.DropAllIndexes();
            collection.EnsureIndex(IndexKeys.GeoSpatial("Coordinates"));

            collection.Insert(new BsonDocument { {"Location", "Washington DC"}, { "x", 4 }, { "y", 2 }, { "Coordinates", new BsonDocument { { "Longitude", 13 }, { "Latitude", 18 } } } });
            collection.Insert(new BsonDocument { { "Location", "Washington DC" }, { "x", 2 }, { "y", 2 }, { "Coordinates", new BsonDocument { { "Longitude", 13 }, { "Latitude", 18 } } } });
            collection.Insert(new BsonDocument { { "Location", "Washington DC" }, { "x", 3 }, { "y", 2 }, { "Coordinates", new BsonDocument { { "Longitude", 13 }, { "Latitude", 18 } } } });
            collection.Insert(new BsonDocument { { "Location", "Washington DC" }, { "x", 1 }, { "y", 2 }, { "Coordinates", new BsonDocument { { "Longitude", 13 }, { "Latitude", 18 } } } });

            var geoNearResult = collection.GeoNear<BsonDocument>(Query.Null, 0, 0, 10);

            Assert.IsNotNull(geoNearResult);

            Assert.AreEqual("onlinetests.testcollection", geoNearResult.NameSpace);
            
            Assert.AreEqual("1100000000000000000000000000000000000000000000000000", geoNearResult.Near);

            Assert.AreEqual(4, geoNearResult.Stats.objectsLoaded);
            Assert.AreNotEqual(-1, geoNearResult.Stats.time);
            Assert.AreNotEqual(0, geoNearResult.Stats.btreelocs);
            Assert.AreNotEqual(0, geoNearResult.Stats.nscanned);
            Assert.AreNotEqual(0, geoNearResult.Stats.avgDistance);
            Assert.AreNotEqual(0, geoNearResult.Stats.maxDistance);

            Assert.IsNotNull(geoNearResult.Results);
            Assert.AreEqual(4, geoNearResult.Results.Count());

            Assert.AreEqual(22.203600417328694, geoNearResult.Results.First().Distance);
            Assert.AreEqual("Washington DC", geoNearResult.Results.First().Value.GetValue("Location").AsString);


            //var result = collection.FindAll().SetSortOrder("x").SetLimit(3).Select(x => x["x"].AsInt32);
            //Assert.AreEqual(3, result.Count());
            //CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
        }



        [Test]
        public void TestGetStats() {
            var dataSize = collection.GetStats();
        }

        [Test]
        public void TestTotalDataSize() {
            var dataSize = collection.GetTotalDataSize();
        }

        [Test]
        public void TestTotalStorageSize() {
            var dataSize = collection.GetTotalStorageSize();
        }

        [Test]
        public void Validate() {
            var result = collection.Validate();
        }
    }
}
