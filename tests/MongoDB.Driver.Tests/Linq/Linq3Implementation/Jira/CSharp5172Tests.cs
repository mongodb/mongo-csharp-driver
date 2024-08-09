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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5172Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_ElemMatch_should_work()
        {
            var collection = GetCollection();
            var filter = Builders<Entity>.Filter.ElemMatch(e => e.Values, x => x > 1 && x < 3);

            var renderedUpdate = (BsonDocument)filter.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry)); ;
            renderedUpdate.Should().Be("{ Values : { $elemMatch : { $gt : 1, $lt : 3 } } }");

            var results = collection.Find(filter).ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Update_PullFilter_should_work()
        {
            var collection = GetCollection();
            var update = Builders<Entity>.Update.PullFilter(e => e.Values, x => x > 1 && x < 3);

            var renderedUpdate = (BsonDocument)update.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry)); ;
            renderedUpdate.Should().Be("{ $pull : { Values : { $gt : 1, $lt : 3 } } }");

            var result = collection.UpdateMany(e => true, update);
            result.ModifiedCount.Should().Be(1);

            var updatedDocuments = collection.Find("{}").ToList();
            updatedDocuments.Should().HaveCount(2);
            updatedDocuments[0].Values.Should().Equal(1, 3);
            updatedDocuments[1].Values.Should().Equal(4, 5, 6);
        }

        private IMongoCollection<Entity> GetCollection()
        {
            var collection = GetCollection<Entity>("test");
            CreateCollection(
                collection,
                new Entity { Id = 1, Values = new List<int> { 1, 2, 3 } },
                new Entity { Id = 2, Values = new List<int> { 4, 5, 6 } });
            return collection;
        }

        private class Entity
        {
            public int Id { get; set; }
            public List<int> Values { get; set; }
        }
    }
}
