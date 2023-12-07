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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4861Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void One_less_than_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => 1 < x.Set.Count);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Set.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void One_less_than_Length_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => 1 < x.Array.Length);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Array.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_greater_than_one_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Set.Count > 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Set.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Length_greater_than_one_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Array.Length > 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Array.1' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, Array = new[] { 1 }, Set = new HashSet<int> { 1 } },
                new C { Id = 2, Array = new[] { 1, 2 }, Set = new HashSet<int> { 1, 2 } },
                new C { Id = 3, Array = new[] { 1, 2, 3 }, Set = new HashSet<int> { 1, 2, 3 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] Array { get; set; }
            public HashSet<int> Set { get; set; }
        }
    }
}
