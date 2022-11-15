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

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3933Tests
    {
        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Group_with_projection_to_implied_BsonDocument_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection<BsonDocument>(linqProvider);

            var aggregate = collection.Aggregate()
                .Group("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Group_with_projection_to_TNewResult_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection<BsonDocument>(linqProvider);

            var aggregate = collection.Aggregate()
                .Group<Result>("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Group_with_expressions_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection<BsonDocument>(linqProvider);

            var aggregate = collection.Aggregate()
                .Group(x => 1, x => new { Count = x.Count() });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = linqProvider switch
            {
                LinqProvider.V2 =>
                    new[]
                    {
                        "{ $group : { _id : 1, Count : { $sum : 1 } } }"
                    },
                LinqProvider.V3 =>
                    new[]
                    {
                        "{ $group : { _id : 1, __agg0 : { $sum : 1 } } }",
                        "{ $project : { Count : '$__agg0', _id : 0 } }"
                    },
                _ => throw new ArgumentException()
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void PipelineDefinitionBuilder_Group_with_projection_to_implied_BsonDocument_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance, linqProvider);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void PipelineDefinitionBuilder_Group_with_projection_to_TOutput_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group<BsonDocument, BsonDocument, Result>("{ _id : 1, Count : { $sum : 1 } }");

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance, linqProvider);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineDefinitionBuilder_Group_with_expressions_should_work_with_LINQ2()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group(x => 1, x => new { Count = x.Count() });

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance, LinqProvider.V2);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void PipelineDefinitionBuilder_Group_with_expressions_should_throw_with_LINQ3()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.Group(x => 1, x => new { Count = x.Count() });

            var exception = Record.Exception(() => Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance, LinqProvider.V3));
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void PipelineDefinitionBuilder_GroupForLinq3_with_expressions_should_throw_with_LINQ2()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.GroupForLinq3(x => 1, x => new { Count = x.Count() });

            var exception = Record.Exception(() => Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance, LinqProvider.V2));
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void PipelineDefinitionBuilder_GroupForLinq3_with_expressions_should_work_with_LINQ3()
        {
            var emptyPipeline = (PipelineDefinition<BsonDocument, BsonDocument>)new EmptyPipelineDefinition<BsonDocument>();

            var pipeline = emptyPipeline.GroupForLinq3(x => 1, x => new { Count = x.Count() });

            var stages = Linq3TestHelpers.Render(pipeline, BsonDocumentSerializer.Instance, LinqProvider.V3);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, __agg0 : { $sum : 1 } } }",
                "{ $project : { Count : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void PipelineStageDefinitionBuilder_Group_with_projection_to_implied_BsonDocument_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group<BsonDocument>("{ _id : 1, Count : { $sum : 1 } }");

            var stage = Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance, linqProvider);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(new[] { stage }, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void PipelineStageDefinitionBuilder_Group_with_projection_to_TOutput_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group<BsonDocument, BsonDocument>("{ _id : 1, Count : { $sum : 1 } }");

            var stage = Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance, linqProvider);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(new[] { stage }, expectedStages);
        }

        [Fact]
        public void PipelineStageDefinitionBuilder_Group_with_expressions_should_work_with_LINQ2()
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group((BsonDocument x) => 1, x => new { Count = x.Count() });

            var stage = Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance, LinqProvider.V2);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, Count : { $sum : 1 } } }"
            };
            Linq3TestHelpers.AssertStages(new[] { stage }, expectedStages);
        }

        [Fact]
        public void PipelineStageDefinitionBuilder_Group_with_expressions_should_throw_with_LINQ3()
        {
            var stageDefinition = PipelineStageDefinitionBuilder.Group((BsonDocument x) => 1, x => new { Count = x.Count() });

            var exception = Record.Exception(() => Linq3TestHelpers.Render(stageDefinition, BsonDocumentSerializer.Instance, LinqProvider.V3));
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void PipelineStageDefinitionBuilder_GroupForLinq3_with_expressions_should_throw_with_LINQ2()
        {
            var (groupStageDefinition, projectStageDefinition) = PipelineStageDefinitionBuilder.GroupForLinq3((BsonDocument x) => 1, x => new { Count = x.Count() });

            var exception = Record.Exception(() => Linq3TestHelpers.Render(groupStageDefinition, BsonDocumentSerializer.Instance, LinqProvider.V2));
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void PipelineStageDefinitionBuilderGroupForLinq3_with_expressions_should_work_with_LINQ3()
        {
            var (groupStageDefinition, projectStageDefinition) = PipelineStageDefinitionBuilder.GroupForLinq3((BsonDocument x) => 1, x => new { Count = x.Count() });

            var groupStage = Linq3TestHelpers.Render(groupStageDefinition, BsonDocumentSerializer.Instance, LinqProvider.V3);
            var groupingSerializer = new IGroupingSerializer<int, BsonDocument>(new Int32Serializer(), BsonDocumentSerializer.Instance);
            var projectStage = Linq3TestHelpers.Render(projectStageDefinition, groupingSerializer, LinqProvider.V3);
            var expectedStages = new[]
            {
                "{ $group : { _id : 1, __agg0 : { $sum : 1 } } }",
                "{ $project : { Count : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(new[] { groupStage, projectStage }, expectedStages);
        }

        private IMongoCollection<TDocument> GetCollection<TDocument>(LinqProvider linqProvider)
        {
            var client = linqProvider == LinqProvider.V2 ? DriverTestConfiguration.Linq2Client : DriverTestConfiguration.Linq3Client;
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
