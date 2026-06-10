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

using System.Linq;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.MultiDoc;

[IterationCount(100)]
[BenchmarkCategory(DriverBenchmarkCategory.BulkWriteBench, DriverBenchmarkCategory.MultiBench, DriverBenchmarkCategory.WriteBench, DriverBenchmarkCategory.DriverBench)]
public class SmallDocBulkInsertBenchmark
{
    private IMongoClient _client;
    private IMongoCollection<BsonDocument> _collection;
    private IMongoCollection<SmallDocPoco> _collectionPocos;
    private IMongoDatabase _database;
    private BsonDocument[] _smallDocuments;
    private SmallDocPoco[] _smallPocos;
    private InsertOneModel<BsonDocument>[] _collectionBulkWriteInsertModels;
    private InsertOneModel<SmallDocPoco>[] _collectionBulkWriteInsertModelsPoco;
    private BulkWriteInsertOneModel<BsonDocument>[] _clientBulkWriteInsertModels;
    private BulkWriteInsertOneModel<SmallDocPoco>[] _clientBulkWriteInsertModelsPoco;

    private static readonly CollectionNamespace __collectionNamespace =
        CollectionNamespace.FromFullName($"{MongoConfiguration.PerfTestDatabaseName}.{MongoConfiguration.PerfTestCollectionName}");

    [Params(2_750_000)]
    public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

    [GlobalSetup]
    public void Setup()
    {
        _client = MongoConfiguration.CreateClient();
        _database = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName);

        var smallDocument = ReadExtendedJson("single_and_multi_document/small_doc.json");
        _smallDocuments = Enumerable.Range(0, 10000).Select(_ => smallDocument.DeepClone().AsBsonDocument).ToArray();
        _smallPocos = _smallDocuments.Select(d => BsonSerializer.Deserialize<SmallDocPoco>(d)).ToArray();

        _collectionBulkWriteInsertModels = _smallDocuments.Select(x => new InsertOneModel<BsonDocument>(x.DeepClone().AsBsonDocument)).ToArray();
        _collectionBulkWriteInsertModelsPoco = _smallPocos.Select(x => new InsertOneModel<SmallDocPoco>(x)).ToArray();
        _clientBulkWriteInsertModels = _smallDocuments.Select(x => new BulkWriteInsertOneModel<BsonDocument>(__collectionNamespace, x.DeepClone().AsBsonDocument)).ToArray();
        _clientBulkWriteInsertModelsPoco = _smallPocos.Select(x => new BulkWriteInsertOneModel<SmallDocPoco>(__collectionNamespace, x)).ToArray();
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _database.DropCollection(MongoConfiguration.PerfTestCollectionName);
        _collection = _database.GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
        _collectionPocos = _database.GetCollection<SmallDocPoco>(MongoConfiguration.PerfTestCollectionName);
    }

    [Benchmark]
    public void InsertManySmallBenchmark()
    {
        _collection.InsertMany(_smallDocuments, new());
    }

    [Benchmark]
    public void InsertManySmallPocoBenchmark()
    {
        _collectionPocos.InsertMany(_smallPocos, new());
    }

    [Benchmark]
    public void SmallDocCollectionBulkWriteInsertBenchmark()
    {
        _ = _collection.BulkWrite(_collectionBulkWriteInsertModels, new());
    }

    [Benchmark]
    public void SmallDocCollectionBulkWriteInsertPocoBenchmark()
    {
        _ = _collectionPocos.BulkWrite(_collectionBulkWriteInsertModelsPoco, new());
    }

    [Benchmark]
    public void SmallDocClientBulkWriteInsertBenchmark()
    {
        _ = _client.BulkWrite(_clientBulkWriteInsertModels, new());
    }

    [Benchmark]
    public void SmallDocClientBulkWriteInsertPocoBenchmark()
    {
        _ = _client.BulkWrite(_clientBulkWriteInsertModelsPoco, new());
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.Dispose();
    }
}
