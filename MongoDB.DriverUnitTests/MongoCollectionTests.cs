/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoCollectionTests
    {
        private class TestClass
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _server.Connect();
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        // TODO: more tests for MongoCollection

        [Test]
        public void TestAggregate()
        {
            if (_server.BuildInfo.Version >= new Version(2, 1, 0))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.Insert(new BsonDocument("x", 3));
                _collection.Insert(new BsonDocument("x", 3));

#pragma warning disable 618
                var commandResult = _collection.Aggregate(
                    new BsonDocument("$group", new BsonDocument { { "_id", "$x" }, { "count", new BsonDocument("$sum", 1) } })
                );
#pragma warning restore
                var dictionary = new Dictionary<int, int>();
                foreach (var result in commandResult.ResultDocuments)
                {
                    var x = result["_id"].AsInt32;
                    var count = result["count"].AsInt32;
                    dictionary[x] = count;
                }
                Assert.AreEqual(3, dictionary.Count);
                Assert.AreEqual(1, dictionary[1]);
                Assert.AreEqual(1, dictionary[2]);
                Assert.AreEqual(2, dictionary[3]);
            }
        }

        [Test]
        public void TestAggregateWithCursor()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 2))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.Insert(new BsonDocument("x", 3));
                _collection.Insert(new BsonDocument("x", 3));

                var args = new AggregateArgs
                {
                    Pipeline = new BsonDocument[]
                    {
                        new BsonDocument("$group", new BsonDocument { { "_id", "$x" }, { "count", new BsonDocument("$sum", 1) } })
                    },
                    OutputMode = AggregateOutputMode.Cursor,
                    BatchSize = 1
                };
                var query = _collection.Aggregate(args);
                var results = query.ToList();

                var dictionary = new Dictionary<int, int>();
                foreach (var result in results)
                {
                    var x = result["_id"].AsInt32;
                    var count = result["count"].AsInt32;
                    dictionary[x] = count;
                }
                Assert.AreEqual(3, dictionary.Count);
                Assert.AreEqual(1, dictionary[1]);
                Assert.AreEqual(1, dictionary[2]);
                Assert.AreEqual(2, dictionary[3]);
            }
        }

        [Test]
        public void TestAggregateWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));

                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new AggregateArgs
                    {
                        Pipeline = new BsonDocument[]
                        {
                            new BsonDocument("$match", Query.Exists("_id").ToBsonDocument())
                        },
                        MaxTime = TimeSpan.FromMilliseconds(1)
                    };
                    var query = _collection.Aggregate(args);
                    Assert.Throws<ExecutionTimeoutException>(() => query.ToList());
                }
            }
        }

        [Test]
        public void TestAggregateWithDollarOut()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 2))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.Insert(new BsonDocument("x", 3));
                _collection.Insert(new BsonDocument("x", 3));

                var args = new AggregateArgs
                {
                    Pipeline = new BsonDocument[]
                    {
                        new BsonDocument("$group", new BsonDocument { { "_id", "$x" }, { "count", new BsonDocument("$sum", 1) } }),
                        new BsonDocument("$out", "temp")
                    }
                };
                var query = _collection.Aggregate(args);
                var results = query.ToList();

                var dictionary = new Dictionary<int, int>();
                foreach (var result in results)
                {
                    var x = result["_id"].AsInt32;
                    var count = result["count"].AsInt32;
                    dictionary[x] = count;
                }
                Assert.AreEqual(3, dictionary.Count);
                Assert.AreEqual(1, dictionary[1]);
                Assert.AreEqual(1, dictionary[2]);
                Assert.AreEqual(2, dictionary[3]);
            }
        }

        [Test]
        public void TestConstructorArgumentChecking()
        {
            var settings = new MongoCollectionSettings();
            Assert.Throws<ArgumentNullException>(() => { new MongoCollection<BsonDocument>(null, "name", settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoCollection<BsonDocument>(_database, null, settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoCollection<BsonDocument>(_database, "name", null); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new MongoCollection<BsonDocument>(_database, "", settings); });
        }

        [Test]
        public void TestCountZero()
        {
            _collection.RemoveAll();
            var count = _collection.Count();
            Assert.AreEqual(0, count);
        }

        [Test]
        public void TestCountOne()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument());
            var count = _collection.Count();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestCountWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new CountArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.Count(args));
                }
            }
        }

        [Test]
        public void TestCountWithQuery()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            var query = Query.EQ("x", 1);
            var count = _collection.Count(query);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestCreateCollection()
        {
            var collection = Configuration.TestCollection;
            collection.Drop();
            Assert.IsFalse(collection.Exists());
            _database.CreateCollection(collection.Name);
            Assert.IsTrue(collection.Exists());
            collection.Drop();
        }

        [Test]
        public void TestCreateCollectionSetCappedSetMaxDocuments()
        {
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            Assert.IsFalse(collection.Exists());
            var options = CollectionOptions.SetCapped(true).SetMaxSize(10000000).SetMaxDocuments(1000);
            _database.CreateCollection(collection.Name, options);
            Assert.IsTrue(collection.Exists());
            var stats = collection.GetStats();
            Assert.IsTrue(stats.IsCapped);
            Assert.IsTrue(stats.StorageSize >= 10000000);
            Assert.IsTrue(stats.MaxDocuments == 1000);
            collection.Drop();
        }

        [Test]
        public void TestCreateCollectionSetCappedSetMaxSize()
        {
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            Assert.IsFalse(collection.Exists());
            var options = CollectionOptions.SetCapped(true).SetMaxSize(10000000);
            _database.CreateCollection(collection.Name, options);
            Assert.IsTrue(collection.Exists());
            var stats = collection.GetStats();
            Assert.IsTrue(stats.IsCapped);
            Assert.IsTrue(stats.StorageSize >= 10000000);
            collection.Drop();
        }

        [Test]
        public void TestEnsureIndex()
        {
            var expectedIndexVersion = (_server.BuildInfo.Version >= new Version(2, 0, 0)) ? 1 : 0;

            _collection.Insert(new BsonDocument("x", 1));
            _collection.DropAllIndexes(); // doesn't drop the index on _id

            var indexes = _collection.GetIndexes();
            Assert.AreEqual(1, indexes.Count);
            Assert.AreEqual(false, indexes[0].DroppedDups);
            Assert.AreEqual(false, indexes[0].IsBackground);
            Assert.AreEqual(false, indexes[0].IsSparse);
            Assert.AreEqual(false, indexes[0].IsUnique);
            Assert.AreEqual(new BsonDocument("_id", 1), indexes[0].Key);
            Assert.AreEqual("_id_", indexes[0].Name);
            Assert.AreEqual(_collection.FullName, indexes[0].Namespace);
            Assert.AreEqual(expectedIndexVersion, indexes[0].Version);

            _collection.DropAllIndexes();
            _collection.EnsureIndex("x");

            indexes = _collection.GetIndexes();
            Assert.AreEqual(2, indexes.Count);
            Assert.AreEqual(false, indexes[0].DroppedDups);
            Assert.AreEqual(false, indexes[0].IsBackground);
            Assert.AreEqual(false, indexes[0].IsSparse);
            Assert.AreEqual(false, indexes[0].IsUnique);
            Assert.AreEqual(new BsonDocument("_id", 1), indexes[0].Key);
            Assert.AreEqual("_id_", indexes[0].Name);
            Assert.AreEqual(_collection.FullName, indexes[0].Namespace);
            Assert.AreEqual(expectedIndexVersion, indexes[0].Version);
            Assert.AreEqual(false, indexes[1].DroppedDups);
            Assert.AreEqual(false, indexes[1].IsBackground);
            Assert.AreEqual(false, indexes[1].IsSparse);
            Assert.AreEqual(false, indexes[1].IsUnique);
            Assert.AreEqual(new BsonDocument("x", 1), indexes[1].Key);
            Assert.AreEqual("x_1", indexes[1].Name);
            Assert.AreEqual(_collection.FullName, indexes[1].Namespace);
            Assert.AreEqual(expectedIndexVersion, indexes[1].Version);

            _collection.DropAllIndexes();
            var options = IndexOptions.SetBackground(true).SetDropDups(true).SetSparse(true).SetUnique(true);
            _collection.EnsureIndex(IndexKeys.Ascending("x").Descending("y"), options);
            indexes = _collection.GetIndexes();
            Assert.AreEqual(2, indexes.Count);
            Assert.AreEqual(false, indexes[0].DroppedDups);
            Assert.AreEqual(false, indexes[0].IsBackground);
            Assert.AreEqual(false, indexes[0].IsSparse);
            Assert.AreEqual(false, indexes[0].IsUnique);
            Assert.AreEqual(new BsonDocument("_id", 1), indexes[0].Key);
            Assert.AreEqual("_id_", indexes[0].Name);
            Assert.AreEqual(_collection.FullName, indexes[0].Namespace);
            Assert.AreEqual(expectedIndexVersion, indexes[0].Version);
            Assert.AreEqual(true, indexes[1].DroppedDups);
            Assert.AreEqual(true, indexes[1].IsBackground);
            Assert.AreEqual(true, indexes[1].IsSparse);
            Assert.AreEqual(true, indexes[1].IsUnique);
            Assert.AreEqual(new BsonDocument { { "x", 1 }, { "y", -1 } }, indexes[1].Key);
            Assert.AreEqual("x_1_y_-1", indexes[1].Name);
            Assert.AreEqual(_collection.FullName, indexes[1].Namespace);
            Assert.AreEqual(expectedIndexVersion, indexes[1].Version);
        }

        [Test]
        public void TestDistinct()
        {
            _collection.RemoveAll();
            _collection.DropAllIndexes();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var values = new HashSet<BsonValue>(_collection.Distinct("x"));
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(true, values.Contains(1));
            Assert.AreEqual(true, values.Contains(2));
            Assert.AreEqual(true, values.Contains(3));
            Assert.AreEqual(false, values.Contains(4));
        }

        [Test]
        public void TestDistinct_Typed()
        {
            _collection.RemoveAll();
            _collection.DropAllIndexes();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var values = new HashSet<int>(_collection.Distinct<int>("x"));
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(true, values.Contains(1));
            Assert.AreEqual(true, values.Contains(2));
            Assert.AreEqual(true, values.Contains(3));
            Assert.AreEqual(false, values.Contains(4));
        }

        [Test]
        public void TestDistinctWithQuery()
        {
            _collection.RemoveAll();
            _collection.DropAllIndexes();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var query = Query.LTE("x", 2);
            var values = new HashSet<BsonValue>(_collection.Distinct("x", query));
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual(true, values.Contains(1));
            Assert.AreEqual(true, values.Contains(2));
            Assert.AreEqual(false, values.Contains(3));
            Assert.AreEqual(false, values.Contains(4));
        }

        [Test]
        public void TestDistinctWithQuery_Typed()
        {
            _collection.RemoveAll();
            _collection.DropAllIndexes();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var query = Query.LTE("x", 2);
            var values = new HashSet<int>(_collection.Distinct<int>("x", query));
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual(true, values.Contains(1));
            Assert.AreEqual(true, values.Contains(2));
            Assert.AreEqual(false, values.Contains(3));
            Assert.AreEqual(false, values.Contains(4));
        }

        [Test]
        public void TestDropAllIndexes()
        {
            _collection.DropAllIndexes();
        }

        [Test]
        public void TestDropIndex()
        {
            _collection.DropAllIndexes();
            Assert.AreEqual(1, _collection.GetIndexes().Count());
            Assert.Throws<MongoCommandException>(() => _collection.DropIndex("x"));

            _collection.EnsureIndex("x");
            Assert.AreEqual(2, _collection.GetIndexes().Count());
            _collection.DropIndex("x");
            Assert.AreEqual(1, _collection.GetIndexes().Count());
        }

        [Test]
        public void TestEnsureIndexTimeToLive()
        {
            if (_server.BuildInfo.Version >= new Version(2, 2))
            {
                _collection.DropAllIndexes();
                Assert.AreEqual(1, _collection.GetIndexes().Count());

                var keys = IndexKeys.Ascending("ts");
                var options = IndexOptions.SetTimeToLive(TimeSpan.FromHours(1));
                _collection.EnsureIndex(keys, options);

                var indexes = _collection.GetIndexes();
                Assert.AreEqual("_id_", indexes[0].Name);
                Assert.AreEqual("ts_1", indexes[1].Name);
                Assert.AreEqual(TimeSpan.FromHours(1), indexes[1].TimeToLive);
            }
        }

        [Test]
        public void TestExplain()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.Find(Query.GT("x", 3)).Explain();
        }

        [Test]
        public void TestFind()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.Find(Query.GT("x", 3));
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(4, result.Select(x => x["x"].AsInt32).FirstOrDefault());
        }

        [Test]
        public void TestFindAndModify()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "priority", 1 }, { "inprogress", false }, { "name", "abc" } });
            _collection.Insert(new BsonDocument { { "_id", 2 }, { "priority", 2 }, { "inprogress", false }, { "name", "def" } });


            var started = DateTime.UtcNow;
            started = started.AddTicks(-(started.Ticks % 10000)); // adjust for MongoDB DateTime precision
            var args = new FindAndModifyArgs
            {
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority"),
                Update = Update.Set("inprogress", true).Set("started", started),
                VersionReturned = FindAndModifyDocumentVersion.Original
            };
            var result = _collection.FindAndModify(args);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(2, result.ModifiedDocument["_id"].AsInt32);
            Assert.AreEqual(2, result.ModifiedDocument["priority"].AsInt32);
            Assert.AreEqual(false, result.ModifiedDocument["inprogress"].AsBoolean);
            Assert.AreEqual("def", result.ModifiedDocument["name"].AsString);
            Assert.IsFalse(result.ModifiedDocument.Contains("started"));

            started = DateTime.UtcNow;
            started = started.AddTicks(-(started.Ticks % 10000)); // adjust for MongoDB DateTime precision
            args = new FindAndModifyArgs
            {
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority"),
                Update = Update.Set("inprogress", true).Set("started", started),
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };
            result = _collection.FindAndModify(args);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1, result.ModifiedDocument["_id"].AsInt32);
            Assert.AreEqual(1, result.ModifiedDocument["priority"].AsInt32);
            Assert.AreEqual(true, result.ModifiedDocument["inprogress"].AsBoolean);
            Assert.AreEqual("abc", result.ModifiedDocument["name"].AsString);
            Assert.AreEqual(started, result.ModifiedDocument["started"].ToUniversalTime());
        }

        [Test]
        public void TestFindAndModifyWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new FindAndModifyArgs
                    {
                        Update = Update.Set("x", 1),
                        MaxTime = TimeSpan.FromMilliseconds(1)
                    };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.FindAndModify(args));
                }
            }
        }

        [Test]
        public void TestFindAndModifyNoMatchingDocument()
        {
            _collection.RemoveAll();

            var started = DateTime.UtcNow;
            started = started.AddTicks(-(started.Ticks % 10000)); // adjust for MongoDB DateTime precision
            var args = new FindAndModifyArgs
            {
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority"),
                Update = Update.Set("inprogress", true).Set("started", started),
                VersionReturned = FindAndModifyDocumentVersion.Original
            };
            var result = _collection.FindAndModify(args);

            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
            Assert.IsNull(result.ModifiedDocument);
            Assert.IsNull(result.GetModifiedDocumentAs<FindAndModifyClass>());
        }

        [Test]
        public void TestFindAndModifyUpsert()
        {
            _collection.RemoveAll();

            var args = new FindAndModifyArgs
            {
                Query = Query.EQ("name", "Tom"),
                Update = Update.Inc("count", 1),
                Upsert = true,
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };
            var result = _collection.FindAndModify(args);

            Assert.AreEqual("Tom", result.ModifiedDocument["name"].AsString);
            Assert.AreEqual(1, result.ModifiedDocument["count"].AsInt32);
        }

        private class FindAndModifyClass
        {
            public ObjectId Id;
            public int Value;
        }

        [Test]
        public void TestFindAndModifyTyped()
        {
            _collection.RemoveAll();
            var obj = new FindAndModifyClass { Id = ObjectId.GenerateNewId(), Value = 1 };
            _collection.Insert(obj);

            var args = new FindAndModifyArgs
            {
                Query = Query.EQ("_id", obj.Id),
                Update = Update.Inc("Value", 1),
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };
            var result = _collection.FindAndModify(args);
            var rehydrated = result.GetModifiedDocumentAs<FindAndModifyClass>();

            Assert.AreEqual(obj.Id, rehydrated.Id);
            Assert.AreEqual(2, rehydrated.Value);
        }

        [Test]
        public void TestFindAndRemove()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 1 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });

            var args = new FindAndRemoveArgs
            {
                Query = Query.EQ("x", 1),
                SortBy = SortBy.Ascending("y")
            };
            var result = _collection.FindAndRemove(args);
            Assert.AreEqual(1, result.ModifiedDocument["y"].ToInt32());
            Assert.AreEqual(1, _collection.Count());
        }

        [Test]
        public void TestFindAndRemoveNoMatchingDocument()
        {
            _collection.RemoveAll();

            var args = new FindAndRemoveArgs
            {
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority")
            };
            var result = _collection.FindAndRemove(args);

            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
            Assert.IsNull(result.ModifiedDocument);
            Assert.IsNull(result.GetModifiedDocumentAs<FindAndModifyClass>());
        }

        [Test]
        public void TestFindAndRemoveWithFields()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 1 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });

            var args = new FindAndRemoveArgs
            {
                Query = Query.EQ("x", 1),
                SortBy = SortBy.Ascending("y"),
                Fields = Fields.Include("_id")
            };
            var result = _collection.FindAndRemove(args);
            Assert.AreEqual(1, result.ModifiedDocument.ElementCount);
            Assert.AreEqual("_id", result.ModifiedDocument.GetElement(0).Name);
        }

        [Test]
        public void TestFindAndRemoveWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new FindAndRemoveArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.FindAndRemove(args));
                }
            }
        }

        [Test]
        public void TestFindNearSphericalFalse()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.Near("Location", -74.0, 40.74);
            var hits = _collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);

            var hit0 = hits[0];
            Assert.AreEqual(-74.0, hit0["Location"][0].AsDouble);
            Assert.AreEqual(40.74, hit0["Location"][1].AsDouble);
            Assert.AreEqual("10gen", hit0["Name"].AsString);
            Assert.AreEqual("Office", hit0["Type"].AsString);

            // with spherical false "Three" is slightly closer than "Two"
            var hit1 = hits[1];
            Assert.AreEqual(-74.0, hit1["Location"][0].AsDouble);
            Assert.AreEqual(41.73, hit1["Location"][1].AsDouble);
            Assert.AreEqual("Three", hit1["Name"].AsString);
            Assert.AreEqual("Coffee", hit1["Type"].AsString);

            var hit2 = hits[2];
            Assert.AreEqual(-75.0, hit2["Location"][0].AsDouble);
            Assert.AreEqual(40.74, hit2["Location"][1].AsDouble);
            Assert.AreEqual("Two", hit2["Name"].AsString);
            Assert.AreEqual("Coffee", hit2["Type"].AsString);

            query = Query.Near("Location", -74.0, 40.74, 0.5); // with maxDistance
            hits = _collection.Find(query).ToArray();
            Assert.AreEqual(1, hits.Length);

            hit0 = hits[0];
            Assert.AreEqual(-74.0, hit0["Location"][0].AsDouble);
            Assert.AreEqual(40.74, hit0["Location"][1].AsDouble);
            Assert.AreEqual("10gen", hit0["Name"].AsString);
            Assert.AreEqual("Office", hit0["Type"].AsString);

            query = Query.Near("Location", -174.0, 40.74, 0.5); // with no hits
            hits = _collection.Find(query).ToArray();
            Assert.AreEqual(0, hits.Length);
        }

        [Test]
        public void TestFindNearSphericalTrue()
        {
            if (_server.BuildInfo.Version >= new Version(1, 7, 0, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
                _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
                _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
                _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

                var query = Query.Near("Location", -74.0, 40.74, double.MaxValue, true); // spherical
                var hits = _collection.Find(query).ToArray();
                Assert.AreEqual(3, hits.Length);

                var hit0 = hits[0];
                Assert.AreEqual(-74.0, hit0["Location"][0].AsDouble);
                Assert.AreEqual(40.74, hit0["Location"][1].AsDouble);
                Assert.AreEqual("10gen", hit0["Name"].AsString);
                Assert.AreEqual("Office", hit0["Type"].AsString);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = hits[1];
                Assert.AreEqual(-75.0, hit1["Location"][0].AsDouble);
                Assert.AreEqual(40.74, hit1["Location"][1].AsDouble);
                Assert.AreEqual("Two", hit1["Name"].AsString);
                Assert.AreEqual("Coffee", hit1["Type"].AsString);

                var hit2 = hits[2];
                Assert.AreEqual(-74.0, hit2["Location"][0].AsDouble);
                Assert.AreEqual(41.73, hit2["Location"][1].AsDouble);
                Assert.AreEqual("Three", hit2["Name"].AsString);
                Assert.AreEqual("Coffee", hit2["Type"].AsString);

                query = Query.Near("Location", -74.0, 40.74, 0.5); // with maxDistance
                hits = _collection.Find(query).ToArray();
                Assert.AreEqual(1, hits.Length);

                hit0 = hits[0];
                Assert.AreEqual(-74.0, hit0["Location"][0].AsDouble);
                Assert.AreEqual(40.74, hit0["Location"][1].AsDouble);
                Assert.AreEqual("10gen", hit0["Name"].AsString);
                Assert.AreEqual("Office", hit0["Type"].AsString);

                query = Query.Near("Location", -174.0, 40.74, 0.5); // with no hits
                hits = _collection.Find(query).ToArray();
                Assert.AreEqual(0, hits.Length);
            }
        }

        [Test]
        public void TestFindOne()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.FindOne();
            Assert.AreEqual(1, result["x"].AsInt32);
            Assert.AreEqual(2, result["y"].AsInt32);
        }

        [Test]
        public void TestFindOneAs()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var result = (TestClass)_collection.FindOneAs(typeof(TestClass));
            Assert.AreEqual(1, result.X);
        }

        [Test]
        public void TestFindOneAsGeneric()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var result = _collection.FindOneAs<TestClass>();
            Assert.AreEqual(1, result.X);
        }

        [Test]
        public void TestFindOneAsGenericWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument { { "X", 1 } });

                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new FindOneArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.FindOneAs<TestClass>(args));
                }
            }
        }

        [Test]
        public void TestFindOneAsGenericWithSkipAndSortyBy()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 2 } });
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var sortBy = SortBy.Ascending("X");
            var args = new FindOneArgs { Skip = 1, SortBy = sortBy };
            var document = _collection.FindOneAs<TestClass>(args);
            Assert.AreEqual(2, document.X);
        }

        [Test]
        public void TestFindOneAsWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument { { "X", 1 } });

                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new FindOneArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.FindOneAs(typeof(TestClass), args));
                }
            }
        }

        [Test]
        public void TestFindOneAsWithSkipAndSortyBy()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 2 } });
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var sortBy = SortBy.Ascending("X");
            var args = new FindOneArgs { Skip = 1, SortBy = sortBy };
            var document = (TestClass)_collection.FindOneAs(typeof(TestClass), args);
            Assert.AreEqual(2, document.X);
        }

        [Test]
        public void TestFindOneById()
        {
            _collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            _collection.Insert(new BsonDocument { { "_id", id }, { "x", 1 }, { "y", 2 } });
            var result = _collection.FindOneById(id);
            Assert.AreEqual(1, result["x"].AsInt32);
            Assert.AreEqual(2, result["y"].AsInt32);
        }

        [Test]
        public void TestFindOneByIdAs()
        {
            _collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            _collection.Insert(new BsonDocument { { "_id", id }, { "X", 1 } });
            var result = (TestClass)_collection.FindOneByIdAs(typeof(TestClass), id);
            Assert.AreEqual(id, result.Id);
            Assert.AreEqual(1, result.X);
        }

        [Test]
        public void TestFindOneByIdAsGeneric()
        {
            _collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            _collection.Insert(new BsonDocument { { "_id", id }, { "X", 1 } });
            var result = _collection.FindOneByIdAs<TestClass>(id);
            Assert.AreEqual(id, result.Id);
            Assert.AreEqual(1, result.X);
        }

        [Test]
        public void TestFindWithinCircleSphericalFalse()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinCircle("Location", -74.0, 40.74, 1.0, false); // not spherical
            var hits = _collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);
            // note: the hits are unordered

            query = Query.WithinCircle("Location", -74.0, 40.74, 0.5, false); // smaller radius
            hits = _collection.Find(query).ToArray();
            Assert.AreEqual(1, hits.Length);

            query = Query.WithinCircle("Location", -174.0, 40.74, 1.0, false); // different part of the world
            hits = _collection.Find(query).ToArray();
            Assert.AreEqual(0, hits.Length);
        }

        [Test]
        public void TestFindWithinCircleSphericalTrue()
        {
            if (_server.BuildInfo.Version >= new Version(1, 7, 0, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
                _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
                _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
                _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

                var query = Query.WithinCircle("Location", -74.0, 40.74, 0.1, true); // spherical
                var hits = _collection.Find(query).ToArray();
                Assert.AreEqual(3, hits.Length);
                // note: the hits are unordered

                query = Query.WithinCircle("Location", -74.0, 40.74, 0.01, false); // smaller radius
                hits = _collection.Find(query).ToArray();
                Assert.AreEqual(1, hits.Length);

                query = Query.WithinCircle("Location", -174.0, 40.74, 0.1, false); // different part of the world
                hits = _collection.Find(query).ToArray();
                Assert.AreEqual(0, hits.Length);
            }
        }

        [Test]
        public void TestFindWithinRectangle()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinRectangle("Location", -75.0, 40, -73.0, 42.0);
            var hits = _collection.Find(query).ToArray();
            Assert.AreEqual(3, hits.Length);
            // note: the hits are unordered
        }

        [Test]
        public void TestFindWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 2))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new BsonDocument("x", 1));

                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var maxTime = TimeSpan.FromMilliseconds(1);
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.FindAll().SetMaxTime(maxTime).ToList());
                }
            }
        }

