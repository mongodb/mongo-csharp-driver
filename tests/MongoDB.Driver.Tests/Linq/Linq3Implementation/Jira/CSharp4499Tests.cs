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

using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4499Tests : Linq3IntegrationTest
    {
        [Fact]
        public void ExpressionFieldDefinition_should_work()
        {
            var collection = CreateCollection();
            var fieldDefinition = new ExpressionFieldDefinition<ConcreteClass, object>(x => x.InternalId);

            var queryable = collection.Aggregate()
                .Sort(Builders<ConcreteClass>.Sort.Ascending(fieldDefinition));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $sort : { InternalId : 1 } }");
        }

        private IMongoCollection<ConcreteClass> CreateCollection()
        {
            var collection = GetCollection<ConcreteClass>("C");
            return collection;
        }

        private class ConcreteClass
        {
            public ObjectId InternalId { get; set; }
        }
    }
}
