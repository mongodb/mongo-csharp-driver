/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using System.Collections.Generic;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PipelineStageDefinitionBuilderTests
    {
        // public methods
        [Theory]
        [InlineData(null, "{ $changeStream : { fullDocument : 'default' } }")]
        [InlineData(false, "{ $changeStream : { fullDocument : 'default' } }")]
        [InlineData(true, "{ $changeStream : { fullDocument : 'default', allChangesForCluster : true } }")]
        public void ChangeStream_with_allChangesForCluster_should_return_the_expected_result(bool? allChangesForCluster, string expectedStage)
        {
            var options = new ChangeStreamStageOptions
            {
                AllChangesForCluster = allChangesForCluster
            };

            var result = PipelineStageDefinitionBuilder.ChangeStream<BsonDocument>(options);

            var stage = RenderStage(result);
            stage.Document.Should().Be(expectedStage);
        }

        [Theory]
        [InlineData(ChangeStreamFullDocumentOption.Default, "{ $changeStream : { fullDocument : 'default' } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, "{ $changeStream : { fullDocument : 'updateLookup' } }")]
        public void ChangeStream_with_fullDocument_should_return_the_expected_result(ChangeStreamFullDocumentOption fullDocument, string expectedStage)
        {
            var options = new ChangeStreamStageOptions
            {
                FullDocument = fullDocument
            };

            var result = PipelineStageDefinitionBuilder.ChangeStream<BsonDocument>(options);

            var stage = RenderStage(result);
            stage.Document.Should().Be(expectedStage);
        }

        [Theory]
        [InlineData(null, "{ $changeStream : { fullDocument : 'default' } }")]
        [InlineData("{ x : 1 }", "{ $changeStream : { fullDocument : 'default', resumeAfter : { x : 1 } } }")]
        public void ChangeStream_with_resumeAfter_should_return_the_expected_result(string resumeAfterJson, string expectedStage)
        {
            var resumeAfter = resumeAfterJson == null ? null : BsonDocument.Parse(resumeAfterJson);
            var options = new ChangeStreamStageOptions
            {
                ResumeAfter = resumeAfter
            };

            var result = PipelineStageDefinitionBuilder.ChangeStream<BsonDocument>(options);

            var stage = RenderStage(result);
            stage.Document.Should().Be(expectedStage);
        }

        [Theory]
        [InlineData(null, null, "{ $changeStream : { fullDocument : 'default' } }")]
        [InlineData(1, 2, "{ $changeStream : { fullDocument : 'default', startAtOperationTime : { $timestamp: { t : 1, i : 2 } } } }")]
        public void ChangeStream_with_startAtOperationTime_should_return_the_expected_result(int? t, int? i, string expectedStage)
        {
            var startAtOperationTime = t.HasValue ? new BsonTimestamp(t.Value, i.Value) : null;
            var options = new ChangeStreamStageOptions
            {
                StartAtOperationTime = startAtOperationTime
            };

            var result = PipelineStageDefinitionBuilder.ChangeStream<BsonDocument>(options);

            var stage = RenderStage(result);
            stage.Document.Should().Be(expectedStage);
        }

        [Fact]
        public void ChangeStream_should_return_the_expected_result_when_options_isNull()
        {
            ChangeStreamStageOptions options = null;

            var result = PipelineStageDefinitionBuilder.ChangeStream<BsonDocument>(options);

            var stage = RenderStage(result);
            stage.Document.Should().Be("{ $changeStream : { fullDocument : \"default\" } }");
        }

        // private methods
        private RenderedPipelineStageDefinition<ChangeStreamDocument<BsonDocument>> RenderStage(PipelineStageDefinition<BsonDocument, ChangeStreamDocument<BsonDocument>> stage)
        {
            return stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
        }
    }
}
