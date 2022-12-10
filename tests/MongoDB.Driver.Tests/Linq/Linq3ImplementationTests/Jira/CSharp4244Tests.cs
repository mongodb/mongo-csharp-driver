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
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4244Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_with_root_should_work()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("6.0");

            var collection = CreateCollection();
            var person = new Person { Id = 2, Name = "Jane Doe" };

            var queryable = collection
                .AsQueryable()
                .Where(p => p == person);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $eq : ['$$ROOT', { _id : 2, Name : 'Jane Doe' }] } } }");

            var results = queryable.ToList();
            results.Single().ShouldBeEquivalentTo(person);
        }

        private IMongoCollection<Person> CreateCollection()
        {
            var collection = GetCollection<Person>("C");

            CreateCollection(
                collection,
                new Person { Id = 1, Name = "John Doe" },
                new Person { Id = 2, Name = "Jane Doe" });

            return collection;
        }

        private class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
