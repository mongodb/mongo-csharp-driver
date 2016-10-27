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
using System.Linq.Expressions;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
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
            var settings = new MongoCollectionSettings();
            _mockCollection = new Mock<IMongoCollection<C>>();
            _mockCollection.SetupGet(c => c.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<C>());
            _mockCollection.SetupGet(c => c.Settings).Returns(settings);
            var options = new AggregateOptions();
            var subject = new AggregateFluent<C, C>(_mockCollection.Object, Enumerable.Empty<IPipelineStageDefinition>(), options);

            return subject;
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

    public class AggregateFluentBucketTests 
    {
        #region static
        // private static fields
        private static CollectionNamespace __collectionNamespace;
        private static IMongoDatabase __database;
        private static Lazy<bool> __ensureTestData;

        // static constructor
        static AggregateFluentBucketTests()
        {
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = DriverTestConfiguration.Client.GetDatabase(databaseNamespace.DatabaseName);
            __collectionNamespace = DriverTestConfiguration.CollectionNamespace;
            __ensureTestData = new Lazy<bool>(CreateTestData);
        }

        // private static methods
        private static bool CreateTestData()
        {
            var documents = new[]
                {
                    BsonDocument.Parse("{ _id: 1, title: \"The Pillars of Society\", artist : \"Grosz\", year: 1926, tags: [ \"painting\", \"satire\", \"Expressionism\", \"caricature\" ] }"),
                    BsonDocument.Parse("{ _id: 2, title: \"Melancholy III\", \"artist\" : \"Munch\", year: 1902, tags: [ \"woodcut\", \"Expressionism\" ] }"),
                    BsonDocument.Parse("{ _id: 3, title: \"Dancer\", \"artist\" : \"Miro\", year: 1925, tags: [ \"oil\", \"Surrealism\", \"painting\" ] }"),
                    BsonDocument.Parse("{ _id: 4, title: \"The Great Wave off Kanagawa\", artist: \"Hokusai\", tags: [ \"woodblock\", \"ukiyo-e\" ] }")
                };

            __database.DropCollection(__collectionNamespace.CollectionName);
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            collection.InsertMany(documents);

            return true;
        }

        private static void EnsureTestData()
        {
            var _ = __ensureTestData.Value;
        }

        #endregion

        // public methods
        [Fact]
        public void Bucket_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (BsonValue)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket<BsonValue>(groupBy, boundaries, defaultBucket);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\" } }");
        }

        [SkippableFact]
        public void Bucket_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (BsonValue)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket<BsonValue>(groupBy, boundaries, defaultBucket);

            var buckets = result.ToList();
            buckets.Count.Should().Be(3);
            var expectedBuckets = new[]
            {
                new AggregateBucketResult<BsonValue>(1900, 1),
                new AggregateBucketResult<BsonValue>(1920, 2),
                new AggregateBucketResult<BsonValue>("Unknown", 1),
            };
            buckets.Should().Equal(expectedBuckets);
        }

        [Fact]
        public void Bucket_with_output_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (BsonValue)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var output = BsonDocument.Parse("{ years : { $push : \"$year\" }, count : { $sum : 1 } }");
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket<BsonValue, BsonDocument>(groupBy, boundaries, output, defaultBucket);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\", output : { years : { $push : \"$year\" }, count : { $sum : 1 } } } }");
        }

        [SkippableFact]
        public void Bucket_with_output_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (BsonValue)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var output = BsonDocument.Parse("{ years : { $push : \"$year\" }, count : { $sum : 1 } }");
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket<BsonValue, BsonDocument>(groupBy, boundaries, output, defaultBucket);

            var buckets = result.ToList();
            buckets.Count.Should().Be(3);
            var expectedBuckets = new[]
            {
                BsonDocument.Parse("{ _id : 1900, years : [ 1902 ], count : 1 }"),
                BsonDocument.Parse("{ _id : 1920, years : [ 1926, 1925 ], count : 2 }"),
                BsonDocument.Parse("{ _id : \"Unknown\", years : [ ], count : 1 }"),
            };
            buckets.Should().Equal(expectedBuckets);
        }

        [Fact]
        public void Bucket_typed_should_add_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket(x => x.Year, boundaries, defaultBucket);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\" } }");
        }

        [SkippableFact]
        public void Bucket_typed_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket(x => x.Year, boundaries, defaultBucket);

            var buckets = result.ToList();
            buckets.Count.Should().Be(3);
            var expectedBuckets = new[]
            {
                new AggregateBucketResult<BsonValue>(1900, 1),
                new AggregateBucketResult<BsonValue>(1920, 2),
                new AggregateBucketResult<BsonValue>("Unknown", 1),
            };
            buckets.Should().Equal(expectedBuckets);
        }

        [Fact]
        public void Bucket_typed_with_output_should_add_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket(
                e => e.Year,
                boundaries,
                g => new { _id = default(BsonValue), Years = g.Select(e => e.Year), Count = g.Count() },
                defaultBucket);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\", output : { Years : { $push : \"$year\" }, Count : { $sum : 1 } } } }");
        }

        [SkippableFact]
        public void Bucket_typed_with_output_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var defaultBucket = (BsonValue)"Unknown";

            var result = subject.Bucket(
                e => e.Year,
                boundaries,
                g => new { _id = default(BsonValue), Years = g.Select(e => e.Year), Count = g.Count() },
                defaultBucket);

            var buckets = result.ToList();
            buckets.Count.Should().Be(3);
            buckets.Select(b => b._id).Should().Equal(1900, 1920, "Unknown");
            buckets[0].Years.Should().Equal(new[] { 1902 });
            buckets[1].Years.Should().Equal(new[] { 1926, 1925 });
            buckets[2].Years.Should().Equal(new int[0]);
            buckets.Select(b => b.Count).Should().Equal(1, 2, 1);
        }

        // nested types
        private class Exhibit
        {
            [BsonId]
            public int Id { get; set; }
            [BsonElement("title")]
            public string Title { get; set; }
            [BsonElement("year")]
            public int Year { get; set; }
            [BsonElement("tags")]
            public string[] Tags { get; set; }
        }
    }
}
