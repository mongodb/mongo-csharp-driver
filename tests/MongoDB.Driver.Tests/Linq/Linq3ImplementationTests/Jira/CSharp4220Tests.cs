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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4220Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Test_a_pipeline_stage_using_aggregate_with_aggregate_expression_should_work()
        {
            RequireServer.Check().Supports(Feature.DocumentsStage);
            var database = GetDatabase();
            var json =
                @"
                [
                    { X : 10 },
                    { X : 2 },
                    { X : 5 }
                ]
                ";
            var array = BsonSerializer.Deserialize<BsonArray>(json);
            var documents = new BsonValueAggregateExpressionDefinition<NoPipelineInput, IEnumerable<C>>(array);

            var pipeline =
                new EmptyPipelineDefinition<NoPipelineInput>()
                .Documents(documents)
                .BucketAuto(groupBy: d => d.X, buckets: 4);

            var stages = Translate(pipeline);
            AssertStages(
                stages,
                "{ $documents : [{ X : 10 }, { X : 2 }, { X : 5 }] }",
                "{ $bucketAuto : { groupBy : '$X', buckets : 4 } }");

            var results = database.Aggregate(pipeline).ToList();
            var comparer = AggregateBucketAutoResultEqualityComparer<int>.Instance;
            results.OrderBy(r => r.Id.Min).WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<int>(2, 5, 1),
                new AggregateBucketAutoResult<int>(5, 10, 1),
                new AggregateBucketAutoResult<int>(10, 10, 1));
        }

        [Fact]
        public void Test_a_pipeline_stage_using_aggregate_with_enumerable_documents_should_work()
        {
            RequireServer.Check().Supports(Feature.DocumentsStage);
            var database = GetDatabase();
            var documents = new C[]
            {
                new C { X = 10 },
                new C { X = 2 },
                new C { X = 5 }
            };

            var pipeline =
                new EmptyPipelineDefinition<NoPipelineInput>()
                .Documents(documents)
                .BucketAuto(groupBy: d => d.X, buckets: 4);

            var stages = Translate(pipeline);
            AssertStages(
                stages,
                "{ $documents : [{ X : 10 }, { X : 2 }, { X : 5 }] }",
                "{ $bucketAuto : { groupBy : '$X', buckets : 4 } }");

            var results = database.Aggregate(pipeline).ToList();
            var comparer = AggregateBucketAutoResultEqualityComparer<int>.Instance;
            results.OrderBy(r => r.Id.Min).WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<int>(2, 5, 1),
                new AggregateBucketAutoResult<int>(5, 10, 1),
                new AggregateBucketAutoResult<int>(10, 10, 1));
        }

        [Fact]
        public void Test_a_pipeline_stage_using_fluent_aggregate_with_aggregate_expression_should_work()
        {
            RequireServer.Check().Supports(Feature.DocumentsStage);
            var database = GetDatabase();
            var json =
                @"
                [
                    { X : 10 },
                    { X : 2 },
                    { X : 5 }
                ]
                ";
            var array = BsonSerializer.Deserialize<BsonArray>(json);
            var documents = new BsonValueAggregateExpressionDefinition<NoPipelineInput, IEnumerable<C>>(array);

            var aggregate = database.Aggregate()
                .Documents(documents)
                .BucketAuto(groupBy: d => d.X, buckets: 4);

            var stages = Translate(database, aggregate);
            AssertStages(
                stages,
                "{ $documents : [{ X : 10 }, { X : 2 }, { X : 5 }] }",
                "{ $bucketAuto : { groupBy : '$X', buckets : 4 } }");

            var results = aggregate.ToList();
            var comparer = AggregateBucketAutoResultEqualityComparer<int>.Instance;
            results.OrderBy(r => r.Id.Min).WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<int>(2, 5, 1),
                new AggregateBucketAutoResult<int>(5, 10, 1),
                new AggregateBucketAutoResult<int>(10, 10, 1));
        }

        [Fact]
        public void Test_a_pipeline_stage_using_fluent_aggregate_with_enumerable_documents_should_work()
        {
            RequireServer.Check().Supports(Feature.DocumentsStage);
            var database = GetDatabase();
            var documents = new C[]
            {
                new C { X = 10 },
                new C { X = 2 },
                new C { X = 5 }
            };

            var aggregate = database.Aggregate()
                .Documents(documents)
                .BucketAuto(groupBy: d => d.X, buckets: 4);

            var stages = Translate(database, aggregate);
            AssertStages(
                stages,
                "{ $documents : [{ X : 10 }, { X : 2 }, { X : 5 }] }",
                "{ $bucketAuto : { groupBy : '$X', buckets : 4 } }");

            var results = aggregate.ToList();
            var comparer = AggregateBucketAutoResultEqualityComparer<int>.Instance;
            results.OrderBy(r => r.Id.Min).WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<int>(2, 5, 1),
                new AggregateBucketAutoResult<int>(5, 10, 1),
                new AggregateBucketAutoResult<int>(10, 10, 1));
        }

        [Theory]
        [ParameterAttributeData]
        public void Documents_with_documents_of_type_NoPipelineInput_should_throw(
            [Values(0, 1, 2)] int count)
        {
            var documents = new NoPipelineInput[count];

            var exception = Record.Exception(() =>
                {
                    new EmptyPipelineDefinition<NoPipelineInput>()
                    .Documents(documents);
                });

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("documents");
            argumentException.Message.Should().Contain("Documents cannot be of type NoPipelineInput.");
        }

        private class C
        {
            public int X { get; set; }
        }
    }
}
