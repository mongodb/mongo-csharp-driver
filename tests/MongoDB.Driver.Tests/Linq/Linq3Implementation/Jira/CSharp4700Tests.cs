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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4700Tests : LinqIntegrationTest<CSharp4700Tests.ClassFixture>
    {
        public CSharp4700Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void OrderBy_Count_should_work()
        {
            var collection = Fixture.Collection;

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

        public class C
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, Name = "John" },
                new C { Id = 2, Name = "John" },
                new C { Id = 6, Name = "Jane" }
            ];
        }
    }
}
