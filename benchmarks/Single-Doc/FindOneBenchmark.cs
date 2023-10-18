using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.Single_Doc;

[BenchmarkCategory("SingleBench", "ReadBench", "DriverBench")]
public class FindOneBenchmark
{
    private MongoClient _client;
    private BsonDocument _tweetDocument;
    private IMongoCollection<BsonDocument> _collection;

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
    public BsonDocument FindOne()
    {
        BsonDocument retrievedDocument = null;
        for (int i = 0; i < 10000; i++)
        {
            retrievedDocument = _collection.Find(new BsonDocument("_id", i)).First();
        }
        return retrievedDocument;
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
            documentCopy.Add("_id", i);
            documents.Add(documentCopy);
        }
        _collection.InsertMany(documents);
    }
}
