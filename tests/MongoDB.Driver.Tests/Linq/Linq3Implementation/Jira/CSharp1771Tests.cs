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
    public class CSharp1771Tests
    {
        [Fact]
        public void Ternary_operator_should_work_in_Where()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => (x.A == 0 ? 42 : x.A) != 144);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $ne : [{ $cond : { if : { $eq : ['$A', 0 ] }, then : 42, else : '$A' } }, 144] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Ternary_operator_should_work_in_Select()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { result = (x.A == 0 ? 42 : x.A) });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { result : { $cond : { if : { $eq : ['$A', 0] }, then : 42, else : '$A' } }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<C> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            return database.GetCollection<C>("test");
        }

        private class C
        {
            public int Id { get; set; }
            public int A { get; set; }
        }
    }
}
