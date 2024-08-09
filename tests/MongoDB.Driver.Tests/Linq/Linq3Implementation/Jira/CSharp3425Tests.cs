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
    public class CSharp3425Tests
    {
        [Fact]
        public void ToLower_with_Equals_should_work_in_Where()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");

            var queryable = collection.AsQueryable()
                .Where(x => x.S.ToLower().Equals("abc"));

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { S : /^abc$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void ToLower_with_Equals_should_work_in_Select()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");

            var queryable = collection.AsQueryable()
                .Select(x => new { Result = x.S.ToLower().Equals("abc") });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Result : { $eq : [{ $toLower : '$S' }, 'abc'] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private class C
        {
            public int Id { get; set; }
            public string S { get; set; }
        }
    }
}
