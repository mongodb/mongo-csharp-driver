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
using MongoDB.Bson;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4116Tests : LinqIntegrationTest<CSharp4116Tests.ClassFixture>
    {
        public CSharp4116Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData(false, "{ $match : { _id : { $type : -1 } } }")]
        [InlineData(true, null)]
        public void Optimize_match_with_expr(bool value, string expectedStage)
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => value);

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        public sealed class ClassFixture : MongoCollectionFixture<BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData => null;
        }
    }
}
