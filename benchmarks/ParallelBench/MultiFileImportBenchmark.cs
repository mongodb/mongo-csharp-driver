using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace benchmarks.ParallelBench;

[IterationCount(2)]
[WarmupCount(1)]
[BenchmarkCategory("ParallelBench", "WriteBench", "DriverBench")]
public class MultiFileImportBenchmark
{
    private MongoClient _client;
    private IMongoDatabase _database;
    // private Action[] _parallelInserts;
    private IMongoCollection<BsonDocument> _collection;
    // private BenchmarkThreadHelper _threadHelper;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        _database = _client.GetDatabase("perftest");
        // _threadHelper = new BenchmarkThreadHelper();
        // _parallelInserts = new Action[100];
        // CreateTasks();
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _database.DropCollection("corpus");
        _collection = _database.GetCollection<BsonDocument>("corpus");
    }

    [Benchmark]
    public void MultiFileImport()
    {
        Task[] tasks = new Task[100];
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Factory.StartNew(ImportFile(i));
        }
        Task.WaitAll(tasks);
        // _threadHelper.ExecuteAndWait();
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }

    // private void CreateTasks()
    // {
    //     for (int i = 0; i < 100; i++)
    //     {
    //         // _parallelInserts[i] = ImportFile(i);
    //         _threadHelper.Add(ImportFile(i));
    //     }
    // }

    private Action ImportFile(int fileNumber)
    {
        return () =>
        {
            string resourcePath = $"../../../../../../../data/parallel/ldjson_multi/ldjson{fileNumber:D3}.txt";
            var documents = new List<BsonDocument>(5000);
            documents.AddRange(File.ReadLines(resourcePath).Select(BsonDocument.Parse));
            _collection.InsertMany(documents);
        };
    }
}
