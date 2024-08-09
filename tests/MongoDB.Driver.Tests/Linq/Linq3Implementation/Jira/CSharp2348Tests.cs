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
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp2348Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Any_with_equals_should_work()
        {
            var collection = CreateCollection();

            var find = collection.Find(x => x.A.Any(v => v == 2));

            var renderedFilter = TranslateFindFilter(collection, find);
            renderedFilter.Should().Be("{ A : 2 }");

            var results = find.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Any_with_or_of_equals_should_work()
        {
            var collection = CreateCollection();

            var find = collection.Find(x => x.A.Any(v => v == 2 || v == 3));

            var renderedFilter = TranslateFindFilter(collection, find);
            var results = find.ToList().OrderBy(x => x.Id).ToList();

            renderedFilter.Should().Be("{ A : { $in : [2, 3] } }");
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Any_with_or_of_equals_and_greater_than_should_work()
        {
            var collection = CreateCollection();

            var find = collection.Find(x => x.A.Any(v => v == 2 || v > 3));

            var renderedFilter = TranslateFindFilter(collection, find);
            var results = find.ToList().OrderBy(x => x.Id).ToList();

            // the ideal translation would be { Roles : { $elemMatch : { $or : [{ $eq : 2 }, { $gt : 3 }] } } }
            // but the server does not support implied element names in combination with $or
            // see: https://jira.mongodb.org/browse/SERVER-93020
            renderedFilter.Should().Be("{ $expr : { $anyElementTrue : { $map : { input : '$A', as : 'v', in : { $or : [{ $eq : ['$$v', 2] }, { $gt : ['$$v', 3] }] } } } } }");
            results.Select(x => x.Id).Should().Equal(2, 4);
        }

        private IMongoCollection<User> CreateCollection()
        {
            var collection = GetCollection<User>("test");
            CreateCollection(
                collection,
                new User { Id = 1, A = new[] { 1 } },
                new User { Id = 2, A = new[] { 1, 2 } },
                new User { Id = 3, A = new[] { 1, 3 } },
                new User { Id = 4, A = new[] { 1, 4 } });
            return collection;
        }

        public class User
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }

        public enum Role
        {
            Admin = 1,
            Editor = 2
        }
    }
}
