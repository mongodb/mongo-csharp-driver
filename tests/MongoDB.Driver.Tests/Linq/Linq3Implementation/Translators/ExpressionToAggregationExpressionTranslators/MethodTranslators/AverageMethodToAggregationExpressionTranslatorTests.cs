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
    public class AverageMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Average_with_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.Decimals.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$Decimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0M, 1.5M, 2.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Average(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.Decimals.Average(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$Decimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0M, 3.0M, 4.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.Doubles.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$Doubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 1.5, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Average(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.Doubles.Average(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$Doubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 3.0, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.Floats.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$Floats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0F, 1.5F, 2.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Average(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.Floats.Average(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$Floats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0F, 3.0F, 4.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.Ints.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$Ints' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 1.5, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Average(x => x * 2)) :
                collection.AsQueryable().Select(x => x.Ints.Average(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$Ints', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 3.0, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.Longs.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$Longs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 1.5, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Average(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.Longs.Average(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$Longs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 3.0, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$NullableDecimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_decimals_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Average(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Average(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$NullableDecimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$NullableDoubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_doubles_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Average(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Average(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$NullableDoubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.NullableFloats.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$NullableFloats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_floats_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Average(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.NullableFloats.Average(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$NullableFloats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.NullableInts.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$NullableInts' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_ints_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Average(x => x * 2)) :
                collection.AsQueryable().Select(x => x.NullableInts.Average(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$NullableInts', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Average()) :
                collection.AsQueryable().Select(x => x.NullableLongs.Average());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : '$NullableLongs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Average_with_nullable_longs_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Average(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.NullableLongs.Average(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$NullableLongs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 4.0);
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
