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
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.MultiDoc
{
    [IterationCount(15)]
    [BenchmarkCategory(DriverBenchmarkCategory.BulkWriteBench, DriverBenchmarkCategory.MultiBench, DriverBenchmarkCategory.WriteBench, DriverBenchmarkCategory.DriverBench)]
    public class BulkWriteMixedOpsBenchmark
    {
        private IMongoClient _client;
        private IMongoCollection<BsonDocument> _collection;
        private IMongoDatabase _database;
        private readonly List<BulkWriteModel> _clientBulkWriteMixedOpsModels = [];
        private readonly List<WriteModel<BsonDocument>> _collectionBulkWriteMixedOpsModels = [];

        private static readonly CollectionNamespace __collectionNamespace =
            CollectionNamespace.FromFullName($"{MongoConfiguration.PerfTestDatabaseName}.{MongoConfiguration.PerfTestCollectionName}");

        [Params(5_500_000)]
        public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateClient();
            _database = _client.GetDatabase(MongoConfiguration.PerfTestDatabaseName);

            var smallDocument = ReadExtendedJson("single_and_multi_document/small_doc.json");
            for (var i = 0; i < 10000; i++)
            {
                _clientBulkWriteMixedOpsModels.Add(new BulkWriteInsertOneModel<BsonDocument>(__collectionNamespace, smallDocument.DeepClone().AsBsonDocument));
                _clientBulkWriteMixedOpsModels.Add(new BulkWriteReplaceOneModel<BsonDocument>(__collectionNamespace, FilterDefinition<BsonDocument>.Empty, smallDocument.DeepClone().AsBsonDocument));
                _clientBulkWriteMixedOpsModels.Add(new BulkWriteDeleteOneModel<BsonDocument>(__collectionNamespace, FilterDefinition<BsonDocument>.Empty));
                
                _collectionBulkWriteMixedOpsModels.Add(new InsertOneModel<BsonDocument>(smallDocument.DeepClone().AsBsonDocument));
                _collectionBulkWriteMixedOpsModels.Add(new ReplaceOneModel<BsonDocument>(FilterDefinition<BsonDocument>.Empty, smallDocument.DeepClone().AsBsonDocument));
                _collectionBulkWriteMixedOpsModels.Add(new DeleteOneModel<BsonDocument>(FilterDefinition<BsonDocument>.Empty));
            }
        }

        [IterationSetup]
        public void BeforeTask()
        {
            _database.DropCollection(MongoConfiguration.PerfTestCollectionName);
            _collection = _database.GetCollection<BsonDocument>(MongoConfiguration.PerfTestCollectionName);
        }

        [Benchmark]
        public void SmallDocCollectionBulkWriteMixedOpsBenchmark()
        {
            _collection.BulkWrite(_collectionBulkWriteMixedOpsModels, new BulkWriteOptions());
        }
        
        [Benchmark]
        public void SmallDocClientBulkWriteMixedOpsBenchmark()
        {
            _client.BulkWrite(_clientBulkWriteMixedOpsModels, new ClientBulkWriteOptions());
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.Dispose();
        }
    }
}
