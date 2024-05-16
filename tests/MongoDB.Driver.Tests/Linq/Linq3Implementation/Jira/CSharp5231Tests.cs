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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5231ests : LinqIntegrationTest<CSharp5231ests.TestDataFixture>
    {
        public CSharp5231ests(ITestOutputHelper testOutputHelper, TestDataFixture fixture)
            : base(testOutputHelper, fixture, server => server.Supports(Feature.AggregateUnionWith))
        {
        }

        [Fact]
        public void Concat_should_work()
        {
            var collection1 = Fixture.Collection1;
            var collection2 = Fixture.Collection2;

            var queryable = collection1.AsQueryable()
                .Select(x => x.X)
                .Concat(collection2.AsQueryable().Select(x => x.X));

            var stages = Translate(collection1, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : '$X', _id : 0 } }",
                "{ $unionWith : { coll : 'collection2', pipeline : [{ $project : { _v : '$X', _id : 0 } }] } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2, 3, 2, 3, 4);
        }

        [Fact]
        public void Union_should_work()
        {
            var collection1 = Fixture.Collection1;
            var collection2 = Fixture.Collection2;

            var queryable = collection1.AsQueryable()
                .Select(x => x.X)
                .Union(collection2.AsQueryable().Select(x => x.X));

            var stages = Translate(collection1, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : '$X', _id : 0 } }",
                "{ $unionWith : { coll : 'collection2', pipeline : [{ $project : { _v : '$X', _id : 0 } }] } }",
                "{ $group : { _id : '$$ROOT' } }",
                "{ $replaceRoot : { newRoot : '$_id' } }");

            var results = queryable.ToList();
            results.OrderBy(x => x).Should().Equal(1, 2, 3, 4);
        }

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        public class D
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        public sealed class TestDataFixture : MongoDatabaseFixture
        {
            public IMongoCollection<C> Collection1 { get; private set; }

            public IMongoCollection<D> Collection2 { get; private set; }

            protected override void InitializeFixture()
            {
                Collection1 = GetCollection<C>("collection1");
                Collection1.InsertMany([
                    new C { Id = 1, X = 1 },
                    new C { Id = 2, X = 2 },
                    new C { Id = 3, X = 3 }]);

                Collection2 = GetCollection<D>("collection2");
                Collection2.InsertMany([
                    new D { Id = 1, X = 2 },
                    new D { Id = 2, X = 3 },
                    new D { Id = 3, X = 4 }]);
            }
        }
    }
}
