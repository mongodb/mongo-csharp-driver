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
using Xunit;

namespace MongoDB.Driver.Tests;

public class AggregateScoreFusionTests : IntegrationTest<AggregateScoreFusionTests.ClassFixture>
{
    public AggregateScoreFusionTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.ScoreFusionStage))
    {
    }

    [Fact]
    public void ScoreFusion_with_named_pipelines_should_return_expected_result()
    {
        var collection = Fixture.Collection;

        var pipelines = new Dictionary<string, PipelineDefinition<ScoredItem, ScoredItem>>
        {
            { "byA", ScoreByPath("$A") },
            { "byB", ScoreByPath("$B") }
        };

        var result = collection.Aggregate()
            .ScoreFusion(pipelines, ScoreFusionNormalization.None)
            .ToList();

        result.Should().HaveCount(5);
        result[0].Id.Should().Be(1);
        result[4].Id.Should().Be(5);
    }

    [Fact]
    public void ScoreFusion_with_auto_named_pipelines_should_return_expected_result()
    {
        var collection = Fixture.Collection;

        var result = collection.Aggregate()
            .ScoreFusion(
                new[] { ScoreByPath("$A"), ScoreByPath("$B") },
                ScoreFusionNormalization.None)
            .ToList();

        result.Should().HaveCount(5);
        result[0].Id.Should().Be(1);
        result[4].Id.Should().Be(5);
    }

    [Fact]
    public void ScoreFusion_using_pipeline_weight_tuples_should_return_expected_result()
    {
        var collection = Fixture.Collection;

        var pipelines = new (PipelineDefinition<ScoredItem, ScoredItem>, double?)[]
        {
            (ScoreByPath("$A"), 1.0),
            (ScoreByPath("$B"), 0.1)
        };

        var result = collection.Aggregate()
            .ScoreFusion(pipelines, ScoreFusionNormalization.None)
            .ToList();

        result.Should().HaveCount(5);
        result[0].Id.Should().Be(5);
        result[4].Id.Should().Be(1);
    }

    [Fact]
    public void ScoreFusion_with_normalization_minMaxScaler_should_return_expected_result()
    {
        var collection = Fixture.Collection;

        var result = collection.Aggregate()
            .ScoreFusion(
                new[] { ScoreByPath("$A") },
                ScoreFusionNormalization.MinMaxScaler)
            .ToList();

        result.Should().HaveCount(5);
        result[0].Id.Should().Be(5);
        result[4].Id.Should().Be(1);
    }

    [Fact]
    public void ScoreFusion_with_method_expression_should_return_expected_result()
    {
        var collection = Fixture.Collection;

        var pipelines = new Dictionary<string, PipelineDefinition<ScoredItem, ScoredItem>>
        {
            { "byA", ScoreByPath("$A") },
            { "byB", ScoreByPath("$B") }
        };

        var options = new ScoreFusionOptions<ScoredItem>
        {
            CombinationMethod = ScoreFusionCombinationMethod.Expression,
            CombinationExpression = BsonDocument.Parse("{ $multiply: ['$$byA', '$$byB'] }")
        };

        var result = collection.Aggregate()
            .ScoreFusion(pipelines, ScoreFusionNormalization.None, weights: null, options)
            .ToList();

        result.Should().HaveCount(5);
        result[0].Id.Should().Be(3);
    }

    [Fact]
    public void ScoreFusion_with_score_details_should_return_expected_metadata()
    {
        var collection = Fixture.Collection;

        var pipelines = new Dictionary<string, PipelineDefinition<ScoredItem, ScoredItem>>
        {
            { "byA", ScoreByPath("$A") },
            { "byB", ScoreByPath("$B") }
        };

        var result = collection.Aggregate()
            .ScoreFusion(
                pipelines,
                ScoreFusionNormalization.None,
                weights: null,
                new ScoreFusionOptions<ScoredItem> { ScoreDetails = true })
            .Limit(1)
            .As<ScoreFusionResult>()
            .Project<ScoreFusionResult>(Builders<ScoreFusionResult>.Projection
                .Exclude("_id")
                .MetaScore(r => r.Score)
                .MetaScoreDetails(r => r.ScoreDetails))
            .ToList();

        result.Should().ContainSingle();
        result[0].Score.Should().BeGreaterThan(0);
        result[0].ScoreDetails.Should().NotBeNull();
        result[0].ScoreDetails.Description.Should().NotBeNullOrEmpty();
        result[0].ScoreDetails.Value.Should().Be(result[0].Score);
        result[0].ScoreDetails.Normalization.Should().Be("none");
        result[0].ScoreDetails.Combination.Should().NotBeNull();
        result[0].ScoreDetails.Details.Should().HaveCount(2);
    }

    private static PipelineDefinition<ScoredItem, ScoredItem> ScoreByPath(string path) =>
        new EmptyPipelineDefinition<ScoredItem>()
            .AppendStage<ScoredItem, ScoredItem, ScoredItem>(
                $"{{ $score: {{ score: '{path}', normalization: 'none' }} }}");

    public class ScoredItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double A { get; set; }
        public double B { get; set; }
    }

    public class ScoreFusionResult : ScoredItem
    {
        public double Score { get; set; }
        public ScoreFusionScoreDetails ScoreDetails { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<ScoredItem> Collection { get; private set; }

        protected override void InitializeFixture()
        {
            Collection = CreateCollection<ScoredItem>("scoreFusionCollection");
            Collection.InsertMany([
                new ScoredItem { Id = 1, Name = "apple",  A = 10, B = 100 },
                new ScoredItem { Id = 2, Name = "banana", A = 20, B = 80 },
                new ScoredItem { Id = 3, Name = "cherry", A = 30, B = 60 },
                new ScoredItem { Id = 4, Name = "date",   A = 40, B = 40 },
                new ScoredItem { Id = 5, Name = "elder",  A = 50, B = 20 }
            ]);
        }
    }
}
