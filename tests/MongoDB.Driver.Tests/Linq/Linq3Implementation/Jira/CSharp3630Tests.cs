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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3630Tests
    {
        [Fact]
        public void GroupBy_with_key_selector_followed_by_Select_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A).Select(g => new { Result = g.First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_followed_by_Select_with_embedded_identity_projection_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A).Select(g => new { Result = g.Select(c => c).First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_and_element_selector_followed_by_Select_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A, c => c).Select(g => new { Result = g.First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_and_element_selector_followed_by_Select_with_embedded_identity_projection_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A, c => c).Select(g => new { Result = g.Select(c => c).First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_and_result_selector_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A, (k, g) => new { Result = g.First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_and_result_selector_with_embedded_identity_projection_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A, (k, g) => new { Result = g.Select(c => c).First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_and_element_selector_and_result_selector_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A, c => c, (k, g) => new { Result = g.First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_key_selector_and_element_selector_and_result_selector_with_embedded_identity_projection_should_translate_as_expected()
        {
            var collection = CreateCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(c => c.A, c => c, (k, g) => new { Result = g.Select(c => c).First() });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$A', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { Result : '$__agg0', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            return database.GetCollection<C>("test");
        }

        public class C
        {
            public int Id { get; set; }
            public int A { get; set; }
        }
    }
}
