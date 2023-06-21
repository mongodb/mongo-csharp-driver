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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3845Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_of_anonymous_class_with_missing_fields_should_work()
        {
            var collection = GetCollection<C>();
            CreateCollection(
                collection,
                new[]
                {
                    new C { Id = 1, S = null, X = 0 },
                    new C { Id = 2, S = "abc", X = 123 }
                });

            var queryable = collection.AsQueryable()
                .Select(c => new { F = c.S, G = c.X });

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { F : '$S', G : '$X', _id : 0 } }"
            };
            AssertStages(stages, expectedStages);

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].F.Should().Be(null);
            results[0].G.Should().Be(0);
            results[1].F.Should().Be("abc");
            results[1].G.Should().Be(123);
        }

        private class C
        {
            public int Id { get; set; }
            [BsonIgnoreIfNull]
            public string S { get; set; }
            [BsonIgnoreIfDefault]
            public int X { get; set; }
        }
    }
}
