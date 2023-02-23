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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4542Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_with_Tuple_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(p => new Tuple<string, string>(p.FirstName, p.LastName));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Item1 : '$FirstName', Item2 : '$LastName', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Item1.Should().Be("John");
            results[0].Item2.Should().Be("Doe");
        }

        [Fact]
        public void Select_with_ValueTuple_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(p => new ValueTuple<string, string>(p.FirstName, p.LastName));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Item1 : '$FirstName', Item2 : '$LastName', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Item1.Should().Be("John");
            results[0].Item2.Should().Be("Doe");
        }

        private IMongoCollection<Person> CreateCollection()
        {
            var collection = GetCollection<Person>("people");

            CreateCollection(
                collection,
                new Person { Id = 1, FirstName = "John", LastName = "Doe" });

            return collection;
        }

        private class Person
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
