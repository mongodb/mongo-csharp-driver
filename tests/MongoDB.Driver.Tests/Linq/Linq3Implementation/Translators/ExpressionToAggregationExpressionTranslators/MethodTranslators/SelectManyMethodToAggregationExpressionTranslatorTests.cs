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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class SelectManyMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<SelectManyMethodToAggregationExpressionTranslatorTests.ClassFixture>
{
    private static readonly bool __concatArraysAndSetUnionAccumulatorsSupported = Feature.ConcatArraysAndSetUnionAccumulators.IsSupported(CoreTestConfiguration.MaxWireVersion);

    public SelectManyMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Enumerable_SelectMany_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Where(x => x.Id == 1).Select(x => x.B.SelectMany(a => a));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { _id : 1 } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', in : '$$a' } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void Enumerable_SelectMany_with_index_should_work()
    {
        RequireServer.Check().Supports(Feature.ArrayIndexAs);

        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Where(x => x.Id == 1).Select(x => x.B.SelectMany((a, i) => a.Select(y => y + i)));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { _id : 1 } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', arrayIndexAs : 'i', in : { $map : { input : '$$a', as : 'y', in : { $add : ['$$y', '$$i'] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 31);
    }

    [Fact]
    public void Queryable_SelectMany_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Where(x => x.Id == 1).Select(x => x.B.AsQueryable().SelectMany(a => a));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { _id : 1 } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', in : '$$a' } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void Queryable_SelectMany_with_index_should_work()
    {
        RequireServer.Check().Supports(Feature.ArrayIndexAs);

        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Where(x => x.Id == 1).Select(x => x.B.AsQueryable().SelectMany((a, i) => a.Select(y => y + i)));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { _id : 1 } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', arrayIndexAs : 'i', in : { $map : { input : '$$a', as : 'y', in : { $add : ['$$y', '$$i'] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 31);
    }

    [Fact]
    public void SelectMany_in_GroupBy_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .GroupBy(x => x.Cat)
            .Select(g => new { Cat = g.Key, AllTags = g.SelectMany(x => x.Tags).ToList() });

        var stages = Translate(collection, queryable);
        if (__concatArraysAndSetUnionAccumulatorsSupported)
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $concatArrays : '$Tags' } } }",
                "{ $project : { Cat : '$_id', AllTags : '$__agg0', _id : 0 } }");
        }
        else
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $push : '$Tags' } } }",
                "{ $project : { Cat : '$_id', AllTags : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");
        }

        var results = queryable.ToList().OrderBy(x => x.Cat).ToList();
        results.Should().HaveCount(2);
        results[0].Cat.Should().Be("A");
        results[0].AllTags.Should().BeEquivalentTo(new[] { "x", "y", "x", "z" });
        results[1].Cat.Should().Be("B");
        results[1].AllTags.Should().BeEquivalentTo(new[] { "y", "z" });
    }

    [Fact]
    public void SelectMany_Distinct_in_GroupBy_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .GroupBy(x => x.Cat)
            .Select(g => new { Cat = g.Key, UniqueTags = g.SelectMany(x => x.Tags).Distinct().ToList() });

        var stages = Translate(collection, queryable);
        if (__concatArraysAndSetUnionAccumulatorsSupported)
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $setUnion : '$Tags' } } }",
                "{ $project : { Cat : '$_id', UniqueTags : '$__agg0', _id : 0 } }");
        }
        else
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $push : '$Tags' } } }",
                "{ $project : { Cat : '$_id', UniqueTags : { $setUnion : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } } }, _id : 0 } }");
        }

        var results = queryable.ToList().OrderBy(x => x.Cat).ToList();
        results.Should().HaveCount(2);
        results[0].Cat.Should().Be("A");
        results[0].UniqueTags.Should().BeEquivalentTo(new[] { "x", "y", "z" });
        results[1].Cat.Should().Be("B");
        results[1].UniqueTags.Should().BeEquivalentTo(new[] { "y", "z" });
    }

    [Theory]
    [InlineData(ServerVersion.Server80)]
    [InlineData(ServerVersion.Server81)]
    public void SelectMany_with_index_in_GroupBy_should_not_be_rewritten_to_accumulator(ServerVersion compatibilityLevel)
    {
        var collection = Fixture.Collection;
        var options = new AggregateOptions { TranslationOptions = new ExpressionTranslationOptions { CompatibilityLevel = compatibilityLevel } };

        var queryable = collection.AsQueryable(options)
            .GroupBy(x => x.Cat)
            .Select(g => new { Cat = g.Key, AllTags = g.SelectMany((x, i) => x.Tags.Select(t => t + i)).ToList() });

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $group : { _id : '$Cat', _elements : { $push : '$$ROOT' } } }",
            "{ $project : { Cat : '$_id', AllTags : { $reduce : { input : { $map : { input : '$_elements', as : 'x', arrayIndexAs : 'i', in : { $map : { input : '$$x.Tags', as : 't', in : { $concat : ['$$t', { $toString : '$$i' }] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.Cat).ToList();
        results.Should().HaveCount(2);
        results[0].Cat.Should().Be("A");
        results[0].AllTags.Should().BeEquivalentTo(new[] { "x0", "y0", "x1", "z1" });
        results[1].Cat.Should().Be("B");
        results[1].AllTags.Should().BeEquivalentTo(new[] { "y0", "z0" });
    }

    [Theory]
    [InlineData(ServerVersion.Server80)]
    [InlineData(ServerVersion.Server81)]
    public void SelectMany_Distinct_with_index_in_GroupBy_should_not_be_rewritten_to_accumulator(ServerVersion compatibilityLevel)
    {
        var collection = Fixture.Collection;
        var options = new AggregateOptions { TranslationOptions = new ExpressionTranslationOptions { CompatibilityLevel = compatibilityLevel } };

        var queryable = collection.AsQueryable(options)
            .GroupBy(x => x.Cat)
            .Select(g => new { Cat = g.Key, UniqueTags = g.SelectMany((x, i) => x.Tags.Select(t => t + i)).Distinct().ToList() });

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $group : { _id : '$Cat', _elements : { $push : '$$ROOT' } } }",
            "{ $project : { Cat : '$_id', UniqueTags : { $setUnion : { $reduce : { input : { $map : { input : '$_elements', as : 'x', arrayIndexAs : 'i', in : { $map : { input : '$$x.Tags', as : 't', in : { $concat : ['$$t', { $toString : '$$i' }] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } } }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.Cat).ToList();
        results.Should().HaveCount(2);
        results[0].Cat.Should().Be("A");
        results[0].UniqueTags.Should().BeEquivalentTo(new[] { "x0", "y0", "x1", "z1" });
        results[1].Cat.Should().Be("B");
        results[1].UniqueTags.Should().BeEquivalentTo(new[] { "y0", "z0" });
    }

    [Fact]
    public void SelectMany_in_GroupBy_with_result_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .GroupBy(x => x.Cat, (key, elements) => new { Cat = key, AllTags = elements.SelectMany(x => x.Tags).ToList() });

        var stages = Translate(collection, queryable);
        if (__concatArraysAndSetUnionAccumulatorsSupported)
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $concatArrays : '$Tags' } } }",
                "{ $project : { Cat : '$_id', AllTags : '$__agg0', _id : 0 } }");
        }
        else
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $push : '$Tags' } } }",
                "{ $project : { Cat : '$_id', AllTags : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");
        }

        var results = queryable.ToList().OrderBy(x => x.Cat).ToList();
        results.Should().HaveCount(2);
        results[0].Cat.Should().Be("A");
        results[0].AllTags.Should().BeEquivalentTo(new[] { "x", "y", "x", "z" });
        results[1].Cat.Should().Be("B");
        results[1].AllTags.Should().BeEquivalentTo(new[] { "y", "z" });
    }

    [Fact]
    public void SelectMany_in_Bucket_should_work()
    {
        var collection = Fixture.Collection;

        var aggregate = collection.Aggregate()
            .Bucket(
                x => x.Id,
                new[] { 1, 3, 5 },
                g => new { Id = g.Key, AllTags = g.SelectMany(x => x.Tags) });

        var stages = Translate(collection, aggregate);
        if (__concatArraysAndSetUnionAccumulatorsSupported)
        {
            AssertStages(stages,
                "{ $bucket : { groupBy : '$_id', boundaries : [1, 3, 5], output : { __agg0 : { $concatArrays : '$Tags' } } } }",
                "{ $project : { _id : '$_id', AllTags : '$__agg0' } }");
        }
        else
        {
            AssertStages(stages,
                "{ $bucket : { groupBy : '$_id', boundaries : [1, 3, 5], output : { __agg0 : { $push : '$Tags' } } } }",
                "{ $project : { _id : '$_id', AllTags : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } } } }");
        }

        var results = aggregate.ToList().OrderBy(x => x.Id).ToList();
        results.Should().HaveCount(2);
        results[0].Id.Should().Be(1);
        results[0].AllTags.Should().BeEquivalentTo(new[] { "x", "y", "x", "z" });
        results[1].Id.Should().Be(3);
        results[1].AllTags.Should().BeEquivalentTo(new[] { "y", "z" });
    }

    [Fact]
    public void SelectMany_Distinct_in_BucketAuto_should_work()
    {
        var collection = Fixture.Collection;

        var aggregate = collection.Aggregate()
            .BucketAuto(
                x => x.Id,
                2,
                g => new { Id = g.Key, UniqueTags = g.SelectMany(x => x.Tags).Distinct() });

        var stages = Translate(collection, aggregate);
        if (__concatArraysAndSetUnionAccumulatorsSupported)
        {
            AssertStages(stages,
                "{ $bucketAuto : { groupBy : '$_id', buckets : 2, output : { __agg0 : { $setUnion : '$Tags' } } } }",
                "{ $project : { _id : '$_id', UniqueTags : '$__agg0' } }");
        }
        else
        {
            AssertStages(stages,
                "{ $bucketAuto : { groupBy : '$_id', buckets : 2, output : { __agg0 : { $push : '$Tags' } } } }",
                "{ $project : { _id : '$_id', UniqueTags : { $setUnion : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } } } } }");
        }

        var results = aggregate.ToList();
        results.Should().HaveCount(2);
        results[0].UniqueTags.Should().BeEquivalentTo(new[] { "x", "y", "z" });
        results[1].UniqueTags.Should().BeEquivalentTo(new[] { "y", "z" });
    }

    [Theory]
    [InlineData(ServerVersion.Server81, true)]
    [InlineData(ServerVersion.Server80, false)]
    public void SelectMany_in_GroupBy_should_emit_accumulator_only_when_CompatibilityLevel_supports_it(ServerVersion compatibilityLevel, bool expectAccumulator)
    {
        var collection = Fixture.Collection;
        var options = new AggregateOptions { TranslationOptions = new ExpressionTranslationOptions { CompatibilityLevel = compatibilityLevel } };

        var queryable = collection.AsQueryable(options)
            .GroupBy(x => x.Cat)
            .Select(g => new { Cat = g.Key, AllTags = g.SelectMany(x => x.Tags).ToList() });

        var stages = Translate(collection, queryable);
        if (expectAccumulator)
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $concatArrays : '$Tags' } } }",
                "{ $project : { Cat : '$_id', AllTags : '$__agg0', _id : 0 } }");
        }
        else
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $push : '$Tags' } } }",
                "{ $project : { Cat : '$_id', AllTags : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");
        }
    }

    [Theory]
    [InlineData(ServerVersion.Server81, true)]
    [InlineData(ServerVersion.Server80, false)]
    public void SelectMany_Distinct_in_GroupBy_should_emit_accumulator_only_when_CompatibilityLevel_supports_it(ServerVersion compatibilityLevel, bool expectAccumulator)
    {
        var collection = Fixture.Collection;
        var options = new AggregateOptions { TranslationOptions = new ExpressionTranslationOptions { CompatibilityLevel = compatibilityLevel } };

        var queryable = collection.AsQueryable(options)
            .GroupBy(x => x.Cat)
            .Select(g => new { Cat = g.Key, UniqueTags = g.SelectMany(x => x.Tags).Distinct().ToList() });

        var stages = Translate(collection, queryable);
        if (expectAccumulator)
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $setUnion : '$Tags' } } }",
                "{ $project : { Cat : '$_id', UniqueTags : '$__agg0', _id : 0 } }");
        }
        else
        {
            AssertStages(stages,
                "{ $group : { _id : '$Cat', __agg0 : { $push : '$Tags' } } }",
                "{ $project : { Cat : '$_id', UniqueTags : { $setUnion : { $reduce : { input : '$__agg0', initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } } }, _id : 0 } }");
        }
    }

    public class C
    {
        public int Id { get; set; }
        public int[][] B { get; set; }
        public string Cat { get; set; }
        public string[] Tags { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, B = new int[][] { [10, 20], [30] }, Cat = "A", Tags = new[] { "x", "y" } },
            new() { Id = 2, Cat = "A", Tags = new[] { "x", "z" } },
            new() { Id = 3, Cat = "B", Tags = new[] { "y", "z" } }
        ];
    }
}