#pragma warning disable 649 // never assigned to
        private class Place
        {
            public ObjectId Id;
            public double[] Location;
            public string Name;
            public string Type;
        }
#pragma warning restore

        [Test]
        public void TestGeoHaystackSearch()
        {
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
                {
                    if (_collection.Exists()) { _collection.Drop(); }
                    _collection.Insert(new Place { Location = new[] { 34.2, 33.3 }, Type = "restaurant" });
                    _collection.Insert(new Place { Location = new[] { 34.2, 37.3 }, Type = "restaurant" });
                    _collection.Insert(new Place { Location = new[] { 59.1, 87.2 }, Type = "office" });
                    _collection.EnsureIndex(IndexKeys.GeoSpatialHaystack("Location", "Type"), IndexOptions.SetBucketSize(1));

                    var args = new GeoHaystackSearchArgs
                    {
                        Near = GeoNearPoint.From(33, 33),
                        AdditionalFieldName = "Type",
                        AdditionalFieldValue = "restaurant",
                        Limit = 30,
                        MaxDistance = 6
                    };
                    var result = _collection.GeoHaystackSearchAs<Place>(args);

                    Assert.IsTrue(result.Ok);
                    Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
                    Assert.AreEqual(2, result.Stats.BTreeMatches);
                    Assert.AreEqual(2, result.Stats.NumberOfHits);
                    Assert.AreEqual(34.2, result.Hits[0].Document.Location[0]);
                    Assert.AreEqual(33.3, result.Hits[0].Document.Location[1]);
                    Assert.AreEqual("restaurant", result.Hits[0].Document.Type);
                    Assert.AreEqual(34.2, result.Hits[1].Document.Location[0]);
                    Assert.AreEqual(37.3, result.Hits[1].Document.Location[1]);
                    Assert.AreEqual("restaurant", result.Hits[1].Document.Type);
                }
            }
        }

        [Test]
        public void TestGeoHaystackSearchWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                using (_database.RequestStart())
                {
                    var instance = _server.RequestConnection.ServerInstance;
                    if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
                    {
                        if (_collection.Exists()) { _collection.Drop(); }
                        _collection.Insert(new Place { Location = new[] { 34.2, 33.3 }, Type = "restaurant" });
                        _collection.Insert(new Place { Location = new[] { 34.2, 37.3 }, Type = "restaurant" });
                        _collection.Insert(new Place { Location = new[] { 59.1, 87.2 }, Type = "office" });
                        _collection.EnsureIndex(IndexKeys.GeoSpatialHaystack("Location", "Type"), IndexOptions.SetBucketSize(1));

                        using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                        {
                            failpoint.Times(1);
                            var args = new GeoHaystackSearchArgs
                            {
                                Near = GeoNearPoint.From(33, 33),
                                AdditionalFieldName = "Type",
                                AdditionalFieldValue = "restaurant",
                                Limit = 30,
                                MaxDistance = 6,
                                MaxTime = TimeSpan.FromMilliseconds(1)
                            };
                            Assert.Throws<ExecutionTimeoutException>(() => _collection.GeoHaystackSearchAs<Place>(args));
                        }
                    }
                }
            }
        }

        [Test]
        public void TestGeoHaystackSearch_Typed()
        {
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
                {
                    if (_collection.Exists()) { _collection.Drop(); }
                    _collection.Insert(new Place { Location = new[] { 34.2, 33.3 }, Type = "restaurant" });
                    _collection.Insert(new Place { Location = new[] { 34.2, 37.3 }, Type = "restaurant" });
                    _collection.Insert(new Place { Location = new[] { 59.1, 87.2 }, Type = "office" });
                    _collection.EnsureIndex(IndexKeys<Place>.GeoSpatialHaystack(x => x.Location, x => x.Type), IndexOptions.SetBucketSize(1));

                    var args = new GeoHaystackSearchArgs
                    {
                        Near = GeoNearPoint.From(33, 33),
                        Limit = 30,
                        MaxDistance = 6
                    }
                    .SetAdditionalField<Place, string>(x => x.Type, "restaurant");
                    var result = _collection.GeoHaystackSearchAs<Place>(args);

                    Assert.IsTrue(result.Ok);
                    Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
                    Assert.AreEqual(2, result.Stats.BTreeMatches);
                    Assert.AreEqual(2, result.Stats.NumberOfHits);
                    Assert.AreEqual(34.2, result.Hits[0].Document.Location[0]);
                    Assert.AreEqual(33.3, result.Hits[0].Document.Location[1]);
                    Assert.AreEqual("restaurant", result.Hits[0].Document.Type);
                    Assert.AreEqual(34.2, result.Hits[1].Document.Location[0]);
                    Assert.AreEqual(37.3, result.Hits[1].Document.Location[1]);
                    Assert.AreEqual("restaurant", result.Hits[1].Document.Type);
                }
            }
        }

        [Test]
        public void TestGeoNear()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { 1.0, 1.0 }, Name = "One", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 2.0 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { 1.0, 3.0 }, Name = "Three", Type = "Library" });
            _collection.Insert(new Place { Location = new[] { 1.0, 4.0 }, Name = "Four", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 5.0 }, Name = "Five", Type = "Coffee" });
            _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

            var args = new GeoNearArgs
            {
                Near = GeoNearPoint.From(0, 0),
                Limit = 100,
                DistanceMultiplier = 1,
                MaxDistance = 100
            };
            var result = _collection.GeoNearAs(typeof(Place), args);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(_collection.FullName, result.Namespace);
            Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
            Assert.IsTrue(result.Stats.BTreeLocations >= 0);
            Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
            Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
            Assert.IsTrue(result.Stats.NumberScanned >= 0);
            Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
            Assert.AreEqual(5, result.Hits.Count);
            Assert.IsTrue(result.Hits[0].Distance > 1.0);
            Assert.AreEqual(1.0, result.Hits[0].RawDocument["Location"][0].AsDouble);
            Assert.AreEqual(1.0, result.Hits[0].RawDocument["Location"][1].AsDouble);
            Assert.AreEqual("One", result.Hits[0].RawDocument["Name"].AsString);
            Assert.AreEqual("Museum", result.Hits[0].RawDocument["Type"].AsString);

            var place = (Place)result.Hits[1].Document;
            Assert.AreEqual(1.0, place.Location[0]);
            Assert.AreEqual(2.0, place.Location[1]);
            Assert.AreEqual("Two", place.Name);
            Assert.AreEqual("Coffee", place.Type);
        }

        [Test]
        public void TestGeoNearGeneric()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { 1.0, 1.0 }, Name = "One", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 2.0 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { 1.0, 3.0 }, Name = "Three", Type = "Library" });
            _collection.Insert(new Place { Location = new[] { 1.0, 4.0 }, Name = "Four", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 5.0 }, Name = "Five", Type = "Coffee" });
            _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

            var args = new GeoNearArgs
            {
                Near = GeoNearPoint.From(0, 0),
                Limit = 100,
                DistanceMultiplier = 1,
                MaxDistance = 100
            };
            var result = _collection.GeoNearAs<Place>(args);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(_collection.FullName, result.Namespace);
            Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
            Assert.IsTrue(result.Stats.BTreeLocations >= 0);
            Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
            Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
            Assert.IsTrue(result.Stats.NumberScanned >= 0);
            Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
            Assert.AreEqual(5, result.Hits.Count);
            Assert.IsTrue(result.Hits[0].Distance > 1.0);
            Assert.AreEqual(1.0, result.Hits[0].RawDocument["Location"][0].AsDouble);
            Assert.AreEqual(1.0, result.Hits[0].RawDocument["Location"][1].AsDouble);
            Assert.AreEqual("One", result.Hits[0].RawDocument["Name"].AsString);
            Assert.AreEqual("Museum", result.Hits[0].RawDocument["Type"].AsString);

            var place = result.Hits[1].Document;
            Assert.AreEqual(1.0, place.Location[0]);
            Assert.AreEqual(2.0, place.Location[1]);
            Assert.AreEqual("Two", place.Name);
            Assert.AreEqual("Coffee", place.Type);
        }

        [Test]
        public void TestGeoNearSphericalFalse()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

            var args = new GeoNearArgs
            {
                Near = GeoNearPoint.From(-74.0, 40.74),
                Limit = 100,
                Spherical = false
            };
            var result = _collection.GeoNearAs<Place>(args);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(_collection.FullName, result.Namespace);
            Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
            Assert.IsTrue(result.Stats.BTreeLocations >= 0);
            Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
            Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
            Assert.IsTrue(result.Stats.NumberScanned >= 0);
            Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
            Assert.AreEqual(3, result.Hits.Count);

            var hit0 = result.Hits[0];
            Assert.IsTrue(hit0.Distance == 0.0);
            Assert.AreEqual(-74.0, hit0.RawDocument["Location"][0].AsDouble);
            Assert.AreEqual(40.74, hit0.RawDocument["Location"][1].AsDouble);
            Assert.AreEqual("10gen", hit0.RawDocument["Name"].AsString);
            Assert.AreEqual("Office", hit0.RawDocument["Type"].AsString);

            // with spherical false "Three" is slightly closer than "Two"
            var hit1 = result.Hits[1];
            Assert.IsTrue(hit1.Distance > 0.0);
            Assert.AreEqual(-74.0, hit1.RawDocument["Location"][0].AsDouble);
            Assert.AreEqual(41.73, hit1.RawDocument["Location"][1].AsDouble);
            Assert.AreEqual("Three", hit1.RawDocument["Name"].AsString);
            Assert.AreEqual("Coffee", hit1.RawDocument["Type"].AsString);

            var hit2 = result.Hits[2];
            Assert.IsTrue(hit2.Distance > 0.0);
            Assert.IsTrue(hit2.Distance > hit1.Distance);
            Assert.AreEqual(-75.0, hit2.RawDocument["Location"][0].AsDouble);
            Assert.AreEqual(40.74, hit2.RawDocument["Location"][1].AsDouble);
            Assert.AreEqual("Two", hit2.RawDocument["Name"].AsString);
            Assert.AreEqual("Coffee", hit2.RawDocument["Type"].AsString);
        }

        [Test]
        public void TestGeoNearSphericalTrue()
        {
            if (_server.BuildInfo.Version >= new Version(1, 7, 0, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
                _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
                _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
                _collection.EnsureIndex(IndexKeys.GeoSpatial("Location"));

                var args = new GeoNearArgs
                {
                    Near = GeoNearPoint.From(-74.0, 40.74),
                    Limit = 100,
                    Spherical = true
                };
                var result = _collection.GeoNearAs<Place>(args);

                Assert.IsTrue(result.Ok);
                Assert.AreEqual(_collection.FullName, result.Namespace);
                Assert.IsTrue(result.Stats.AverageDistance >= 0.0);
                Assert.IsTrue(result.Stats.BTreeLocations >= 0);
                Assert.IsTrue(result.Stats.Duration >= TimeSpan.Zero);
                Assert.IsTrue(result.Stats.MaxDistance >= 0.0);
                Assert.IsTrue(result.Stats.NumberScanned >= 0);
                Assert.IsTrue(result.Stats.ObjectsLoaded >= 0);
                Assert.AreEqual(3, result.Hits.Count);

                var hit0 = result.Hits[0];
                Assert.IsTrue(hit0.Distance == 0.0);
                Assert.AreEqual(-74.0, hit0.RawDocument["Location"][0].AsDouble);
                Assert.AreEqual(40.74, hit0.RawDocument["Location"][1].AsDouble);
                Assert.AreEqual("10gen", hit0.RawDocument["Name"].AsString);
                Assert.AreEqual("Office", hit0.RawDocument["Type"].AsString);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = result.Hits[1];
                Assert.IsTrue(hit1.Distance > 0.0);
                Assert.AreEqual(-75.0, hit1.RawDocument["Location"][0].AsDouble);
                Assert.AreEqual(40.74, hit1.RawDocument["Location"][1].AsDouble);
                Assert.AreEqual("Two", hit1.RawDocument["Name"].AsString);
                Assert.AreEqual("Coffee", hit1.RawDocument["Type"].AsString);

                var hit2 = result.Hits[2];
                Assert.IsTrue(hit2.Distance > 0.0);
                Assert.IsTrue(hit2.Distance > hit1.Distance);
                Assert.AreEqual(-74.0, hit2.RawDocument["Location"][0].AsDouble);
                Assert.AreEqual(41.73, hit2.RawDocument["Location"][1].AsDouble);
                Assert.AreEqual("Three", hit2.RawDocument["Name"].AsString);
                Assert.AreEqual("Coffee", hit2.RawDocument["Type"].AsString);
            }
        }

        [Test]
        public void TestGeoNearWithGeoJsonPoints()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)), Name = "10gen", Type = "Office" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 41.73)), Name = "Three", Type = "Coffee" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-75.0, 40.74)), Name = "Two", Type = "Coffee" });
                _collection.EnsureIndex(IndexKeys.GeoSpatialSpherical("Location"));

                var args = new GeoNearArgs
                {
                    Near = GeoNearPoint.From(GeoJson.Point(GeoJson.Geographic(-74.0, 40.74))),
                    Spherical = true
                };
                var result = _collection.GeoNearAs<PlaceGeoJson>(args);
                var hits = result.Hits;

                var hit0 = hits[0].Document;
                Assert.AreEqual(-74.0, hit0.Location.Coordinates.Longitude);
                Assert.AreEqual(40.74, hit0.Location.Coordinates.Latitude);
                Assert.AreEqual("10gen", hit0.Name);
                Assert.AreEqual("Office", hit0.Type);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = hits[1].Document;
                Assert.AreEqual(-75.0, hit1.Location.Coordinates.Longitude);
                Assert.AreEqual(40.74, hit1.Location.Coordinates.Latitude);
                Assert.AreEqual("Two", hit1.Name);
                Assert.AreEqual("Coffee", hit1.Type);

                var hit2 = hits[2].Document;
                Assert.AreEqual(-74.0, hit2.Location.Coordinates.Longitude);
                Assert.AreEqual(41.73, hit2.Location.Coordinates.Latitude);
                Assert.AreEqual("Three", hit2.Name);
                Assert.AreEqual("Coffee", hit2.Type);
            }
        }

        [Test]
        public void TestGeoNearWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new BsonDocument("loc", new BsonArray { 0, 0 }));
                _collection.EnsureIndex(IndexKeys.GeoSpatial("loc"));

                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new GeoNearArgs
                    {
                        Near = GeoNearPoint.From(0, 0),
                        MaxTime = TimeSpan.FromMilliseconds(1)
                    };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.GeoNearAs<BsonDocument>(args));
                }
            }
        }

        private class PlaceGeoJson
        {
            public ObjectId Id { get; set; }
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
        }

        [Test]
        public void TestGeoSphericalIndex()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)), Name = "10gen" , Type = "Office" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 41.73)), Name = "Three" , Type = "Coffee" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-75.0, 40.74)), Name = "Two"   , Type = "Coffee" });
                _collection.EnsureIndex(IndexKeys.GeoSpatialSpherical("Location"));

                // TODO: add Query builder support for 2dsphere queries
                var query = Query<PlaceGeoJson>.Near(x => x.Location, GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)));

                var cursor = _collection.FindAs<PlaceGeoJson>(query);
                var hits = cursor.ToArray();

                var hit0 = hits[0];
                Assert.AreEqual(-74.0, hit0.Location.Coordinates.Longitude);
                Assert.AreEqual(40.74, hit0.Location.Coordinates.Latitude);
                Assert.AreEqual("10gen", hit0.Name);
                Assert.AreEqual("Office", hit0.Type);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = hits[1];
                Assert.AreEqual(-75.0, hit1.Location.Coordinates.Longitude);
                Assert.AreEqual(40.74, hit1.Location.Coordinates.Latitude);
                Assert.AreEqual("Two", hit1.Name);
                Assert.AreEqual("Coffee", hit1.Type);

                var hit2 = hits[2];
                Assert.AreEqual(-74.0, hit2.Location.Coordinates.Longitude);
                Assert.AreEqual(41.73, hit2.Location.Coordinates.Latitude);
                Assert.AreEqual("Three", hit2.Name);
                Assert.AreEqual("Coffee", hit2.Type);
            }
        }

        [Test]
        public void TestGetIndexes()
        {
            _collection.DropAllIndexes();
            var indexes = _collection.GetIndexes();
            Assert.AreEqual(1, indexes.Count);
            Assert.AreEqual("_id_", indexes[0].Name);
            // see additional tests in TestEnsureIndex
        }

        [Test]
        public void TestGetMore()
        {
            using (_server.RequestStart(_database))
            {
                _collection.RemoveAll();
                var count = _server.Primary.MaxMessageLength / 1000000;
                for (int i = 0; i < count; i++)
                {
                    var document = new BsonDocument("data", new BsonBinaryData(new byte[1000000]));
                    _collection.Insert(document);
                }
                var list = _collection.FindAll().ToList();
            }
        }

        [Test]
        public void TestGroup()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var initial = new BsonDocument("count", 0);
            var reduce = "function(doc, prev) { prev.count += 1 }";
            var results = _collection.Group(Query.Null, "x", initial, reduce, null).ToArray();
            Assert.AreEqual(3, results.Length);
            Assert.AreEqual(1, results[0]["x"].ToInt32());
            Assert.AreEqual(2, results[0]["count"].ToInt32());
            Assert.AreEqual(2, results[1]["x"].ToInt32());
            Assert.AreEqual(1, results[1]["count"].ToInt32());
            Assert.AreEqual(3, results[2]["x"].ToInt32());
            Assert.AreEqual(3, results[2]["count"].ToInt32());
        }

        [Test]
        public void TestGroupByFunction()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var keyFunction = (BsonJavaScript)"function(doc) { return { x : doc.x }; }";
            var initial = new BsonDocument("count", 0);
            var reduce = (BsonJavaScript)"function(doc, prev) { prev.count += 1 }";
            var results = _collection.Group(Query.Null, keyFunction, initial, reduce, null).ToArray();
            Assert.AreEqual(3, results.Length);
            Assert.AreEqual(1, results[0]["x"].ToInt32());
            Assert.AreEqual(2, results[0]["count"].ToInt32());
            Assert.AreEqual(2, results[1]["x"].ToInt32());
            Assert.AreEqual(1, results[1]["count"].ToInt32());
            Assert.AreEqual(3, results[2]["x"].ToInt32());
            Assert.AreEqual(3, results[2]["count"].ToInt32());
        }

        [Test]
        public void TestHashedIndex()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new BsonDocument { { "x", "abc" } });
                _collection.Insert(new BsonDocument { { "x", "def" } });
                _collection.Insert(new BsonDocument { { "x", "ghi" } });
                _collection.EnsureIndex(IndexKeys.Hashed("x"));

                var query = Query.EQ("x", "abc");
                var cursor = _collection.FindAs<BsonDocument>(query);
                var documents = cursor.ToArray();

                Assert.AreEqual(1, documents.Length);
                Assert.AreEqual("abc", documents[0]["x"].AsString);

                var explain = cursor.Explain();
                Assert.AreEqual("BtreeCursor x_hashed", explain["cursor"].AsString);
            }
        }

        [Test]
        public void TestIndexExists()
        {
            _collection.DropAllIndexes();
            Assert.AreEqual(false, _collection.IndexExists("x"));

            _collection.EnsureIndex("x");
            Assert.AreEqual(true, _collection.IndexExists("x"));

            _collection.EnsureIndex(IndexKeys.Ascending("y"));
            Assert.AreEqual(true, _collection.IndexExists(IndexKeys.Ascending("y")));
        }

        [Test]
        public void TestInsertBatchContinueOnError()
        {
            var collection = Configuration.TestCollection;
            collection.Drop();
            collection.EnsureIndex(IndexKeys.Ascending("x"), IndexOptions.SetUnique(true));

            var batch = new BsonDocument[]
            {
                new BsonDocument("x", 1),
                new BsonDocument("x", 1), // duplicate
                new BsonDocument("x", 2),
                new BsonDocument("x", 2), // duplicate
                new BsonDocument("x", 3),
                new BsonDocument("x", 3) // duplicate
            };

            // try the batch without ContinueOnError
            try
            {
                collection.InsertBatch(batch);
            }
            catch (WriteConcernException)
            {
                Assert.AreEqual(1, collection.Count());
                Assert.AreEqual(1, collection.FindOne()["x"].AsInt32);
            }

            // try the batch again with ContinueOnError
            if (_server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                try
                {
                    var options = new MongoInsertOptions { Flags = InsertFlags.ContinueOnError };
                    collection.InsertBatch(batch, options);
                }
                catch (WriteConcernException)
                {
                    Assert.AreEqual(3, collection.Count());
                }
            }
        }

        [Test]
        public void TestInsertBatchMultipleBatchesWriteConcernDisabledContinueOnErrorFalse()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Unacknowledged };
            var collection = Configuration.TestDatabase.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            using (Configuration.TestDatabase.RequestStart())
            {
                var maxMessageLength = Configuration.TestServer.RequestConnection.ServerInstance.MaxMessageLength;

                var filler = new string('x', maxMessageLength / 3); // after overhead results in two documents per sub-batch
                var documents = new BsonDocument[]
                {
                    // first sub-batch
                    new BsonDocument { { "_id", 1 }, { "filler", filler } },
                    new BsonDocument { { "_id", 2 }, { "filler", filler } },
                    // second sub-batch
                    new BsonDocument { { "_id", 3 }, { "filler", filler } },
                    new BsonDocument { { "_id", 3 }, { "filler", filler } }, // duplicate _id error
                    // third sub-batch
                    new BsonDocument { { "_id", 4 }, { "filler", filler } },
                    new BsonDocument { { "_id", 5 }, { "filler", filler } },
                };

                var options = new MongoInsertOptions { Flags = InsertFlags.None }; // no ContinueOnError
                var result = collection.InsertBatch(documents, options);

                Assert.AreEqual(null, result);
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 1)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 2)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 3)));
                Assert.AreEqual(0, collection.Count(Query.EQ("_id", 4)));
                Assert.AreEqual(0, collection.Count(Query.EQ("_id", 5)));
            }
        }

        [Test]
        public void TestInsertBatchMultipleBatchesWriteConcernDisabledContinueOnErrorTrue()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Unacknowledged };
            var collection = Configuration.TestDatabase.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            using (Configuration.TestDatabase.RequestStart())
            {
                var maxMessageLength = Configuration.TestServer.RequestConnection.ServerInstance.MaxMessageLength;

                var filler = new string('x', maxMessageLength / 3); // after overhead results in two documents per sub-batch
                var documents = new BsonDocument[]
                {
                    // first sub-batch
                    new BsonDocument { { "_id", 1 }, { "filler", filler } },
                    new BsonDocument { { "_id", 2 }, { "filler", filler } },
                    // second sub-batch
                    new BsonDocument { { "_id", 3 }, { "filler", filler } },
                    new BsonDocument { { "_id", 3 }, { "filler", filler } }, // duplicate _id error
                    // third sub-batch
                    new BsonDocument { { "_id", 4 }, { "filler", filler } },
                    new BsonDocument { { "_id", 5 }, { "filler", filler } },
                };

                var options = new MongoInsertOptions { Flags = InsertFlags.ContinueOnError };
                var result = collection.InsertBatch(documents, options);

                Assert.AreEqual(null, result);
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 1)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 2)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 3)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 4)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 5)));
            }
        }

        [Test]
        public void TestInsertBatchMultipleBatchesWriteConcernEnabledContinueOnErrorFalse()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Acknowledged };
            var collection = Configuration.TestDatabase.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            using (Configuration.TestDatabase.RequestStart())
            {
                var maxMessageLength = Configuration.TestServer.RequestConnection.ServerInstance.MaxMessageLength;

                var filler = new string('x', maxMessageLength / 3); // after overhead results in two documents per sub-batch
                var documents = new BsonDocument[]
                {
                    // first sub-batch
                    new BsonDocument { { "_id", 1 }, { "filler", filler } },
                    new BsonDocument { { "_id", 2 }, { "filler", filler } },
                    // second sub-batch
                    new BsonDocument { { "_id", 3 }, { "filler", filler } },
                    new BsonDocument { { "_id", 3 }, { "filler", filler } }, // duplicate _id error
                    // third sub-batch
                    new BsonDocument { { "_id", 4 }, { "filler", filler } },
                    new BsonDocument { { "_id", 5 }, { "filler", filler } },
                };

                try
                {
                    var options = new MongoInsertOptions { Flags = InsertFlags.None }; // no ContinueOnError
                    collection.InsertBatch(documents, options);
                }
                catch (WriteConcernException ex)
                {
                    var results = (IEnumerable<WriteConcernResult>)ex.Data["results"];
                    Assert.AreEqual(2, results.Count());

                    var result1 = results.ElementAt(0);
                    Assert.AreEqual(false, result1.HasLastErrorMessage);

                    var result2 = results.ElementAt(1);
                    Assert.AreEqual(true, result2.HasLastErrorMessage);
                }

                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 1)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 2)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 3)));
                Assert.AreEqual(0, collection.Count(Query.EQ("_id", 4)));
                Assert.AreEqual(0, collection.Count(Query.EQ("_id", 5)));
            }
        }

        [Test]
        public void TestInsertBatchMultipleBatchesWriteConcernEnabledContinueOnErrorTrue()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Acknowledged };
            var collection = Configuration.TestDatabase.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            using (Configuration.TestDatabase.RequestStart())
            {
                var maxMessageLength = Configuration.TestServer.RequestConnection.ServerInstance.MaxMessageLength;

                var filler = new string('x', maxMessageLength / 3); // after overhead results in two documents per sub-batch
                var documents = new BsonDocument[]
                {
                    // first sub-batch
                    new BsonDocument { { "_id", 1 }, { "filler", filler } },
                    new BsonDocument { { "_id", 2 }, { "filler", filler } },
                    // second sub-batch
                    new BsonDocument { { "_id", 3 }, { "filler", filler } },
                    new BsonDocument { { "_id", 3 }, { "filler", filler } }, // duplicate _id error
                    // third sub-batch
                    new BsonDocument { { "_id", 4 }, { "filler", filler } },
                    new BsonDocument { { "_id", 5 }, { "filler", filler } },
                };

                try
                {
                    var options = new MongoInsertOptions { Flags = InsertFlags.ContinueOnError };
                    collection.InsertBatch(documents, options);
                }
                catch (WriteConcernException ex)
                {
                    var results = (IEnumerable<WriteConcernResult>)ex.Data["results"];
                    Assert.AreEqual(3, results.Count());

                    var result1 = results.ElementAt(0);
                    Assert.AreEqual(false, result1.HasLastErrorMessage);

                    var result2 = results.ElementAt(1);
                    Assert.AreEqual(true, result2.HasLastErrorMessage);

                    var result3 = results.ElementAt(2);
                    Assert.AreEqual(false, result3.HasLastErrorMessage);
                }

                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 1)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 2)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 3)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 4)));
                Assert.AreEqual(1, collection.Count(Query.EQ("_id", 5)));
            }
        }

        [Test]
        public void TestInsertBatchSmallFinalSubbatch()
        {
            var collectionName = Configuration.TestCollection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Unacknowledged };
            var collection = Configuration.TestDatabase.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            using (Configuration.TestDatabase.RequestStart())
            {
                var maxMessageLength = Configuration.TestServer.RequestConnection.ServerInstance.MaxMessageLength;
                var documentCount = maxMessageLength / (1024 * 1024) + 1; // 1 document will overflow to second sub batch

                var documents = new BsonDocument[documentCount];
                for (var i = 0; i < documentCount; i++)
                {
                    var document = new BsonDocument
                    {
                        { "_id", i },
                        { "filler", new string('x', 1024 * 1024) }
                    };
                    documents[i] = document;
                }

                collection.InsertBatch(documents);

                Assert.AreEqual(documentCount, collection.Count());
            }
        }

        [Test]
        public void TestIsCappedFalse()
        {
            var collection = _database.GetCollection("notcappedcollection");
            collection.Drop();
            _database.CreateCollection("notcappedcollection");

            Assert.AreEqual(true, collection.Exists());
            Assert.AreEqual(false, collection.IsCapped());
        }

        [Test]
        public void TestIsCappedTrue()
        {
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            var options = CollectionOptions.SetCapped(true).SetMaxSize(100000);
            _database.CreateCollection("cappedcollection", options);

            Assert.AreEqual(true, collection.Exists());
            Assert.AreEqual(true, collection.IsCapped());
        }

        [Test]
        public void TestLenientRead()
        {
            var settings = new MongoCollectionSettings { ReadEncoding = new UTF8Encoding(false, false) };
            var collection = _database.GetCollection(Configuration.TestCollection.Name, settings);

            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "x", "abc" } };
            var bson = document.ToBson();
            bson[28] = 0xc0; // replace 'a' with invalid lone first code point (not followed by 10xxxxxx)

            // use a RawBsonDocument to sneak the invalid bytes into the database
            var rawBsonDocument = new RawBsonDocument(bson);
            collection.Insert(rawBsonDocument);

            var rehydrated = collection.FindOne(Query.EQ("_id", document["_id"]));
            Assert.AreEqual("\ufffd" + "bc", rehydrated["x"].AsString);
        }

        [Test]
        public void TestLenientWrite()
        {
            var settings = new MongoCollectionSettings { WriteEncoding = new UTF8Encoding(false, false) };
            var collection = _database.GetCollection(Configuration.TestCollection.Name, settings);

            var document = new BsonDocument("x", "\udc00"); // invalid lone low surrogate
            collection.Save(document);

            var rehydrated = collection.FindOne(Query.EQ("_id", document["_id"]));
            Assert.AreEqual("\ufffd", rehydrated["x"].AsString);
        }

