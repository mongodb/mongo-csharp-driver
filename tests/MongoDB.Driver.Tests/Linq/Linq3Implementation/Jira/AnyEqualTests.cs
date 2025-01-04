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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class AnyEqualTests : Linq3IntegrationTest
    {
        [Fact]
        public void Any_equal_should_translate_to_in()
        {
            var collection = GetCollection();

            var obj = new[] { 1, 2, 3 };
            var queryable = collection.AsQueryable()
                .Where(x => obj.Any(y => x.X == y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : { $in : [1, 2, 3] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Any_equal_should_translate_to_in_nested_object()
        {
            var collection = GetCollection();

            var obj = new[] { 1, 2, 3 };
            var queryable = collection.AsQueryable()
                .Where(x => obj.Any(y => x.Doc.X == y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Doc.X' : { $in : [1, 2, 3] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1, Doc = new N { X = 1 }},
                new C { Id = 2, X = 4, Doc = new N { X = 4 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }

            public N Doc { get; set; }
        }

        private class N
        {
            public int X { get; set; }
        }
    }
}
