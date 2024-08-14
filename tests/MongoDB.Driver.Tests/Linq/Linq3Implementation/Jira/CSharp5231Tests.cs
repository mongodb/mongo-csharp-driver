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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5231ests : Linq3IntegrationTest
    {
        [Fact]
        public void Concat_should_work()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var collection1 = GetCollection1();
            var collection2 = GetCollection2();

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
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var collection1 = GetCollection1();
            var collection2 = GetCollection2();

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

        private IMongoCollection<C> GetCollection1()
        {
            var collection = GetCollection<C>("collection1");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1 },
                new C { Id = 2, X = 2 },
                new C { Id = 3, X = 3 });
            return collection;
        }

        private IMongoCollection<D> GetCollection2()
        {
            var collection = GetCollection<D>("collection2");
            CreateCollection(
                collection,
                new D { Id = 1, X = 2 },
                new D { Id = 2, X = 3 },
                new D { Id = 3, X = 4 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class D
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
