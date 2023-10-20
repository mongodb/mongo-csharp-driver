using System;
using MongoDB.Bson;
using MongoDB.Driver;
using BenchmarkDotNet.Attributes;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.Single_Doc;

[IterationCount(100)]
[BenchmarkCategory("SingleBench", "WriteBench", "DriverBench")]
public class InsertOneLargeBenchmark
{
    private MongoClient _client;
    private IMongoDatabase _database;
    private BsonDocument _largeDocument;
    private IMongoCollection<BsonDocument> _collection;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        _largeDocument = ReadExtendedJson("../../../../../../../data/single_and_multi_document/large_doc.json");
        _database = _client.GetDatabase("perftest");
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _database.DropCollection("corpus");
        _collection = _database.GetCollection<BsonDocument>("corpus");
    }

    [Benchmark]
    public void InsertOneLarge()
    {
        for (int i = 0; i < 10; i++)
        {
            _largeDocument.Remove("_id");
            _collection.InsertOne(_largeDocument);
        }
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }
}
