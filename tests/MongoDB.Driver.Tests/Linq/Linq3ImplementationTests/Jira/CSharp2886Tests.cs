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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp2886Tests : Linq3IntegrationTest
    {
        [Fact]
        public void SelectMany_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .SelectMany(x => x.A);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : '$A', _id : 0 } }",
                "{ $unwind : '$_v' }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2, 3, 4);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, A = new[] { 1, 2 } },
                new C { Id = 2, A = new[] { 3, 4 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
