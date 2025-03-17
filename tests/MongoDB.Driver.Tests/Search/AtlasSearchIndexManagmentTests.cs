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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.Logging;
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
        private readonly IMongoClient _mongoClient;
        private readonly BsonDocument _indexDefinition = BsonDocument.Parse("{ mappings: { dynamic: false } }");
        private readonly BsonDocument _vectorIndexDefinition = BsonDocument.Parse("{ fields: [ { type: 'vector', path: 'plot_embedding', numDimensions: 1536, similarity: 'euclidean' } ] }");

        public AtlasSearchIndexManagementTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED");

            var atlasSearchUri = CoreTestConfiguration.ConnectionString.ToString();

            _mongoClient = new MongoClient(atlasSearchUri);
            _database = _mongoClient.GetDatabase("dotnet-test");
            var collectionName = GetRandomName();

            _database.CreateCollection(collectionName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
        }

        protected override void DisposeInternal()
        {
            _collection.Database.DropCollection(_collection.CollectionNamespace.CollectionName);
            _mongoClient.Dispose();
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public Task Case1_driver_should_successfully_create_and_list_search_indexes(
            [Values(false, true)] bool async) =>
            CreateIndexAndValidate(async ? "test-search-index-async" : "test-search-index", _indexDefinition, async);

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case2_driver_should_successfully_create_multiple_indexes_in_batch(
            [Values(false, true)] bool async)
        {
            var indexDefinition1 = new CreateSearchIndexModel(async ? "test-search-index-1-async" : "test-search-index-1", _indexDefinition);
            var indexDefinition2 = new CreateSearchIndexModel(async ? "test-search-index-2-async" : "test-search-index-2", _indexDefinition);

            var indexNamesActual = async
                ? await _collection.SearchIndexes.CreateManyAsync(new[] { indexDefinition1, indexDefinition2 })
                : _collection.SearchIndexes.CreateMany(new[] { indexDefinition1, indexDefinition2 });

            indexNamesActual.Should().BeEquivalentTo(indexDefinition1.Name, indexDefinition2.Name);

            var indexes = await GetIndexes(async, indexDefinition1.Name, indexDefinition2.Name);

            indexes[0]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinition);
            indexes[1]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinition);
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case3_driver_can_successfully_drop_search_indexes(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-search-index-async" : "test-search-index";

            await CreateIndexAndValidate(indexName, _indexDefinition, async);
            if (async)
            {
                await _collection.SearchIndexes.DropOneAsync(indexName);
            }
            else
            {
                _collection.SearchIndexes.DropOne(indexName);
            }
            
            while (true)
            {
                List<BsonDocument> indexes;
                if (async)
                {
                    var cursor = await _collection.SearchIndexes.ListAsync();
                    indexes = await cursor.ToListAsync();
                }
                else
                {
                    indexes = _collection.SearchIndexes.List().ToList();
                }
                
                if (indexes.Count == 0)
                {
                    return;
                }

                Thread.Sleep(IndexesPollPeriod);
            }
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case4_driver_can_update_a_search_index(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-search-index-async" : "test-search-index";
            var indexNewDefinition = BsonDocument.Parse("{ mappings: { dynamic: true }}");

            await CreateIndexAndValidate(indexName, _indexDefinition, async);
            if (async)
            {
                await _collection.SearchIndexes.UpdateAsync(indexName, indexNewDefinition);
            }
            else
            {
                _collection.SearchIndexes.Update(indexName, indexNewDefinition);
            }
            
            var updatedIndex = await GetIndexes(async, indexName);
            updatedIndex[0]["latestDefinition"].AsBsonDocument.Should().Be(indexNewDefinition);
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case5_dropSearchIndex_suppresses_namespace_not_found_errors(
            [Values(false, true)] bool async)
        {
            var collection = _database.GetCollection<BsonDocument>("non_existent_collection");

            if (async)
            {
                await collection.SearchIndexes.DropOneAsync("non_existing_index");
            }
            else
            {
                collection.SearchIndexes.DropOne("non_existing_index");
            }
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case6_driver_can_create_and_list_search_indexes_with_non_default_read_write_concern(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-search-index-case6-async" : "test-search-index-case6";

            var collection = _collection
                .WithReadConcern(ReadConcern.Majority)
                .WithWriteConcern(WriteConcern.WMajority);

            var indexNameCreated = async
                ? await collection.SearchIndexes.CreateOneAsync(_indexDefinition, indexName)
                : collection.SearchIndexes.CreateOne(_indexDefinition, indexName);
            
            indexNameCreated.Should().Be(indexName);

            var indexes = await GetIndexes(async, indexName);
            indexes[0]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinition);
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case7_driver_can_handle_search_index_types_when_creating_indexes(
            [Values(false, true)] bool async)
        {
            string indexName1, indexName2, indexName3;
            if (async)
            {
                indexName1 = "test-search-index-case7-implicit-async";
                indexName2 = "test-search-index-case7-explicit-async";
                indexName3 = "test-search-index-case7-vector-async";
            }
            else
            {
                indexName1 = "test-search-index-case7-implicit";
                indexName2 = "test-search-index-case7-explicit";
                indexName3 = "test-search-index-case7-vector";
            }

            var indexCreated = await CreateIndexAndValidate(indexName1, _indexDefinition, async);
            indexCreated["type"].AsString.Should().Be("search");

            var indexNameCreated = async
                ? await _collection.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel(indexName2, SearchIndexType.Search, _indexDefinition))
                : _collection.SearchIndexes.CreateOne(new CreateSearchIndexModel(indexName2, SearchIndexType.Search, _indexDefinition));
            indexNameCreated.Should().Be(indexName2);

            var indexCreated2 = await GetIndexes(async, indexName2);
            indexCreated2[0]["type"].AsString.Should().Be("search");

            indexNameCreated = async
                ? await _collection.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel(indexName3, SearchIndexType.VectorSearch, _vectorIndexDefinition))
                : _collection.SearchIndexes.CreateOne(new CreateSearchIndexModel(indexName3, SearchIndexType.VectorSearch, _vectorIndexDefinition));
            indexNameCreated.Should().Be(indexName3);
            
            var indexCreated3 = await GetIndexes(async, indexName3);
            indexCreated3[0]["type"].AsString.Should().Be("vectorSearch");
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Case8_driver_requires_explicit_type_to_create_vector_search_index(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-search-index-case8-error-async" : "test-search-index-case8-error";

            var exception = async
                ? await Record.ExceptionAsync(() => _collection.SearchIndexes.CreateOneAsync(_vectorIndexDefinition, indexName))
                : Record.Exception(() => _collection.SearchIndexes.CreateOne(_vectorIndexDefinition, indexName));
            
            exception.Message.Should().Contain("Attribute mappings missing");
        }

        private async Task<BsonDocument> CreateIndexAndValidate(string indexName, BsonDocument indexDefinition, bool async)
        {
            var indexNameActual = async
                ? await _collection.SearchIndexes.CreateOneAsync(indexDefinition, indexName)
                : _collection.SearchIndexes.CreateOne(indexDefinition, indexName);
            
            indexNameActual.Should().Be(indexName);

            var result = await GetIndexes(async, indexName);
            return result[0];
        }

        private async Task<BsonDocument[]> GetIndexes(bool async, params string[] indexNames)
        {
            while (true)
            {
                List<BsonDocument> indexes;
                if (async)
                {
                    var cursor = await _collection.SearchIndexes.ListAsync();
                    indexes = await cursor.ToListAsync();
                }
                else
                {
                    indexes = _collection.SearchIndexes.List().ToList();
                }

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
