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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp2195Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_should_work()
        {
            var collection = CreateCollection();
            var builder = Builders<RawBsonDocument>.Filter;
            var filter = builder.Eq(x => x["life"], 42);

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<RawBsonDocument>();
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            renderedFilter.Should().Be("{ life : 42 }");

            var results = collection.FindSync(filter).ToList();
            results.Select(x => x["_id"].AsInt32).Should().Equal(2);
        }

        [Fact]
        public void Where_should_work()
        {
            var collection = CreateCollection();

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

        private IMongoCollection<RawBsonDocument> CreateCollection()
        {
            var collection = GetCollection<BsonDocument>();

            CreateCollection(
                collection,
                new BsonDocument { { "_id", 1 }, { "life", 41 } },
                new BsonDocument { { "_id", 2 }, { "life", 42 } });

            return GetCollection<RawBsonDocument>();
        }
    }
}
