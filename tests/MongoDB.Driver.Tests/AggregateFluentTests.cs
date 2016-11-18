/* Copyright 2015-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
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
        public void ToCursor_should_call_Aggregate(
            [Values(false, true)] bool async)
        {
            var subject =
                CreateSubject()
                .Match(Builders<C>.Filter.Eq(c => c.X, 1));

            Predicate<PipelineDefinition<C, C>> isExpectedPipeline = pipeline =>
            {
                var renderedPipeline = RenderPipeline(pipeline);
                return
                    renderedPipeline.Documents.Count == 1 &&
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $match : { X : 1 } }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(C);
            };

            IAsyncCursor<C> cursor;
            if (async)
            {
                cursor = subject.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockCollection.Verify(
                    c => c.AggregateAsync<C>(
                        It.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                cursor = subject.ToCursor(CancellationToken.None);

                _mockCollection.Verify(
                    c => c.Aggregate<C>(
                        It.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                        It.IsAny<AggregateOptions>(),
                        CancellationToken.None),
                    Times.Once);
            }
        }

        // private methods
        private IAggregateFluent<C> CreateSubject()
        {
            var mockDatabase = new Mock<IMongoDatabase>();
            SetupDatabaseGetCollectionMethod<C>(mockDatabase);

            var settings = new MongoCollectionSettings();
            _mockCollection = new Mock<IMongoCollection<C>>();
            _mockCollection.SetupGet(c => c.Database).Returns(mockDatabase.Object);
            _mockCollection.SetupGet(c => c.DocumentSerializer).Returns(settings.SerializerRegistry.GetSerializer<C>());
            _mockCollection.SetupGet(c => c.Settings).Returns(settings);
            var options = new AggregateOptions();
            var subject = new AggregateFluent<C, C>(_mockCollection.Object, new EmptyPipelineDefinition<C>(), options);

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

        // nested types
        public class C
        {
            public int X;
        }

        public class D : C
        {
            public int Y;
        }
    }
}
