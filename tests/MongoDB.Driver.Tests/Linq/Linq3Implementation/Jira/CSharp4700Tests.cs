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

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4700Tests : Linq3IntegrationTest
    {
        [Fact]
        public void OrderBy_Count_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(x => x.Name)
                .OrderBy(x => x.Count());

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();

            AssertStages(
                stages,
                "{ $group : { _id : '$Name', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $size : '$_elements' } } }",
                "{ $sort : { _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            results.Should().HaveCount(2);
            results[0].Key.Should().Be("Jane");
            results[0].Count().Should().Be(1);
            results[1].Key.Should().Be("John");
            results[1].Count().Should().Be(2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, Name = "John" },
                new C { Id = 2, Name = "John" },
                new C { Id = 6, Name = "Jane" });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

    }
}
