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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3933Tests
    {
        [Fact]
        public void Aggregate_Group_with_projection_to_implied_BsonDocument_should_work()
        {
            var collection = GetCollection<BsonDocument>();

            var aggregate = collection.Aggregate()
                .Group("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Aggregate_Group_with_projection_to_TNewResult_should_work()
        {
            var collection = GetCollection<BsonDocument>();

            var aggregate = collection.Aggregate()
                .Group<Result>("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Aggregate_Group_with_expressions_should_work()
        {
            var collection = GetCollection<BsonDocument>();

            var aggregate = collection.Aggregate()
                .Group(x => 1, x => new { Count = x.Count() });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages =
                new[]
                {
                    "{ $group : { _id : 1, __agg0 : { $sum : 1 } } }",
                    "{ $project : { Count : '$__agg0', _id : 0 } }"
                };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineDefinitionBuilder_Group_with_projection_to_implied_BsonDocument_should_work()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineDefinitionBuilder_Group_with_projection_to_TOutput_should_work()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group<BsonDocument, BsonDocument, Result>("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineDefinitionBuilder_Group_with_expressions_should_work()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group(x => 1, x => new { Count = x.Count() });

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance);
            var expectedStages =
                new[]
                {
                    "{ $group : { _id : 1, __agg0 : { $sum : 1 } } }",
                    "{ $project : { Count : '$__agg0', _id : 0 } }"
                };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineStageDefinitionBuilder_Group_with_projection_to_implied_BsonDocument_should_work()
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group<BsonDocument>("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineStageDefinitionBuilder_Group_with_projection_to_TOutput_should_work()
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group<BsonDocument, BsonDocument>("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineStageDefinitionBuilder_Group_with_expressions_should_work()
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group((BsonDocument x) => 1, x => new { Count = x.Count() });

            var stages = Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance);
            var expectedStages =
                new[]
                {
                    "{ $group : { _id : 1, __agg0 : { $sum : 1 } } }",
                    "{ $project : { Count : '$__agg0', _id : 0 } }"
                };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<TDocument> GetCollection<TDocument>()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            return database.GetCollection<TDocument>("test");
        }

        private class Result
        {
            public int Id { get; set; }
            public int Count { get; set; }
        }
    }
}
