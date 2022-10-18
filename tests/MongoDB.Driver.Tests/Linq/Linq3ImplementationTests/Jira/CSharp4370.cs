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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4370Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_with_Id_represented_as_ObjectId_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.SomeId == "bbbbbbbbbbbbbbbbbbbbbbbb");

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { SomeId : ObjectId('bbbbbbbbbbbbbbbbbbbbbbbb') } }");

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(2);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("products");
            var database = collection.Database;

            CreateCollection(
                collection,
                new C { Id = 1, SomeId = "aaaaaaaaaaaaaaaaaaaaaaaa" },
                new C { Id = 2, SomeId = "bbbbbbbbbbbbbbbbbbbbbbbb" });

            return collection;
        }

        public class C
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.ObjectId)]
            public string SomeId { get; set; }
        }
    }
}
