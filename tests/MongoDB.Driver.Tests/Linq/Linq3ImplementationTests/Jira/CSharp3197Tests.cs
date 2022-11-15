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
    public class CSharp3197Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_select_should_work()
        {
            var collection = CreateCollection();

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

        private IMongoCollection<Person> CreateCollection()
        {
            var collection = GetCollection<Person>("C");

            CreateCollection(
                collection,
                new Person { Id = 1, Age = 42 });

            return collection;
        }

        private class Person
        {
            public int Id { get; set; }
            public int Age { get; set; }
        }
    }
}
