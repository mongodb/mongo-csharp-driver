using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace benchmarks.ParallelBench;

[BenchmarkCategory("ParallelBench", "ReadBench", "DriverBench")]
public class MultiFileExportBenchmark
{
    private MongoClient _client;
    private IMongoDatabase _database;
    private DirectoryInfo _tmpDirectory;
    private IMongoCollection<BsonDocument> _collection;

    [GlobalSetup]
    public void Setup()
    {
        _client = new MongoClient();
        _client.DropDatabase("perftest");
        _database = _client.GetDatabase("perftest");
        _database.DropCollection("corpus");
        _collection = _database.GetCollection<BsonDocument>("corpus");
        _tmpDirectory = Directory.CreateDirectory("../../../../../../../data/parallel/tmpLDJSON");

        PopulateCollection();
    }

    [IterationSetup]
    public void BeforeTask()
    {
        ClearDirectory();
    }

    [Benchmark]
    public void MultiFileExport()
    {
        Task[] tasks = new Task[100];
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Factory.StartNew(ExportFile(i));
        }
        Task.WaitAll(tasks);
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
        ClearDirectory();
        _tmpDirectory.Delete();
    }

    private void ClearDirectory()
    {
        foreach (var file in _tmpDirectory.EnumerateFiles())
        {
            file.Delete();
        }
    }

    private Action ExportFile(int fileNumber)
    {
        return () =>
        {
            string filepath = $"../../../../../../../data/parallel/tmpLDJSON/ldjson{fileNumber:D3}.txt";
            var documents = _collection.Find(Builders<BsonDocument>.Filter.Empty).Skip(fileNumber * 5000).Limit(5000).ToList();

            using StreamWriter streamWriter = File.CreateText(filepath);
            foreach (var document in documents)
            {
                streamWriter.WriteLine(document.ToJson());
            }
        };
    }

    private void PopulateCollection()
    {
        for (int i = 0; i < 100; i++)
        {
            string resourcePath = $"../../../../../../../data/parallel/ldjson_multi/ldjson{i:D3}.txt";
            var documents = new List<BsonDocument>(5000);
            documents.AddRange(File.ReadLines(resourcePath).Select(BsonDocument.Parse));
            _collection.InsertMany(documents);
        }
    }

}
