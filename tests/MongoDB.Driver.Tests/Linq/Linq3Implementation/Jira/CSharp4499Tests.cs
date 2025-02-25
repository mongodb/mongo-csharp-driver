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
using MongoDB.Bson;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4499Tests : LinqIntegrationTest<CSharp4499Tests.ClassFixture>
    {
        public CSharp4499Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void ExpressionFieldDefinition_should_work()
        {
            var collection = Fixture.Collection;
            var fieldDefinition = new ExpressionFieldDefinition<ConcreteClass, object>(x => x.InternalId);

            var queryable = collection.Aggregate()
                .Sort(Builders<ConcreteClass>.Sort.Ascending(fieldDefinition));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $sort : { InternalId : 1 } }");
        }

        public class ConcreteClass
        {
            public ObjectId InternalId { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<ConcreteClass>
        {
            protected override IEnumerable<ConcreteClass> InitialData => null;
        }
    }
}
