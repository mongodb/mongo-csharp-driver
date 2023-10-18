using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.Multi_Doc;


[BenchmarkCategory("MultiBench", "WriteBench", "DriverBench")]
public class InsertManySmallBenchmark
{
    private MongoClient _client;
    private IMongoDatabase _database;
    private List<BsonDocument> _smallDocuments;
    private IMongoCollection<BsonDocument> _collection;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        var smallDocument = ReadExtendedJson("../../../../../../../data/single_and_multi_document/small_doc.json");
        _database = _client.GetDatabase("perftest");

        _smallDocuments = new List<BsonDocument>();
        for (int i = 0; i < 10000; i++)
        {
            var documentCopy = smallDocument.DeepClone().AsBsonDocument;
            _smallDocuments.Add(documentCopy);
        }
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _database.DropCollection("corpus");
        _collection = _database.GetCollection<BsonDocument>("corpus");
    }

    [Benchmark]
    public void InsertManySmall()
    {
        _collection.InsertMany(_smallDocuments, new InsertManyOptions());
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }
}
