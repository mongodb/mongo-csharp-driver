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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Search
{
    [Category("AtlasSearchIndexHelpers")]
    public class AtlasSearchIndexManagementTests : LoggableTestClass
    {
        private const int Timeout = 5 * 60 * 1000;
        private const int IndexesPollPeriod = 5000;

        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IMongoDatabase _database;
        private readonly DisposableMongoClient _disposableMongoClient;
        private readonly BsonDocument _indexDefinition = BsonDocument.Parse("{ mappings: { dynamic: false } }");

        public AtlasSearchIndexManagementTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED");

            var atlasSearchUri = CoreTestConfiguration.ConnectionString.ToString();

            _disposableMongoClient = new(new MongoClient(atlasSearchUri), CreateLogger<DisposableMongoClient>());
            _database = _disposableMongoClient.GetDatabase("dotnet-test");
            var collectionName = GetRandomName();

            _database.CreateCollection(collectionName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
        }

        protected override void DisposeInternal()
        {
            _collection.Database.DropCollection(_collection.CollectionNamespace.CollectionName);
            _disposableMongoClient.Dispose();
        }

        [Fact(Timeout = Timeout)]
        public Task Case1_driver_should_successfully_create_and_list_search_indexes() =>
            CreateIndexAndValidate("test-search-index");

        [Fact(Timeout = Timeout)]
        public async Task Case2_driver_should_successfully_create_multiple_indexes_in_batch()
        {
            var indexDefinition1 = new CreateSearchIndexModel("test-search-index-1", _indexDefinition);
            var indexDefinition2 = new CreateSearchIndexModel("test-search-index-2", _indexDefinition);

            var indexNamesActual = await _collection.SearchIndexes.CreateManyAsync(new[] { indexDefinition1, indexDefinition2 });

            indexNamesActual.Should().BeEquivalentTo(indexDefinition1.Name, indexDefinition2.Name);

            var indexes = await GetIndexes(indexDefinition1.Name, indexDefinition2.Name);

            indexes[0]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinition);
            indexes[1]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinition);
        }

        [Fact(Timeout = Timeout)]
        public async Task Case3_driver_can_successfully_drop_search_indexes()
        {
            const string indexName = "test-search-index";

            await CreateIndexAndValidate(indexName);

            await _collection.SearchIndexes.DropOneAsync(indexName);

            while (true)
            {
                var cursor = await _collection.SearchIndexes.ListAsync();
                var indexes = await cursor.ToListAsync();
                if (indexes.Count == 0)
                {
                    return;
                }

                Thread.Sleep(IndexesPollPeriod);
            }
        }

        [Fact(Timeout = Timeout)]
        public async Task Case4_driver_can_update_a_search_index()
        {
            const string indexName = "test-search-index";
            var indexNewDefinition = BsonDocument.Parse("{ mappings: { dynamic: true }}");

            await CreateIndexAndValidate(indexName);

            await _collection.SearchIndexes.UpdateAsync(indexName, indexNewDefinition);

            var updatedIndex = await GetIndexes(indexName);
            updatedIndex[0]["latestDefinition"].AsBsonDocument.Should().Be(indexNewDefinition);
        }

        [Fact(Timeout = Timeout)]
        public async Task Case5_dropSearchIndex_suppresses_namespace_not_found_errors()
        {
            var collection = _database.GetCollection<BsonDocument>("non_existent_collection");
            await collection.SearchIndexes.DropOneAsync("non_existing_index");
        }

        [Fact(Timeout = Timeout)]
        public async Task Case6_driver_can_create_and_list_search_indexes_with_non_default_read_write_concern()
        {
            const string indexName = "test-search-index-case6";

            var collection = _collection
                .WithReadConcern(ReadConcern.Majority)
                .WithWriteConcern(WriteConcern.WMajority);

            var indexNameCreated = await collection.SearchIndexes.CreateOneAsync(_indexDefinition, indexName);
            indexNameCreated.Should().Be(indexName);

            var indexes = await GetIndexes(indexName);
            indexes[0]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinition);
        }

        private async Task<BsonDocument> CreateIndexAndValidate(string indexName)
        {
            var indexNameActual = await _collection.SearchIndexes.CreateOneAsync(_indexDefinition, indexName);
            indexNameActual.Should().Be(indexName);

            var result = await GetIndexes(indexName);

            return result[0];
        }

        private async Task<BsonDocument[]> GetIndexes(params string[] indexNames)
        {
            while (true)
            {
                var cursor = await _collection.SearchIndexes.ListAsync();
                var indexes = await cursor.ToListAsync();

                var indexesFiltered = indexes
                    .Where(i => indexNames.Contains(TryGetValue<string>(i, "name")) && TryGetValue<bool>(i, "queryable"))
                    .ToArray();

                if (indexesFiltered.Length == indexNames.Length)
                {
                    return indexesFiltered;
                }

                Thread.Sleep(IndexesPollPeriod);
            }
        }

        private static string GetRandomName() => $"test_{Guid.NewGuid():N}";

        private static T TryGetValue<T>(BsonDocument document, string name)
        {
            if (!document.TryGetValue(name, out var value))
            {
                return default;
            }

            var result = BsonTypeMapper.MapToDotNetValue(value);
            return (T)result;
        }
    }
}
