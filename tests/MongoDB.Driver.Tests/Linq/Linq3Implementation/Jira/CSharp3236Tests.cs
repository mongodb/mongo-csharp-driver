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
    public class CSharp3236Tests : LinqIntegrationTest<CSharp3236Tests.ClassFixture>
    {
        public CSharp3236Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Select_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(p => new
                {
                    Id = p.Id,
                    Comments = p.Comments.Where(c => c.Text.Contains("test"))
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : '$_id', Comments : { $filter : { input : '$Comments', as : 'c', cond : { $gte : [{ $indexOfCP : ['$$c.Text', 'test'] }, 0] } } } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.Comments.Select(c => c.Id).Should().Equal(1, 3);
        }

        public class Post
        {
            public int Id { get; set; }
            public List<Comment> Comments { get; set; }

        }

        public class Comment
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Post>
        {
            protected override IEnumerable<Post> InitialData =>
            [
                new Post
                {
                    Id = 1,
                    Comments = new List<Comment>
                    {
                        new Comment { Id = 1, Text = "this is a test comment" },
                        new Comment { Id = 2, Text = "this is not" },
                        new Comment { Id = 3, Text = "and this is another test comment" }
                    }
                }
            ];
        }
    }
}
