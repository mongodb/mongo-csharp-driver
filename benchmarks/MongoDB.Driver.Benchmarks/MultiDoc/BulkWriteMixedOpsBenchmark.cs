/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.MultiDoc;

[IterationCount(15)]
[BenchmarkCategory(DriverBenchmarkCategory.BulkWriteBench, DriverBenchmarkCategory.MultiBench, DriverBenchmarkCategory.WriteBench, DriverBenchmarkCategory.DriverBench)]
public class BulkWriteMixedOpsBenchmark
{
    private IMongoClient _client;
    private IMongoCollection<BsonDocument> _collection;
    private IMongoCollection<SmallDocPoco> _collectionPoco;
    private IMongoDatabase _database;
    private readonly List<BulkWriteModel> _clientBulkWriteMixedOpsModels = [];
    private readonly List<BulkWriteModel> _clientBulkWriteMixedOpsPocoModels = [];
    private readonly List<WriteModel<BsonDocument>> _collectionBulkWriteMixedOpsModels = [];
    private readonly List<WriteModel<SmallDocPoco>> _collectionBulkWriteMixedOpsPocoModels = [];

    private static readonly string[] __collectionNamespaces = Enumerable.Range(0, 10)
        .Select(i => $"{MongoConfiguration.PerfTestDatabaseName}.{MongoConfiguration.PerfTestCollectionName}_{i}")
        .ToArray();

    [Params(5_500_000)]
    public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

    [GlobalSetup]
    public void Setup()
    {
        _client = MongoConfiguration.CreateClient();

        var smallDocument = ReadExtendedJson("single_and_multi_document/small_doc.json");
        for (var i = 0; i < 10000; i++)
        {
            var collectionName = __collectionNamespaces[i % __collectionNamespaces.Length];

            _clientBulkWriteMixedOpsModels.Add(new BulkWriteInsertOneModel<BsonDocument>(collectionName, smallDocument.DeepClone().AsBsonDocument));
            _clientBulkWriteMixedOpsModels.Add(new BulkWriteReplaceOneModel<BsonDocument>(collectionName, FilterDefinition<BsonDocument>.Empty, smallDocument.DeepClone().AsBsonDocument));
            _clientBulkWriteMixedOpsModels.Add(new BulkWriteDeleteOneModel<BsonDocument>(collectionName, FilterDefinition<BsonDocument>.Empty));

            _collectionBulkWriteMixedOpsModels.Add(new InsertOneModel<BsonDocument>(smallDocument.DeepClone().AsBsonDocument));
            _collectionBulkWriteMixedOpsModels.Add(new ReplaceOneModel<BsonDocument>(FilterDefinition<BsonDocument>.Empty, smallDocument.DeepClone().AsBsonDocument));
            _collectionBulkWriteMixedOpsModels.Add(new DeleteOneModel<BsonDocument>(FilterDefinition<BsonDocument>.Empty));

            _clientBulkWriteMixedOpsPocoModels.Add(new BulkWriteInsertOneModel<SmallDocPoco>(collectionName, BsonSerializer.Deserialize<SmallDocPoco>(smallDocument)));
            _clientBulkWriteMixedOpsPocoModels.Add(new BulkWriteReplaceOneModel<SmallDocPoco>(collectionName, FilterDefinition<SmallDocPoco>.Empty, BsonSerializer.Deserialize<SmallDocPoco>(smallDocument)));
            _clientBulkWriteMixedOpsPocoModels.Add(new BulkWriteDeleteOneModel<SmallDocPoco>(collectionName, FilterDefinition<SmallDocPoco>.Empty));

            _collectionBulkWriteMixedOpsPocoModels.Add(new InsertOneModel<SmallDocPoco>(BsonSerializer.Deserialize<SmallDocPoco>(smallDocument)));
            _collectionBulkWriteMixedOpsPocoModels.Add(new ReplaceOneModel<SmallDocPoco>(FilterDefinition<SmallDocPoco>.Empty, BsonSerializer.Deserialize<SmallDocPoco>(smallDocument)));
            _collectionBulkWriteMixedOpsPocoModels.Add(new DeleteOneModel<SmallDocPoco>(FilterDefinition<SmallDocPoco>.Empty));
        }
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _client.DropDatabase(MongoConfiguration.PerfTestDatabaseName);

        _database = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName);
        foreach (var collectionName in __collectionNamespaces)
        {
            _database.CreateCollection(collectionName.Split('.')[1]);
        }

        _collection = _database.GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
        _collectionPoco = _database.GetCollection<SmallDocPoco>(MongoConfiguration.PerfTestCollectionName);
    }

    [Benchmark]
    public void SmallDocCollectionBulkWriteMixedOpsBenchmark()
    {
        _collection.BulkWrite(_collectionBulkWriteMixedOpsModels, new());
    }

    [Benchmark]
    public void SmallDocCollectionBulkWriteMixedOpsPocoBenchmark()
    {
        _collectionPoco.BulkWrite(_collectionBulkWriteMixedOpsPocoModels, new());
    }

    [Benchmark]
    public void SmallDocClientBulkWriteMixedOpsBenchmark()
    {
        _client.BulkWrite(_clientBulkWriteMixedOpsModels, new());
    }

    [Benchmark]
    public void SmallDocClientBulkWriteMixedOpsPocoBenchmark()
    {
        _client.BulkWrite(_clientBulkWriteMixedOpsPocoModels, new());
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.Dispose();
    }
}
