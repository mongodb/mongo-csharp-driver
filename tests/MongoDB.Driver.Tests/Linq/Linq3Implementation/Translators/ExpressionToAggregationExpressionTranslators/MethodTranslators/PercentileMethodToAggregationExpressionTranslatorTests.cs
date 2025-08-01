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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class PercentileMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<PercentileMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public PercentileMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture, server => server.Supports(Feature.PercentileOperator))
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Percentile(new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Decimals.Percentile(new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Decimals', p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0);
            results[1].Should().Equal(1.0);
            results[2].Should().Equal(2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_decimals_multiple_percentiles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Percentile(new[] { 0.25, 0.75 })) :
                collection.AsQueryable().Select(x => x.Decimals.Percentile(new[] { 0.25, 0.75 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Decimals', p : [0.25, 0.75], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0, 1.0);
            results[1].Should().Equal(1.0, 2.0);
            results[2].Should().Equal(1.0, 3.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Percentile(y => y * 2.0M, new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Decimals.Percentile(y => y * 2.0M, new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : { $map : { input : '$Decimals', as : 'y', in : { $multiply : ['$$y', NumberDecimal(2)] } } }, p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(2.0);
            results[1].Should().Equal(2.0);
            results[2].Should().Equal(4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Percentile(new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Doubles.Percentile(new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Doubles', p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0);
            results[1].Should().Equal(1.0);
            results[2].Should().Equal(2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Percentile(y => y * 2.0, new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Doubles.Percentile(y => y * 2.0, new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : { $map : { input : '$Doubles', as : 'y', in : { $multiply : ['$$y', 2.0] } } }, p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(2.0);
            results[1].Should().Equal(2.0);
            results[2].Should().Equal(4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Percentile(new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Floats.Percentile(new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Floats', p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0);
            results[1].Should().Equal(1.0);
            results[2].Should().Equal(2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Percentile(y => y * 2.0F, new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Floats.Percentile(y => y * 2.0F, new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : { $map : { input : '$Floats', as : 'y', in : { $multiply : ['$$y', 2.0] } } }, p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(2.0);
            results[1].Should().Equal(2.0);
            results[2].Should().Equal(4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Percentile(new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Ints.Percentile(new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Ints', p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0);
            results[1].Should().Equal(1.0);
            results[2].Should().Equal(2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Percentile(y => y * 2, new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Ints.Percentile(y => y * 2, new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : { $map : { input : '$Ints', as : 'y', in : { $multiply : ['$$y', 2] } } }, p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(2.0);
            results[1].Should().Equal(2.0);
            results[2].Should().Equal(4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Percentile(new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Longs.Percentile(new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Longs', p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0);
            results[1].Should().Equal(1.0);
            results[2].Should().Equal(2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Percentile(y => y * 2L, new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.Longs.Percentile(y => y * 2L, new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : { $map : { input : '$Longs', as : 'y', in : { $multiply : ['$$y', NumberLong(2)] } } }, p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(2.0);
            results[1].Should().Equal(2.0);
            results[2].Should().Equal(4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_nullable_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Percentile(new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Percentile(new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$NullableDecimals', p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal((double?)null);
            results[1].Should().Equal((double?)null);
            results[2].Should().Equal(2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Percentile_with_nullable_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Percentile(y => y * 2.0M, new[] { 0.5 })) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Percentile(y => y * 2.0M, new[] { 0.5 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : { $map : { input : '$NullableDecimals', as : 'y', in : { $multiply : ['$$y', NumberDecimal(2)] } } }, p : [0.5], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal((double?)null);
            results[1].Should().Equal((double?)null);
            results[2].Should().Equal(4.0);
        }

        [Fact]
        public void Percentile_with_list_input_should_work()
        {
            var collection = Fixture.Collection;
            var percentiles = new List<double> { 0.25, 0.5, 0.75 };

            var queryable = collection.AsQueryable().Select(x => x.Doubles.Percentile(percentiles));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $percentile : { input : '$Doubles', p : [0.25, 0.5, 0.75], method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results[0].Should().Equal(1.0, 1.0, 1.0);
            results[1].Should().Equal(1.0, 1.0, 2.0);
            results[2].Should().Equal(1.0, 2.0, 3.0);
        }

        public class C
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal[] Decimals { get; set; }
            public double[] Doubles { get; set; }
            public float[] Floats { get; set; }
            public int[] Ints { get; set; }
            public long[] Longs { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal?[] NullableDecimals { get; set; }
            public double?[] NullableDoubles { get; set; }
            public float?[] NullableFloats { get; set; }
            public int?[] NullableInts { get; set; }
            public long?[] NullableLongs { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new()
                {
                    Id = 1,
                    Decimals = [1.0M],
                    Doubles = [1.0],
                    Floats = [1.0F],
                    Ints = [1],
                    Longs = [1L],
                    NullableDecimals = [],
                    NullableDoubles = [],
                    NullableFloats = [],
                    NullableInts = [],
                    NullableLongs = []
                },
                new()
                {
                    Id = 2,
                    Decimals = [1.0M, 2.0M],
                    Doubles = [1.0, 2.0],
                    Floats = [1.0F, 2.0F],
                    Ints = [1, 2],
                    Longs = [1L, 2L],
                    NullableDecimals = [null],
                    NullableDoubles = [null],
                    NullableFloats = [null],
                    NullableInts = [null],
                    NullableLongs = [null]
                },
                new()
                {
                    Id = 3,
                    Decimals = [1.0M, 2.0M, 3.0M],
                    Doubles = [1.0, 2.0, 3.0],
                    Floats = [1.0F, 2.0F, 3.0F],
                    Ints = [1, 2, 3],
                    Longs = [1L, 2L, 3L],
                    NullableDecimals = [null, 1.0M, 2.0M, 3.0M],
                    NullableDoubles = [null, 1.0, 2.0, 3.0],
                    NullableFloats = [null, 1.0F, 2.0F, 3.0F],
                    NullableInts = [null, 1, 2, 3],
                    NullableLongs = [null, 1L, 2L, 3L]
                }
            ];
        }
    }
}