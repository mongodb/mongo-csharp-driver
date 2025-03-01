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
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4872Tests : LinqIntegrationTest<CSharp4872Tests.ClassFixture>
    {
        public CSharp4872Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Append_constant_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Append(4).ToList()) :
                collection.AsQueryable().Select(x => x.A.Append(4).ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $concatArrays : ['$A', [4]] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(1, 2, 3, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Append_expression_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Append(x.B).ToList()) :
                collection.AsQueryable().Select(x => x.A.Append(x.B).ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $concatArrays : ['$A', ['$B']] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(1, 2, 3, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Prepend_constant_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Prepend(4).ToList()) :
                collection.AsQueryable().Select(x => x.A.Prepend(4).ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $concatArrays : [[4], '$A'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(4, 1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Prepend_expression_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Prepend(x.B).ToList()) :
                collection.AsQueryable().Select(x => x.A.Prepend(x.B).ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $concatArrays : [['$B'], '$A'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(4, 1, 2, 3);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public int B { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, A = [1, 2, 3], B = 4 }
            ];
        }
    }
}
