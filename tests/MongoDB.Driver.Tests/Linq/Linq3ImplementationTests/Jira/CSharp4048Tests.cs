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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4048Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Take_with_group_should_work()
        {
            var collection = GetCollection<C>();
            var documents = new[]
            {
                new C { Id = 1, X = 11 },
                new C { Id = 2, X = 22 }
            };
            CreateCollection(collection, documents);

            var queryable = collection.AsQueryable()
                .GroupBy(x => x.Id)
                .Select(x => new { Id = x.Key, Take = x.Take(1) });

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', _elements : { $push: '$$ROOT' } } }",
                "{ $project : { Id : '$_id', Take  : { $slice : ['$_elements', 1] }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].ToJson().Should().Be("{ \"_id\" : 1, \"Take\" : [{ \"_id\" : 1, \"X\" : 11 }] }");
            results[1].ToJson().Should().Be("{ \"_id\" : 2, \"Take\" : [{ \"_id\" : 2, \"X\" : 22 }] }");
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
