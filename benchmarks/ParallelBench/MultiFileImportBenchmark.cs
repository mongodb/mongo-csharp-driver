using System;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace benchmarks.ParallelBench
{
    [WarmupCount(2)]
    [IterationCount(4)]
    [BenchmarkCategory("ParallelBench", "WriteBench", "DriverBench")]
    public class MultiFileImportBenchmark
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;

        [GlobalSetup]
        public void Setup()
        {
            string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
            _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
            _client.DropDatabase("perftest");
            _database = _client.GetDatabase("perftest");
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
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.DropDatabase("perftest");
        }

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
}
