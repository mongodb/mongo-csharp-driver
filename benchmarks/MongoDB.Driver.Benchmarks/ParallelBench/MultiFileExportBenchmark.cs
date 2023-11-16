/* Copyright 2021-present MongoDB Inc.
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

using System;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace MongoDB.Benchmarks.ParallelBench
{
    [WarmupCount(3)]
    [IterationCount(5)]
    [BenchmarkCategory(DriverBenchmarkCategory.ParallelBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class MultiFileExportBenchmark
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        private DirectoryInfo _tmpDirectory;
        private IMongoCollection<BsonDocument> _collection;

        [GlobalSetup]
        public void Setup()
        {
            string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
            _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
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

                using (StreamWriter streamWriter = File.CreateText(filepath))
                {
                    foreach (var document in documents)
                    {
                        streamWriter.WriteLine(document.ToJson());
                    }
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
}
