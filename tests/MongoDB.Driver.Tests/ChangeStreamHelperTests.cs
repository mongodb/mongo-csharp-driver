/* Copyright 2018-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ChangeStreamHelperTests
    {
        [Fact]
        public void CreateChangeStreamOperation_for_client_returns_expected_result()
        {
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Limit(1);
            var options = new ChangeStreamOptions
            {
                BatchSize = 123,
                Collation = new Collation("en-us"),
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var readConcern = new ReadConcern();
            var messageEncoderSettings = new MessageEncoderSettings();
            var renderedPipeline = RenderPipeline(pipeline);

            var result = ChangeStreamHelper.CreateChangeStreamOperation(pipeline, options, readConcern, messageEncoderSettings);

            result.BatchSize.Should().Be(options.BatchSize);
            result.Collation.Should().BeSameAs(options.Collation);
            result.CollectionNamespace.Should().BeNull();
            result.DatabaseNamespace.Should().BeNull();
            result.FullDocument.Should().Be(options.FullDocument);
            result.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            result.MessageEncoderSettings.Should().BeSameAs(messageEncoderSettings);
            result.Pipeline.Should().Equal(renderedPipeline.Documents);
            result.ReadConcern.Should().BeSameAs(readConcern);
            result.ResultSerializer.Should().BeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            result.ResumeAfter.Should().BeSameAs(options.ResumeAfter);
            result.StartAfter.Should().BeSameAs(options.StartAfter);
            result.StartAtOperationTime.Should().BeSameAs(options.StartAtOperationTime);
        }

        [Fact]
        public void CreateChangeStreamOperation_for_database_returns_expected_result()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase.SetupGet(m => m.DatabaseNamespace).Returns(databaseNamespace);          
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Limit(1);
            var options = new ChangeStreamOptions
            {
                BatchSize = 123,
                Collation = new Collation("en-us"),
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var readConcern = new ReadConcern();
            var messageEncoderSettings = new MessageEncoderSettings();
            var renderedPipeline = RenderPipeline(pipeline);

            var result = ChangeStreamHelper.CreateChangeStreamOperation(mockDatabase.Object, pipeline, options, readConcern, messageEncoderSettings);

            result.BatchSize.Should().Be(options.BatchSize);
            result.Collation.Should().BeSameAs(options.Collation);
            result.CollectionNamespace.Should().BeNull();
            result.DatabaseNamespace.Should().BeSameAs(databaseNamespace);
            result.FullDocument.Should().Be(options.FullDocument);
            result.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            result.MessageEncoderSettings.Should().BeSameAs(messageEncoderSettings);
            result.Pipeline.Should().Equal(renderedPipeline.Documents);
            result.ReadConcern.Should().BeSameAs(readConcern);
            result.ResultSerializer.Should().BeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            result.ResumeAfter.Should().BeSameAs(options.ResumeAfter);
            result.StartAfter.Should().BeSameAs(options.StartAfter);
            result.StartAtOperationTime.Should().BeSameAs(options.StartAtOperationTime);
        }

        [Fact]
        public void CreateChangeStreamOperation_for_collection_returns_expected_result()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var collectionNamespace = new CollectionNamespace(databaseNamespace, "collectionName");
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            mockCollection.SetupGet(m => m.CollectionNamespace).Returns(collectionNamespace);
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Limit(1);
            var documentSerializer = BsonDocumentSerializer.Instance;
            var options = new ChangeStreamOptions
            {
                BatchSize = 123,
                Collation = new Collation("en-us"),
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var readConcern = new ReadConcern();
            var messageEncoderSettings = new MessageEncoderSettings();
            var renderedPipeline = RenderPipeline(pipeline);

            var result = ChangeStreamHelper.CreateChangeStreamOperation(mockCollection.Object, pipeline, documentSerializer, options, readConcern, messageEncoderSettings);

            result.BatchSize.Should().Be(options.BatchSize);
            result.Collation.Should().BeSameAs(options.Collation);
            result.CollectionNamespace.Should().BeSameAs(collectionNamespace);
            result.DatabaseNamespace.Should().BeNull();
            result.FullDocument.Should().Be(options.FullDocument);
            result.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            result.MessageEncoderSettings.Should().BeSameAs(messageEncoderSettings);
            result.Pipeline.Should().Equal(renderedPipeline.Documents);
            result.ReadConcern.Should().BeSameAs(readConcern);
            result.ResultSerializer.Should().BeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            result.ResumeAfter.Should().BeSameAs(options.ResumeAfter);
            result.StartAfter.Should().BeSameAs(options.StartAfter);
            result.StartAtOperationTime.Should().BeSameAs(options.StartAtOperationTime);
        }

        // private methods
        private RenderedPipelineDefinition<ChangeStreamDocument<BsonDocument>> RenderPipeline(PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
            return pipeline.Render(inputSerializer, serializerRegistry);
        }
    }
}