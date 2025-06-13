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
    public class CSharp3197Tests : LinqIntegrationTest<CSharp3197Tests.ClassFixture>
    {
        public CSharp3197Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Select_select_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(i => new { A = i.Age })
                .Select(i => new { B = i.A });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { A : '$Age', _id : 0 } }",
                "{ $project : { B : '$A', _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(new { B = 42 });
        }

        public class Person
        {
            public int Id { get; set; }
            public int Age { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Person>
        {
            protected override IEnumerable<Person> InitialData =>
            [
                new Person { Id = 1, Age = 42 }
            ];
        }
    }
}
