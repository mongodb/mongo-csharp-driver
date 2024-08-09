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

using System;
using System.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp1486Tests
    {
        [Fact]
        public void Group_with_First_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");

            var aggregate = collection.Aggregate()
                .SortByDescending(x => x.CreatedDate)
                .Group(x => x.Key, g => g.First());

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $sort : { CreatedDate : -1 } }",
                "{ $group : { _id : '$Key', __agg0 : { $first : '$$ROOT' } } }",
                "{ $project : { _v : '$__agg0', _id : 0  } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private class C
        {
            public int Id { get; set; }
            public DateTime CreatedDate { get; set; }
            public string Key { get; set; }
        }
    }
}
