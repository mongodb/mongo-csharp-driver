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
using MongoDB.Bson;
using MongoDB.Driver;
using BenchmarkDotNet.Attributes;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.SingleDoc
{
    [IterationCount(100)]
    [BenchmarkCategory(DriverBenchmarkCategory.SingleBench, DriverBenchmarkCategory.WriteBench, DriverBenchmarkCategory.DriverBench)]
    public class InsertOneLargeBenchmark
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        private BsonDocument _largeDocument;
        private IMongoCollection<BsonDocument> _collection;

        [GlobalSetup]
        public void Setup()
        {
            string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
            _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
            _client.DropDatabase("perftest");
            _largeDocument = ReadExtendedJson("../../../../../../../data/single_and_multi_document/large_doc.json");
            _database = _client.GetDatabase("perftest");
        }

        [IterationSetup]
        public void BeforeTask()
        {
            _database.DropCollection("corpus");
            _collection = _database.GetCollection<BsonDocument>("corpus");
        }

        [Benchmark]
        public void InsertOneLarge()
        {
            for (int i = 0; i < 10; i++)
            {
                _largeDocument.Remove("_id");
                _collection.InsertOne(_largeDocument);
            }
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.DropDatabase("perftest");
        }
    }
}
