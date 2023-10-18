using System;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace benchmarks.Single_Doc;

public class RunCommandBenchmark
{
    private MongoClient _client;
    private IMongoDatabase _database;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _database = _client.GetDatabase("admin");
    }

    [Benchmark]
    public BsonDocument RunCommand()
    {
        BsonDocument result = null;
        for (int i = 0; i < 10000; i++)
        {
            result = _database.RunCommand<BsonDocument>(new BsonDocument("hello", true));
        }
        return result;
    }
}
