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
    [WarmupCount(2)]
    [IterationCount(4)]
    [BenchmarkCategory(DriverBenchmarkCategory.ParallelBench, DriverBenchmarkCategory.WriteBench, DriverBenchmarkCategory.DriverBench)]
    public class MultiFileImportBenchmark
    {
        private DisposableMongoClient _client;
        private IMongoCollection<BsonDocument> _collection;
        private IMongoDatabase _database;

        [Params(565000000)]
        public int BenchmarkDataSetSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateDisposableClient();
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
            ThreadingUtilities.ExecuteOnNewThreads(16, threadNumber =>
            {
                var numFilesToImport = threadNumber == 15 ? 10 : 6;
                var startingFileNumber = threadNumber * 6;
                for (int i = 0; i < numFilesToImport; i++)
                {
                    var resourcePath = $"{DataFolderPath}parallel/ldjson_multi/ldjson{(startingFileNumber+i):D3}.txt";
                    var documents = new List<BsonDocument>(5000);
                    documents.AddRange(File.ReadLines(resourcePath).Select(BsonDocument.Parse));
                    _collection.InsertMany(documents);
                }
            }, 100000);
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.Dispose();
        }
    }
}
