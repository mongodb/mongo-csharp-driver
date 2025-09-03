﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class UnionMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<UnionMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public UnionMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Enumerable_Union_should_work(
            [Values(false, true)] bool withNestedAsQueryableSource2)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryableSource2 ?
                collection.AsQueryable().Select(x => x.A.Union(x.B.AsQueryable())) :
                collection.AsQueryable().Select(x => x.A.Union(x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $setUnion : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].Should().Equal();
            results[1].Should().Equal(1);
            results[2].Should().Equal(1);
            results[3].Should().BeEquivalentTo(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Queryable_Union_should_work(
            [Values(false, true)] bool withNestedAsQueryableSource2)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryableSource2 ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Union(x.B.AsQueryable())) :
                collection.AsQueryable().Select(x => x.A.AsQueryable().Union(x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $setUnion : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].Should().Equal();
            results[1].Should().Equal(1);
            results[2].Should().Equal(1);
            results[3].Should().BeEquivalentTo(1, 2, 3);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public int[] B { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 0, A = new int[0], B = new int[0] },
                new C { Id = 1, A = new int[0], B = new int[] { 1 } },
                new C { Id = 2, A = new int[] { 1 }, B = new int[0] },
                new C { Id = 3, A = new int[] { 1, 2 }, B = new int[] { 2, 3 } }
            ];
        }
    }
}
