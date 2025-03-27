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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateRankFusionTests : IntegrationTest<AggregateRankFusionTests.ClassFixture>
    {
        public AggregateRankFusionTests(ClassFixture fixture)
            : base(fixture, server =>  server.Supports(Feature.RankFusionStage))
        {
        }

        [Fact]
        public void RankFusion_with_named_pipelines_should_return_expected_result()
        {
            var collection = Fixture.Collection;

            var pipelines = new Dictionary<string, PipelineDefinition<SimplePerson, SimplePerson>>
            {
                {
                    "match1",
                    new EmptyPipelineDefinition<SimplePerson>()
                        .Match(p => p.Name == "John")
                        .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age))
                },
                {
                    "match2",
                    new EmptyPipelineDefinition<SimplePerson>()
                        .Match(p => p.Name == "Jane")
                        .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age))
                }
            };

            var weights = new Dictionary<string, double>
            {
                { "match2", 2.0 }
            };

            var result = collection.Aggregate()
                .RankFusion(pipelines, weights)
                .Project<SimplePerson>(Builders<SimplePerson>.Projection.Exclude("_id"))
                .ToList();

            result.Count.Should().Be(4);
            result[0].Age.Should().Be(34);
            result[1].Age.Should().Be(43);
            result[2].Age.Should().Be(24);
            result[3].Age.Should().Be(42);
        }

        [Fact]
        public void RankFusion_without_named_pipelines_should_return_expected_result()
        {
            var collection = Fixture.Collection;

            var pipelines = new[]
            {
                new EmptyPipelineDefinition<SimplePerson>()
                    .Match(p => p.Name == "John")
                    .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age)),

                new EmptyPipelineDefinition<SimplePerson>()
                    .Match(p => p.Name == "Jane")
                    .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age))
            };

            var result = collection.Aggregate()
                .RankFusion(pipelines)
                .Project<SimplePerson>(Builders<SimplePerson>.Projection.Exclude("_id"))
                .ToList();

            result.Count.Should().Be(4);
            result[0].Age.Should().Be(24);
            result[1].Age.Should().Be(34);
            result[2].Age.Should().Be(42);
            result[3].Age.Should().Be(43);
        }

        [Fact]
        public void RankFusion_using_pipeline_weight_tuples_should_return_expected_result()
        {
            var collection = Fixture.Collection;

            var pipelines = new (PipelineDefinition<SimplePerson, SimplePerson>, double?)[]
            {
                (new EmptyPipelineDefinition<SimplePerson>()
                    .Match(p => p.Name == "John")
                    .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age)), 0.4),

                (new EmptyPipelineDefinition<SimplePerson>()
                    .Match(p => p.Name == "Jane")
                    .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age)), 0.6)
            };

            var result = collection.Aggregate()
                .RankFusion(pipelines)
                .Project<SimplePerson>(Builders<SimplePerson>.Projection.Exclude("_id"))
                .ToList();

            result.Count.Should().Be(4);
            result[0].Age.Should().Be(34);
            result[1].Age.Should().Be(43);
            result[2].Age.Should().Be(24);
            result[3].Age.Should().Be(42);
        }

        [Fact]
        public void RankFusion_with_score_details_should_return_expected_result()
        {
            var collection = Fixture.Collection;

            var pipelines = new[]
            {
                new EmptyPipelineDefinition<SimplePerson>()
                    .Match(p => p.Name == "John")
                    .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age)),

                new EmptyPipelineDefinition<SimplePerson>()
                    .Match(p => p.Name == "Jane")
                    .Sort(Builders<SimplePerson>.Sort.Ascending(p => p.Age))
            };

            var result = collection.Aggregate()
                .RankFusion(pipelines, new RankFusionOptions<SimplePerson> { ScoreDetails = true})
                .Limit(1)
                .As<RankFusionResult>()
                .Project<RankFusionResult>(Builders<RankFusionResult>.Projection
                    .Exclude("_id")
                    .MetaScore(r => r.Score)
                    .MetaScoreDetails(r => r.ScoreDetails))
                .ToList();

            result.Should().ContainSingle();
            result[0].Score.Should().BeGreaterThan(0);
            result[0].ScoreDetails.Should().NotBeNull();

            result[0].ScoreDetails.Description.Should().NotBeNullOrEmpty();
            result[0].ScoreDetails.Value.Should().Be(result[0].Score);
            result[0].ScoreDetails.Details.Should().NotBeEmpty();
        }

        public class SimplePerson
        {
            public ObjectId Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class RankFusionResult : SimplePerson
        {
            public double Score { get; set; }
            public RankFusionScoreDetails ScoreDetails { get; set; }
        }

        public sealed class ClassFixture : MongoDatabaseFixture
        {
            public IMongoCollection<SimplePerson> Collection { get; private set; }

            protected override void InitializeFixture()
            {
                Collection = CreateCollection<SimplePerson>("personCollection");
                Collection.InsertMany([
                    new SimplePerson { Name = "John", Age = 42 },
                    new SimplePerson { Name = "Jane", Age = 43 },
                    new SimplePerson { Name = "John", Age = 24 },
                    new SimplePerson { Name = "Jane", Age = 34 }
                ]);
            }
        }
    }
}