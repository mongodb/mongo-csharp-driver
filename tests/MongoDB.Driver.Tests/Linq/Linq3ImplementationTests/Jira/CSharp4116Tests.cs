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
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4116Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(false, "{ $match : { _id : { $type : -1 } } }")]
        [InlineData(true, "{ $match : { } }")]
        public void Optimize_match_with_expr(bool value, string expectedStage)
        {
            var collection = GetCollection<BsonDocument>();
            var queryable = collection.AsQueryable()
                .Where(x => value);

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
    }
}
