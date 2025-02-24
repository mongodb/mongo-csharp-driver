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
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class MqlFieldTests : LinqIntegrationTest<MqlFieldTests.ClassFixture>
    {
        public MqlFieldTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Select_Mql_Field_should_work_with_BsonDocument()
        {
            var collection = Fixture.GetCollection<BsonDocument>();

            var queryable = collection.AsQueryable()
                .Select(root => Mql.Field(root, "X", Int32Serializer.Instance) + 1); // like root.X except BsonDocument does not have a property called X

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $add : ['$X', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 3);
        }

        [Fact]
        public void Select_Mql_Field_should_work_with_POCO()
        {
            var collection = Fixture.GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(root => Mql.Field(root, "X", Int32Serializer.Instance) + 1); // like root.X except C does not have a property called X

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $add : ['$X', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 3);
        }

        [Fact]
        public void Where_Mql_Field_should_work_with_BsonDocument()
        {
            var collection = Fixture.GetCollection<BsonDocument>();

            var queryable = collection.AsQueryable()
                .Where(root => Mql.Field(root, "X", Int32Serializer.Instance) == 1); // like root.X except BsonDocument does not have a property called X

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x["_id"].AsInt32).Should().Equal(1);
        }

        [Fact]
        public void Where_Mql_Field_should_work_with_POCO()
        {
            var collection = Fixture.GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Where(root => Mql.Field(root, "X", Int32Serializer.Instance) == 1); // like root.X except C does not have a property called X

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [BsonIgnoreExtraElements]
        public class C
        {
            public int Id { get; set; }
        }

        public sealed class ClassFixture : MongoDatabaseFixture
        {
            private readonly string CollectionName = nameof(MqlFieldTests);

            public IMongoCollection<TDocument> GetCollection<TDocument>()
                => Database.GetCollection<TDocument>(CollectionName);

            protected override void InitializeFixture()
            {
                var collection = CreateCollection<BsonDocument>(CollectionName);
                collection.InsertMany([
                    new BsonDocument { { "_id", 1 }, { "X", 1 } },
                    new BsonDocument { { "_id", 2 }, { "X", 2 } }
                ]);
            }
        }
    }
}
