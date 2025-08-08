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
    public class MedianMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<MedianMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public MedianMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture, server => server.Supports(Feature.MedianOperator))
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.Decimals.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$Decimals', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0M, 1.0M, 2.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Median(y => y * 2.0M)) :
                collection.AsQueryable().Select(x => x.Decimals.Median(y => y * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$Decimals', as : 'y', in : { $multiply : ['$$y', NumberDecimal(2)] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0M, 2.0M, 4.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.Doubles.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$Doubles', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 1.0, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Median(y => y * 2.0)) :
                collection.AsQueryable().Select(x => x.Doubles.Median(y => y * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$Doubles', as : 'y', in : { $multiply : ['$$y', 2.0] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 2.0, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.Floats.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$Floats', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0F, 1.0F, 2.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Median(y => y * 2.0F)) :
                collection.AsQueryable().Select(x => x.Floats.Median(y => y * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$Floats', as : 'y', in : { $multiply : ['$$y', 2.0] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0F, 2.0F, 4.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.Ints.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$Ints', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Median(y => y * 2)) :
                collection.AsQueryable().Select(x => x.Ints.Median(y => y * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$Ints', as : 'y', in : { $multiply : ['$$y', 2] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 2, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.Longs.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$Longs', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Median(y => y * 2L)) :
                collection.AsQueryable().Select(x => x.Longs.Median(y => y * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$Longs', as : 'y', in : { $multiply : ['$$y', NumberLong(2)] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 2, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$NullableDecimals', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Median(y => y * 2.0M)) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Median(y => y * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$NullableDecimals', as : 'y', in : { $multiply : ['$$y', NumberDecimal(2)] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$NullableDoubles', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Median(y => y * 2.0)) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Median(y => y * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$NullableDoubles', as : 'y', in : { $multiply : ['$$y', 2.0] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.NullableFloats.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$NullableFloats', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Median(y => y * 2.0F)) :
                collection.AsQueryable().Select(x => x.NullableFloats.Median(y => y * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$NullableFloats', as : 'y', in : { $multiply : ['$$y', 2.0] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.NullableInts.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$NullableInts', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Median(y => y * 2)) :
                collection.AsQueryable().Select(x => x.NullableInts.Median(y => y * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$NullableInts', as : 'y', in : { $multiply : ['$$y', 2] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Median()) :
                collection.AsQueryable().Select(x => x.NullableLongs.Median());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : '$NullableLongs', method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Median_with_nullable_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Median(y => y * 2L)) :
                collection.AsQueryable().Select(x => x.NullableLongs.Median(y => y * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $median : { input : { $map : { input : '$NullableLongs', as : 'y', in : { $multiply : ['$$y', NumberLong(2)] } } }, method : 'approximate' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4);
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