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

using System.Linq;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.TestHelpers;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.MultiDoc
{
    [IterationTime(2000)]
    [BenchmarkCategory(DriverBenchmarkCategory.MultiBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class FindManyBenchmark
    {
        private IMongoClient _client;
        private IMongoCollection<BsonDocument> _collection;
        private BsonDocument _tweetDocument;

        [Params(16_220_000)]
        public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateClient();
            _tweetDocument = ReadExtendedJson("single_and_multi_document/tweet.json");
            _collection = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName).GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);

            PopulateCollection();
        }

        [Benchmark]
        public void FindManyAndEmptyCursor()
        {
            _collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.Dispose();
        }

        private void PopulateCollection()
        {
            var documents = Enumerable.Range(0, 10000).Select(_ => _tweetDocument.DeepClone().AsBsonDocument);
            _collection.InsertMany(documents);
        }
    }
}
