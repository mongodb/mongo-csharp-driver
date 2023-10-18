using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.Multi_Doc;


[BenchmarkCategory("MultiBench", "ReadBench", "DriverBench")]
public class FindManyBenchmark
{
    private MongoClient _client;
    private IMongoCollection<BsonDocument> _collection;
    private BsonDocument _tweetDocument;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        _tweetDocument = ReadExtendedJson("../../../../../../../data/single_and_multi_document/tweet.json");
        _collection = _client.GetDatabase("perftest").GetCollection<BsonDocument>("corpus");
        PopulateCollection();
    }

    [Benchmark]
    public void FindManyAndEmptyCursor()
    {
        _collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }

    private void PopulateCollection()
    {
        var documents = new List<BsonDocument>();
        for (int i = 0; i < 10000; i++)
        {
            var documentCopy = _tweetDocument.DeepClone().AsBsonDocument;
            documents.Add(documentCopy);
        }
        _collection.InsertMany(documents);
    }
}
