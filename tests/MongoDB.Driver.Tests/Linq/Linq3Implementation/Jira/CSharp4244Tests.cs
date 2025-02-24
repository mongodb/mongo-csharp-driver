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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4244Tests : LinqIntegrationTest<CSharp4244Tests.ClassFixture>
    {
        public CSharp4244Tests(ClassFixture fixture)
            : base(fixture, server => server.VersionGreaterThanOrEqualTo("6.0"))
        {
        }

        [Fact]
        public void Where_with_root_should_work()
        {
            var collection = Fixture.Collection;
            var person = new Person { Id = 2, Name = "Jane Doe" };

            var queryable = collection
                .AsQueryable()
                .Where(p => p == person);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $eq : ['$$ROOT', { _id : 2, Name : 'Jane Doe' }] } } }");

            var results = queryable.ToList();
            results.Single().ShouldBeEquivalentTo(person);
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Person>
        {
            protected override IEnumerable<Person> InitialData =>
            [
                new Person { Id = 1, Name = "John Doe" },
                new Person { Id = 2, Name = "Jane Doe" }
            ];
        }
    }
}
