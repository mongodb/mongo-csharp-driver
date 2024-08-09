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
    public class MaxOrMinMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Max_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Documents.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.Documents.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Documents' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Select(x => x["X"].AsInt32).Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Documents.AsQueryable().Max(x => x["X"])) :
                collection.AsQueryable().Select(x => x.Documents.Max(x => x["X"]));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Documents.X' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.Decimals.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Decimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0M, 2.0M, 3.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_decimals_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Max(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.Decimals.Max(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$Decimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal('2.0')] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0M, 4.0M, 6.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.Doubles.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Doubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 2.0, 3.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_doubles_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Max(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.Doubles.Max(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$Doubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 4.0, 6.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.Floats.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Floats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0F, 2.0F, 3.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_floats_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Max(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.Floats.Max(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$Floats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0F, 4.0F, 6.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.Ints.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Ints' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_ints_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Max(x => x * 2)) :
                collection.AsQueryable().Select(x => x.Ints.Max(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$Ints', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 4, 6);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.Longs.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$Longs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1L, 2L, 3L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_longs_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Max(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.Longs.Max(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$Longs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2L, 4L, 6L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$NullableDecimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_decimals_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Max(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Max(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$NullableDecimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal('2.0')] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$NullableDoubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_doubles_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Max(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Max(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$NullableDoubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.NullableFloats.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$NullableFloats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_floats_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Max(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.NullableFloats.Max(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$NullableFloats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.NullableInts.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$NullableInts' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_ints_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Max(x => x * 2)) :
                collection.AsQueryable().Select(x => x.NullableInts.Max(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$NullableInts', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Max()) :
                collection.AsQueryable().Select(x => x.NullableLongs.Max());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : '$NullableLongs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_of_nullable_longs_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Max(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.NullableLongs.Max(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$NullableLongs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_should_work(
           [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Documents.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.Documents.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Documents' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Select(x => x["X"].AsInt32).Should().Equal(1, 1, 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Documents.AsQueryable().Min(x => x["X"])) :
                collection.AsQueryable().Select(x => x.Documents.Min(x => x["X"]));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Documents.X' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 1, 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.Decimals.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Decimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0M, 1.0M, 1.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_decimals_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Decimals.AsQueryable().Min(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.Decimals.Min(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$Decimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal('2.0')] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0M, 2.0M, 2.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.Doubles.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Doubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0, 1.0, 1.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_doubles_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Doubles.AsQueryable().Min(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.Doubles.Min(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$Doubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0, 2.0, 2.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.Floats.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Floats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1.0F, 1.0F, 1.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_floats_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Floats.AsQueryable().Min(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.Floats.Min(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$Floats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2.0F, 2.0F, 2.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.Ints.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Ints' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 1, 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_ints_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Ints.AsQueryable().Min(x => x * 2)) :
                collection.AsQueryable().Select(x => x.Ints.Min(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$Ints', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");
            var results = queryable.ToList();
            results.Should().Equal(2, 2, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.Longs.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$Longs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1L, 1L, 1L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_longs_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Longs.AsQueryable().Min(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.Longs.Min(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$Longs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2L, 2L, 2L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_decimals_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$NullableDecimals' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_decimals_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDecimals.AsQueryable().Min(x => x * 2.0M)) :
                collection.AsQueryable().Select(x => x.NullableDecimals.Min(x => x * 2.0M));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$NullableDecimals', as : 'x', in : { $multiply : ['$$x', NumberDecimal('2.0')] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6.0M);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_doubles_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$NullableDoubles' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_doubles_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableDoubles.AsQueryable().Min(x => x * 2.0)) :
                collection.AsQueryable().Select(x => x.NullableDoubles.Min(x => x * 2.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$NullableDoubles', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_floats_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.NullableFloats.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$NullableFloats' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_floats_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableFloats.AsQueryable().Min(x => x * 2.0F)) :
                collection.AsQueryable().Select(x => x.NullableFloats.Min(x => x * 2.0F));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$NullableFloats', as : 'x', in : { $multiply : ['$$x', 2.0] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6.0F);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_ints_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.NullableInts.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$NullableInts' }, _id : 0 } }");
            var results = queryable.ToList();
            results.Should().Equal(null, null, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_ints_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableInts.AsQueryable().Min(x => x * 2)) :
                collection.AsQueryable().Select(x => x.NullableInts.Min(x => x * 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$NullableInts', as : 'x', in : { $multiply : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_longs_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Min()) :
                collection.AsQueryable().Select(x => x.NullableLongs.Min());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : '$NullableLongs' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 3L);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_of_nullable_longs_with_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.NullableLongs.AsQueryable().Min(x => x * 2L)) :
                collection.AsQueryable().Select(x => x.NullableLongs.Min(x => x * 2L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $min : { $map : { input : '$NullableLongs', as : 'x', in : { $multiply : ['$$x', NumberLong(2)] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(null, null, 6L);
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
                    Documents = new string[] { "{ X : 1 }" }.Select(BsonDocument.Parse).ToArray(),
                    Doubles = new double[] { 1.0 },
                    Floats = new float[] { 1.0F },
                    Ints = new int[] { 1 },
                    Longs = new long[] { 1L },
                    NullableDecimals = new decimal?[] { },
                    NullableDoubles = new double?[] { },
                    NullableFloats = new float?[] { },
                    NullableInts = new int?[] { },
                    NullableLongs = new long?[] { }
                },
                new C
                {
                    Id = 2,
                    Decimals = new decimal[] { 1.0M, 2.0M },
                    Documents = new string[] { "{ X : 1 }", "{ X : 2 }" }.Select(BsonDocument.Parse).ToArray(),
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
                    Decimals = new decimal[] { 1.0M, 2.0M , 3.0M},
                    Documents = new string[] { "{ X : 1 }", "{ X : 2 }", "{ X : 3 }" }.Select(BsonDocument.Parse).ToArray(),
                    Doubles = new double[] { 1.0, 2.0, 3.0 },
                    Floats = new float[] { 1.0F, 2.0F, 3.0F },
                    Ints = new int[] { 1, 2, 3 },
                    Longs = new long[] { 1L, 2L, 3L },
                    NullableDecimals = new decimal?[] { null, 3.0M },
                    NullableDoubles = new double?[] { null, 3.0 },
                    NullableFloats = new float?[] { null, 3.0F },
                    NullableInts = new int?[] { null, 3 },
                    NullableLongs = new long?[] { null, 3L }
                });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal[] Decimals { get; set; }
            public BsonDocument[] Documents { get; set; }
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
