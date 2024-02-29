/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchDefinitionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("{ operator: 'parameter' }")]
        public void BsonDocument_implicit_cast_should_create_BsonSearchDefinition(string searchDefinitionJson)
        {
            var searchDefinitionDocument = searchDefinitionJson != null ? BsonDocument.Parse(searchDefinitionJson) : null;
            var searchDefinition = (SearchDefinition<BsonDocument>)searchDefinitionDocument;

            if (searchDefinitionJson == null)
            {
                searchDefinition.Should().BeNull();
            }
            else
            {
                var subject = searchDefinition.Should().BeOfType<BsonDocumentSearchDefinition<BsonDocument>>().Subject;
                subject.Document.Should().Be(searchDefinitionDocument);
            }
        }

        [Fact]
        public void Pipeline_with_BsonDocumentSearchDefinition_should_render_correctly_multiple_times()
        {
            var searchOptions = new SearchOptions<BsonDocument>()
            {
                IndexName = "test",
                ScoreDetails = true,
            };

            var document = new BsonDocument("operator", "parameter");
            var searchDefinition = (BsonDocumentSearchDefinition<BsonDocument>)document;

            var pipeline = (new EmptyPipelineDefinition<BsonDocument>()).Search(searchDefinition, searchOptions);

            var renderedPipeline = pipeline.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
            AssertStages();

            renderedPipeline = pipeline.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
            AssertStages();

            void AssertStages()
            {
                var stages = renderedPipeline.Documents;
                stages.Count.Should().Be(1);
                stages[0].Should().Be("{ $search: { operator: 'parameter', index: 'test', scoreDetails: true} }");
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("{ operator: 'parameter' }")]
        public void String_implicit_cast_should_create_JsonSearchDefinition(string searchDefinitionJson)
        {
            var searchDefinition = (SearchDefinition<BsonDocument>)searchDefinitionJson;

            if (searchDefinitionJson == null)
            {
                searchDefinition.Should().BeNull();
            }
            else
            {
                var subject = searchDefinition.Should().BeOfType<JsonSearchDefinition<BsonDocument>>().Subject;
                subject.Json.Should().Be(searchDefinitionJson);
            }
        }
    }
}
