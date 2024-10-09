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

using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.TestHelpers;

namespace MongoDB.Benchmarks.SingleDoc
{
    [IterationTime(3000)]
    [BenchmarkCategory("RunBench")]
    public class RunCommandBenchmark
    {
        private IMongoClient _client;
        private IMongoDatabase _database;

        [Params(130_000)]
        public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

        [GlobalSetup]
        public void Setup()
        {
            _client = BenchmarkHelper.MongoConfiguration.CreateClient();
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

        [GlobalCleanup]
        public void Teardown()
        {
            _client.Dispose();
        }
    }
}
