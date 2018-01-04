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
        [InlineData(ChangeStreamFullDocumentOption.Default, null, "{ $changeStream : { fullDocument : \"default\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, null, "{ $changeStream : { fullDocument : \"updateLookup\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.Default, "{ a : 1 }", "{ $changeStream : { fullDocument : \"default\", resumeAfter : { a : 1 } } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, "{ a : 1 }", "{ $changeStream : { fullDocument : \"updateLookup\", resumeAfter : { a : 1 } } }")]
        public void ChangeStream_should_return_the_expected_result(
            ChangeStreamFullDocumentOption fullDocument,
            string resumeAfterString,
            string expectedStage)
        {
            var resumeAfter = resumeAfterString == null ? null : BsonDocument.Parse(resumeAfterString);
            var options = new ChangeStreamStageOptions
            {
                FullDocument = fullDocument,
                ResumeAfter = resumeAfter
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
