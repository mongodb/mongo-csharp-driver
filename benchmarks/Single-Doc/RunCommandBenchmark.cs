using System;
using MongoDB.Bson;
using MongoDB.Driver;
using BenchmarkDotNet.Attributes;

namespace benchmarks.Single_Doc;

[IterationTime(3000)]
public class RunCommandBenchmark
{
    private MongoClient _client;
    private IMongoDatabase _database;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _database = _client.GetDatabase("admin");
    }

    [Benchmark]
    public void RunCommand()
    {
        for (int i = 0; i < 10000; i++)
        {
            _database.RunCommand<BsonDocument>(new BsonDocument("hello", true));
        }
    }
}
