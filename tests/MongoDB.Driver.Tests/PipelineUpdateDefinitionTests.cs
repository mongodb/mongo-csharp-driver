/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PipelineUpdateDefinitionTests
    {
        [Fact]
        public void PipelineUpdateDefinition_should_return_expected_result()
        {
            var subject = CreateSubject("{ $addFields : { x : 2 } }");
            var result = subject.ToString();
            result.Should().Be("[{ \"$addFields\" : { \"x\" : 2 } }]");
        }

        [Fact]
        public void PipelineUpdateDefinition_should_work_with_pipeline_builder()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .AppendStage("{ $addFields : { x : 2 } }", BsonDocumentSerializer.Instance);
            var subject = CreateSubject(pipeline);
            var result = subject.ToString();
            result.Should().Be("[{ \"$addFields\" : { \"x\" : 2 } }]");
        }

        [Fact]
        public void PipelineUpdateDefinition_should_work_with_pipeline_builder_and_replaceWith()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .ReplaceWith((AggregateExpressionDefinition<BsonDocument, BsonDocument>)"{ _id : \"$_id\", s : { $sum : [\"$X\", \"$Y\"] } }");
            var subject = CreateSubject(pipeline);
            var result = subject.ToString();
            result.Should().Be("[{ \"$replaceWith\" : { \"_id\" : \"$_id\", \"s\" : { \"$sum\" : [\"$X\", \"$Y\"] } } }]");
        }

        private PipelineUpdateDefinition<BsonDocument> CreateSubject(params string[] stages)
        {
            return CreateSubject(PipelineDefinition<BsonDocument, BsonDocument>.Create(stages));
        }

        private PipelineUpdateDefinition<BsonDocument> CreateSubject(PipelineDefinition<BsonDocument, BsonDocument> pipeline)
        {
            return new PipelineUpdateDefinition<BsonDocument>(pipeline);
        }
    }
}
