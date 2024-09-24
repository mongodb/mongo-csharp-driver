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

using System.Collections.Concurrent;
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
        private IMongoClient _client;
        private IMongoCollection<BsonDocument> _collection;
        private IMongoDatabase _database;
        private string _tmpDirectoryPath;
        private ConcurrentQueue<(string, int)> _filesToDownload;

        [Params(565_000_000)]
        public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateClient();
            _database = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName);
            _collection = _database.GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
            _tmpDirectoryPath = $"{DataFolderPath}parallel/tmpLDJSON";
            _filesToDownload = new ConcurrentQueue<(string, int)>();

            PopulateCollection();
        }

        [IterationSetup]
        public void BeforeTask()
        {
            CreateEmptyDirectory(_tmpDirectoryPath);
            AddFilesToQueue(_filesToDownload, _tmpDirectoryPath, "ldjson", 100);
        }

        [Benchmark]
        public void MultiFileExport()
        {
            ThreadingUtilities.ExecuteOnNewThreads(16, _ =>
            {
                while (_filesToDownload.TryDequeue(out var fileToDownloadInfo))
                {
                    var (filePath, fileNumber) = fileToDownloadInfo;
                    var documents = _collection.Find(Builders<BsonDocument>.Filter.Empty).Skip(fileNumber * 5000).Limit(5000).ToList();

                    using (var streamWriter = File.CreateText(filePath))
                    {
                        foreach (var document in documents)
                        {
                            streamWriter.WriteLine(document.ToJson());
                        }
                    }
                }
            }, 100_000);
        }

        [GlobalCleanup]
        public void Teardown()
        {
            Directory.Delete(_tmpDirectoryPath, true);
            _client.Dispose();
        }

        private void PopulateCollection()
        {
            for (int i = 0; i < 100; i++)
            {
                var resourcePath = $"{DataFolderPath}parallel/ldjson_multi/ldjson{i:D3}.txt";
                var documents = File.ReadLines(resourcePath).Select(BsonDocument.Parse).ToArray();
                _collection.InsertMany(documents);
            }
        }
    }
}