#pragma warning disable 649 // never assigned to
        private class TestMapReduceDocument
        {
            public string Id;
            [BsonElement("value")]
            public TestMapReduceValue Value;
        }

        private class TestMapReduceValue
        {
            [BsonElement("count")]
            public int Count;
        }
#pragma warning restore

        [Test]
        public void TestMapReduce()
        {
            // this is Example 1 on p. 87 of MongoDB: The Definitive Guide
            // by Kristina Chodorow and Michael Dirolf

            _collection.Drop();
            _collection.Insert(new BsonDocument { { "A", 1 }, { "B", 2 } });
            _collection.Insert(new BsonDocument { { "B", 1 }, { "C", 2 } });
            _collection.Insert(new BsonDocument { { "X", 1 }, { "B", 2 } });

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


            var result = _collection.MapReduce(new MapReduceArgs
            {
                MapFunction = map,
                ReduceFunction = reduce,
                OutputMode = MapReduceOutputMode.Replace,
                OutputCollectionName = "mrout"
            });

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(result.Duration >= TimeSpan.Zero);
            Assert.AreEqual(9, result.EmitCount);
            Assert.AreEqual(5, result.OutputCount);
            Assert.AreEqual(3, result.InputCount);
            Assert.IsNotNullOrEmpty(result.CollectionName);

            var expectedCounts = new Dictionary<string, int>
            {
                { "A", 1 },
                { "B", 3 },
                { "C", 1 },
                { "X", 1 },
                { "_id", 3 }
            };

            // read output collection ourselves
            foreach (var document in _database.GetCollection(result.CollectionName).FindAll())
            {
                var key = document["_id"].AsString;
                var count = document["value"]["count"].ToInt32();
                Assert.AreEqual(expectedCounts[key], count);
            }

            // test GetResults
            foreach (var document in result.GetResults())
            {
                var key = document["_id"].AsString;
                var count = document["value"]["count"].ToInt32();
                Assert.AreEqual(expectedCounts[key], count);
            }

            // test GetResultsAs<>
            foreach (var document in result.GetResultsAs<TestMapReduceDocument>())
            {
                Assert.AreEqual(expectedCounts[document.Id], document.Value.Count);
            }
        }

        [Test]
        public void TestMapReduceInline()
        {
            // this is Example 1 on p. 87 of MongoDB: The Definitive Guide
            // by Kristina Chodorow and Michael Dirolf

            if (_server.BuildInfo.Version >= new Version(1, 7, 4, 0))
            {
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument { { "A", 1 }, { "B", 2 } });
                _collection.Insert(new BsonDocument { { "B", 1 }, { "C", 2 } });
                _collection.Insert(new BsonDocument { { "X", 1 }, { "B", 2 } });

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

                var result = _collection.MapReduce(new MapReduceArgs
                {
                    MapFunction = map,
                    ReduceFunction = reduce
                });

                Assert.IsTrue(result.Ok);
                Assert.IsTrue(result.Duration >= TimeSpan.Zero);
                Assert.AreEqual(9, result.EmitCount);
                Assert.AreEqual(5, result.OutputCount);
                Assert.AreEqual(3, result.InputCount);
                Assert.IsNullOrEmpty(result.CollectionName);

                var expectedCounts = new Dictionary<string, int>
                {
                    { "A", 1 },
                    { "B", 3 },
                    { "C", 1 },
                    { "X", 1 },
                    { "_id", 3 }
                };

                // test InlineResults as BsonDocuments
                foreach (var document in result.InlineResults)
                {
                    var key = document["_id"].AsString;
                    var count = document["value"]["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test InlineResults as TestInlineResultDocument
                foreach (var document in result.GetInlineResultsAs<TestMapReduceDocument>())
                {
                    var key = document.Id;
                    var count = document.Value.Count;
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test GetResults
                foreach (var document in result.GetResults())
                {
                    var key = document["_id"].AsString;
                    var count = document["value"]["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test GetResultsAs<>
                foreach (var document in result.GetResultsAs<TestMapReduceDocument>())
                {
                    Assert.AreEqual(expectedCounts[document.Id], document.Value.Count);
                }
            }
        }

        [Test]
        public void TestMapReduceInlineWithMaxTime()
        {
            if (_server.BuildInfo.Version >= new Version(2, 5, 3))
            {
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument("x", 1)); // make sure collection has at least one document so map gets called

                using (var failpoint = new FailPoint(FailPoint.MaxTimeAlwaysTimeout, _server))
                {
                    failpoint.Times(1);
                    var args = new MapReduceArgs
                    {
                        MapFunction = "function() { }",
                        ReduceFunction = "function(key, value) { return 0; }",
                        MaxTime = TimeSpan.FromMilliseconds(1)
                    };
                    Assert.Throws<ExecutionTimeoutException>(() => _collection.MapReduce(args));
                }
            }
        }

        [Test]
        public void TestMapReduceInlineWithQuery()
        {
            // this is Example 1 on p. 87 of MongoDB: The Definitive Guide
            // by Kristina Chodorow and Michael Dirolf

            if (_server.BuildInfo.Version >= new Version(1, 7, 4, 0))
            {
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument { { "A", 1 }, { "B", 2 } });
                _collection.Insert(new BsonDocument { { "B", 1 }, { "C", 2 } });
                _collection.Insert(new BsonDocument { { "X", 1 }, { "B", 2 } });

                var query = Query.Exists("B");

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

                var result = _collection.MapReduce(new MapReduceArgs
                {
                    Query = query,
                    MapFunction = map,
                    ReduceFunction = reduce
                });

                Assert.IsTrue(result.Ok);
                Assert.IsTrue(result.Duration >= TimeSpan.Zero);
                Assert.AreEqual(9, result.EmitCount);
                Assert.AreEqual(5, result.OutputCount);
                Assert.AreEqual(3, result.InputCount);
                Assert.IsNullOrEmpty(result.CollectionName);

                var expectedCounts = new Dictionary<string, int>
                {
                    { "A", 1 },
                    { "B", 3 },
                    { "C", 1 },
                    { "X", 1 },
                    { "_id", 3 }
                };

                // test InlineResults as BsonDocuments
                foreach (var document in result.InlineResults)
                {
                    var key = document["_id"].AsString;
                    var count = document["value"]["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test InlineResults as TestInlineResultDocument
                foreach (var document in result.GetInlineResultsAs<TestMapReduceDocument>())
                {
                    var key = document.Id;
                    var count = document.Value.Count;
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test GetResults
                foreach (var document in result.GetResults())
                {
                    var key = document["_id"].AsString;
                    var count = document["value"]["count"].ToInt32();
                    Assert.AreEqual(expectedCounts[key], count);
                }

                // test GetResultsAs<>
                foreach (var document in result.GetResultsAs<TestMapReduceDocument>())
                {
                    Assert.AreEqual(expectedCounts[document.Id], document.Value.Count);
                }
            }
        }

        [Test]
        public void TestReIndex()
        {
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
                {
                    _collection.RemoveAll();
                    _collection.Insert(new BsonDocument("x", 1));
                    _collection.Insert(new BsonDocument("x", 2));
                    _collection.DropAllIndexes();
                    _collection.EnsureIndex("x");
                    // note: prior to 1.8.1 the reIndex command was returning duplicate ok elements
                    try
                    {
                        var result = _collection.ReIndex();
                        Assert.AreEqual(2, result.Response["nIndexes"].ToInt32());
                        Assert.AreEqual(2, result.Response["nIndexesWas"].ToInt32());
                    }
                    catch (InvalidOperationException ex)
                    {
                        Assert.AreEqual("Duplicate element name 'ok'.", ex.Message);
                    }
                }
            }
        }

        [Test]
        public void TestSetFields()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.FindAll().SetFields("x").FirstOrDefault();
            Assert.AreEqual(2, result.ElementCount);
            Assert.AreEqual("_id", result.GetElement(0).Name);
            Assert.AreEqual("x", result.GetElement(1).Name);
        }

        [Test]
        public void TestSetHint()
        {
            _collection.DropAllIndexes();
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            _collection.EnsureIndex(IndexKeys.Ascending("x"));
            var query = Query.EQ("x", 1);
            var cursor = _collection.Find(query).SetHint(new BsonDocument("x", 1));
            var count = 0;
            foreach (var document in cursor)
            {
                Assert.AreEqual(1, ++count);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestSetHintByIndexName()
        {
            _collection.DropAllIndexes();
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            _collection.EnsureIndex(IndexKeys.Ascending("x"), IndexOptions.SetName("xIndex"));
            var query = Query.EQ("x", 1);
            var cursor = _collection.Find(query).SetHint("xIndex");
            var count = 0;
            foreach (var document in cursor)
            {
                Assert.AreEqual(1, ++count);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestSortAndLimit()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.FindAll().SetSortOrder("x").SetLimit(3).Select(x => x["x"].AsInt32);
            Assert.AreEqual(3, result.Count());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
        }

        [Test]
        public void TestGetStats()
        {
            var stats = _collection.GetStats();
        }

        [Test]
        public void TestGetStatsUsePowerOf2Sizes()
        {
            if (_server.BuildInfo.Version >= new Version(2, 2))
            {
                _collection.Drop();
                _database.CreateCollection(_collection.Name); // collMod command only works if collection exists

                var command = new CommandDocument
                {
                    { "collMod", _collection.Name },
                    { "usePowerOf2Sizes", true }
                };
                _database.RunCommand(command);

                var stats = _collection.GetStats();
                Assert.IsTrue((stats.UserFlags & CollectionUserFlags.UsePowerOf2Sizes) != 0);
            }
        }

        [Test]
        public void TestStrictRead()
        {
            var settings = new MongoCollectionSettings { ReadEncoding = new UTF8Encoding(false, true) };
            var collection = _database.GetCollection(Configuration.TestCollection.Name, settings);

            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "x", "abc" } };
            var bson = document.ToBson();
            bson[28] = 0xc0; // replace 'a' with invalid lone first code point (not followed by 10xxxxxx)

            // use a RawBsonDocument to sneak the invalid bytes into the database
            var rawBsonDocument = new RawBsonDocument(bson);
            collection.Insert(rawBsonDocument);

            Assert.Throws<DecoderFallbackException>(() => { var rehydrated = collection.FindOne(Query.EQ("_id", document["_id"])); });
        }

        [Test]
        public void TestStrictWrite()
        {
            var settings = new MongoCollectionSettings { WriteEncoding = new UTF8Encoding(false, true) };
            var collection = _database.GetCollection(Configuration.TestCollection.Name, settings);

            var document = new BsonDocument("x", "\udc00"); // invalid lone low surrogate
            Assert.Throws<EncoderFallbackException>(() => { collection.Insert(document); });
        }

        [Test]
        public void TestTextIndex()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0, 0))
            {
                var enableTextSearchCommand = new CommandDocument
                {
                    { "setParameter", 1 },
                    { "textSearchEnabled", true }
                };
                var adminDatabase = _server.GetDatabase("admin");
                adminDatabase.RunCommand(enableTextSearchCommand);

                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new BsonDocument("x", "The quick brown fox"));
                _collection.Insert(new BsonDocument("x", "jumped over the fence"));
                _collection.EnsureIndex(new IndexKeysDocument("x", "text"));

                var textSearchCommand = new CommandDocument
                {
                    { "text", _collection.Name },
                    { "search", "fox" }
                };
                var commandResult = _database.RunCommand(textSearchCommand);
                var response = commandResult.Response;

                Assert.AreEqual(1, response["stats"]["nfound"].ToInt32());
                Assert.AreEqual("The quick brown fox", response["results"][0]["obj"]["x"].AsString);
            }
        }

        [Test]
        public void TestTotalDataSize()
        {
            var dataSize = _collection.GetTotalDataSize();
        }

        [Test]
        public void TestTotalStorageSize()
        {
            var dataSize = _collection.GetTotalStorageSize();
        }

        [Test]
        public void TestUpdate()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Update(Query.EQ("x", 1), Update.Set("x", 2));
            var document = _collection.FindOne();
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestUpdateEmptyQueryDocument()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Update(new QueryDocument(), Update.Set("x", 2));
            var document = _collection.FindOne();
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestUpdateNullQuery()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Update(Query.Null, Update.Set("x", 2));
            var document = _collection.FindOne();
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestValidate()
        {
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
                {
                    // ensure collection exists
                    _collection.RemoveAll();
                    _collection.Insert(new BsonDocument("x", 1));

                    var result = _collection.Validate();
                    var ns = result.Namespace;
                    var firstExtent = result.FirstExtent;
                    var lastExtent = result.LastExtent;
                    var extentCount = result.ExtentCount;
                    var dataSize = result.DataSize;
                    var nrecords = result.RecordCount;
                    var lastExtentSize = result.LastExtentSize;
                    var padding = result.Padding;
                    var firstExtentDetails = result.FirstExtentDetails;
                    var loc = firstExtentDetails.Loc;
                    var xnext = firstExtentDetails.XNext;
                    var xprev = firstExtentDetails.XPrev;
                    var nsdiag = firstExtentDetails.NSDiag;
                    var size = firstExtentDetails.Size;
                    var firstRecord = firstExtentDetails.FirstRecord;
                    var lastRecord = firstExtentDetails.LastRecord;
                    var deletedCount = result.DeletedCount;
                    var deletedSize = result.DeletedSize;
                    var nindexes = result.IndexCount;
                    var keysPerIndex = result.KeysPerIndex;
                    var valid = result.IsValid;
                    var errors = result.Errors;
                    var warning = result.Warning;
                }
            }
        }
    }
}
