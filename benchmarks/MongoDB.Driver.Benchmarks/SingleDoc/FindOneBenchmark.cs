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

namespace MongoDB.Benchmarks.SingleDoc
{
    [IterationTime(3000)]
    [BenchmarkCategory(DriverBenchmarkCategory.SingleBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class FindOneBenchmark
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
            _collection = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName).GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
            _tweetDocument = ReadExtendedJson("single_and_multi_document/tweet.json");

            PopulateCollection();
        }

        [Benchmark]
        public void FindOne()
        {
            for (int i = 0; i < 10000; i++)
            {
                _collection.Find(new BsonDocument("_id", i)).First();
            }
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.Dispose();
        }

        private void PopulateCollection()
        {
            var documents = Enumerable.Range(0, 10000)
                .Select(i => _tweetDocument.DeepClone().AsBsonDocument.Add("_id", i));
            _collection.InsertMany(documents);
        }
    }
}
