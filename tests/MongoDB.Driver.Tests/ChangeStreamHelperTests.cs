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
using Shouldly;
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
                FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.WhenAvailable,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var readConcern = new ReadConcern();
            var messageEncoderSettings = new MessageEncoderSettings();
            var renderedPipeline = RenderPipeline(pipeline);

            var result = ChangeStreamHelper.CreateChangeStreamOperation(pipeline, options, readConcern, messageEncoderSettings, retryRequested: true, translationOptions: null);

            result.BatchSize.ShouldBe(options.BatchSize);
            result.Collation.ShouldBeSameAs(options.Collation);
            result.CollectionNamespace.ShouldBeNull();
            result.DatabaseNamespace.ShouldBeNull();
            result.FullDocument.ShouldBe(options.FullDocument);
            result.FullDocumentBeforeChange.ShouldBe(options.FullDocumentBeforeChange);
            result.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            result.MessageEncoderSettings.ShouldBeSameAs(messageEncoderSettings);
            result.Pipeline.ShouldBe(renderedPipeline.Documents);
            result.ReadConcern.ShouldBeSameAs(readConcern);
            result.ResultSerializer.ShouldBeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            result.ResumeAfter.ShouldBeSameAs(options.ResumeAfter);
            result.RetryRequested.ShouldBe(true);
            result.StartAfter.ShouldBeSameAs(options.StartAfter);
            result.StartAtOperationTime.ShouldBeSameAs(options.StartAtOperationTime);
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
                FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Required,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var readConcern = new ReadConcern();
            var messageEncoderSettings = new MessageEncoderSettings();
            var renderedPipeline = RenderPipeline(pipeline);

            var result = ChangeStreamHelper.CreateChangeStreamOperation(mockDatabase.Object, pipeline, options, readConcern, messageEncoderSettings, retryRequested: true, translationOptions: null);

            result.BatchSize.ShouldBe(options.BatchSize);
            result.Collation.ShouldBeSameAs(options.Collation);
            result.CollectionNamespace.ShouldBeNull();
            result.DatabaseNamespace.ShouldBeSameAs(databaseNamespace);
            result.FullDocument.ShouldBe(options.FullDocument);
            result.FullDocumentBeforeChange.ShouldBe(options.FullDocumentBeforeChange);
            result.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            result.MessageEncoderSettings.ShouldBeSameAs(messageEncoderSettings);
            result.Pipeline.ShouldBe(renderedPipeline.Documents);
            result.ReadConcern.ShouldBeSameAs(readConcern);
            result.ResultSerializer.ShouldBeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            result.ResumeAfter.ShouldBeSameAs(options.ResumeAfter);
            result.RetryRequested.ShouldBe(true);
            result.StartAfter.ShouldBeSameAs(options.StartAfter);
            result.StartAtOperationTime.ShouldBeSameAs(options.StartAtOperationTime);
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
                FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Off,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var readConcern = new ReadConcern();
            var messageEncoderSettings = new MessageEncoderSettings();
            var renderedPipeline = RenderPipeline(pipeline);

            var result = ChangeStreamHelper.CreateChangeStreamOperation(mockCollection.Object, pipeline, documentSerializer, options, readConcern, messageEncoderSettings, retryRequested: true, translationOptions: null);

            result.BatchSize.ShouldBe(options.BatchSize);
            result.Collation.ShouldBeSameAs(options.Collation);
            result.CollectionNamespace.ShouldBeSameAs(collectionNamespace);
            result.DatabaseNamespace.ShouldBeNull();
            result.FullDocument.ShouldBe(options.FullDocument);
            result.FullDocumentBeforeChange.ShouldBe(options.FullDocumentBeforeChange);
            result.MaxAwaitTime.ShouldBe(options.MaxAwaitTime);
            result.MessageEncoderSettings.ShouldBeSameAs(messageEncoderSettings);
            result.Pipeline.ShouldBe(renderedPipeline.Documents);
            result.ReadConcern.ShouldBeSameAs(readConcern);
            result.ResultSerializer.ShouldBeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            result.ResumeAfter.ShouldBeSameAs(options.ResumeAfter);
            result.RetryRequested.ShouldBe(true);
            result.StartAfter.ShouldBeSameAs(options.StartAfter);
            result.StartAtOperationTime.ShouldBeSameAs(options.StartAtOperationTime);
        }

        // private methods
        private RenderedPipelineDefinition<ChangeStreamDocument<BsonDocument>> RenderPipeline(PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
            return pipeline.Render(new(inputSerializer, serializerRegistry, translationOptions: null));
        }
    }
}
