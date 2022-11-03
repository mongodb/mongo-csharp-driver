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
    public class CSharp3236Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_should_work()
        {
            var collection = CreateCollection();

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
                "{ $project : { Id : '$_id', Comments : { $filter : { input : '$Comments', as : 'c', cond : { $gte : [{ $indexOfCP : ['$$c.Text', 'test'] }, 0] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.Comments.Select(c => c.Id).Should().Equal(1, 3);
        }

        private IMongoCollection<Post> CreateCollection()
        {
            var collection = GetCollection<Post>("C");

            CreateCollection(
                collection,
                new Post
                {
                    Id = 1,
                    Comments = new List<Comment>
                    {
                        new Comment { Id = 1, Text = "this is a test comment" },
                        new Comment { Id = 2, Text = "this is not" },
                        new Comment { Id = 3, Text = "and this is another test comment" }
                    }
                });

            return collection;
        }

        private class Post
        {
            public int Id { get; set; }
            public List<Comment> Comments { get; set; }

        }

        public class Comment
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }
    }
}
