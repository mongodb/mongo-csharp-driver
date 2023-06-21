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
    public class CSharp4061Tests : Linq3IntegrationTest
    {
        [Fact]
        public void AggregateFluent_ToString_should_return_expected_result()
        {
            var collection = GetCollection<C>();
            var subject = collection.Aggregate()
                .Group(x => x.X, g => new { X = g.Key, Count = g.Count() });

            var result = subject.ToString();

            result.Should().Be("aggregate([{ \"$group\" : { \"_id\" : \"$X\", \"__agg0\" : { \"$sum\" : 1 } } }, { \"$project\" : { \"X\" : \"$_id\", \"Count\" : \"$__agg0\", \"_id\" : 0 } }])");
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
