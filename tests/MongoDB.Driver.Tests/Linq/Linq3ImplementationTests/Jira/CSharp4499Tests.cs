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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4499Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void ExpressionFieldDefinition_should_work([Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);
            var fieldDefinition = new ExpressionFieldDefinition<ConcreteClass, object>(x => x.InternalId);

            var queryable = collection.Aggregate()
                .Sort(Builders<ConcreteClass>.Sort.Ascending(fieldDefinition));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $sort : { InternalId : 1 } }");
        }

        private IMongoCollection<ConcreteClass> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<ConcreteClass>("C", linqProvider);
            return collection;
        }

        private class ConcreteClass
        {
            public ObjectId InternalId { get; set; }
        }
    }
}
