/* Copyright 2010-2016 MongoDB Inc.
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
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core;
using MongoDB.Driver.GeoJsonObjectModel;
using FluentAssertions;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests
{
    public class MongoCollectionTests
    {
        private class TestClass
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
        }

        private MongoServer _server;
        private MongoServerInstance _primary;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        public MongoCollectionTests()
        {
            _server = LegacyTestConfiguration.Server;
            _primary = _server.Instances.First(x => x.IsPrimary);
            _database = LegacyTestConfiguration.Database;
            _collection = LegacyTestConfiguration.Collection;
        }

        // TODO: more tests for MongoCollection

        [Fact]
        public void TestAggregate()
        {
            if (_server.BuildInfo.Version >= new Version(2, 2, 0))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.Insert(new BsonDocument("x", 3));
                _collection.Insert(new BsonDocument("x", 3));

                var pipeline = new[]
                {
                    new BsonDocument("$group", new BsonDocument { { "_id", "$x" }, { "count", new BsonDocument("$sum", 1) } })
                };
                var expectedResult = new[]
                {
                    new BsonDocument { { "_id", 1 }, { "count", 1 }},
                    new BsonDocument { { "_id", 2 }, { "count", 1 }},
                    new BsonDocument { { "_id", 3 }, { "count", 2 }},
                };

                var result = _collection.Aggregate(new AggregateArgs { Pipeline = pipeline });

                result.Should().BeEquivalentTo(expectedResult);
            }
        }

        [Fact]
        public void TestAggregateAllowDiskUsage()
        {
            if (_primary.Supports(FeatureId.AggregateAllowDiskUse))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();

                var query = _collection.Aggregate(new AggregateArgs
                {
                    Pipeline = new BsonDocument[]
                    {
                        new BsonDocument("$project", new BsonDocument("x", 1))
                    },
                    AllowDiskUse = true
                });
                var results = query.ToList(); // all we can test is that the server doesn't reject the allowDiskUsage argument

                Assert.Equal(0, results.Count);
            }
        }

        [Fact]
        public void TestAggregateCursor()
        {
            if (_primary.Supports(FeatureId.AggregateCursor))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.Insert(new BsonDocument("x", 3));
                _collection.Insert(new BsonDocument("x", 3));

                var query = _collection.Aggregate(new AggregateArgs
                {
                    Pipeline = new BsonDocument[]
                    {
                        new BsonDocument("$group", new BsonDocument { { "_id", "$x" }, { "count", new BsonDocument("$sum", 1) } })
                    },
                    OutputMode = AggregateOutputMode.Cursor,
                    BatchSize = 1
                });
                var results = query.ToList();

                var dictionary = new Dictionary<int, int>();
                foreach (var result in results)
                {
                    var x = result["_id"].AsInt32;
                    var count = result["count"].AsInt32;
                    dictionary[x] = count;
                }
                Assert.Equal(3, dictionary.Count);
                Assert.Equal(1, dictionary[1]);
                Assert.Equal(1, dictionary[2]);
                Assert.Equal(2, dictionary[3]);
            }
        }

        [Fact]
        public void TestAggregateExplain()
        {
            if (_primary.Supports(FeatureId.AggregateExplain))
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("x", 1));

                var result = _collection.AggregateExplain(new AggregateArgs
                {
                    Pipeline = new BsonDocument[]
                    {
                        new BsonDocument("$project", new BsonDocument("x", "$x"))
                    }
                });

                Assert.True(result.Response.Contains("stages"));
            }
        }

        [Fact]
        public void TestAggregateMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.RemoveAll();
                        _collection.DropAllIndexes();
                        _collection.Insert(new BsonDocument("x", 1));

                        failpoint.SetAlwaysOn();
                        var query = _collection.Aggregate(new AggregateArgs
                        {
                            Pipeline = new BsonDocument[]
                            {
                                new BsonDocument("$match", Query.Exists("_id").ToBsonDocument())
                            },
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        });
                        Assert.Throws<MongoExecutionTimeoutException>(() => query.ToList());
                    }
                }
            }
        }

        [Fact]
        public void TestAggregateOutputToCollection()
        {
            if (_primary.Supports(FeatureId.AggregateOutputToCollection))
            {
                _collection.RemoveAll();
                _collection.DropAllIndexes();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.Insert(new BsonDocument("x", 3));
                _collection.Insert(new BsonDocument("x", 3));

                var query = _collection.Aggregate(new AggregateArgs
                {
                    BypassDocumentValidation = true,
                    Pipeline = new BsonDocument[]
                    {
                        new BsonDocument("$group", new BsonDocument { { "_id", "$x" }, { "count", new BsonDocument("$sum", 1) } }),
                        new BsonDocument("$out", "temp")
                    }
                });
                var results = query.ToList();

                var dictionary = new Dictionary<int, int>();
                foreach (var result in results)
                {
                    var x = result["_id"].AsInt32;
                    var count = result["count"].AsInt32;
                    dictionary[x] = count;
                }
                Assert.Equal(3, dictionary.Count);
                Assert.Equal(1, dictionary[1]);
                Assert.Equal(1, dictionary[2]);
                Assert.Equal(2, dictionary[3]);
            }
        }

        [SkippableFact]
        public void TestAggregateWriteConcern()
        {
            RequireServer.Check().Supports(Feature.AggregateOut, Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var writeConcern = new WriteConcern(9);
            var args = new AggregateArgs
            {
                Pipeline = new[] { BsonDocument.Parse("{ $out : 'out' }") }
            };

            var exception = Record.Exception(() => _collection.WithWriteConcern(writeConcern).Aggregate(args));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestBulkDelete()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 3)).RemoveOne();
            bulk.Execute();

            Assert.Equal(1, _collection.Count());
            Assert.Equal(2, _collection.FindOne()["x"].ToInt32());
        }

        [Fact]
        public void TestBulkInsert()
        {
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Insert(new BsonDocument("x", 2));
            bulk.Insert(new BsonDocument("x", 3));
            bulk.Execute();

            Assert.Equal(3, _collection.Count());
        }

        [Fact]
        public void TestBulkUpdate()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.GT("x", 0)).Update(Update.Set("z", 1));
            bulk.Find(Query.EQ("x", 3)).UpdateOne(Update.Set("z", 3));
            bulk.Find(Query.EQ("x", 4)).Upsert().UpdateOne(Update.Set("z", 4));
            bulk.Execute();

            Assert.Equal(4, _collection.Count());
            foreach (var document in _collection.FindAll())
            {
                var x = document["x"].ToInt32();
                var z = document["z"].ToInt32();
                var expected = (x == 2) ? 1 : x;
                Assert.Equal(expected, z);
            }
        }

        [Fact]
        public void TestBulkWrite()
        {
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Insert(new BsonDocument("x", 2));
            bulk.Insert(new BsonDocument("x", 3));
            bulk.Insert(new BsonDocument("x", 4));
            bulk.Find(Query.GT("x", 2)).Update(Update.Inc("x", 10));
            bulk.Find(Query.EQ("x", 13)).RemoveOne();
            bulk.Find(Query.EQ("x", 14)).RemoveOne();
            bulk.Execute();

            Assert.Equal(2, _collection.Count());
        }

        [Fact]
        public void TestBulkWriteCounts()
        {
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Find(Query.EQ("x", 1)).UpdateOne(Update.Set("x", 2));
            bulk.Find(Query.EQ("x", 2)).RemoveOne();
            var result = bulk.Execute();

            Assert.Equal(1, result.DeletedCount);
            Assert.Equal(1, result.InsertedCount);
            if (_primary.Supports(FeatureId.WriteCommands))
            {
                Assert.Equal(true, result.IsModifiedCountAvailable);
                Assert.Equal(1, result.ModifiedCount);
            }
            else
            {
                Assert.Equal(false, result.IsModifiedCountAvailable);
                Assert.Throws<NotSupportedException>(() => { var _ = result.ModifiedCount; });
            }
            Assert.Equal(3, result.RequestCount);
            Assert.Equal(1, result.MatchedCount);
        }

        [Fact]
        public void TestBulkWriteCountsWithUpsert()
        {
            _collection.Drop();
            var id = new BsonObjectId(ObjectId.GenerateNewId());

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.EQ("_id", id)).Upsert().UpdateOne(Update.Set("x", 2));
            bulk.Find(Query.EQ("_id", id)).Upsert().UpdateOne(Update.Set("x", 2));
            bulk.Find(Query.EQ("_id", id)).UpdateOne(Update.Set("x", 3));
            var result = bulk.Execute();

            Assert.Equal(0, result.DeletedCount);
            Assert.Equal(0, result.InsertedCount);
            if (_primary.Supports(FeatureId.WriteCommands))
            {
                Assert.Equal(true, result.IsModifiedCountAvailable);
                Assert.Equal(1, result.ModifiedCount);
            }
            else
            {
                Assert.Equal(false, result.IsModifiedCountAvailable);
                Assert.Throws<NotSupportedException>(() => { var _ = result.ModifiedCount; });
            }
            Assert.Equal(3, result.RequestCount);
            Assert.Equal(2, result.MatchedCount);
            Assert.Equal(1, result.Upserts.Count);
            Assert.Equal(0, result.Upserts.First().Index);
            Assert.Equal(id, result.Upserts.First().Id);
        }

        [Fact]
        public void TestBulkWriteOrdered()
        {
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Execute();

            Assert.Equal(1, _collection.Count());
        }

        [Fact]
        public void TestBulkWriteUnordered()
        {
            _collection.Drop();

            var bulk = _collection.InitializeUnorderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Execute();

            Assert.Equal(0, _collection.Count());
        }

        [Fact]
        public void TestConstructorArgumentChecking()
        {
            var settings = new MongoCollectionSettings();
            Assert.Throws<ArgumentNullException>(() => { new MongoCollection<BsonDocument>(null, "name", settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoCollection<BsonDocument>(_database, null, settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoCollection<BsonDocument>(_database, "name", null); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new MongoCollection<BsonDocument>(_database, "", settings); });
        }

        [Fact]
        public void TestCountZero()
        {
            _collection.RemoveAll();
            var count = _collection.Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void TestCountOne()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument());
            var count = _collection.Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void TestCountWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        failpoint.SetAlwaysOn();
                        var args = new CountArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.Count(args));
                    }
                }
            }
        }

        [SkippableFact]
        public void TestCountWithMaxTimeFromFind()
        {
            RequireServer.Check();
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        failpoint.SetAlwaysOn();
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.Find(Query.EQ("x", 1)).SetMaxTime(TimeSpan.FromMilliseconds(1)).Count());
                    }
                }
            }
        }

        [Fact]
        public void TestCountWithQuery()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            var query = Query.EQ("x", 1);
            var count = _collection.Count(query);
            Assert.Equal(1, count);
        }

        [SkippableFact]
        public void TestCountWithReadPreferenceFromFind()
        {
            RequireServer.Check();
            _collection.Drop();
            var all = LegacyTestConfiguration.Server.Secondaries.Length + 1;
            var options = new MongoInsertOptions { WriteConcern = new WriteConcern(w: all) };
            _collection.Insert(new BsonDocument("x", 1), options);
            _collection.Insert(new BsonDocument("x", 2), options);
            var count = _collection.Find(Query.EQ("x", 1)).SetReadPreference(ReadPreference.Secondary).Count();
            Assert.Equal(1, count);
        }

        [SkippableFact]
        public void TestCountWithHint()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.CreateIndex(IndexKeys.Ascending("x"));
            var query = Query.EQ("x", 1);
            var count = _collection.Count(new CountArgs
            {
                Hint = new BsonDocument("x", 1),
                Query = query
            });
            Assert.Equal(1, count);
        }

        [SkippableFact]
        public void TestCountWithHintFromFind()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.CreateIndex(IndexKeys.Ascending("x"));
            var count = _collection.Find(Query.EQ("x", 1)).SetHint(new BsonDocument("x", 1)).Count();
            Assert.Equal(1, count);
        }

        [SkippableFact]
        public void TestCountWithHintAndLimitFromFind()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.CreateIndex(IndexKeys.Ascending("x"));
            var count = _collection.Find(Query.EQ("x", 1)).SetHint(new BsonDocument("x", 1)).SetLimit(2).Size();
            Assert.Equal(1, count);
        }

        [Fact]
        public void TestCreateCollection()
        {
            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            Assert.False(collection.Exists());
            _database.CreateCollection(collection.Name);
            Assert.True(collection.Exists());
            collection.Drop();
        }

        [Theory]
        [ParameterAttributeData]
        public void TestCreateCollectionSetAutoIndexId(
            [Values(false, true)]
            bool autoIndexId)
        {
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            var options = CollectionOptions.SetAutoIndexId(autoIndexId);
            var expectedIndexCount = autoIndexId ? 1 : 0;

            _database.CreateCollection(collection.Name, options);

            var indexCount = collection.GetIndexes().Count;
            Assert.Equal(expectedIndexCount, indexCount);
        }

        [SkippableFact]
        public void TestCreateCollectionSetCappedSetMaxDocuments()
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            Assert.False(collection.Exists());
            var options = CollectionOptions.SetCapped(true).SetMaxSize(10000).SetMaxDocuments(1000);
            _database.CreateCollection(collection.Name, options);
            Assert.True(collection.Exists());
            var stats = collection.GetStats();
            Assert.True(stats.IsCapped);
            Assert.True(stats.StorageSize >= 10000);
            Assert.True(stats.MaxDocuments == 1000);
            collection.Drop();
        }

        [SkippableFact]
        public void TestCreateCollectionSetCappedSetMaxSize()
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            Assert.False(collection.Exists());
            var options = CollectionOptions.SetCapped(true).SetMaxSize(10000);
            _database.CreateCollection(collection.Name, options);
            Assert.True(collection.Exists());
            var stats = collection.GetStats();
            Assert.True(stats.IsCapped);
            Assert.True(stats.StorageSize >= 10000);
            collection.Drop();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void TestCreateCollectionSetNoPadding(
            [Values(false, true)]
            bool noPadding)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.0").ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            var userFlags = noPadding ? CollectionUserFlags.NoPadding : CollectionUserFlags.None;
            var options = new CollectionOptionsDocument
            {
                { "flags", (int)userFlags }
            };

            _database.CreateCollection(collection.Name, options);

            var stats = collection.GetStats();
            Assert.Equal(userFlags, stats.UserFlags);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void TestCreateCollectionSetUsePowerOf2Sizes(
            [Values(false, true)]
            bool usePowerOf2Sizes)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            var userFlags = usePowerOf2Sizes ? CollectionUserFlags.UsePowerOf2Sizes : CollectionUserFlags.None;
            var options = new CollectionOptionsDocument
            {
                { "flags", (int)userFlags }
            };

            _database.CreateCollection(collection.Name, options);

            var stats = collection.GetStats();
            Assert.Equal(userFlags, stats.UserFlags);
        }

        [Fact]
        public void TestCreateIndex()
        {
            _collection.Insert(new BsonDocument("x", 1));
            _collection.DropAllIndexes(); // doesn't drop the index on _id

            var indexes = _collection.GetIndexes().ToList();
            Assert.Equal(1, indexes.Count);
            Assert.Equal(false, indexes[0].DroppedDups);
            Assert.Equal(false, indexes[0].IsBackground);
            Assert.Equal(false, indexes[0].IsSparse);
            Assert.Equal(false, indexes[0].IsUnique);
            Assert.Equal(new IndexKeysDocument("_id", 1), indexes[0].Key);
            Assert.Equal("_id_", indexes[0].Name);
            Assert.Equal(_collection.FullName, indexes[0].Namespace);
            Assert.True(indexes[0].Version >= 0);

            _collection.DropAllIndexes();
            var result = _collection.CreateIndex("x");

            var expectedResult = new ExpectedWriteConcernResult();
            CheckExpectedResult(expectedResult, result);

            indexes = _collection.GetIndexes().OrderBy(x => x.Name).ToList();
            Assert.Equal(2, indexes.Count);
            Assert.Equal(false, indexes[0].DroppedDups);
            Assert.Equal(false, indexes[0].IsBackground);
            Assert.Equal(false, indexes[0].IsSparse);
            Assert.Equal(false, indexes[0].IsUnique);
            Assert.Equal(new IndexKeysDocument("_id", 1), indexes[0].Key);
            Assert.Equal("_id_", indexes[0].Name);
            Assert.Equal(_collection.FullName, indexes[0].Namespace);
            Assert.True(indexes[0].Version >= 0);
            Assert.Equal(false, indexes[1].DroppedDups);
            Assert.Equal(false, indexes[1].IsBackground);
            Assert.Equal(false, indexes[1].IsSparse);
            Assert.Equal(false, indexes[1].IsUnique);
            Assert.Equal(new IndexKeysDocument("x", 1), indexes[1].Key);
            Assert.Equal("x_1", indexes[1].Name);
            Assert.Equal(_collection.FullName, indexes[1].Namespace);
            Assert.True(indexes[1].Version >= 0);

            // note: DropDups is silently ignored in server 2.8
            if (_primary.BuildInfo.Version < new Version(2, 7, 0))
            {
                _collection.DropAllIndexes();
                var options = IndexOptions.SetBackground(true).SetDropDups(true).SetSparse(true).SetUnique(true);
                result = _collection.CreateIndex(IndexKeys.Ascending("x").Descending("y"), options);

                expectedResult = new ExpectedWriteConcernResult();
                CheckExpectedResult(expectedResult, result);

                indexes = _collection.GetIndexes().OrderBy(x => x.Name).ToList();
                Assert.Equal(2, indexes.Count);
                Assert.Equal(false, indexes[0].DroppedDups);
                Assert.Equal(false, indexes[0].IsBackground);
                Assert.Equal(false, indexes[0].IsSparse);
                Assert.Equal(false, indexes[0].IsUnique);
                Assert.Equal(new IndexKeysDocument("_id", 1), indexes[0].Key);
                Assert.Equal("_id_", indexes[0].Name);
                Assert.Equal(_collection.FullName, indexes[0].Namespace);
                Assert.True(indexes[0].Version >= 0);
                Assert.Equal(true, indexes[1].DroppedDups);
                Assert.Equal(true, indexes[1].IsBackground);
                Assert.Equal(true, indexes[1].IsSparse);
                Assert.Equal(true, indexes[1].IsUnique);
                Assert.Equal(new IndexKeysDocument { { "x", 1 }, { "y", -1 } }, indexes[1].Key);
                Assert.Equal("x_1_y_-1", indexes[1].Name);
                Assert.Equal(_collection.FullName, indexes[1].Namespace);
                Assert.True(indexes[1].Version >= 0);
            }
        }

        [SkippableFact]
        public void TestCreateIndexWithStorageEngine()
        {
            RequireServer.Check().StorageEngine("wiredTiger");
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.DropAllIndexes(); // doesn't drop the index on _id

            _collection.CreateIndex(
               IndexKeys.Ascending("x"),
                IndexOptions.SetStorageEngineOptions(
                    new BsonDocument("wiredTiger", new BsonDocument("configString", "block_compressor=zlib"))));

            var result = _collection.GetIndexes();
            Assert.Equal(2, result.Count);
        }

        [SkippableFact]
        public void TestCreateIndexWriteConcern()
        {
            RequireServer.Check().Supports(Feature.AggregateOut, Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var writeConcern = new WriteConcern(9);
            var keys = IndexKeys.Ascending("x");

            var exception = Record.Exception(() => _collection.WithWriteConcern(writeConcern).CreateIndex(keys));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestCreateIndexWithPartialFilterExpression()
        {
            _collection.Drop();
            var keys = IndexKeys.Ascending("x");
            var options = IndexOptions<BsonDocument>.SetPartialFilterExpression(Query.GT("x", 0));

            _collection.CreateIndex(keys, options);

            var indexes = _collection.GetIndexes();
            var index = indexes.Where(i => i.Name == "x_1").Single();
            Assert.Equal(BsonDocument.Parse("{ x : { $gt : 0 } }"), index.RawDocument["partialFilterExpression"]);
        }

        [Fact]
        public void TestDistinct()
        {
            _collection.RemoveAll();
            _collection.DropAllIndexes();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var values = new HashSet<BsonValue>(_collection.Distinct("x"));
            Assert.Equal(3, values.Count);
            Assert.Equal(true, values.Contains(1));
            Assert.Equal(true, values.Contains(2));
            Assert.Equal(true, values.Contains(3));
            Assert.Equal(false, values.Contains(4));
        }

        [Fact]
        public void TestDistinct_Typed()
        {
            _collection.RemoveAll();
            _collection.DropAllIndexes();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            var values = new HashSet<int>(_collection.Distinct<int>("x"));
            Assert.Equal(3, values.Count);
            Assert.Equal(true, values.Contains(1));
            Assert.Equal(true, values.Contains(2));
            Assert.Equal(true, values.Contains(3));
            Assert.Equal(false, values.Contains(4));
        }

        [Fact]
        public void TestDistinctWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.Drop();
                        _collection.Insert(new BsonDocument("x", 1)); // ensure collection is not empty

                        failpoint.SetAlwaysOn();
                        var args = new DistinctArgs
                        {
                            Key = "x",
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.Distinct<BsonValue>(args));
                    }
                }
            }
        }

        [Fact]
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
            Assert.Equal(2, values.Count);
            Assert.Equal(true, values.Contains(1));
            Assert.Equal(true, values.Contains(2));
            Assert.Equal(false, values.Contains(3));
            Assert.Equal(false, values.Contains(4));
        }

        [Fact]
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
            Assert.Equal(2, values.Count);
            Assert.Equal(true, values.Contains(1));
            Assert.Equal(true, values.Contains(2));
            Assert.Equal(false, values.Contains(3));
            Assert.Equal(false, values.Contains(4));
        }

        [Fact]
        public void TestDropAllIndexes()
        {
            _collection.DropAllIndexes();
        }

        [Fact]
        public void TestDropIndex()
        {
            _collection.DropAllIndexes();
            Assert.Equal(1, _collection.GetIndexes().Count());
            Assert.Throws<MongoCommandException>(() => _collection.DropIndex("x"));

            _collection.CreateIndex("x");
            Assert.Equal(2, _collection.GetIndexes().Count());
            _collection.DropIndex("x");
            Assert.Equal(1, _collection.GetIndexes().Count());
        }

        [SkippableFact]
        public void TestDropIndexWriteConcern()
        {
            RequireServer.Check().Supports(Feature.AggregateOut, Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var writeConcern = new WriteConcern(9);

            var exception = Record.Exception(() => _collection.WithWriteConcern(writeConcern).DropIndex("x"));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestCreateIndexTimeToLive()
        {
            if (_server.BuildInfo.Version >= new Version(2, 2, 0))
            {
                _collection.DropAllIndexes();
                Assert.Equal(1, _collection.GetIndexes().Count());

                var keys = IndexKeys.Ascending("ts");
                var options = IndexOptions.SetTimeToLive(TimeSpan.FromHours(1));
                var result = _collection.CreateIndex(keys, options);

                var expectedResult = new ExpectedWriteConcernResult();
                CheckExpectedResult(expectedResult, result);

                var indexes = _collection.GetIndexes();
                Assert.Equal("_id_", indexes[0].Name);
                Assert.Equal("ts_1", indexes[1].Name);
                Assert.Equal(TimeSpan.FromHours(1), indexes[1].TimeToLive);
            }
        }

        [Fact]
        public void TestExplain()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            _collection.Find(Query.GT("x", 3)).Explain();
        }

        [Fact]
        public void TestFind()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.Find(Query.GT("x", 3));
            Assert.Equal(1, result.Count());
            Assert.Equal(4, result.Select(x => x["x"].AsInt32).FirstOrDefault());
        }

        [Fact]
        public void TestFindAndModify()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "priority", 1 }, { "inprogress", false }, { "name", "abc" } });
            _collection.Insert(new BsonDocument { { "_id", 2 }, { "priority", 2 }, { "inprogress", false }, { "name", "def" } });
            _collection.Insert(new BsonDocument { { "_id", 3 }, { "priority", 3 }, { "inprogress", false }, { "name", "ghi" } });


            var started = DateTime.UtcNow;
            started = started.AddTicks(-(started.Ticks % 10000)); // adjust for MongoDB DateTime precision
            var args = new FindAndModifyArgs
            {
                BypassDocumentValidation = true,
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority"),
                Update = Update.Set("inprogress", true).Set("started", started)
            };
            var result = _collection.FindAndModify(args);

            Assert.True(result.Ok);
            Assert.Equal(3, result.ModifiedDocument["_id"].AsInt32);
            Assert.Equal(3, result.ModifiedDocument["priority"].AsInt32);
            Assert.Equal(false, result.ModifiedDocument["inprogress"].AsBoolean);
            Assert.Equal("ghi", result.ModifiedDocument["name"].AsString);
            Assert.False(result.ModifiedDocument.Contains("started"));

            started = DateTime.UtcNow;
            started = started.AddTicks(-(started.Ticks % 10000)); // adjust for MongoDB DateTime precision
            args = new FindAndModifyArgs
            {
                BypassDocumentValidation = true,
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority"),
                Update = Update.Set("inprogress", true).Set("started", started),
                VersionReturned = FindAndModifyDocumentVersion.Original
            };
            result = _collection.FindAndModify(args);

            Assert.True(result.Ok);
            Assert.Equal(2, result.ModifiedDocument["_id"].AsInt32);
            Assert.Equal(2, result.ModifiedDocument["priority"].AsInt32);
            Assert.Equal(false, result.ModifiedDocument["inprogress"].AsBoolean);
            Assert.Equal("def", result.ModifiedDocument["name"].AsString);
            Assert.False(result.ModifiedDocument.Contains("started"));

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

            Assert.True(result.Ok);
            Assert.Equal(1, result.ModifiedDocument["_id"].AsInt32);
            Assert.Equal(1, result.ModifiedDocument["priority"].AsInt32);
            Assert.Equal(true, result.ModifiedDocument["inprogress"].AsBoolean);
            Assert.Equal("abc", result.ModifiedDocument["name"].AsString);
            Assert.Equal(started, result.ModifiedDocument["started"].ToUniversalTime());
        }

        [Fact]
        public void TestFindAndModifyWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        failpoint.SetAlwaysOn();
                        var args = new FindAndModifyArgs
                        {
                            Update = Update.Set("x", 1),
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.FindAndModify(args));
                    }
                }
            }
        }

        [Fact]
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

            Assert.True(result.Ok);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.ModifiedDocument);
            Assert.Null(result.GetModifiedDocumentAs<FindAndModifyClass>());
        }

        [Fact]
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

            Assert.Equal("Tom", result.ModifiedDocument["name"].AsString);
            Assert.Equal(1, result.ModifiedDocument["count"].AsInt32);
        }

        [SkippableFact]
        public void TestFindAndModifyReplaceWithWriteConcernError()
        {
            RequireServer.Check().Supports(Feature.FindAndModifyWriteConcern).ClusterType(ClusterType.ReplicaSet);
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            var collectionSettings = new MongoCollectionSettings
            {
                WriteConcern = new WriteConcern(9)
            };
            var collection = _database.GetCollection(_collection.Name, collectionSettings);
            var args = new FindAndModifyArgs
            {
                Query = Query.EQ("_id", 1),
                Update = Update.Replace(new BsonDocument { { "_id", 1 }, { "x", 2 } }),
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };

            BsonDocument modifiedDocument;
            if (_server.BuildInfo.Version >= new Version(3, 2, 0))
            {
                Action action = () => collection.FindAndModify(args);

                var exception = action.ShouldThrow<MongoWriteConcernException>().Which;
                var commandResult = exception.Result;
                modifiedDocument = commandResult["value"].AsBsonDocument;
            }
            else
            {
                var result = collection.FindAndModify(args);

                modifiedDocument = result.ModifiedDocument;
            }

            modifiedDocument.Should().Be("{ _id : 1, x : 2 }");
        }

        [SkippableFact]
        public void TestFindAndModifyUpdateWithWriteConcernError()
        {
            RequireServer.Check().Supports(Feature.FindAndModifyWriteConcern).ClusterType(ClusterType.ReplicaSet);
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            var collectionSettings = new MongoCollectionSettings
            {
                WriteConcern = new WriteConcern(9)
            };
            var collection = _database.GetCollection(_collection.Name, collectionSettings);
            var args = new FindAndModifyArgs
            {
                Query = Query.EQ("x", 1),
                Update = Update.Set("x", 2),
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };

            BsonDocument modifiedDocument;
            if (_server.BuildInfo.Version >= new Version(3, 2, 0))
            {
                Action action = () => collection.FindAndModify(args);

                var exception = action.ShouldThrow<MongoWriteConcernException>().Which;
                var commandResult = exception.Result;
                modifiedDocument = commandResult["value"].AsBsonDocument;
            }
            else
            {
                var result = collection.FindAndModify(args);

                modifiedDocument = result.ModifiedDocument;
            }

            modifiedDocument.Should().Be("{ _id : 1, x : 2 }");
        }

        private class FindAndModifyClass
        {
            public ObjectId Id;
            public int Value;
        }

        [Fact]
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

            Assert.Equal(obj.Id, rehydrated.Id);
            Assert.Equal(2, rehydrated.Value);
        }

        [Fact]
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
            Assert.Equal(1, result.ModifiedDocument["y"].ToInt32());
            Assert.Equal(1, _collection.Count());
        }

        [Fact]
        public void TestFindAndRemoveNoMatchingDocument()
        {
            _collection.RemoveAll();

            var args = new FindAndRemoveArgs
            {
                Query = Query.EQ("inprogress", false),
                SortBy = SortBy.Descending("priority")
            };
            var result = _collection.FindAndRemove(args);

            Assert.True(result.Ok);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.ModifiedDocument);
            Assert.Null(result.GetModifiedDocumentAs<FindAndModifyClass>());
        }

        [Fact]
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
            Assert.Equal(1, result.ModifiedDocument.ElementCount);
            Assert.Equal("_id", result.ModifiedDocument.GetElement(0).Name);
        }

        [Fact]
        public void TestFindAndRemoveWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        failpoint.SetAlwaysOn();
                        var args = new FindAndRemoveArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.FindAndRemove(args));
                    }
                }
            }
        }

        [SkippableFact]
        public void TestFindAndRemoveWithWriteConcernError()
        {
            RequireServer.Check().Supports(Feature.FindAndModifyWriteConcern).ClusterType(ClusterType.ReplicaSet);
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            var collectionSettings = new MongoCollectionSettings
            {
                WriteConcern = new WriteConcern(9)
            };
            var collection = _database.GetCollection(_collection.Name, collectionSettings);
            var args = new FindAndRemoveArgs
            {
                Query = Query.EQ("x", 1)              
            };

            BsonDocument modifiedDocument;
            if (_server.BuildInfo.Version >= new Version(3, 2, 0))
            {
                Action action = () => collection.FindAndRemove(args);

                var exception = action.ShouldThrow<MongoWriteConcernException>().Which;
                var commandResult = exception.Result;
                modifiedDocument = commandResult["value"].AsBsonDocument;
            }
            else
            {
                var result = collection.FindAndRemove(args);

                modifiedDocument = result.ModifiedDocument;
            }

            modifiedDocument.Should().Be("{ _id : 1, x : 1 }");
            _collection.Count().Should().Be(0);
        }

        [Fact]
        public void TestFindNearSphericalFalse()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.Near("Location", -74.0, 40.74);
            var hits = _collection.Find(query).ToArray();
            Assert.Equal(3, hits.Length);

            var hit0 = hits[0];
            Assert.Equal(-74.0, hit0["Location"][0].AsDouble);
            Assert.Equal(40.74, hit0["Location"][1].AsDouble);
            Assert.Equal("10gen", hit0["Name"].AsString);
            Assert.Equal("Office", hit0["Type"].AsString);

            // with spherical false "Three" is slightly closer than "Two"
            var hit1 = hits[1];
            Assert.Equal(-74.0, hit1["Location"][0].AsDouble);
            Assert.Equal(41.73, hit1["Location"][1].AsDouble);
            Assert.Equal("Three", hit1["Name"].AsString);
            Assert.Equal("Coffee", hit1["Type"].AsString);

            var hit2 = hits[2];
            Assert.Equal(-75.0, hit2["Location"][0].AsDouble);
            Assert.Equal(40.74, hit2["Location"][1].AsDouble);
            Assert.Equal("Two", hit2["Name"].AsString);
            Assert.Equal("Coffee", hit2["Type"].AsString);

            query = Query.Near("Location", -74.0, 40.74, 0.5); // with maxDistance
            hits = _collection.Find(query).ToArray();
            Assert.Equal(1, hits.Length);

            hit0 = hits[0];
            Assert.Equal(-74.0, hit0["Location"][0].AsDouble);
            Assert.Equal(40.74, hit0["Location"][1].AsDouble);
            Assert.Equal("10gen", hit0["Name"].AsString);
            Assert.Equal("Office", hit0["Type"].AsString);

            query = Query.Near("Location", -174.0, 40.74, 0.5); // with no hits
            hits = _collection.Find(query).ToArray();
            Assert.Equal(0, hits.Length);
        }

        [Fact]
        public void TestFindNearSphericalTrue()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
                _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
                _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
                _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

                var query = Query.Near("Location", -74.0, 40.74, double.MaxValue, true); // spherical
                var hits = _collection.Find(query).ToArray();
                Assert.Equal(3, hits.Length);

                var hit0 = hits[0];
                Assert.Equal(-74.0, hit0["Location"][0].AsDouble);
                Assert.Equal(40.74, hit0["Location"][1].AsDouble);
                Assert.Equal("10gen", hit0["Name"].AsString);
                Assert.Equal("Office", hit0["Type"].AsString);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = hits[1];
                Assert.Equal(-75.0, hit1["Location"][0].AsDouble);
                Assert.Equal(40.74, hit1["Location"][1].AsDouble);
                Assert.Equal("Two", hit1["Name"].AsString);
                Assert.Equal("Coffee", hit1["Type"].AsString);

                var hit2 = hits[2];
                Assert.Equal(-74.0, hit2["Location"][0].AsDouble);
                Assert.Equal(41.73, hit2["Location"][1].AsDouble);
                Assert.Equal("Three", hit2["Name"].AsString);
                Assert.Equal("Coffee", hit2["Type"].AsString);

                query = Query.Near("Location", -74.0, 40.74, 0.5); // with maxDistance
                hits = _collection.Find(query).ToArray();
                Assert.Equal(1, hits.Length);

                hit0 = hits[0];
                Assert.Equal(-74.0, hit0["Location"][0].AsDouble);
                Assert.Equal(40.74, hit0["Location"][1].AsDouble);
                Assert.Equal("10gen", hit0["Name"].AsString);
                Assert.Equal("Office", hit0["Type"].AsString);

                query = Query.Near("Location", -174.0, 40.74, 0.5); // with no hits
                hits = _collection.Find(query).ToArray();
                Assert.Equal(0, hits.Length);
            }
        }

        [Fact]
        public void TestFindOne()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.FindOne();
            Assert.Equal(1, result["x"].AsInt32);
            Assert.Equal(2, result["y"].AsInt32);
        }

        [Fact]
        public void TestFindOneAs()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var result = (TestClass)_collection.FindOneAs(typeof(TestClass));
            Assert.Equal(1, result.X);
        }

        [Fact]
        public void TestFindOneAsGeneric()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var result = _collection.FindOneAs<TestClass>();
            Assert.Equal(1, result.X);
        }

        [Fact]
        public void TestFindOneAsGenericWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.RemoveAll();
                        _collection.Insert(new BsonDocument { { "X", 1 } });

                        failpoint.SetAlwaysOn();
                        var args = new FindOneArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.FindOneAs<TestClass>(args));
                    }
                }
            }
        }

        [Fact]
        public void TestFindOneAsGenericWithSkipAndSortyBy()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 2 } });
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var sortBy = SortBy.Ascending("X");
            var args = new FindOneArgs { Skip = 1, SortBy = sortBy };
            var document = _collection.FindOneAs<TestClass>(args);
            Assert.Equal(2, document.X);
        }

        [Fact]
        public void TestFindOneAsWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.RemoveAll();
                        _collection.Insert(new BsonDocument { { "X", 1 } });

                        failpoint.SetAlwaysOn();
                        var args = new FindOneArgs { MaxTime = TimeSpan.FromMilliseconds(1) };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.FindOneAs(typeof(TestClass), args));
                    }
                }
            }
        }

        [Fact]
        public void TestFindOneAsWithSkipAndSortyBy()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "X", 2 } });
            _collection.Insert(new BsonDocument { { "X", 1 } });
            var sortBy = SortBy.Ascending("X");
            var args = new FindOneArgs { Skip = 1, SortBy = sortBy };
            var document = (TestClass)_collection.FindOneAs(typeof(TestClass), args);
            Assert.Equal(2, document.X);
        }

        [Fact]
        public void TestFindOneById()
        {
            _collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            _collection.Insert(new BsonDocument { { "_id", id }, { "x", 1 }, { "y", 2 } });
            var result = _collection.FindOneById(id);
            Assert.Equal(1, result["x"].AsInt32);
            Assert.Equal(2, result["y"].AsInt32);
        }

        [Fact]
        public void TestFindOneByIdAs()
        {
            _collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            _collection.Insert(new BsonDocument { { "_id", id }, { "X", 1 } });
            var result = (TestClass)_collection.FindOneByIdAs(typeof(TestClass), id);
            Assert.Equal(id, result.Id);
            Assert.Equal(1, result.X);
        }

        [Fact]
        public void TestFindOneByIdAsGeneric()
        {
            _collection.RemoveAll();
            var id = ObjectId.GenerateNewId();
            _collection.Insert(new BsonDocument { { "_id", id }, { "X", 1 } });
            var result = _collection.FindOneByIdAs<TestClass>(id);
            Assert.Equal(id, result.Id);
            Assert.Equal(1, result.X);
        }

        [Fact]
        public void TestFindWithinCircleSphericalFalse()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinCircle("Location", -74.0, 40.74, 1.0, false); // not spherical
            var hits = _collection.Find(query).ToArray();
            Assert.Equal(3, hits.Length);
            // note: the hits are unordered

            query = Query.WithinCircle("Location", -74.0, 40.74, 0.5, false); // smaller radius
            hits = _collection.Find(query).ToArray();
            Assert.Equal(1, hits.Length);

            query = Query.WithinCircle("Location", -174.0, 40.74, 1.0, false); // different part of the world
            hits = _collection.Find(query).ToArray();
            Assert.Equal(0, hits.Length);
        }

        [Fact]
        public void TestFindWithinCircleSphericalTrue()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
                _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
                _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
                _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

                var query = Query.WithinCircle("Location", -74.0, 40.74, 0.1, true); // spherical
                var hits = _collection.Find(query).ToArray();
                Assert.Equal(3, hits.Length);
                // note: the hits are unordered

                query = Query.WithinCircle("Location", -74.0, 40.74, 0.01, false); // smaller radius
                hits = _collection.Find(query).ToArray();
                Assert.Equal(1, hits.Length);

                query = Query.WithinCircle("Location", -174.0, 40.74, 0.1, false); // different part of the world
                hits = _collection.Find(query).ToArray();
                Assert.Equal(0, hits.Length);
            }
        }

        [Fact]
        public void TestFindWithinRectangle()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var query = Query.WithinRectangle("Location", -75.0, 40, -73.0, 42.0);
            var hits = _collection.Find(query).ToArray();
            Assert.Equal(3, hits.Length);
            // note: the hits are unordered
        }

        [Fact]
        public void TestFindWithMaxScan()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            var docs = Enumerable.Range(0, 10).Select(x => new BsonDocument("_id", x));
            _collection.InsertBatch(docs);

            var results = _collection.FindAll().SetMaxScan(4).ToList();
            Assert.Equal(4, results.Count);
        }

        [Fact]
        public void TestFindWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        if (_collection.Exists()) { _collection.Drop(); }
                        _collection.Insert(new BsonDocument("x", 1));

                        failpoint.SetAlwaysOn();
                        var maxTime = TimeSpan.FromMilliseconds(1);
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.FindAll().SetMaxTime(maxTime).ToList());
                    }
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

        [Fact]
        public void TestGeoHaystackSearch()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { 34.2, 33.3 }, Type = "restaurant" });
                _collection.Insert(new Place { Location = new[] { 34.2, 37.3 }, Type = "restaurant" });
                _collection.Insert(new Place { Location = new[] { 59.1, 87.2 }, Type = "office" });
                _collection.CreateIndex(IndexKeys.GeoSpatialHaystack("Location", "Type"), IndexOptions.SetBucketSize(1));

                var args = new GeoHaystackSearchArgs
                {
                    Near = new XYPoint(33, 33),
                    AdditionalFieldName = "Type",
                    AdditionalFieldValue = "restaurant",
                    Limit = 30,
                    MaxDistance = 6
                };
                var result = _collection.GeoHaystackSearchAs<Place>(args);

                Assert.True(result.Ok);
                Assert.True(result.Stats.Duration >= TimeSpan.Zero);
                Assert.Equal(2, result.Stats.BTreeMatches);
                Assert.Equal(2, result.Stats.NumberOfHits);
                Assert.Equal(34.2, result.Hits[0].Document.Location[0]);
                Assert.Equal(33.3, result.Hits[0].Document.Location[1]);
                Assert.Equal("restaurant", result.Hits[0].Document.Type);
                Assert.Equal(34.2, result.Hits[1].Document.Location[0]);
                Assert.Equal(37.3, result.Hits[1].Document.Location[1]);
                Assert.Equal("restaurant", result.Hits[1].Document.Type);
            }
        }

        [Fact]
        public void TestGeoHaystackSearchWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
                {
                    using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                    {
                        if (failpoint.IsSupported())
                        {
                            if (_collection.Exists()) { _collection.Drop(); }
                            _collection.Insert(new Place { Location = new[] { 34.2, 33.3 }, Type = "restaurant" });
                            _collection.Insert(new Place { Location = new[] { 34.2, 37.3 }, Type = "restaurant" });
                            _collection.Insert(new Place { Location = new[] { 59.1, 87.2 }, Type = "office" });
                            _collection.CreateIndex(IndexKeys.GeoSpatialHaystack("Location", "Type"), IndexOptions.SetBucketSize(1));

                            failpoint.SetAlwaysOn();
                            var args = new GeoHaystackSearchArgs
                            {
                                Near = new XYPoint(33, 33),
                                AdditionalFieldName = "Type",
                                AdditionalFieldValue = "restaurant",
                                Limit = 30,
                                MaxDistance = 6,
                                MaxTime = TimeSpan.FromMilliseconds(1)
                            };
                            Assert.Throws<MongoExecutionTimeoutException>(() => _collection.GeoHaystackSearchAs<Place>(args));
                        }
                    }
                }
            }
        }

        [Fact]
        public void TestGeoHaystackSearch_Typed()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { 34.2, 33.3 }, Type = "restaurant" });
                _collection.Insert(new Place { Location = new[] { 34.2, 37.3 }, Type = "restaurant" });
                _collection.Insert(new Place { Location = new[] { 59.1, 87.2 }, Type = "office" });
                _collection.CreateIndex(IndexKeys<Place>.GeoSpatialHaystack(x => x.Location, x => x.Type), IndexOptions.SetBucketSize(1));

                var args = new GeoHaystackSearchArgs
                {
                    Near = new XYPoint(33, 33),
                    Limit = 30,
                    MaxDistance = 6
                }
                .SetAdditionalField<Place, string>(x => x.Type, "restaurant");
                var result = _collection.GeoHaystackSearchAs<Place>(args);

                Assert.True(result.Ok);
                Assert.True(result.Stats.Duration >= TimeSpan.Zero);
                Assert.Equal(2, result.Stats.BTreeMatches);
                Assert.Equal(2, result.Stats.NumberOfHits);
                Assert.Equal(34.2, result.Hits[0].Document.Location[0]);
                Assert.Equal(33.3, result.Hits[0].Document.Location[1]);
                Assert.Equal("restaurant", result.Hits[0].Document.Type);
                Assert.Equal(34.2, result.Hits[1].Document.Location[0]);
                Assert.Equal(37.3, result.Hits[1].Document.Location[1]);
                Assert.Equal("restaurant", result.Hits[1].Document.Type);
            }
        }

        [Fact]
        public void TestGeoNear()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { 1.0, 1.0 }, Name = "One", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 2.0 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { 1.0, 3.0 }, Name = "Three", Type = "Library" });
            _collection.Insert(new Place { Location = new[] { 1.0, 4.0 }, Name = "Four", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 5.0 }, Name = "Five", Type = "Coffee" });
            _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var args = new GeoNearArgs
            {
                Near = new XYPoint(0, 0),
                Limit = 100,
                DistanceMultiplier = 1,
                MaxDistance = 100
            };
            var result = _collection.GeoNearAs(typeof(Place), args);

            Assert.True(result.Ok);
            Assert.Equal(_collection.FullName, result.Namespace);
            Assert.True(result.Stats.AverageDistance >= 0.0);
#pragma warning disable 618
            Assert.True(result.Stats.BTreeLocations >= -1);
#pragma warning restore
            Assert.True(result.Stats.Duration >= TimeSpan.Zero);
            Assert.True(result.Stats.MaxDistance >= 0.0);
#pragma warning disable 618
            Assert.True(result.Stats.NumberScanned >= -1);
#pragma warning restore
            Assert.True(result.Stats.ObjectsLoaded >= 0);
            Assert.Equal(5, result.Hits.Count);
            Assert.True(result.Hits[0].Distance > 1.0);
            Assert.Equal(1.0, result.Hits[0].RawDocument["Location"][0].AsDouble);
            Assert.Equal(1.0, result.Hits[0].RawDocument["Location"][1].AsDouble);
            Assert.Equal("One", result.Hits[0].RawDocument["Name"].AsString);
            Assert.Equal("Museum", result.Hits[0].RawDocument["Type"].AsString);

            var place = (Place)result.Hits[1].Document;
            Assert.Equal(1.0, place.Location[0]);
            Assert.Equal(2.0, place.Location[1]);
            Assert.Equal("Two", place.Name);
            Assert.Equal("Coffee", place.Type);
        }

        [Fact]
        public void TestGeoNearGeneric()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { 1.0, 1.0 }, Name = "One", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 2.0 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { 1.0, 3.0 }, Name = "Three", Type = "Library" });
            _collection.Insert(new Place { Location = new[] { 1.0, 4.0 }, Name = "Four", Type = "Museum" });
            _collection.Insert(new Place { Location = new[] { 1.0, 5.0 }, Name = "Five", Type = "Coffee" });
            _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var args = new GeoNearArgs
            {
                Near = new XYPoint(0, 0),
                Limit = 100,
                DistanceMultiplier = 1,
                MaxDistance = 100
            };
            var result = _collection.GeoNearAs<Place>(args);

            Assert.True(result.Ok);
            Assert.Equal(_collection.FullName, result.Namespace);
            Assert.True(result.Stats.AverageDistance >= 0.0);
#pragma warning disable 618
            Assert.True(result.Stats.BTreeLocations >= -1);
#pragma warning restore
            Assert.True(result.Stats.Duration >= TimeSpan.Zero);
            Assert.True(result.Stats.MaxDistance >= 0.0);
#pragma warning disable 618
            Assert.True(result.Stats.NumberScanned >= -1);
#pragma warning restore
            Assert.True(result.Stats.ObjectsLoaded >= 0);
            Assert.Equal(5, result.Hits.Count);
            Assert.True(result.Hits[0].Distance > 1.0);
            Assert.Equal(1.0, result.Hits[0].RawDocument["Location"][0].AsDouble);
            Assert.Equal(1.0, result.Hits[0].RawDocument["Location"][1].AsDouble);
            Assert.Equal("One", result.Hits[0].RawDocument["Name"].AsString);
            Assert.Equal("Museum", result.Hits[0].RawDocument["Type"].AsString);

            var place = result.Hits[1].Document;
            Assert.Equal(1.0, place.Location[0]);
            Assert.Equal(2.0, place.Location[1]);
            Assert.Equal("Two", place.Name);
            Assert.Equal("Coffee", place.Type);
        }

        [Fact]
        public void TestGeoNearSphericalFalse()
        {
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
            _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
            _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
            _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

            var args = new GeoNearArgs
            {
                Near = new XYPoint(-74.0, 40.74),
                Limit = 100,
                Spherical = false
            };
            var result = _collection.GeoNearAs<Place>(args);

            Assert.True(result.Ok);
            Assert.Equal(_collection.FullName, result.Namespace);
            Assert.True(result.Stats.AverageDistance >= 0.0);
#pragma warning disable 618
            Assert.True(result.Stats.BTreeLocations >= -1);
#pragma warning restore
            Assert.True(result.Stats.Duration >= TimeSpan.Zero);
            Assert.True(result.Stats.MaxDistance >= 0.0);
#pragma warning disable 618
            Assert.True(result.Stats.NumberScanned >= -1);
#pragma warning restore
            Assert.True(result.Stats.ObjectsLoaded >= 0);
            Assert.Equal(3, result.Hits.Count);

            var hit0 = result.Hits[0];
            Assert.True(hit0.Distance == 0.0);
            Assert.Equal(-74.0, hit0.RawDocument["Location"][0].AsDouble);
            Assert.Equal(40.74, hit0.RawDocument["Location"][1].AsDouble);
            Assert.Equal("10gen", hit0.RawDocument["Name"].AsString);
            Assert.Equal("Office", hit0.RawDocument["Type"].AsString);

            // with spherical false "Three" is slightly closer than "Two"
            var hit1 = result.Hits[1];
            Assert.True(hit1.Distance > 0.0);
            Assert.Equal(-74.0, hit1.RawDocument["Location"][0].AsDouble);
            Assert.Equal(41.73, hit1.RawDocument["Location"][1].AsDouble);
            Assert.Equal("Three", hit1.RawDocument["Name"].AsString);
            Assert.Equal("Coffee", hit1.RawDocument["Type"].AsString);

            var hit2 = result.Hits[2];
            Assert.True(hit2.Distance > 0.0);
            Assert.True(hit2.Distance > hit1.Distance);
            Assert.Equal(-75.0, hit2.RawDocument["Location"][0].AsDouble);
            Assert.Equal(40.74, hit2.RawDocument["Location"][1].AsDouble);
            Assert.Equal("Two", hit2.RawDocument["Name"].AsString);
            Assert.Equal("Coffee", hit2.RawDocument["Type"].AsString);
        }

        [Fact]
        public void TestGeoNearSphericalTrue()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new Place { Location = new[] { -74.0, 40.74 }, Name = "10gen", Type = "Office" });
                _collection.Insert(new Place { Location = new[] { -75.0, 40.74 }, Name = "Two", Type = "Coffee" });
                _collection.Insert(new Place { Location = new[] { -74.0, 41.73 }, Name = "Three", Type = "Coffee" });
                _collection.CreateIndex(IndexKeys.GeoSpatial("Location"));

                var args = new GeoNearArgs
                {
                    Near = new XYPoint(-74.0, 40.74),
                    Limit = 100,
                    Spherical = true
                };
                var result = _collection.GeoNearAs<Place>(args);

                Assert.True(result.Ok);
                Assert.Equal(_collection.FullName, result.Namespace);
                Assert.True(result.Stats.AverageDistance >= 0.0);
#pragma warning disable 618
                Assert.True(result.Stats.BTreeLocations >= -1);
#pragma warning restore
                Assert.True(result.Stats.Duration >= TimeSpan.Zero);
                Assert.True(result.Stats.MaxDistance >= 0.0);
#pragma warning disable 618
                Assert.True(result.Stats.NumberScanned >= -1);
#pragma warning restore
                Assert.True(result.Stats.ObjectsLoaded >= 0);
                Assert.Equal(3, result.Hits.Count);

                var hit0 = result.Hits[0];
                Assert.True(hit0.Distance == 0.0);
                Assert.Equal(-74.0, hit0.RawDocument["Location"][0].AsDouble);
                Assert.Equal(40.74, hit0.RawDocument["Location"][1].AsDouble);
                Assert.Equal("10gen", hit0.RawDocument["Name"].AsString);
                Assert.Equal("Office", hit0.RawDocument["Type"].AsString);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = result.Hits[1];
                Assert.True(hit1.Distance > 0.0);
                Assert.Equal(-75.0, hit1.RawDocument["Location"][0].AsDouble);
                Assert.Equal(40.74, hit1.RawDocument["Location"][1].AsDouble);
                Assert.Equal("Two", hit1.RawDocument["Name"].AsString);
                Assert.Equal("Coffee", hit1.RawDocument["Type"].AsString);

                var hit2 = result.Hits[2];
                Assert.True(hit2.Distance > 0.0);
                Assert.True(hit2.Distance > hit1.Distance);
                Assert.Equal(-74.0, hit2.RawDocument["Location"][0].AsDouble);
                Assert.Equal(41.73, hit2.RawDocument["Location"][1].AsDouble);
                Assert.Equal("Three", hit2.RawDocument["Name"].AsString);
                Assert.Equal("Coffee", hit2.RawDocument["Type"].AsString);
            }
        }

        [Fact]
        public void TestGeoNearWithGeoJsonPoints()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)), Name = "10gen", Type = "Office" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 41.73)), Name = "Three", Type = "Coffee" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-75.0, 40.74)), Name = "Two", Type = "Coffee" });
                _collection.CreateIndex(IndexKeys.GeoSpatialSpherical("Location"));

                var args = new GeoNearArgs
                {
                    Near = GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)),
                    Spherical = true
                };
                var result = _collection.GeoNearAs<PlaceGeoJson>(args);
                var hits = result.Hits;

                var hit0 = hits[0].Document;
                Assert.Equal(-74.0, hit0.Location.Coordinates.Longitude);
                Assert.Equal(40.74, hit0.Location.Coordinates.Latitude);
                Assert.Equal("10gen", hit0.Name);
                Assert.Equal("Office", hit0.Type);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = hits[1].Document;
                Assert.Equal(-75.0, hit1.Location.Coordinates.Longitude);
                Assert.Equal(40.74, hit1.Location.Coordinates.Latitude);
                Assert.Equal("Two", hit1.Name);
                Assert.Equal("Coffee", hit1.Type);

                var hit2 = hits[2].Document;
                Assert.Equal(-74.0, hit2.Location.Coordinates.Longitude);
                Assert.Equal(41.73, hit2.Location.Coordinates.Latitude);
                Assert.Equal("Three", hit2.Name);
                Assert.Equal("Coffee", hit2.Type);
            }
        }

        [Fact]
        public void TestGeoNearWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        if (_collection.Exists()) { _collection.Drop(); }
                        _collection.Insert(new BsonDocument("loc", new BsonArray { 0, 0 }));
                        _collection.CreateIndex(IndexKeys.GeoSpatial("loc"));

                        failpoint.SetAlwaysOn();
                        var args = new GeoNearArgs
                        {
                            Near = new XYPoint(0, 0),
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.GeoNearAs<BsonDocument>(args));
                    }
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

        [Fact]
        public void TestGeoSphericalIndex()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)), Name = "10gen", Type = "Office" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-74.0, 41.73)), Name = "Three", Type = "Coffee" });
                _collection.Insert(new PlaceGeoJson { Location = GeoJson.Point(GeoJson.Geographic(-75.0, 40.74)), Name = "Two", Type = "Coffee" });
                _collection.CreateIndex(IndexKeys.GeoSpatialSpherical("Location"));

                // TODO: add Query builder support for 2dsphere queries
                var query = Query<PlaceGeoJson>.Near(x => x.Location, GeoJson.Point(GeoJson.Geographic(-74.0, 40.74)));

                var cursor = _collection.FindAs<PlaceGeoJson>(query);
                var hits = cursor.ToArray();

                var hit0 = hits[0];
                Assert.Equal(-74.0, hit0.Location.Coordinates.Longitude);
                Assert.Equal(40.74, hit0.Location.Coordinates.Latitude);
                Assert.Equal("10gen", hit0.Name);
                Assert.Equal("Office", hit0.Type);

                // with spherical true "Two" is considerably closer than "Three"
                var hit1 = hits[1];
                Assert.Equal(-75.0, hit1.Location.Coordinates.Longitude);
                Assert.Equal(40.74, hit1.Location.Coordinates.Latitude);
                Assert.Equal("Two", hit1.Name);
                Assert.Equal("Coffee", hit1.Type);

                var hit2 = hits[2];
                Assert.Equal(-74.0, hit2.Location.Coordinates.Longitude);
                Assert.Equal(41.73, hit2.Location.Coordinates.Latitude);
                Assert.Equal("Three", hit2.Name);
                Assert.Equal("Coffee", hit2.Type);
            }
        }

        [Fact]
        public void TestGetIndexes()
        {
            _collection.DropAllIndexes();
            var indexes = _collection.GetIndexes();
            Assert.Equal(1, indexes.Count);
            Assert.Equal("_id_", indexes[0].Name);
            // see additional tests in TestEnsureIndex
        }

        [Fact]
        public void TestGetMore()
        {
            _collection.RemoveAll();
            var count = _primary.MaxMessageLength / 1000000;
            for (int i = 0; i < count; i++)
            {
                var document = new BsonDocument("data", new BsonBinaryData(new byte[1000000]));
                _collection.Insert(document);
            }
            _collection.FindAll().ToList();
        }

        [Fact]
        public void TestGroupWithFinalizeFunction()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));

            var results = _collection.Group(new GroupArgs
            {
                KeyFields = GroupBy.Keys("x"),
                Initial = new BsonDocument("count", 0),
                ReduceFunction = "function(doc, prev) { prev.count += 1 }",
                FinalizeFunction = "function(result) { result.count = -result.count; }"
            }).ToArray();

            Assert.Equal(3, results.Length);
            Assert.Equal(1, results[0]["x"].ToInt32());
            Assert.Equal(-2, results[0]["count"].ToInt32());
            Assert.Equal(2, results[1]["x"].ToInt32());
            Assert.Equal(-1, results[1]["count"].ToInt32());
            Assert.Equal(3, results[2]["x"].ToInt32());
            Assert.Equal(-3, results[2]["count"].ToInt32());
        }

        [Fact]
        public void TestGroupWithKeyFields()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));

            var results = _collection.Group(new GroupArgs
            {
                KeyFields = GroupBy.Keys("x"),
                Initial = new BsonDocument("count", 0),
                ReduceFunction = "function(doc, prev) { prev.count += 1 }"
            }).ToArray();

            Assert.Equal(3, results.Length);
            Assert.Equal(1, results[0]["x"].ToInt32());
            Assert.Equal(2, results[0]["count"].ToInt32());
            Assert.Equal(2, results[1]["x"].ToInt32());
            Assert.Equal(1, results[1]["count"].ToInt32());
            Assert.Equal(3, results[2]["x"].ToInt32());
            Assert.Equal(3, results[2]["count"].ToInt32());
        }

        [Fact]
        public void TestGroupWithKeyFunction()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));

            var results = _collection.Group(new GroupArgs
            {
                KeyFunction = "function(doc) { return { x : doc.x }; }",
                Initial = new BsonDocument("count", 0),
                ReduceFunction = "function(doc, prev) { prev.count += 1 }"
            }).ToArray();

            Assert.Equal(3, results.Length);
            Assert.Equal(1, results[0]["x"].ToInt32());
            Assert.Equal(2, results[0]["count"].ToInt32());
            Assert.Equal(2, results[1]["x"].ToInt32());
            Assert.Equal(1, results[1]["count"].ToInt32());
            Assert.Equal(3, results[2]["x"].ToInt32());
            Assert.Equal(3, results[2]["count"].ToInt32());
        }

        [Fact]
        public void TestGroupWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.Drop();
                        _collection.Insert(new BsonDocument("x", 1)); // ensure collection is not empty

                        failpoint.SetAlwaysOn();
                        var args = new GroupArgs
                        {
                            KeyFields = GroupBy.Keys("x"),
                            Initial = new BsonDocument("count", 0),
                            ReduceFunction = "function(doc, prev) { prev.count += 1 }",
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.Group(args));
                    }
                }
            }
        }

        [Fact]
        public void TestGroupWithQuery()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));
            _collection.Insert(new BsonDocument("x", 3));

            var results = _collection.Group(new GroupArgs
            {
                Query = Query.LT("x", 3),
                KeyFields = GroupBy.Keys("x"),
                Initial = new BsonDocument("count", 0),
                ReduceFunction = "function(doc, prev) { prev.count += 1 }"
            }).ToArray();

            Assert.Equal(2, results.Length);
            Assert.Equal(1, results[0]["x"].ToInt32());
            Assert.Equal(2, results[0]["count"].ToInt32());
            Assert.Equal(2, results[1]["x"].ToInt32());
            Assert.Equal(1, results[1]["count"].ToInt32());
        }

        [Fact]
        public void TestHashedIndex()
        {
            if (_server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                if (_collection.Exists()) { _collection.Drop(); }
                var expectedName = "x_hashed";
                var expectedKey = "{ x : \"hashed\" }";

                _collection.CreateIndex(IndexKeys.Hashed("x"));

                var index = _collection.GetIndexes().FirstOrDefault(x => x.Name == expectedName);
                Assert.NotNull(index);
                Assert.Equal(new IndexKeysDocument(BsonDocument.Parse(expectedKey)), index.Key);
            }
        }

        [Fact]
        public void TestIndexExists()
        {
            _collection.DropAllIndexes();
            Assert.Equal(false, _collection.IndexExists("x"));

            _collection.CreateIndex("x");
            Assert.Equal(true, _collection.IndexExists("x"));

            _collection.CreateIndex(IndexKeys.Ascending("y"));
            Assert.Equal(true, _collection.IndexExists(IndexKeys.Ascending("y")));
        }

        [Fact]
        public void TestInsertBatchContinueOnError()
        {
            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.Ascending("x"), IndexOptions.SetUnique(true));

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
            var exception = Assert.Throws<MongoDuplicateKeyException>(() => collection.InsertBatch(batch));
            var result = exception.WriteConcernResult;

            var expectedResult = new ExpectedWriteConcernResult
            {
                HasLastErrorMessage = true
            };
            CheckExpectedResult(expectedResult, result);

            Assert.Equal(1, collection.Count());
            Assert.Equal(1, collection.FindOne()["x"].AsInt32);

            // try the batch again with ContinueOnError
            if (_server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                // first remove the automatically generated _ids from the documents
                foreach (var document in batch)
                {
                    document.Remove("_id");
                }

                var options = new MongoInsertOptions
                {
                    BypassDocumentValidation = true,
                    Flags = InsertFlags.ContinueOnError
                };
                exception = Assert.Throws<MongoDuplicateKeyException>(() => collection.InsertBatch(batch, options));
                result = exception.WriteConcernResult;

                expectedResult = new ExpectedWriteConcernResult
                {
                    HasLastErrorMessage = true
                };
                CheckExpectedResult(expectedResult, result);

                Assert.Equal(3, collection.Count());
            }
        }

        [Fact]
        public void TestInsertBatchMultipleBatchesWriteConcernDisabledContinueOnErrorFalse()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Unacknowledged };
            var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            var maxMessageLength = _primary.MaxMessageLength;

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
            var results = collection.InsertBatch(documents, options);
            Assert.Equal(null, results);

            Assert.Equal(1, collection.Count(Query.EQ("_id", 1)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 2)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 3)));
            Assert.Equal(0, collection.Count(Query.EQ("_id", 4)));
            Assert.Equal(0, collection.Count(Query.EQ("_id", 5)));
        }

        [Fact]
        public void TestInsertBatchMultipleBatchesWriteConcernDisabledContinueOnErrorTrue()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Unacknowledged };
            var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            var maxMessageLength = _primary.MaxMessageLength;

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
            var results = collection.InsertBatch(documents, options);
            Assert.Equal(null, results);

            for (int i = 1; i <= 5; i++)
            {
                if (!SpinWait.SpinUntil(() => collection.Count(Query.EQ("_id", i)) == 1, TimeSpan.FromSeconds(5)))
                {
                    Assert.True(false, $"_id {i} does not exist.");
                }
            }
        }

        [Fact]
        public void TestInsertBatchMultipleBatchesWriteConcernEnabledContinueOnErrorFalse()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Acknowledged };
            var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            var maxMessageLength = _primary.MaxMessageLength;

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
            var exception = Assert.Throws<MongoDuplicateKeyException>(() => { collection.InsertBatch(documents, options); });
            var result = exception.WriteConcernResult;

            var expectedResult = new ExpectedWriteConcernResult
            {
                HasLastErrorMessage = true
            };
            CheckExpectedResult(expectedResult, result);

            var results = ((IEnumerable<WriteConcernResult>)exception.Data["results"]).ToArray();
            if (_primary.Supports(FeatureId.WriteCommands))
            {
                // it the opcode was emulated there will just be one synthesized result
                Assert.Equal(1, results.Length);
                Assert.Equal(true, results[0].HasLastErrorMessage);
            }
            else
            {
                Assert.Equal(2, results.Length);
                Assert.Equal(false, results[0].HasLastErrorMessage);
                Assert.Equal(true, results[1].HasLastErrorMessage);
            }

            Assert.Equal(1, collection.Count(Query.EQ("_id", 1)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 2)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 3)));
            Assert.Equal(0, collection.Count(Query.EQ("_id", 4)));
            Assert.Equal(0, collection.Count(Query.EQ("_id", 5)));
        }

        [Fact]
        public void TestInsertBatchMultipleBatchesWriteConcernEnabledContinueOnErrorTrue()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.Acknowledged };
            var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>(collectionName, collectionSettings);
            if (collection.Exists()) { collection.Drop(); }

            var maxMessageLength = _primary.MaxMessageLength;

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
            var exception = Assert.Throws<MongoDuplicateKeyException>(() => { collection.InsertBatch(documents, options); });
            var result = exception.WriteConcernResult;

            var expectedResult = new ExpectedWriteConcernResult()
            {
                HasLastErrorMessage = true
            };
            CheckExpectedResult(expectedResult, result);

            var results = ((IEnumerable<WriteConcernResult>)exception.Data["results"]).ToArray();
            if (_primary.Supports(FeatureId.WriteCommands))
            {
                // it the opcode was emulated there will just be one synthesized result
                Assert.Equal(1, results.Length);
                Assert.Equal(true, results[0].HasLastErrorMessage);
            }
            else
            {
                Assert.Equal(3, results.Length);
                Assert.Equal(false, results[0].HasLastErrorMessage);
                Assert.Equal(true, results[1].HasLastErrorMessage);
                Assert.Equal(false, results[2].HasLastErrorMessage);
            }

            Assert.Equal(1, collection.Count(Query.EQ("_id", 1)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 2)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 3)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 4)));
            Assert.Equal(1, collection.Count(Query.EQ("_id", 5)));
        }

        [Fact]
        public void TestInsertBatchSmallFinalSubbatch()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>(collectionName);
            if (collection.Exists()) { collection.Drop(); }

            var maxMessageLength = _primary.MaxMessageLength;
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

            Assert.Equal(documentCount, collection.Count());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void TestInsertBatchSplittingNearMaxWriteBatchCount(int maxBatchCountDelta)
        {
            var count = _primary.MaxBatchCount + maxBatchCountDelta;
            _collection.Drop();
            var documents = Enumerable.Range(0, count).Select(n => new BsonDocument("n", n));
            var expectedNumberOfResults = maxBatchCountDelta == 1 ? 2 : 1;
            if (_server.Primary.BuildInfo.Version >= new Version(2, 6, 0))
            {
                // emulated InsertOpcodes always return a single emulated result
                expectedNumberOfResults = 1;
            }

            var results = _collection.InsertBatch(documents);

            Assert.Equal(expectedNumberOfResults, results.Count());
            Assert.Equal(count, _collection.Count());
        }

        [Fact]
        public void TestInsertDuplicateKey()
        {
            var collection = _database.GetCollection("duplicatekeys");
            collection.Drop();

            var result = collection.Insert(new BsonDocument("_id", 1));
            var expectedResult = new ExpectedWriteConcernResult();
            CheckExpectedResult(expectedResult, result);

            var exception = Assert.Throws<MongoDuplicateKeyException>(() => { collection.Insert(new BsonDocument("_id", 1)); });
            result = exception.WriteConcernResult;
            expectedResult = new ExpectedWriteConcernResult
            {
                HasLastErrorMessage = true
            };
            CheckExpectedResult(expectedResult, result);
        }

        [SkippableFact]
        public void TestInsertWithWriteConcernError()
        {
            RequireServer.Check().Supports(Feature.WriteCommands).ClusterType(ClusterType.ReplicaSet);
            _collection.RemoveAll();
            var document = new BsonDocument { { "_id", 1 }, { "x", 1 } };
            var collectionSettings = new MongoCollectionSettings
            {
                WriteConcern = new WriteConcern(9)
            };
            var collection = _database.GetCollection(_collection.Name, collectionSettings);

            Action action = () => collection.Insert(document);

            action.ShouldThrow<MongoWriteConcernException>();
            _collection.FindOne().Should().Be(document);
        }

        [Fact]
        public void TestIsCappedFalse()
        {
            var collection = _database.GetCollection("notcappedcollection");
            collection.Drop();
            _database.CreateCollection("notcappedcollection");

            Assert.Equal(true, collection.Exists());
            Assert.Equal(false, collection.IsCapped());
        }

        [Fact]
        public void TestIsCappedTrue()
        {
            var collection = _database.GetCollection("cappedcollection");
            collection.Drop();
            var options = CollectionOptions.SetCapped(true).SetMaxSize(10000);
            _database.CreateCollection("cappedcollection", options);

            Assert.Equal(true, collection.Exists());
            Assert.Equal(true, collection.IsCapped());
        }

        [Fact]
        public void TestLenientRead()
        {
            var settings = new MongoCollectionSettings { ReadEncoding = Utf8Encodings.Lenient };
            var collection = _database.GetCollection(LegacyTestConfiguration.Collection.Name, settings);

            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "x", "abc" } };
            var bson = document.ToBson();
            bson[28] = 0xc0; // replace 'a' with invalid lone first code point (not followed by 10xxxxxx)

            // use a RawBsonDocument to sneak the invalid bytes into the database
            var rawBsonDocument = new RawBsonDocument(bson);
            collection.Insert(rawBsonDocument);

            var rehydrated = collection.FindOne(Query.EQ("_id", document["_id"]));
            Assert.Equal("\ufffd" + "bc", rehydrated["x"].AsString);
        }

        [Fact]
        public void TestLenientWrite()
        {
            var settings = new MongoCollectionSettings { WriteEncoding = Utf8Encodings.Lenient };
            var collection = _database.GetCollection(LegacyTestConfiguration.Collection.Name, settings);

            var document = new BsonDocument("x", "\udc00"); // invalid lone low surrogate
            var result = collection.Save(document);

            var expectedResult = new ExpectedWriteConcernResult();
            CheckExpectedResult(expectedResult, result);

            var rehydrated = collection.FindOne(Query.EQ("_id", document["_id"]));
            Assert.Equal("\ufffd", rehydrated["x"].AsString);
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

        [Fact]
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
                BypassDocumentValidation = true,
                MapFunction = map,
                ReduceFunction = reduce,
                OutputMode = MapReduceOutputMode.Replace,
                OutputCollectionName = "mrout"
            });

            Assert.True(result.Ok);
            Assert.True(result.Duration >= TimeSpan.Zero);
            Assert.Equal(9, result.EmitCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Equal(3, result.InputCount);
            result.CollectionName.Should().NotBeNullOrEmpty();

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
                Assert.Equal(expectedCounts[key], count);
            }

            // test GetResults
            foreach (var document in result.GetResults())
            {
                var key = document["_id"].AsString;
                var count = document["value"]["count"].ToInt32();
                Assert.Equal(expectedCounts[key], count);
            }

            // test GetResultsAs<>
            foreach (var document in result.GetResultsAs<TestMapReduceDocument>())
            {
                Assert.Equal(expectedCounts[document.Id], document.Value.Count);
            }
        }

        [Fact]
        public void TestMapReduceInline()
        {
            // this is Example 1 on p. 87 of MongoDB: The Definitive Guide
            // by Kristina Chodorow and Michael Dirolf

            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
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

                Assert.True(result.Ok);
                Assert.True(result.Duration >= TimeSpan.Zero);
                Assert.Equal(9, result.EmitCount);
                Assert.Equal(5, result.OutputCount);
                Assert.Equal(3, result.InputCount);
                result.CollectionName.Should().BeNullOrEmpty();

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
                    Assert.Equal(expectedCounts[key], count);
                }

                // test InlineResults as TestInlineResultDocument
                foreach (var document in result.GetInlineResultsAs<TestMapReduceDocument>())
                {
                    var key = document.Id;
                    var count = document.Value.Count;
                    Assert.Equal(expectedCounts[key], count);
                }

                // test GetResults
                foreach (var document in result.GetResults())
                {
                    var key = document["_id"].AsString;
                    var count = document["value"]["count"].ToInt32();
                    Assert.Equal(expectedCounts[key], count);
                }

                // test GetResultsAs<>
                foreach (var document in result.GetResultsAs<TestMapReduceDocument>())
                {
                    Assert.Equal(expectedCounts[document.Id], document.Value.Count);
                }
            }
        }

        [Fact]
        public void TestMapReduceInlineWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.RemoveAll();
                        _collection.Insert(new BsonDocument("x", 1)); // make sure collection has at least one document so map gets called

                        failpoint.SetAlwaysOn();
                        var args = new MapReduceArgs
                        {
                            MapFunction = "function() { }",
                            ReduceFunction = "function(key, value) { return 0; }",
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.MapReduce(args));
                    }
                }
            }
        }

        [Fact]
        public void TestMapReduceInlineWithQuery()
        {
            // this is Example 1 on p. 87 of MongoDB: The Definitive Guide
            // by Kristina Chodorow and Michael Dirolf

            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                _collection.Drop();
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

                Assert.True(result.Ok);
                Assert.True(result.Duration >= TimeSpan.Zero);
                Assert.Equal(9, result.EmitCount);
                Assert.Equal(5, result.OutputCount);
                Assert.Equal(3, result.InputCount);
                result.CollectionName.Should().BeNullOrEmpty();

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
                    Assert.Equal(expectedCounts[key], count);
                }

                // test InlineResults as TestInlineResultDocument
                foreach (var document in result.GetInlineResultsAs<TestMapReduceDocument>())
                {
                    var key = document.Id;
                    var count = document.Value.Count;
                    Assert.Equal(expectedCounts[key], count);
                }

                // test GetResults
                foreach (var document in result.GetResults())
                {
                    var key = document["_id"].AsString;
                    var count = document["value"]["count"].ToInt32();
                    Assert.Equal(expectedCounts[key], count);
                }

                // test GetResultsAs<>
                foreach (var document in result.GetResultsAs<TestMapReduceDocument>())
                {
                    Assert.Equal(expectedCounts[document.Id], document.Value.Count);
                }
            }
        }

        [SkippableFact]
        public void TestMapReduceWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "A", 1 }, { "B", 2 } });
            _collection.Insert(new BsonDocument { { "B", 1 }, { "C", 2 } });
            _collection.Insert(new BsonDocument { { "X", 1 }, { "B", 2 } });
            var writeConcern = new WriteConcern(9);
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
            var args = new MapReduceArgs
            {
                BypassDocumentValidation = true,
                MapFunction = map,
                ReduceFunction = reduce,
                OutputMode = MapReduceOutputMode.Replace,
                OutputCollectionName = "mrout"
            };

            var exception = Record.Exception(() => _collection.WithWriteConcern(writeConcern).MapReduce(args));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestParallelScan()
        {
            if (_primary.Supports(FeatureId.ParallelScanCommand))
            {
                var numberOfDocuments = 2000;
                var numberOfCursors = 3;

                _collection.Drop();
                for (int i = 0; i < numberOfDocuments; i++)
                {
                    _collection.Insert(new BsonDocument("_id", i));
                }

                var enumerators = _collection.ParallelScanAs(typeof(BsonDocument), new ParallelScanArgs
                {
                    BatchSize = 100,
                    NumberOfCursors = numberOfCursors
                });
                Assert.True(enumerators.Count >= 1);

                var ids = new List<int>();
                foreach (var enumerator in enumerators)
                {
                    while (enumerator.MoveNext())
                    {
                        var document = (BsonDocument)enumerator.Current;
                        var id = document["_id"].ToInt32();
                        ids.Add(id);
                    }
                }

                ids.Should().BeEquivalentTo(Enumerable.Range(0, numberOfDocuments));
            }
        }

        [Fact]
        public void TestReIndex()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument("x", 1));
                _collection.Insert(new BsonDocument("x", 2));
                _collection.DropAllIndexes();
                _collection.CreateIndex("x");
                // note: prior to 1.8.1 the reIndex command was returning duplicate ok elements
                try
                {
                    var result = _collection.ReIndex();
                    Assert.Equal(2, result.Response["nIndexes"].ToInt32());
                    Assert.Equal(2, result.Response["nIndexesWas"].ToInt32());
                }
                catch (InvalidOperationException ex)
                {
                    Assert.Equal("Duplicate element name 'ok'.", ex.Message);
                }
            }
        }

        [SkippableFact]
        public void TestReIndexWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            EnsureCollectionExists(_collection.Name);
            var writeConcern = new WriteConcern(9);

            var exception = Record.Exception(() => _collection.WithWriteConcern(writeConcern).ReIndex());

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestRemove()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            var result = _collection.Remove(Query.EQ("x", 1));

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 1
            };
            CheckExpectedResult(expectedResult, result);

            Assert.Equal(0, _collection.Count());
        }

        [Fact]
        public void TestRemoveNoMatchingDocument()
        {
            _collection.Drop();
            var result = _collection.Remove(Query.EQ("x", 1));

            var expectedResult = new ExpectedWriteConcernResult
            {
                UpdatedExisting = false
            };
            CheckExpectedResult(expectedResult, result);

            Assert.Equal(0, _collection.Count());
        }

        [Fact]
        public void TestRemoveUnacknowledeged()
        {
            using (_server.RequestStart())
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("x", 1));
                var result = _collection.Remove(Query.EQ("x", 1), WriteConcern.Unacknowledged);

                Assert.Equal(null, result);
                Assert.Equal(0, _collection.Count());
            }
        }

        [SkippableFact]
        public void TestRemoveWithWriteConcernError()
        {
            RequireServer.Check().Supports(Feature.WriteCommands).ClusterType(ClusterType.ReplicaSet);
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            var collectionSettings = new MongoCollectionSettings
            {
                WriteConcern = new WriteConcern(9)
            };
            var collection = _database.GetCollection(_collection.Name, collectionSettings);
            var query = Query.EQ("x", 1);

            Action action = () => collection.Remove(query);

            action.ShouldThrow<MongoWriteConcernException>();
            _collection.Count().Should().Be(0);
        }

        [Fact]
        public void TestSetFields()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.FindAll().SetFields("x").FirstOrDefault();
            Assert.Equal(2, result.ElementCount);
            Assert.Equal("_id", result.GetElement(0).Name);
            Assert.Equal("x", result.GetElement(1).Name);
        }

        [Fact]
        public void TestSetHint()
        {
            _collection.DropAllIndexes();
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            _collection.CreateIndex(IndexKeys.Ascending("x"));
            var query = Query.EQ("x", 1);
            var cursor = _collection.Find(query).SetHint(new BsonDocument("x", 1));
            var count = 0;
            foreach (var document in cursor)
            {
                Assert.Equal(1, ++count);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestSetHintByIndexName()
        {
            _collection.DropAllIndexes();
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            _collection.CreateIndex(IndexKeys.Ascending("x"), IndexOptions.SetName("xIndex"));
            var query = Query.EQ("x", 1);
            var cursor = _collection.Find(query).SetHint("xIndex");
            var count = 0;
            foreach (var document in cursor)
            {
                Assert.Equal(1, ++count);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestSortAndLimit()
        {
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "x", 4 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "y", 2 } });
            _collection.Insert(new BsonDocument { { "x", 1 }, { "y", 2 } });
            var result = _collection.FindAll().SetSortOrder("x").SetLimit(3).Select(x => x["x"].AsInt32).ToList();
            Assert.Equal(3, result.Count());
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void TestGetStats()
        {
            _collection.GetStats();
        }

        [SkippableFact]
        public void TestGetStatsNoPadding()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.0").ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            _collection.Drop();
            _database.CreateCollection(_collection.Name); // collMod command only works if collection exists

            var command = new CommandDocument
            {
                { "collMod", _collection.Name },
                { "noPadding", true }
            };
            _database.RunCommand(command);

            var stats = _collection.GetStats();
            Assert.True((stats.UserFlags & CollectionUserFlags.NoPadding) != 0);
        }

        [SkippableFact]
        public void TestGetStatsUsePowerOf2Sizes()
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            // SERVER-8409: only run this when talking to a non-mongos 2.2 server or >= 2.4.
            if ((_server.BuildInfo.Version >= new Version(2, 2, 0) && _primary.InstanceType != MongoServerInstanceType.ShardRouter)
                || _server.BuildInfo.Version >= new Version(2, 4, 0))
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
                Assert.True((stats.UserFlags & CollectionUserFlags.UsePowerOf2Sizes) != 0);
            }
        }

        [Fact]
        public void TestGetStatsWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime))
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.Drop();
                        _collection.Insert(new BsonDocument("x", 1)); // ensure collection is not empty

                        failpoint.SetAlwaysOn();
                        var args = new GetStatsArgs
                        {
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.GetStats(args));
                    }
                }
            }
        }

        [Fact]
        public void TestGetStatsWithScale()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1)); // ensure collection is not empty

            var stats1 = _collection.GetStats();
            var args = new GetStatsArgs { Scale = 2 };
            var stats2 = _collection.GetStats(args);
            Assert.Equal(stats1.DataSize / 2, stats2.DataSize);
        }

        [Fact]
        public void TestStrictRead()
        {
            var settings = new MongoCollectionSettings { ReadEncoding = Utf8Encodings.Strict };
            var collection = _database.GetCollection(LegacyTestConfiguration.Collection.Name, settings);

            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "x", "abc" } };
            var bson = document.ToBson();
            bson[28] = 0xc0; // replace 'a' with invalid lone first code point (not followed by 10xxxxxx)

            // use a RawBsonDocument to sneak the invalid bytes into the database
            var rawBsonDocument = new RawBsonDocument(bson);
            collection.Insert(rawBsonDocument);

            Assert.Throws<DecoderFallbackException>(() => collection.FindOne(Query.EQ("_id", document["_id"])));
        }

        [Fact]
        public void TestStrictWrite()
        {
            var settings = new MongoCollectionSettings { WriteEncoding = Utf8Encodings.Strict };
            var collection = _database.GetCollection(LegacyTestConfiguration.Collection.Name, settings);

            var document = new BsonDocument("x", "\udc00"); // invalid lone low surrogate
            Assert.Throws<EncoderFallbackException>(() => { collection.Insert(document); });
        }

        [Fact]
        public void TestUpdate()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            var options = new MongoUpdateOptions { BypassDocumentValidation = true };

            var result = _collection.Update(Query.EQ("x", 1), Update.Set("x", 2), options);

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 1,
                UpdatedExisting = true
            };
            CheckExpectedResult(expectedResult, result);

            var document = _collection.FindOne();
            Assert.Equal(2, document["x"].AsInt32);
            Assert.Equal(1, _collection.Count());
        }

        [Fact]
        public void TestUpdateNoMatchingDocument()
        {
            _collection.Drop();
            var result = _collection.Update(Query.EQ("x", 1), Update.Set("x", 2));

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 0
            };
            CheckExpectedResult(expectedResult, result);

            Assert.Equal(0, _collection.Count());
        }

        [Fact]
        public void TestUpdateEmptyQueryDocument()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            var result = _collection.Update(new QueryDocument(), Update.Set("x", 2));

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 1,
                UpdatedExisting = true
            };
            CheckExpectedResult(expectedResult, result);

            var document = _collection.FindOne();
            Assert.Equal(2, document["x"].AsInt32);
            Assert.Equal(2, _collection.Count());
        }

        [Fact]
        public void TestUpdateNullQuery()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 1));
            var result = _collection.Update(Query.Null, Update.Set("x", 2));

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 1,
                UpdatedExisting = true
            };
            CheckExpectedResult(expectedResult, result);

            var document = _collection.FindOne();
            Assert.Equal(2, document["x"].AsInt32);
            Assert.Equal(2, _collection.Count());
        }

        [Fact]
        public void TestUpdateUnacknowledged()
        {
            using (_server.RequestStart())
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("x", 1));
                var result = _collection.Update(Query.EQ("x", 1), Update.Set("x", 2), WriteConcern.Unacknowledged);

                Assert.Equal(null, result);

                var document = _collection.FindOne();
                Assert.Equal(2, document["x"].AsInt32);
                Assert.Equal(1, _collection.Count());
            }
        }

        [SkippableFact]
        public void TestUpdateWithWriteConcernError()
        {
            RequireServer.Check().Supports(Feature.WriteCommands).ClusterType(ClusterType.ReplicaSet);
            _collection.RemoveAll();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            var collectionSettings = new MongoCollectionSettings
            {
                WriteConcern = new WriteConcern(9)
            };
            var collection = _database.GetCollection(_collection.Name, collectionSettings);
            var query = Query.EQ("x", 1);
            var update = Update.Set("x", 2);

            Action action = () => collection.Update(query, update);

            action.ShouldThrow<MongoWriteConcernException>();
            _collection.FindOne().Should().Be("{ _id : 1, x : 2 }");
        }

        [Fact]
        public void TestUpsertExisting()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            var result = _collection.Update(Query.EQ("x", 1), Update.Set("x", 2), UpdateFlags.Upsert);

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 1,
                UpdatedExisting = true
            };
            CheckExpectedResult(expectedResult, result);

            var document = _collection.FindOne();
            Assert.Equal(2, document["x"].AsInt32);
            Assert.Equal(1, _collection.Count());
        }

        [Fact]
        public void TestUpsertInsert()
        {
            _collection.Drop();
            var id = new BsonObjectId(ObjectId.GenerateNewId());
            var result = _collection.Update(Query.EQ("_id", id), Update.Set("x", 2), UpdateFlags.Upsert);

            var expectedResult = new ExpectedWriteConcernResult
            {
                DocumentsAffected = 1,
                Upserted = id
            };
            CheckExpectedResult(expectedResult, result);

            var document = _collection.FindOne();
            Assert.Equal(2, document["x"].AsInt32);
            Assert.Equal(1, _collection.Count());
        }

        [Fact]
        public void TestUpsertDuplicateKey()
        {
            var collection = _database.GetCollection("duplicatekeys");
            collection.Drop();

            collection.Insert(new BsonDocument("_id", 1));

            Assert.Throws<MongoDuplicateKeyException>(() =>
            {
                var query = Query.And(Query.EQ("_id", 1), Query.EQ("x", 1));
                var update = Update.Set("x", 1);
                collection.Update(query, update, UpdateFlags.Upsert);
            });
        }

        [SkippableFact]
        public void TestValidate()
        {
            RequireServer.Check().StorageEngine("mmapv1");
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                // ensure collection exists
                _collection.Drop();
                _collection.Insert(new BsonDocument("x", 1));

                var result = _collection.Validate();
                Assert.Equal(_collection.FullName, result.Namespace);

                // just test that all the values can be extracted without throwing an exception, we don't know what the correct values should be
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

        [Fact]
        public void TestValidateWithFull()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                // ensure collection exists
                _collection.Drop();
                _collection.Insert(new BsonDocument("x", 1));

                var result = _collection.Validate(new ValidateCollectionArgs
                {
                    Full = true
                });

                Assert.Equal(_collection.FullName, result.Namespace);
            }
        }

        [Fact]
        public void TestValidateWithMaxTime()
        {
            if (_primary.Supports(FeatureId.MaxTime) && _primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                {
                    if (failpoint.IsSupported())
                    {
                        _collection.Drop();
                        _collection.Insert(new BsonDocument("x", 1)); // ensure collection is not empty

                        failpoint.SetAlwaysOn();
                        var args = new ValidateCollectionArgs
                        {
                            MaxTime = TimeSpan.FromMilliseconds(1)
                        };
                        Assert.Throws<MongoExecutionTimeoutException>(() => _collection.Validate(args));
                    }
                }
            }
        }

        [Fact]
        public void TestValidateWithScanData()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                // ensure collection exists
                _collection.Drop();
                _collection.Insert(new BsonDocument("x", 1));

                var result = _collection.Validate(new ValidateCollectionArgs
                {
                    ScanData = true
                });

                Assert.Equal(_collection.FullName, result.Namespace);
            }
        }

        [Fact]
        public void TestWithReadConcern()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = _collection.WithReadConcern(originalReadConcern);
            var newReadConcern = new ReadConcern(ReadConcernLevel.Majority);

            var result = subject.WithReadConcern(newReadConcern);

            subject.Settings.ReadConcern.Should().BeSameAs(originalReadConcern);
            result.Settings.ReadConcern.Should().BeSameAs(newReadConcern);
            result.WithReadConcern(originalReadConcern).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void TestWithReadPreference()
        {
            var originalReadPreference = new ReadPreference(ReadPreferenceMode.Secondary);
            var subject = _collection.WithReadPreference(originalReadPreference);
            var newReadPreference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

            var result = subject.WithReadPreference(newReadPreference);

            subject.Settings.ReadPreference.Should().BeSameAs(originalReadPreference);
            result.Settings.ReadPreference.Should().BeSameAs(newReadPreference);
            result.WithReadPreference(originalReadPreference).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void TestWithWriteConcern()
        {
            var originalWriteConcern = new WriteConcern(2);
            var subject = _collection.WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.Should().BeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.Should().BeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.Should().Be(subject.Settings);
        }

        // private methods
        private void CheckExpectedResult(ExpectedWriteConcernResult expectedResult, WriteConcernResult result)
        {
            Assert.Equal(expectedResult.DocumentsAffected ?? 0, result.DocumentsAffected);
            Assert.Equal(expectedResult.HasLastErrorMessage ?? false, result.HasLastErrorMessage);
            if (expectedResult.LastErrorMessage != null)
            {
                Assert.Equal(expectedResult.LastErrorMessage, result.LastErrorMessage);
            }
            Assert.Equal(expectedResult.Upserted, result.Upserted);
            Assert.Equal(expectedResult.UpdatedExisting ?? false, result.UpdatedExisting);
        }

        private void DropCollection(string collectionName)
        {
            _database.DropCollection(collectionName);
        }

        private void EnsureCollectionExists(string collectionName)
        {
            _database.DropCollection(collectionName);
            _database.CreateCollection(collectionName);
        }

        // nested types
        private class ExpectedWriteConcernResult
        {
            // private fields
            private int? _documentsAffected;
            private bool? _hasLastErrorMessage;
            private string _lastErrorMessage;
            private BsonValue _upserted;
            private bool? _updatedExisting;

            // public properties
            public int? DocumentsAffected
            {
                get { return _documentsAffected; }
                set { _documentsAffected = value; }
            }

            public bool? HasLastErrorMessage
            {
                get { return _hasLastErrorMessage; }
                set { _hasLastErrorMessage = value; }
            }

            public string LastErrorMessage
            {
                get { return _lastErrorMessage; }
                set { _lastErrorMessage = value; }
            }

            public BsonValue Upserted
            {
                get { return _upserted; }
                set { _upserted = value; }
            }

            public bool? UpdatedExisting
            {
                get { return _updatedExisting; }
                set { _updatedExisting = value; }
            }
        }
    }
}
