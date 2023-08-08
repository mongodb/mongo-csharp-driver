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
    public class CSharp4063Tests : Linq3IntegrationTest
    {
        [Fact]
        public void GroupBy_with_bool_should_work()
        {
            var collection = GetCollection<C>();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(
                x => x.Id,
                (k, g) => new { Value = g.Count(x => x.Bool) });

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : { $cond : { if : '$Bool', then : 1, else : 0 } } } } }",
                "{ $project : { Value : '$__agg0', _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void GroupBy_with_nullable_bool_should_work()
        {
            var collection = GetCollection<C>();
            var subject = collection.AsQueryable();

            var queryable = subject.GroupBy(
                x => x.Id,
                (k, g) => new { Value = g.Count(x => x.NullableBool.HasValue && x.NullableBool.Value) });

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $group : { _id : '$_id', __agg0 : { $sum : { $cond : { if : { $and : [{ $ne : ['$NullableBool', null] }, '$NullableBool'] }, then : 1, else : 0 } } } } }",
                "{ $project : { Value : '$__agg0', _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        private class C
        {
            public int Id { get; set; }
            public bool Bool { get; set; }
            public bool? NullableBool { get; set; }
        }
    }
}
