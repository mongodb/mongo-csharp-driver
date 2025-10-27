﻿/* Copyright 2010-present MongoDB Inc.
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
        private readonly BsonDocument _indexDefinitionWithFields = BsonDocument.Parse("{ mappings: { dynamic: false, fields: { } } }");
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
            [Values(false, true)] bool async,
            [Values(false, true)] bool includeFields)
        {
            var indexDefinition1 = new CreateSearchIndexModel(
                async ? "test-search-index-1-async" : "test-search-index-1",
                includeFields ? _indexDefinitionWithFields : _indexDefinition);

            var indexDefinition2 = new CreateSearchIndexModel(
                async ? "test-search-index-2-async" : "test-search-index-2",
                includeFields ? _indexDefinitionWithFields : _indexDefinition);

            var indexNamesActual = async
                ? await _collection.SearchIndexes.CreateManyAsync(new[] { indexDefinition1, indexDefinition2 })
                : _collection.SearchIndexes.CreateMany(new[] { indexDefinition1, indexDefinition2 });

            indexNamesActual.Should().BeEquivalentTo(indexDefinition1.Name, indexDefinition2.Name);

            var indexes = await GetIndexes(async, indexDefinition1.Name, indexDefinition2.Name);

            indexes[0]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinitionWithFields);
            indexes[1]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinitionWithFields);
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
            var indexNewDefinition = BsonDocument.Parse("{ mappings: { dynamic: true, fields: { } }}");

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
            [Values(false, true)] bool async,
            [Values(false, true)] bool includeFields)
        {
            var indexName = async ? "test-search-index-case6-async" : "test-search-index-case6";

            var collection = _collection
                .WithReadConcern(ReadConcern.Majority)
                .WithWriteConcern(WriteConcern.WMajority);

            var indexNameCreated = async
                ? await collection.SearchIndexes.CreateOneAsync(includeFields
                    ? _indexDefinitionWithFields
                    : _indexDefinition, indexName)
                : collection.SearchIndexes.CreateOne(
                    includeFields
                        ? _indexDefinitionWithFields
                        : _indexDefinition, indexName);

            indexNameCreated.Should().Be(indexName);

            var indexes = await GetIndexes(async, indexName);
            indexes[0]["latestDefinition"].AsBsonDocument.Should().Be(_indexDefinitionWithFields);
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
                ? await Record.ExceptionAsync(() => _collection.SearchIndexes.CreateOneAsync(
                    new CreateSearchIndexModel(indexName, _vectorIndexDefinition).Definition, indexName))
                : Record.Exception(() => _collection.SearchIndexes.CreateOne(
                    new CreateSearchIndexModel(indexName, _vectorIndexDefinition).Definition, indexName));

            exception.Message.Should().Contain("Command createSearchIndexes failed: \"userCommand.indexes[0].mappings\" is required.");
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Can_create_Atlas_vector_index_for_all_options_using_typed_API(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-index-vector-optional-async" : "test-index-vector-optional";

            var indexModel = new CreateVectorSearchIndexModel<EntityWithVector>(
                e => e.Floats, indexName, VectorSimilarity.Cosine, dimensions: 2)
            {
                HnswMaxEdges = 18, HnswNumEdgeCandidates = 102, Quantization = VectorQuantization.Scalar
            };

            var collection = _database.GetCollection<EntityWithVector>(_collection.CollectionNamespace.CollectionName);
            var createdName = async
                ? await collection.SearchIndexes.CreateOneAsync(indexModel)
                : collection.SearchIndexes.CreateOne(indexModel);

            createdName.Should().Be(indexName);

            var index = (await GetIndexes(async, indexName))[0];
            index["type"].AsString.Should().Be("vectorSearch");

            var fields = index["latestDefinition"].AsBsonDocument["fields"].AsBsonArray;
            fields.Count.Should().Be(1);

            var indexField = fields[0].AsBsonDocument;
            indexField["type"].AsString.Should().Be("vector");
            indexField["path"].AsString.Should().Be("Floats");
            indexField["numDimensions"].AsInt32.Should().Be(2);
            indexField["similarity"].AsString.Should().Be("cosine");
            indexField["quantization"].AsString.Should().Be("scalar");
            indexField["hnswOptions"].AsBsonDocument["maxEdges"].AsInt32.Should().Be(18);
            indexField["hnswOptions"].AsBsonDocument["numEdgeCandidates"].AsInt32.Should().Be(102);
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Can_create_Atlas_vector_index_for_required_only_options_using_typed_API(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-index-vector-required-async" : "test-index-vector-required";

            var indexModel = new CreateVectorSearchIndexModel<EntityWithVector>("vectors", indexName, VectorSimilarity.Euclidean, dimensions: 4);

            var collection = _database.GetCollection<EntityWithVector>(_collection.CollectionNamespace.CollectionName);
            var createdName = async
                ? await collection.SearchIndexes.CreateOneAsync(indexModel)
                : collection.SearchIndexes.CreateOne(indexModel);

            createdName.Should().Be(indexName);

            var index = (await GetIndexes(async, indexName))[0];
            index["type"].AsString.Should().Be("vectorSearch");

            var fields = index["latestDefinition"].AsBsonDocument["fields"].AsBsonArray;
            fields.Count.Should().Be(1);

            var indexField = fields[0].AsBsonDocument;
            indexField["type"].AsString.Should().Be("vector");
            indexField["path"].AsString.Should().Be("vectors");
            indexField["numDimensions"].AsInt32.Should().Be(4);
            indexField["similarity"].AsString.Should().Be("euclidean");

            indexField.Contains("quantization").Should().Be(false);
            indexField.Contains("hnswOptions").Should().Be(false);
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Can_create_Atlas_vector_index_for_all_options_using_typed_API_with_filters(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-index-vector-typed-filters-async" : "test-index-typed-filters";

            var indexModel = new CreateVectorSearchIndexModel<EntityWithVector>(
                e => e.Floats,
                indexName,
                VectorSimilarity.Cosine,
                dimensions: 2,
                e => e.Filter1, e => e.Filter2, e => e.Filter3)
            {
                HnswMaxEdges = 18,
                HnswNumEdgeCandidates = 102,
                Quantization = VectorQuantization.Scalar,
            };

            var collection = _database.GetCollection<EntityWithVector>(_collection.CollectionNamespace.CollectionName);
            var createdName = async
                ? await collection.SearchIndexes.CreateOneAsync(indexModel)
                : collection.SearchIndexes.CreateOne(indexModel);

            createdName.Should().Be(indexName);

            var index = (await GetIndexes(async, indexName))[0];
            index["type"].AsString.Should().Be("vectorSearch");

            var fields = index["latestDefinition"].AsBsonDocument["fields"].AsBsonArray;
            fields.Count.Should().Be(4);

            var indexField = fields[0].AsBsonDocument;
            indexField["type"].AsString.Should().Be("vector");
            indexField["path"].AsString.Should().Be("Floats");
            indexField["numDimensions"].AsInt32.Should().Be(2);
            indexField["similarity"].AsString.Should().Be("cosine");
            indexField["quantization"].AsString.Should().Be("scalar");
            indexField["hnswOptions"].AsBsonDocument["maxEdges"].AsInt32.Should().Be(18);
            indexField["hnswOptions"].AsBsonDocument["numEdgeCandidates"].AsInt32.Should().Be(102);

            for (var i = 1; i <= 3; i++)
            {
                var filterField = fields[i].AsBsonDocument;
                filterField["type"].AsString.Should().Be("filter");
                filterField["path"].AsString.Should().Be($"Filter{i}");
            }
        }

        [Theory(Timeout = Timeout)]
        [ParameterAttributeData]
        public async Task Can_create_Atlas_vector_index_for_required_only_options_using_typed_API_with_filters(
            [Values(false, true)] bool async)
        {
            var indexName = async ? "test-index-untyped-filters-async" : "test-index-untyped-filters";

            var indexModel = new CreateVectorSearchIndexModel<EntityWithVector>(
                "vectors",
                indexName,
                VectorSimilarity.Euclidean,
                dimensions: 4,
                "f1", "f2", "f3");

            var collection = _database.GetCollection<EntityWithVector>(_collection.CollectionNamespace.CollectionName);
            var createdName = async
                ? await collection.SearchIndexes.CreateOneAsync(indexModel)
                : collection.SearchIndexes.CreateOne(indexModel);

            createdName.Should().Be(indexName);

            var index = (await GetIndexes(async, indexName))[0];
            index["type"].AsString.Should().Be("vectorSearch");

            var fields = index["latestDefinition"].AsBsonDocument["fields"].AsBsonArray;
            fields.Count.Should().Be(4);

            var indexField = fields[0].AsBsonDocument;
            indexField["type"].AsString.Should().Be("vector");
            indexField["path"].AsString.Should().Be("vectors");
            indexField["numDimensions"].AsInt32.Should().Be(4);
            indexField["similarity"].AsString.Should().Be("euclidean");

            indexField.Contains("quantization").Should().Be(false);
            indexField.Contains("hnswOptions").Should().Be(false);

            for (var i = 1; i <= 3; i++)
            {
                var filterField = fields[i].AsBsonDocument;
                filterField["type"].AsString.Should().Be("filter");
                filterField["path"].AsString.Should().Be($"f{i}");
            }
        }

        private class EntityWithVector
        {
            public ObjectId Id { get; set; }
            public float[] Floats { get; set; }
            public bool Filter1 { get; set; }
            public string Filter2 { get; set; }
            public int Filter3 { get; set; }
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
