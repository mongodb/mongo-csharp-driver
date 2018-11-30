/* Copyright 2015-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateFluentTests
    {
        private Mock<IMongoCollection<C>> _mockCollection;

        [Theory]
        [ParameterAttributeData]
        public void As_should_add_the_expected_stage(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            var result = subject
                .Match("{ X : 1 }")
                .As<BsonDocument>();

            Predicate<PipelineDefinition<C, BsonDocument>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 1 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $match : { X : 1 } }") &&
                    renderedPipeline.OutputSerializer is BsonDocumentSerializer;
            };

            if (async)
            {
                result.ToCursorAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<BsonDocument>(
                        It.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                result.ToCursor();

                _mockCollection.Verify(
                    c => c.Aggregate<BsonDocument>(
                        It.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [InlineData(ChangeStreamFullDocumentOption.Default, null, "{ $changeStream : { fullDocument : \"default\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, null, "{ $changeStream : { fullDocument : \"updateLookup\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.Default, "{ a : 1 }", "{ $changeStream : { fullDocument : \"default\", resumeAfter : { a : 1 } } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, "{ a : 1 }", "{ $changeStream : { fullDocument : \"updateLookup\", resumeAfter : { a : 1 } } }")]
        public void ChangeStream_should_add_the_expected_stage(
            ChangeStreamFullDocumentOption fullDocument,
            string resumeAfterString,
            string expectedStage)
        {
            var resumeAfter = resumeAfterString == null ? null : BsonDocument.Parse(resumeAfterString);

            var subject = CreateSubject();
            var options = new ChangeStreamStageOptions
            {
                FullDocument = fullDocument,
                ResumeAfter = resumeAfter
            };

            var result = subject.ChangeStream(options);

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<C>();
            var stages = RenderStages(result.Stages, inputSerializer, serializerRegistry);
            stages.Count.Should().Be(1);
            stages[0].Document.Should().Be(expectedStage);
        }

        [Fact]
        public void ChangeStream_should_add_the_expected_stage_when_options_is_null()
        {
            var subject = CreateSubject();
            ChangeStreamStageOptions options = null;

            var result = subject.ChangeStream(options);

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<C>();
            var stages = RenderStages(result.Stages, inputSerializer, serializerRegistry);
            stages.Count.Should().Be(1);
            stages[0].Document.Should().Be("{ $changeStream : { fullDocument : \"default\" } }");
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_add_the_expected_stage(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();

            var result = subject.Count();

            Predicate<PipelineDefinition<C, AggregateCountResult>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 1 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $count : 'count' }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(AggregateCountResult);
            };

            if (async)
            {
                result.ToCursorAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<AggregateCountResult>(
                        It.Is<PipelineDefinition<C, AggregateCountResult>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                result.ToCursor();

                _mockCollection.Verify(
                    c => c.Aggregate<AggregateCountResult>(
                        It.Is<PipelineDefinition<C, AggregateCountResult>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Count_should_return_the_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.AggregateCountStage);
            var client = DriverTestConfiguration.Client;
            var databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            var collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestMethod();
            var database = client.GetDatabase(databaseNamespace.DatabaseName);
            database.DropCollection(collectionNamespace.CollectionName);
            var collection = database.GetCollection<BsonDocument>(collectionNamespace.CollectionName);
            collection.InsertOne(new BsonDocument());
            var subject = collection.Aggregate();

            long result;
            if (async)
            {
                result = subject.Count().SingleAsync().GetAwaiter().GetResult().Count;
            }
            else
            {
                result = subject.Count().Single().Count;
            }

            result.Should().Be(1);
        }

        [SkippableFact]
        public void Lookup_with_let_and_bsondocuments_params_should_return_the_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateLet);

            string databaseName = "test";
            string ordersCollectionName = "orders";
            string warehousesCollectionName = "warehouses";

            var client = CreateClient();
            DropCollection(client, databaseName, ordersCollectionName);
            DropCollection(client, databaseName, warehousesCollectionName);

            var ordersCollection = client.GetDatabase(databaseName).GetCollection<BsonDocument>(ordersCollectionName);
            var warehousesCollection = client.GetDatabase(databaseName).GetCollection<BsonDocument>(warehousesCollectionName);

            var orderDocuments = new[]
            {
                new BsonDocument { { "item", "almonds" }, { "price", 12 }, { "ordered", 2 } },
                new BsonDocument { { "item", "pecans" }, { "price", 20 }, { "ordered", 1 } },
                new BsonDocument { { "item", "cookies" }, { "price", 10 }, { "ordered", 60 } }
            };
            ordersCollection.InsertMany(orderDocuments);

            var warehouseDocuments = new[]
            {
                new BsonDocument { { "stock_item", "almonds" }, { "instock", 120 } },
                new BsonDocument { { "stock_item", "pecans" }, { "instock", 80 } },
                new BsonDocument { { "stock_item", "almonds" }, { "instock", 60 } },
                new BsonDocument { { "stock_item", "cookies" }, { "instock", 40 } },
                new BsonDocument { { "stock_item", "cookies" }, { "instock", 80 } }
            };
            warehousesCollection.InsertMany(warehouseDocuments);

            var lookupPipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Match(new BsonDocument("$expr",
                    new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$stock_item", "$$order_item" }),
                        new BsonDocument("$gte", new BsonArray { "$instock", "$$order_qty" })
                    })))
                .Project<BsonDocument, BsonDocument, BsonDocument>(
                    Builders<BsonDocument>.Projection
                        .Exclude("stock_item")
                        .Exclude("_id"));

            var result = ordersCollection
                .Aggregate()
                .Lookup<BsonDocument, BsonDocument, IEnumerable<BsonDocument>, BsonDocument>(
                    warehousesCollection,
                    new BsonDocument { { "order_item", "$item" }, { "order_qty", "$ordered" } },
                    lookupPipeline,
                    "stockdata")
                .ToList()
                .Select(item =>
                {
                    var document = item.ToBsonDocument();
                    document.Remove("_id");
                    return document;
                })
                .ToList();

            result.Count.Should().Be(3);
            result[0].Should().Be("{ 'item' : 'almonds', 'price' : 12, 'ordered' : 2, 'stockdata' : [{ 'instock' : 120 }, { 'instock' : 60 }] }");
            result[1].Should().Be("{ 'item' : 'pecans', 'price' : 20, 'ordered' : 1, 'stockdata' : [{ 'instock' : 80 }] }");
            result[2].Should().Be("{ 'item' : 'cookies', 'price' : 10, 'ordered' : 60, 'stockdata' : [{ 'instock' : 80 }] }");
        }

        public class Order
        {
            [BsonElement("_id")]
            public ObjectId Id { get; set; }
            [BsonElement("item")]
            public string Item { get; set; }
            [BsonElement("price")]
            public int Price { get; set; }
            [BsonElement("ordered")]
            public int Ordered { get; set; }
            [BsonElement("stockdata")]
            public IEnumerable<StockData> StockData { get; set; }
        }

        public class Warehouse
        {
            [BsonElement("_id")]
            public ObjectId Id { get; set; }
            [BsonElement("stock_item")]
            public string StockItem { get; set; }
            [BsonElement("instock")]
            public int Instock { get; set; }
        }

        public class StockData
        {
            [BsonElement("instock")]
            public int Instock { get; set; }
        }

        [SkippableFact]
        public void Lookup_with_let_should_return_the_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateLet);

            string databaseName = "test";
            string ordersCollectionName = "orders";
            string warehousesCollectionName = "warehouses";

            var client = CreateClient();
            DropCollection(client, databaseName, ordersCollectionName);
            DropCollection(client, databaseName, warehousesCollectionName);

            var ordersCollection = client.GetDatabase(databaseName).GetCollection<Order>(ordersCollectionName);
            var warehousesCollection = client.GetDatabase(databaseName).GetCollection<Warehouse>(warehousesCollectionName);

            var orderDocuments = new[]
            {
                new Order { Item = "almonds", Price = 12, Ordered = 2 },
                new Order { Item = "pecans", Price = 20, Ordered = 1 },
                new Order { Item = "cookies", Price = 10, Ordered = 60 }
            };
            ordersCollection.InsertMany(orderDocuments);

            var warehouseDocuments = new[]
            {
                new Warehouse { StockItem = "almonds", Instock = 120 },
                new Warehouse { StockItem = "pecans", Instock = 80 },
                new Warehouse { StockItem = "almonds", Instock = 60 },
                new Warehouse { StockItem = "cookies", Instock = 40 },
                new Warehouse { StockItem = "cookies", Instock = 80 },
            };
            warehousesCollection.InsertMany(warehouseDocuments);

            var lookupPipeline = new EmptyPipelineDefinition<Warehouse>()
                .Match(new BsonDocument("$expr",
                    new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$stock_item", "$$order_item" }),
                        new BsonDocument("$gte", new BsonArray { "$instock", "$$order_qty" })
                    })))
                .Project<Warehouse, Warehouse, StockData>(
                    Builders<Warehouse>.Projection
                        .Exclude(warehouses => warehouses.StockItem)
                        .Exclude(warehouses => warehouses.Id));

            var result = ordersCollection
                .Aggregate()
                .Lookup<Order, Warehouse, StockData, IEnumerable<StockData>, Order>(
                    warehousesCollection,
                    new BsonDocument { { "order_item", "$item" }, { "order_qty", "$ordered" } },
                    lookupPipeline,
                    order => order.StockData)
                .ToList()
                .Select(item =>
                {
                    var document = item.ToBsonDocument();
                    document.Remove("_id");
                    return document;
                })
                .ToList();

            result.Count.Should().Be(3);
            result[0].Should().Be("{ 'item' : 'almonds', 'price' : 12, 'ordered' : 2, 'stockdata' : [{ 'instock' : 120 }, { 'instock' : 60 }] }");
            result[1].Should().Be("{ 'item' : 'pecans', 'price' : 20, 'ordered' : 1, 'stockdata' : [{ 'instock' : 80 }] }");
            result[2].Should().Be("{ 'item' : 'cookies', 'price' : 10, 'ordered' : 60, 'stockdata' : [{ 'instock' : 80 }] }");
        }

        [SkippableFact]
        public void Lookup_with_let_and_mismatched_pipeline_condition_should_return_the_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateLet);

            string databaseName = "test";
            string ordersCollectionName = "orders";
            string warehousesCollectionName = "warehouses";

            var client = CreateClient();
            DropCollection(client, databaseName, ordersCollectionName);
            DropCollection(client, databaseName, warehousesCollectionName);

            var ordersCollection = client.GetDatabase(databaseName).GetCollection<Order>(ordersCollectionName);
            var warehousesCollection = client.GetDatabase(databaseName).GetCollection<Warehouse>(warehousesCollectionName);

            var orderDocuments = new[]
            {
                new Order { Item = "almonds", Price = 12, Ordered = 2 },
                new Order { Item = "pecans", Price = 20, Ordered = 1 },
                new Order { Item = "cookies", Price = 10, Ordered = 60 }
            };
            ordersCollection.InsertMany(orderDocuments);

            var warehouseDocuments = new[]
            {
                new Warehouse { StockItem = "almonds", Instock = 120 },
                new Warehouse { StockItem = "pecans", Instock = 80 },
                new Warehouse { StockItem = "almonds", Instock = 60 },
                new Warehouse { StockItem = "cookies", Instock = 40 },
                new Warehouse { StockItem = "cookies", Instock = 80 },
            };
            warehousesCollection.InsertMany(warehouseDocuments);

            var lookupPipeline = new EmptyPipelineDefinition<Warehouse>()
                .Match(new BsonDocument("$expr",
                    new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$stock_item", "not_exist_item" }),
                    })))
                .Project<Warehouse, Warehouse, StockData>(
                    Builders<Warehouse>.Projection
                        .Exclude(warehouses => warehouses.StockItem)
                        .Exclude(warehouses => warehouses.Id));

            var result = ordersCollection
                .Aggregate()
                .Lookup(
                    warehousesCollection,
                    new BsonDocument { { "order_item", "$item" }, { "order_qty", "$ordered" } },
                    lookupPipeline,
                    new ExpressionFieldDefinition<Order, IEnumerable<StockData>>(order => order.StockData))
                .ToList()
                .Select(item =>
                {
                    var document = item.ToBsonDocument();
                    document.Remove("_id");
                    return document;
                })
                .ToList();

            result.Count.Should().Be(3);
            result[0].Should().Be("{ 'item' : 'almonds', 'price' : 12, 'ordered' : 2, 'stockdata' : [] }");
            result[1].Should().Be("{ 'item' : 'pecans', 'price' : 20, 'ordered' : 1, 'stockdata' : [] }");
            result[2].Should().Be("{ 'item' : 'cookies', 'price' : 10, 'ordered' : 60, 'stockdata' : [] }");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Lookup_without_let_should_return_the_expected_result([Values(null, "{}")] string emptyLetValue)
        {
            RequireServer.Check().Supports(Feature.AggregateLet);

            string databaseName = "test";
            string ordersCollectionName = "orders";
            string warehousesCollectionName = "warehouses";

            var client = CreateClient();
            DropCollection(client, databaseName, ordersCollectionName);
            DropCollection(client, databaseName, warehousesCollectionName);

            var ordersCollection = client.GetDatabase(databaseName).GetCollection<Order>(ordersCollectionName);
            var warehousesCollection = client.GetDatabase(databaseName).GetCollection<Warehouse>(warehousesCollectionName);

            var orderDocuments = new[]
            {
                new Order { Item = "almonds", Price = 12, Ordered = 2 },
                new Order { Item = "pecans", Price = 20, Ordered = 1 },
                new Order { Item = "cookies", Price = 10, Ordered = 60 }
            };
            ordersCollection.InsertMany(orderDocuments);

            var warehouseDocuments = new[]
            {
                new Warehouse { StockItem = "almonds", Instock = 120 },
                new Warehouse { StockItem = "pecans", Instock = 80 },
                new Warehouse { StockItem = "almonds", Instock = 60 },
                new Warehouse { StockItem = "cookies", Instock = 40 },
                new Warehouse { StockItem = "cookies", Instock = 80 },
            };
            warehousesCollection.InsertMany(warehouseDocuments);

            var lookupPipeline = new EmptyPipelineDefinition<Warehouse>()
                .Project<Warehouse, Warehouse, StockData>(
                    Builders<Warehouse>.Projection
                        .Exclude(warehouses => warehouses.StockItem)
                        .Exclude(warehouses => warehouses.Id));

            var result = ordersCollection
                .Aggregate()
                .Lookup(
                    warehousesCollection,
                    emptyLetValue != null ? BsonDocument.Parse(emptyLetValue) : null,
                    lookupPipeline,
                    new ExpressionFieldDefinition<Order, IEnumerable<StockData>>(order => order.StockData))
                .ToList()
                .Select(item =>
                {
                    var document = item.ToBsonDocument();
                    document.Remove("_id");
                    return document;
                })
                .ToList();

            result.Count.Should().Be(3);
            result[0].Should().Be("{ 'item' : 'almonds', 'price' : 12, 'ordered' : 2, 'stockdata' : [{ 'instock' : 120 }, { 'instock' : 80 }, { 'instock' : 60 }, { 'instock' : 40 }, { 'instock' : 80 }] }");
            result[1].Should().Be("{ 'item' : 'pecans', 'price' : 20, 'ordered' : 1, 'stockdata' : [{ 'instock' : 120 }, { 'instock' : 80 }, { 'instock' : 60 }, { 'instock' : 40 }, { 'instock' : 80 }] }");
            result[2].Should().Be("{ 'item' : 'cookies', 'price' : 10, 'ordered' : 60, 'stockdata' : [{ 'instock' : 120 }, { 'instock' : 80 }, { 'instock' : 60 }, { 'instock' : 40 }, { 'instock' : 80 }] }");
        }

        [Theory]
        [ParameterAttributeData]
        public void OfType_should_add_the_expected_stage(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            var result = subject
                .SortBy(c => c.X)
                .OfType<D>()
                .Match(d => d.Y == 2);

            Predicate<PipelineDefinition<C, D>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 3 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $sort : { X : 1 } }") &&
                    renderedPipeline.Documents[1] == BsonDocument.Parse("{ $match : { _t : \"D\" } }") &&
                    renderedPipeline.Documents[2] == BsonDocument.Parse("{ $match : { Y : 2 } }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(D);
            };

            if (async)
            {
                result.ToCursorAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<D>(
                        It.Is<PipelineDefinition<C, D>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                result.ToCursor();

                _mockCollection.Verify(
                    c => c.Aggregate<D>(
                        It.Is<PipelineDefinition<C, D>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Out_should_add_the_expected_stage_and_call_Aggregate(
            [Values(false, true)] bool async)
        {
            var subject = 
                CreateSubject()
                .Match(Builders<C>.Filter.Eq(c => c.X, 1));
            var collectionName = "test";

            Predicate<PipelineDefinition<C, C>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 2 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $match : { X : 1 } }") &&
                    renderedPipeline.Documents[1] == BsonDocument.Parse("{ $out : \"test\" }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(C);
            };

            IAsyncCursor<C> cursor;
            if (async)
            {
                cursor = subject.OutAsync(collectionName, CancellationToken.None).GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<C>(
                        It.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                cursor = subject.Out(collectionName, CancellationToken.None);

                _mockCollection.Verify(
                    c => c.Aggregate<C>(
                        It.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceRoot_should_add_the_expected_stage(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();

            var result = subject
                .ReplaceRoot<BsonDocument>("$X");

            Predicate<PipelineDefinition<C, BsonDocument>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 1 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $replaceRoot : { newRoot : '$X' } }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(BsonDocument);
            };

            if (async)
            {
                result.ToCursorAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<BsonDocument>(
                        It.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                result.ToCursor();

                _mockCollection.Verify(
                    c => c.Aggregate<BsonDocument>(
                        It.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void SortByCount_should_add_the_expected_stage(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();

            var result = subject
                .SortByCount<int>("$X");

            Predicate<PipelineDefinition<C, AggregateSortByCountResult<int>>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 1 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $sortByCount : '$X' }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(AggregateSortByCountResult<int>);
            };

            if (async)
            {
                result.ToCursorAsync().GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<AggregateSortByCountResult<int>>(
                        It.Is<PipelineDefinition<C, AggregateSortByCountResult<int>>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                result.ToCursor();

                _mockCollection.Verify(
                    c => c.Aggregate<AggregateSortByCountResult<int>>(
                        It.Is<PipelineDefinition<C, AggregateSortByCountResult<int>>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ToCursor_should_call_collection_Aggregate_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var session = usingSession ? new Mock<IClientSessionHandle>().Object : null;
            var subject =
                CreateSubject(session)
                .Match(Builders<C>.Filter.Eq(c => c.X, 1));
            var expectedPipeline = ((AggregateFluent<C, C>)subject)._pipeline();
            var expectedOptions = subject.Options;
            var cancellationToken = new CancellationTokenSource().Token;

            if (async)
            {
                var _ = subject.ToCursorAsync(cancellationToken).GetAwaiter().GetResult();

                if (usingSession)
                {
                    _mockCollection.Verify(
                        c => c.AggregateAsync<C>(
                            session,
                            expectedPipeline,
                            expectedOptions,
                            cancellationToken),
                        Times.Once);
                }
                else
                {
                    _mockCollection.Verify(
                        c => c.AggregateAsync<C>(
                            expectedPipeline,
                            expectedOptions,
                            cancellationToken),
                        Times.Once);
                }
            }
            else
            {
                var _ = subject.ToCursor(cancellationToken);

                if (usingSession)
                {
                    _mockCollection.Verify(
                        c => c.Aggregate<C>(
                            session,
                            expectedPipeline,
                            expectedOptions,
                            cancellationToken),
                        Times.Once);
                }
                else
                {
                    _mockCollection.Verify(
                        c => c.Aggregate<C>(
                            expectedPipeline,
                            expectedOptions,
                            cancellationToken),
                        Times.Once);
                }
            }
        }

        // private methods
        private IAggregateFluent<C> CreateSubject(IClientSessionHandle session = null)
        {
            var mockDatabase = new Mock<IMongoDatabase>();
            SetupDatabaseGetCollectionMethod<C>(mockDatabase);

            var settings = new MongoCollectionSettings();
            _mockCollection = new Mock<IMongoCollection<C>>();
            _mockCollection.SetupGet(c => c.Database).Returns(mockDatabase.Object);
            _mockCollection.SetupGet(c => c.DocumentSerializer).Returns(settings.SerializerRegistry.GetSerializer<C>());
            _mockCollection.SetupGet(c => c.Settings).Returns(settings);
            var options = new AggregateOptions();
            var subject = new AggregateFluent<C, C>(session, _mockCollection.Object, new EmptyPipelineDefinition<C>(), options);

            return subject;
        }

        private void SetupDatabaseGetCollectionMethod<TDocument>(Mock<IMongoDatabase> mockDatabase)
        {
            mockDatabase
                .Setup(d => d.GetCollection<TDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns((string collectionName, MongoCollectionSettings settings) =>
                {
                    var mockCollection = new Mock<IMongoCollection<TDocument>>();
                    mockCollection.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace(new DatabaseNamespace("test"), collectionName));
                    return mockCollection.Object;
                });
        }

        private RenderedPipelineDefinition<TOutput> RenderPipeline<TInput, TOutput>(PipelineDefinition<TInput, TOutput> pipeline)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<TInput>();
            return pipeline.Render(inputSerializer, serializerRegistry);
        }

        private List<IRenderedPipelineStageDefinition> RenderStages(
            IList<IPipelineStageDefinition> stages, 
            IBsonSerializer inputSerializer, 
            IBsonSerializerRegistry serializerRegistry)
        {
            var renderedStages = new List<IRenderedPipelineStageDefinition>();

            foreach (var stage in stages)
            {
                var renderedStage = stage.Render(inputSerializer, serializerRegistry);
                renderedStages.Add(renderedStage);
                inputSerializer = renderedStage.OutputSerializer;
            }

            return renderedStages;
        }

        // nested types
        public class C
        {
            public int X;
        }

        public class D : C
        {
            public int Y;
        }

        private IMongoClient CreateClient()
        {
            return new MongoClient(CoreTestConfiguration.ConnectionString.ToString());
        }

        private void DropCollection(IMongoClient client, string databaseName, string collectionName)
        {
            client.GetDatabase(databaseName).DropCollection(collectionName);
        }
    }

    internal static class AggregateFluentReflector
    {
        public static IMongoCollection<TDocument> _collection<TDocument, TResult>(this AggregateFluent<TDocument, TResult> obj)
        {
            var fieldInfo = typeof(AggregateFluent<TDocument, TResult>).GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IMongoCollection<TDocument>)fieldInfo.GetValue(obj);
        }

        public static AggregateOptions _options<TDocument, TResult>(this AggregateFluent<TDocument, TResult> obj)
        {
            var fieldInfo = typeof(AggregateFluent<TDocument, TResult>).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
            return (AggregateOptions)fieldInfo.GetValue(obj);
        }

        public static PipelineDefinition<TDocument, TResult> _pipeline<TDocument, TResult>(this AggregateFluent<TDocument, TResult> obj)
        {
            var fieldInfo = typeof(AggregateFluent<TDocument, TResult>).GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance);
            return (PipelineDefinition<TDocument, TResult>)fieldInfo.GetValue(obj);
        }

        public static IClientSessionHandle _session<TDocument, TResult>(this AggregateFluent<TDocument, TResult> obj)
        {
            var fieldInfo = typeof(AggregateFluent<TDocument, TResult>).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClientSessionHandle)fieldInfo.GetValue(obj);
        }
    }
}