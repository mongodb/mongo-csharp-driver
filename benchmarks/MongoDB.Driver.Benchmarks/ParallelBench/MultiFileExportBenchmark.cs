/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver;
using MongoDB.Driver.TestHelpers;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.ParallelBench
{
    [WarmupCount(3)]
    [IterationCount(5)]
    [BenchmarkCategory(DriverBenchmarkCategory.ParallelBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class MultiFileExportBenchmark
    {
        private DisposableMongoClient _client;
        private IMongoCollection<BsonDocument> _collection;
        private IMongoDatabase _database;
        private DirectoryInfo _tmpDirectory;

        [Params(565000000)]
        public int BenchmarkDataSetSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateDisposableClient();
            _database = _client.GetDatabase("perftest");
            _collection = _database.GetCollection<BsonDocument>("corpus");
            _tmpDirectory = Directory.CreateDirectory($"{DataFolderPath}parallel/tmpLDJSON");

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
            ThreadingUtilities.ExecuteOnNewThreads(16, threadNumber =>
            {
                var numFilesToExport = threadNumber == 15 ? 10 : 6;
                var startingFileNumber = threadNumber * 6;
                for (int i = 0; i < numFilesToExport; i++)
                {
                    var filepath = $"{DataFolderPath}parallel/tmpLDJSON/ldjson{(startingFileNumber+i):D3}.txt";
                    var documents = _collection.Find(Builders<BsonDocument>.Filter.Empty).Skip((startingFileNumber+i) * 5000).Limit(5000).ToList();

                    using (StreamWriter streamWriter = File.CreateText(filepath))
                    {
                        foreach (var document in documents)
                        {
                            streamWriter.WriteLine(document.ToJson());
                        }
                    }
                }
            }, 100000);
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _tmpDirectory.Delete(true);
            _client.Dispose();
        }

        private void ClearDirectory()
        {
            foreach (var file in _tmpDirectory.EnumerateFiles())
            {
                file.Delete();
            }
        }

        private void PopulateCollection()
        {
            for (int i = 0; i < 100; i++)
            {
                var resourcePath = $"{DataFolderPath}parallel/ldjson_multi/ldjson{i:D3}.txt";
                var documents = new List<BsonDocument>(5000);
                documents.AddRange(File.ReadLines(resourcePath).Select(BsonDocument.Parse));
                _collection.InsertMany(documents);
            }
        }
    }
}
