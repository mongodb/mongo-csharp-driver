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
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class MqlFieldTests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_Mql_Field_should_work_with_BsonDocument()
        {
            var collection = GetCollection<BsonDocument>();

            var queryable = collection.AsQueryable()
                .Select(root => Mql.Field(root, "X", Int32Serializer.Instance) + 1); // like root.X except BsonDocument does not have a property called X

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $add : ['$X', 1] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(2);
        }

        [Fact]
        public void Select_Mql_Field_should_work_with_POCO()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(root => Mql.Field(root, "X", Int32Serializer.Instance) + 1); // like root.X except C does not have a property called X

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $add : ['$X', 1] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(2);
        }

        private IMongoCollection<TDocument> GetCollection<TDocument>()
        {
            var collection = GetCollection<BsonDocument>("test");
            var document = new BsonDocument { { "_id", 1 }, { "X", 1 } };
            CreateCollection(collection, document);

            var database = collection.Database;
            var collectionName = collection.CollectionNamespace.CollectionName;
            return database.GetCollection<TDocument>(collectionName);
        }

        private class C
        {
            public int Id { get; set; }
        }
    }
}
