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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4557Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_with_ContainsKey_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.Foo.ContainsKey("bar"));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Foo.bar' : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Select_with_ContainsKey_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(x => x.Foo.ContainsKey("bar"));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$Foo.bar' }, 'missing']  }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, true);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, Foo = new Dictionary<string, int> { { "foo", 100 } } },
                new C { Id = 2, Foo = new Dictionary<string, int> { { "bar", 100 } } });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public Dictionary<string, int> Foo { get; set; }
        }
    }
}
