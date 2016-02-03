/* Copyright 2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class AggregateFluentTests
    {
        private IMongoCollection<C> _collection;

        [Test]
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

                _collection.Received().AggregateAsync<BsonDocument>(
                    Arg.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
            else
            {
                result.ToCursor();

                _collection.Received().Aggregate<BsonDocument>(
                    Arg.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
        }

        [Test]
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

                _collection.Received().AggregateAsync<D>(
                    Arg.Is<PipelineDefinition<C, D>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
            else
            {
                result.ToCursor();

                _collection.Received().Aggregate<D>(
                    Arg.Is<PipelineDefinition<C, D>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
        }

        [Test]
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
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $match : { X : 1 } } }") &&
                    renderedPipeline.Documents[1] == BsonDocument.Parse("{ $out : \"test\" }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(C);
            };

            IAsyncCursor<C> cursor;
            if (async)
            {
                cursor = subject.OutAsync(collectionName, CancellationToken.None).GetAwaiter().GetResult();

                _collection.Received().AggregateAsync<C>(
                    Arg.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
            else
            {
                cursor = subject.Out(collectionName, CancellationToken.None);

                _collection.Received().Aggregate<C>(
                    Arg.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
        }

        [Test]
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
                    renderedPipeline.Documents[0] == BsonDocument.Parse("{ $match : { X : 1 } } }") &&
                    renderedPipeline.OutputSerializer.ValueType == typeof(C);
            };

            IAsyncCursor<C> cursor;
            if (async)
            {
                cursor = subject.ToCursorAsync(CancellationToken.None).GetAwaiter().GetResult();

                _collection.Received().AggregateAsync<C>(
                    Arg.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
            else
            {
                cursor = subject.ToCursor(CancellationToken.None);

                _collection.Received().Aggregate<C>(
                    Arg.Is<PipelineDefinition<C, C>>(pipeline => isExpectedPipeline(pipeline)),
                    Arg.Any<AggregateOptions>(),
                    CancellationToken.None);
            }
        }

        // private methods
        private IAggregateFluent<C> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            _collection = Substitute.For<IMongoCollection<C>>();
            _collection.DocumentSerializer.Returns(BsonSerializer.SerializerRegistry.GetSerializer<C>());
            _collection.Settings.Returns(settings);
            var options = new AggregateOptions();
            var subject = new AggregateFluent<C, C>(_collection, Enumerable.Empty<IPipelineStageDefinition>(), options);

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
}
