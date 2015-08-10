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
        public void As_should_add_the_expected_stage()
        {
            var subject = CreateSubject();

            var result = subject
                .Match("{ X : 1 }")
                .As<BsonDocument>();
            var cursor = result.ToCursorAsync().GetAwaiter().GetResult();

            Predicate<PipelineDefinition<C, BsonDocument>> isExpectedPipeline = pipeline => 
            {
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var inputSerializer = serializerRegistry.GetSerializer<C>();
                var rendederedPipeline = pipeline.Render(inputSerializer, serializerRegistry);
                return 
                    rendederedPipeline.Documents.Count == 1 && 
                    rendederedPipeline.Documents[0] == BsonDocument.Parse("{ $match : { X : 1 } }") &&
                    rendederedPipeline.OutputSerializer is BsonDocumentSerializer;
            };

            _collection.Received().AggregateAsync<BsonDocument>(
                Arg.Is<PipelineDefinition<C, BsonDocument>>(pipeline => isExpectedPipeline(pipeline)),
                Arg.Any<AggregateOptions>(),
                CancellationToken.None);
        }

        [Test]
        public void OfType_should_add_the_expected_stage()
        {
            var subject = CreateSubject();

            var result = subject
                .SortBy(c => c.X)
                .OfType<D>()
                .Match(d => d.Y == 2);
            var cursor = result.ToCursorAsync().GetAwaiter().GetResult();

            Predicate<PipelineDefinition<C, D>> isExpectedPipeline = pipeline =>
            {
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var inputSerializer = serializerRegistry.GetSerializer<C>();
                var rendederedPipeline = pipeline.Render(inputSerializer, serializerRegistry);
                var isExpected =
                    rendederedPipeline.Documents.Count == 3 &&
                    rendederedPipeline.Documents[0] == BsonDocument.Parse("{ $sort : { X : 1 } }") &&
                    rendederedPipeline.Documents[1] == BsonDocument.Parse("{ $match : { _t : \"D\" } }") &&
                    rendederedPipeline.Documents[2] == BsonDocument.Parse("{ $match : { Y : 2 } }") &&
                    rendederedPipeline.OutputSerializer.ValueType == typeof(D);
                return isExpected;
            };

            _collection.Received().AggregateAsync<D>(
                Arg.Is<PipelineDefinition<C, D>>(pipeline => isExpectedPipeline(pipeline)),
                Arg.Any<AggregateOptions>(),
                CancellationToken.None);
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
