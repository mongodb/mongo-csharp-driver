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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class SumMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Sum_with_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.Decimals.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$Decimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0M, 3.0M, 6.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Sum(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.Decimals.Sum(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Decimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0M, 6.0M, 12.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.Doubles.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$Doubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 3.0, 6.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Sum(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.Doubles.Sum(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Doubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 6.0, 12.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.Floats.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$Floats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0F, 3.0F, 6.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Sum(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.Floats.Sum(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Floats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0F, 6.0F, 12.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.Ints.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$Ints' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 3, 6);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Sum(x => x * 2)) :
                collection.AsQueryable().Select(x => x.Ints.Sum(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Ints', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 6, 12);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.Longs.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$Longs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1L, 3L, 6L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Sum(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.Longs.Sum(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Longs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2L, 6L, 12L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$NullableDecimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 6.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Sum(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Sum(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$NullableDecimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 12.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$NullableDoubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 6.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Sum(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Sum(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$NullableDoubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 12.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.NullableFloats.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$NullableFloats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 6.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Sum(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.NullableFloats.Sum(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$NullableFloats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 12.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.NullableInts.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$NullableInts' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 6);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Sum(x => x * 2)) :
                collection.AsQueryable().Select(x => x.NullableInts.Sum(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$NullableInts', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 12);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Sum()) :
                collection.AsQueryable().Select(x => x.NullableLongs.Sum());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : '$NullableLongs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 6L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sum_with_nullable_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Sum(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.NullableLongs.Sum(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$NullableLongs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 12L);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C
                {
                    Id = 1,
                    Decimals = new decimal[] { 1.0M },
                    Doubles = new double[] { 1.0 },
                    Floats = new float[] { 1.0F },
                    Ints = new int[] { 1 },
                    Longs = new long[] { 1L },
                    NullableDecimals = new decimal?[0] { },
                    NullableDoubles = new double?[0] { },
                    NullableFloats = new float?[0] { },
                    NullableInts = new int?[0] { },
                    NullableLongs = new long?[0] { }
                },
                new C
                {
                    Id = 2,
                    Decimals = new decimal[] { 1.0M, 2.0M },
                    Doubles = new double[] { 1.0, 2.0 },
                    Floats = new float[] { 1.0F, 2.0F },
                    Ints = new int[] { 1, 2 },
                    Longs = new long[] { 1L, 2L },
                    NullableDecimals = new decimal?[] { null },
                    NullableDoubles = new double?[] { null },
                    NullableFloats = new float?[] { null },
                    NullableInts = new int?[] { null },
                    NullableLongs = new long?[] { null }
                },
                new C
                {
                    Id = 3,
                    Decimals = new decimal[] { 1.0M, 2.0M, 3.0M },
                    Doubles = new double[] { 1.0, 2.0, 3.0 },
                    Floats = new float[] { 1.0F, 2.0F, 3.0F },
                    Ints = new int[] { 1, 2, 3 },
                    Longs = new long[] { 1L, 2L, 3L },
                    NullableDecimals = new decimal?[] { null, 1.0M, 2.0M, 3.0M },
                    NullableDoubles = new double?[] { null, 1.0, 2.0, 3.0 },
                    NullableFloats = new float?[] { null, 1.0F, 2.0F, 3.0F },
                    NullableInts = new int?[] { null, 1, 2, 3 },
                    NullableLongs = new long?[] { null, 1L, 2L, 3L }
                });
            return collection;
        }

        private class C
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
    }
}
