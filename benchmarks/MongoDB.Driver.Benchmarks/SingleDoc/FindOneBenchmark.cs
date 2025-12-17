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
using MongoDB.Benchmarks.MultiDoc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.SingleDoc;

[IterationTime(3000)]
[BenchmarkCategory(DriverBenchmarkCategory.SingleBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
public class FindOneBenchmark
{
    private const int Iterations = 10_000;

    private IMongoClient _client;
    private IMongoCollection<BsonDocument> _collection;
    private IMongoCollection<Tweet> _collectionPoco;
    private BsonDocument _tweetDocument;
    private Tweet _tweetDocumentPoco;

    [Params(16_220_000)]
    public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

    [GlobalSetup]
    public void Setup()
    {
        _client = MongoConfiguration.CreateClient();
        var db = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName);
        db.DropCollection(MongoConfiguration.PerfTestCollectionName);
        _collection = db.GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
        _collectionPoco = db.GetCollection<Tweet>(MongoConfiguration.PerfTestCollectionName);
        _tweetDocument = ReadExtendedJson("single_and_multi_document/tweet.json");
        _tweetDocumentPoco = BsonSerializer.Deserialize<Tweet>(_tweetDocument);

        PopulateCollection();
    }

    [Benchmark]
    public void FindOne()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _ = _collection.Find(new BsonDocument("_id", i)).First();
        }
    }

    [Benchmark]
    public void FindOnePoco()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _ = _collectionPoco.Find(t => t.Id == i).First();
        }
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.Dispose();
    }

    private void PopulateCollection()
    {
        var documents = Enumerable.Range(0, Iterations)
            .Select(i => _tweetDocument.DeepClone().AsBsonDocument.Add("_id", i));
        _collection.InsertMany(documents);
    }
}
