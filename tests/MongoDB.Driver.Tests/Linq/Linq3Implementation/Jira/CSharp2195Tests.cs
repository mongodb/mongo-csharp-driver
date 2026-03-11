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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2195Tests : LinqIntegrationTest<CSharp2195Tests.ClassFixture>
    {
        public CSharp2195Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Filter_should_work()
        {
            var collection = Fixture.Collection;
            var builder = Builders<RawBsonDocument>.Filter;
            var filter = builder.Eq(x => x["life"], 42);

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<RawBsonDocument>();
            var renderedFilter = filter.Render(new(documentSerializer, serializerRegistry));
            renderedFilter.Should().Be("{ life : 42 }");

            var results = collection.FindSync(filter).ToList();
            results.Select(x => x["_id"].AsInt32).Should().Equal(2);
        }

        [Fact]
        public void Where_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Where(x => x["life"] == 42);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { life : 42 } }");

            var results = queryable.ToList();
            results.Select(x => x["_id"].AsInt32).Should().Equal(2);
        }

        public sealed class ClassFixture : MongoCollectionFixture<RawBsonDocument, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
            [
                new BsonDocument { { "_id", 1 }, { "life", 41 } },
                new BsonDocument { { "_id", 2 }, { "life", 42 } }
            ];
        }
    }
}
