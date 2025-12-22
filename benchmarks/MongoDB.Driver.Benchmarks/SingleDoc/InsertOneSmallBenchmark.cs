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

using BenchmarkDotNet.Attributes;
using MongoDB.Benchmarks.MultiDoc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.SingleDoc;

[IterationCount(100)]
[BenchmarkCategory(DriverBenchmarkCategory.SingleBench, DriverBenchmarkCategory.WriteBench, DriverBenchmarkCategory.DriverBench)]
public class InsertOneSmallBenchmark
{
    private const int Iterations = 10_000;

    private IMongoClient _client;
    private IMongoCollection<BsonDocument> _collection;
    private IMongoCollection<SmallDocPoco> _collectionPoco;
    private IMongoDatabase _database;
    private BsonDocument _smallDocument;
    private SmallDocPoco _smallDocumentPoco;

    [Params(2_750_000)]
    public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

    [GlobalSetup]
    public void Setup()
    {
        _client = MongoConfiguration.CreateClient();
        _database = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName);
        _smallDocument = ReadExtendedJson("single_and_multi_document/small_doc.json");
        _smallDocumentPoco = BsonSerializer.Deserialize<SmallDocPoco>(_smallDocument);
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _database.DropCollection(MongoConfiguration.PerfTestCollectionName);
        _collection = _database.GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
        _collectionPoco = _database.GetCollection<SmallDocPoco>(MongoConfiguration.PerfTestCollectionName);
    }

    [Benchmark]
    public void InsertOneSmall()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _smallDocument.Remove("_id");
            _collection.InsertOne(_smallDocument);
        }
    }

    [Benchmark]
    public void InsertOneSmallPoco()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _collectionPoco.InsertOne(_smallDocumentPoco);
        }
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.Dispose();
    }
}
