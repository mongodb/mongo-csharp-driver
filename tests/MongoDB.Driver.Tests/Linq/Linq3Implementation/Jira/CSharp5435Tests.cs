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
    public class CSharp5435Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Test()
        {
            var coll = GetCollection();
            var doc = new MyDocument();
            var filter = Builders<MyDocument>.Filter.Eq(x => x.Id, doc.Id);

            var pipelineError = new EmptyPipelineDefinition<MyDocument>()
                .Set(x => new MyDocument()
                {
                    ValueObject = new MyValue()
                    {
                        Value = x.ValueObject == null ? 1 : x.ValueObject.Value + 1
                    }
                });
            var updateError = Builders<MyDocument>.Update.Pipeline(pipelineError);

            coll.UpdateOne(filter, updateError, new() { IsUpsert = true });
        }

        [Fact]
        public void TestDerived()
        {
            var coll = GetCollection();
            var doc = new MyDocument();
            var filter = Builders<MyDocument>.Filter.Eq(x => x.Id, doc.Id);

            var pipelineError = new EmptyPipelineDefinition<MyDocument>()
                .Set(x => new MyDocument()
                {
                    ValueObject = new MyDerivedValue()
                    {
                        Value = x.ValueObject == null ? 1 : x.ValueObject.Value + 1,
                        B = 42
                    }
                });
            var updateError = Builders<MyDocument>.Update.Pipeline(pipelineError);

            coll.UpdateOne(filter, updateError, new() { IsUpsert = true });
        }

        private IMongoCollection<MyDocument> GetCollection()
        {
            var collection = GetCollection<MyDocument>("test");
            CreateCollection(
                collection.Database.GetCollection<BsonDocument>("test"),
                BsonDocument.Parse("{ _id : 1 }"),
                BsonDocument.Parse("{ _id : 2, X : null }"),
                BsonDocument.Parse("{ _id : 3, X : 3 }"));
            return collection;
        }

        class MyDocument
        {
            [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
            public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

            public MyValue ValueObject { get; set; }

            public long Long { get; set; }
        }

        class MyValue
        {
            public int Value { get; set; }
        }

        class MyDerivedValue : MyValue
        {
            public int B { get; set; }
        }
    }
}
