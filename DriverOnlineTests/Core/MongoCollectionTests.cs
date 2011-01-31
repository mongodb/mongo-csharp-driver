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
using MongoDB.Bson.DefaultSerializer;
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
            server = MongoServer.Create("mongodb://localhost/?safe=true");
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
            collection.DropAllIndexes();
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
            collection.DropAllIndexes();
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
        public void TestFindNearSphericalFalse() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.Near("Location", -74.0, 40.74);
            var hits = collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);

            var hit0 = hits[0];
            Assert.AreEqual(-74.0, hit0["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit0["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("10gen", hit0["Name"].AsString);
            Assert.AreEqual("Office", hit0["Type"].AsString);

            // with spherical false "Three" is slightly closer than "Two"
            var hit1 = hits[1];
            Assert.AreEqual(-74.0, hit1["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(41.73, hit1["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Three", hit1["Name"].AsString);
            Assert.AreEqual("Coffee", hit1["Type"].AsString);

            var hit2 = hits[2];
            Assert.AreEqual(-75.0, hit2["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit2["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Two", hit2["Name"].AsString);
            Assert.AreEqual("Coffee", hit2["Type"].AsString);

            query = Query.Near("Location", -74.0, 40.74, 0.5); // with maxDistance
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(1, hits.Length);

            hit0 = hits[0];
            Assert.AreEqual(-74.0, hit0["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit0["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("10gen", hit0["Name"].AsString);
            Assert.AreEqual("Office", hit0["Type"].AsString);

            query = Query.Near("Location", -174.0, 40.74, 0.5); // with no hits
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(0, hits.Length);
        }

        [Test]
        public void TestFindNearSphericalTrue() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.Near("Location", -74.0, 40.74, double.MaxValue, true); // spherical
            var hits = collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);

            var hit0 = hits[0];
            Assert.AreEqual(-74.0, hit0["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit0["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("10gen", hit0["Name"].AsString);
            Assert.AreEqual("Office", hit0["Type"].AsString);

            // with spherical true "Two" is considerably closer than "Three"
            var hit1 = hits[1];
            Assert.AreEqual(-75.0, hit1["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit1["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Two", hit1["Name"].AsString);
            Assert.AreEqual("Coffee", hit1["Type"].AsString);

            var hit2 = hits[2];
            Assert.AreEqual(-74.0, hit2["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(41.73, hit2["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Three", hit2["Name"].AsString);
            Assert.AreEqual("Coffee", hit2["Type"].AsString);

            query = Query.Near("Location", -74.0, 40.74, 0.5); // with maxDistance
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(1, hits.Length);

            hit0 = hits[0];
            Assert.AreEqual(-74.0, hit0["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit0["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("10gen", hit0["Name"].AsString);
            Assert.AreEqual("Office", hit0["Type"].AsString);

            query = Query.Near("Location", -174.0, 40.74, 0.5); // with no hits
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(0, hits.Length);
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
        public void TestFindWithinCircleSphericalFalse() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinCircle("Location", -74.0, 40.74, 1.0, false); // not spherical
            var hits = collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);
            // note: the hits are unordered

            query = Query.WithinCircle("Location", -74.0, 40.74, 0.5, false); // smaller radius
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(1, hits.Length);

            query = Query.WithinCircle("Location", -174.0, 40.74, 1.0, false); // different part of the world
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(0, hits.Length);
        }

        [Test]
        public void TestFindWithinCircleSphericalTrue() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinCircle("Location", -74.0, 40.74, 0.1, true); // spherical
            var hits = collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);
            // note: the hits are unordered

            query = Query.WithinCircle("Location", -74.0, 40.74, 0.01, false); // smaller radius
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(1, hits.Length);

            query = Query.WithinCircle("Location", -174.0, 40.74, 0.1, false); // different part of the world
            hits = collection.Find(query).ToArray();
            Assert.AreEqual(0, hits.Length);
        }

        [Test]
        public void TestFindWithinRectangle() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinRectangle("Location", -75.0, 40, -73.0, 42.0);
            var hits = collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);
            // note: the hits are unordered
        }

#pragma warning disable 649 // never assigned to
        private class Place {
            public ObjectId Id;
            public double[] Location;
            public string Name;
            public string Type;
        }
#pragma warning restore

        [Test]
        public void TestGeoNear() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { 1.0, 1.0 }, Name = "One", Type = "Museum" });
            collection.Insert(new Place { Location = new[] { 1.0, 2.0 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { 1.0, 3.0 }, Name = "Three", Type = "Library" });
            collection.Insert(new Place { Location = new[] { 1.0, 4.0 }, Name = "Four", Type = "Museum" });
            collection.Insert(new Place { Location = new[] { 1.0, 5.0 }, Name = "Five", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var options = GeoNearOptions
                .SetDistanceMultiplier(1)
                .SetMaxDistance(100);
            var result = collection.GeoNearAs<Place>(Query.Null, 0.0, 0.0, 100, options);
            Assert.IsTrue(result.Ok);
            Assert.AreEqual("onlinetests.testcollection", result.Namespace);
            Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
            Assert.IsTrue(result.Stats.BTreeLocations >= 0);
            Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
            Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
            Assert.IsTrue(result.Stats.NumberScanned >= 0);
            Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
            Assert.AreEqual(5, result.Hits.Count);
            Assert.IsTrue(result.Hits[0].Distance > 1.0);
            Assert.AreEqual(1.0, result.Hits[0].RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(1.0, result.Hits[0].RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("One", result.Hits[0].RawDocument["Name"].AsString);
            Assert.AreEqual("Museum", result.Hits[0].RawDocument["Type"].AsString);

            var place = result.Hits[1].Document;
            Assert.AreEqual(1.0, place.Location[0]);
            Assert.AreEqual(2.0, place.Location[1]);
            Assert.AreEqual("Two", place.Name);
            Assert.AreEqual("Coffee", place.Type);
        }

        [Test]
        public void TestGeoNearSphericalFalse() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var options = GeoNearOptions.SetSpherical(false);
            var result = collection.GeoNearAs<Place>(Query.Null, -74.0, 40.74, 100, options);
            Assert.IsTrue(result.Ok);
            Assert.AreEqual("onlinetests.testcollection", result.Namespace);
            Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
            Assert.IsTrue(result.Stats.BTreeLocations >= 0);
            Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
            Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
            Assert.IsTrue(result.Stats.NumberScanned >= 0);
            Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
            Assert.AreEqual(3, result.Hits.Count);

            var hit0 = result.Hits[0];
            Assert.IsTrue(hit0.Distance == 0.0);
            Assert.AreEqual(-74.0, hit0.RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit0.RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("10gen", hit0.RawDocument["Name"].AsString);
            Assert.AreEqual("Office", hit0.RawDocument["Type"].AsString);

            // with spherical false "Three" is slightly closer than "Two"
            var hit1 = result.Hits[1];
            Assert.IsTrue(hit1.Distance > 0.0);
            Assert.AreEqual(-74.0, hit1.RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(41.73, hit1.RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Three", hit1.RawDocument["Name"].AsString);
            Assert.AreEqual("Coffee", hit1.RawDocument["Type"].AsString);

            var hit2 = result.Hits[2];
            Assert.IsTrue(hit2.Distance > 0.0);
            Assert.IsTrue(hit2.Distance > hit1.Distance);
            Assert.AreEqual(-75.0, hit2.RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit2.RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Two", hit2.RawDocument["Name"].AsString);
            Assert.AreEqual("Coffee", hit2.RawDocument["Type"].AsString);
        }

        [Test]
        public void TestGeoNearSphericalTrue() {
            if (collection.Exists()) { collection.Drop(); }
            collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var options = GeoNearOptions.SetSpherical(true);
            var result = collection.GeoNearAs<Place>(Query.Null, -74.0, 40.74, 100, options);
            Assert.IsTrue(result.Ok);
            Assert.AreEqual("onlinetests.testcollection", result.Namespace);
            Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
            Assert.IsTrue(result.Stats.BTreeLocations >= 0);
            Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
            Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
            Assert.IsTrue(result.Stats.NumberScanned >= 0);
            Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
            Assert.AreEqual(3, result.Hits.Count);

            var hit0 = result.Hits[0];
            Assert.IsTrue(hit0.Distance == 0.0);
            Assert.AreEqual(-74.0, hit0.RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit0.RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("10gen", hit0.RawDocument["Name"].AsString);
            Assert.AreEqual("Office", hit0.RawDocument["Type"].AsString);

            // with spherical true "Two" is considerably closer than "Three"
            var hit1 = result.Hits[1];
            Assert.IsTrue(hit1.Distance > 0.0);
            Assert.AreEqual(-75.0, hit1.RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(40.74, hit1.RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Two", hit1.RawDocument["Name"].AsString);
            Assert.AreEqual("Coffee", hit1.RawDocument["Type"].AsString);

            var hit2 = result.Hits[2];
            Assert.IsTrue(hit2.Distance > 0.0);
            Assert.IsTrue(hit2.Distance > hit1.Distance);
            Assert.AreEqual(-74.0, hit2.RawDocument["Location"].AsBsonArray[0].AsDouble);
            Assert.AreEqual(41.73, hit2.RawDocument["Location"].AsBsonArray[1].AsDouble);
            Assert.AreEqual("Three", hit2.RawDocument["Name"].AsString);
            Assert.AreEqual("Coffee", hit2.RawDocument["Type"].AsString);
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
        public void TestGroupByFunction() {
            collection.RemoveAll();
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 1));
            collection.Insert(new BsonDocument("x", 2));
            collection.Insert(new BsonDocument("x", 3));
            collection.Insert(new BsonDocument("x", 3));
            collection.Insert(new BsonDocument("x", 3));
            var keyFunction = (BsonJavaScript) "function(doc) { return { x : doc.x }; }";
            var initial = new BsonDocument("count", 0);
            var reduce = (BsonJavaScript) "function(doc, prev) { prev.count += 1 }";
            var results = collection.Group(Query.Null, keyFunction, initial, reduce, null).ToArray();
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
                var options = MapReduceOptions.SetOutput("mrout");
                var result = collection.MapReduce(map, reduce, options);
                Assert.IsTrue(result.Ok);
                Assert.IsTrue(result.Duration >= TimeSpan.Zero);
                Assert.AreEqual(9, result.EmitCount);
                Assert.AreEqual(5, result.OutputCount);
                Assert.AreEqual(3, result.InputCount);
                Assert.IsNotNullOrEmpty(result.CollectionName);

                var expectedCounts = new Dictionary<string, int> {
                    { "A", 1 },
                    { "B", 3 },
                    { "C", 1 },
                    { "X", 1 },
                    { "_id", 3 }
                };
                foreach (var document in database[result.CollectionName].FindAll()) {
                    var key = document["_id"].AsString;
                    var count = document["value"].AsBsonDocument["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }
            }
        }

#pragma warning disable 649 // never assigned to
        private class TestInlineResultDocument {
            public string Id;
            [BsonElement("value")]
            public TestInlineResultValue Value;
        }

        private class TestInlineResultValue {
            [BsonElement("count")]
            public int Count;
        }
#pragma warning restore

        [Test]
        public void TestMapReduceInline() {
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
                var options = MapReduceOptions.SetOutput(MapReduceOutput.Inline);
                var result = collection.MapReduce(map, reduce, options);
                Assert.IsTrue(result.Ok);
                Assert.IsTrue(result.Duration >= TimeSpan.Zero);
                Assert.AreEqual(9, result.EmitCount);
                Assert.AreEqual(5, result.OutputCount);
                Assert.AreEqual(3, result.InputCount);
                Assert.IsNullOrEmpty(result.CollectionName);

                var expectedCounts = new Dictionary<string, int> {
                    { "A", 1 },
                    { "B", 3 },
                    { "C", 1 },
                    { "X", 1 },
                    { "_id", 3 }
                };

                // test InlineResults as BsonDocuments
                foreach (var document in result.InlineResults) {
                    var key = document["_id"].AsString;
                    var count = document["value"].AsBsonDocument["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test InlineResults as TestInlineResultDocument
                foreach (var document in result.GetInlineResultsAs<TestInlineResultDocument>()) {
                    var key = document.Id;
                    var count = document.Value.Count;
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
