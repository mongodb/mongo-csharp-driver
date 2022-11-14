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

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3237Tests : Linq3IntegrationTest
    {
        [Fact]
        public void ToList_in_Select_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(p => new
                {
                    Id = p.Id,
                    Comments = p.Comments.ToList()
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Id : '$_id', Comments : '$Comments', _id : 0 } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.Comments.Should().BeOfType<List<string>>();
            result.Comments.Should().Equal("a", "b", "c");
        }

        private IMongoCollection<Post> CreateCollection()
        {
            var collection = GetCollection<Post>();

            CreateCollection(
                collection,
                new Post { Id = 1, Comments = new[] { "a", "b", "c" } });

            return collection;
        }

        private class Post
        {
            public int Id { get; set; }
            public IEnumerable<string> Comments { get; set; }
        }
    }
}
