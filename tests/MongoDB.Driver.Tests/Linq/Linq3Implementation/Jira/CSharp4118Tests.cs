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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4118Tests : LinqIntegrationTest<CSharp4118Tests.ClassFixture>
    {
        public CSharp4118Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Known_serializers_should_not_propagate_past_anonymous_class()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Select(x => new { S = "abc", HasId = x.Id != "000000000000000000000000" });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { S : 'abc', HasId : { $ne : ['$_id', ObjectId('000000000000000000000000')] }, _id : 0 } }");
        }

        [Fact]
        public void Known_serializers_should_not_propagate_past_class_with_member_initializers()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Select(x => new R { S = "abc", HasId = x.Id != "000000000000000000000000" });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { S : 'abc', HasId : { $ne : ['$_id', ObjectId('000000000000000000000000')] }, _id : 0 } }");
        }

        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; }
        }

        private class R
        {
            public string S { get; set; }
            public bool HasId { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData => null;
        }
    }
}
