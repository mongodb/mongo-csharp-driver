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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4118Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Known_serializers_should_not_propagate_past_anonymous_class()
        {
            var collection = GetCollection<C>();
            var queryable = collection.AsQueryable()
                .Select(x => new { S = "abc", HasId = x.Id != "000000000000000000000000" });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { S : 'abc', HasId : { $ne : ['$_id', ObjectId('000000000000000000000000')] }, _id : 0 } }");
        }

        [Fact]
        public void Known_serializers_should_not_propagate_past_class_with_member_initializers()
        {
            var collection = GetCollection<C>();
            var queryable = collection.AsQueryable()
                .Select(x => new R { S = "abc", HasId = x.Id != "000000000000000000000000" });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { S : 'abc', HasId : { $ne : ['$_id', ObjectId('000000000000000000000000')] }, _id : 0 } }");
        }

        private class C
        {
            [BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; }
        }

        private class R
        {
            public string S { get; set; }
            public bool HasId { get; set; }
        }
    }
}
