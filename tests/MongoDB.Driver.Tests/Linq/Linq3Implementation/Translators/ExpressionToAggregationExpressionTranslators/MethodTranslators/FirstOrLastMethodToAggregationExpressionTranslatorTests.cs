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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class FirstOrLastMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void First_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().First()) :
                collection.AsQueryable().Select(x => x.A.First());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : ['$A', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 1, 1, 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void First_with_predicate_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().First(x => x > 1)) :
                collection.AsQueryable().Select(x => x.A.First(x => x > 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } }, 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 2, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstOrDefault_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().FirstOrDefault()) :
                collection.AsQueryable().Select(x => x.A.FirstOrDefault());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cond : { if : { $eq : [{ $size : '$A' }, 0] }, then : 0, else : { $arrayElemAt : ['$A', 0] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 1, 1, 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstOrDefault_with_predicate_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().FirstOrDefault(x => x > 1)) :
                collection.AsQueryable().Select(x => x.A.FirstOrDefault(x => x > 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { values : { $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : 0, else : { $arrayElemAt : ['$$values', 0] } } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 2, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Last_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Last()) :
                collection.AsQueryable().Select(x => x.A.Last());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : ['$A', -1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Last_with_predicate_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Last(x => x > 1)) :
                collection.AsQueryable().Select(x => x.A.Last(x => x > 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } }, -1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void LastOrDefault_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().LastOrDefault()) :
                collection.AsQueryable().Select(x => x.A.LastOrDefault());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cond : { if : { $eq : [{ $size : '$A' }, 0] }, then : 0, else : { $arrayElemAt : ['$A', -1] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void LastOrDefault_with_predicate_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().LastOrDefault(x => x > 1)) :
                collection.AsQueryable().Select(x => x.A.LastOrDefault(x => x > 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { values : { $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : 0, else : { $arrayElemAt : ['$$values', -1] } } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 2, 3);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 0, A = new int[0] },
                new C { Id = 1, A = new int[] { 1 } },
                new C { Id = 2, A = new int[] { 1, 2 } },
                new C { Id = 3, A = new int[] { 1, 2, 3 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
