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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5435Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Test_set_ValueObject_Value_using_creator_map()
        {
            var coll = GetCollection();
            var doc = new MyDocument();
            var filter = Builders<MyDocument>.Filter.Eq(x => x.Id, doc.Id);

            var pipelineError = new EmptyPipelineDefinition<MyDocument>()
                .Set(x => new MyDocument()
                {
                    ValueObject = new MyValue(x.ValueObject == null ? 1 : x.ValueObject.Value + 1)
                });
            var updateError = Builders<MyDocument>.Update.Pipeline(pipelineError);

            var updateStages =
                updateError.Render(new(coll.DocumentSerializer, BsonSerializer.SerializerRegistry))
                    .AsBsonArray
                    .Cast<BsonDocument>();
            AssertStages(updateStages, "{ $set : { ValueObject : { Value : { $cond : { if : { $eq : ['$ValueObject', null] }, then : 1, else : { $add : ['$ValueObject.Value', 1] } } } } } }");

            coll.UpdateOne(filter, updateError, new() { IsUpsert = true });
        }

        [Fact]
        public void Test_set_ValueObject_Value_using_property_setter()
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

            var updateStages =
                updateError.Render(new(coll.DocumentSerializer, BsonSerializer.SerializerRegistry))
                    .AsBsonArray
                    .Cast<BsonDocument>();
            AssertStages(updateStages, "{ $set : { ValueObject : { Value : { $cond : { if : { $eq : ['$ValueObject', null] }, then : 1, else : { $add : ['$ValueObject.Value', 1] } } } } } }");

            coll.UpdateOne(filter, updateError, new() { IsUpsert = true });
        }

        [Fact]
        public void Test_set_ValueObject_to_derived_value_using_property_setter()
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

        [Fact]
        public void Test_set_X_using_constructor()
        {
            var coll = GetCollection();
            var doc = new MyDocument();
            var filter = Builders<MyDocument>.Filter.Eq(x => x.Id, doc.Id);

            var pipelineError = new EmptyPipelineDefinition<MyDocument>()
                .Set(x => new MyDocument()
                {
                    X = new X(x.Y)
                });
            var updateError = Builders<MyDocument>.Update.Pipeline(pipelineError);

            var updateStages =
                updateError.Render(new(coll.DocumentSerializer, BsonSerializer.SerializerRegistry))
                    .AsBsonArray
                    .Cast<BsonDocument>();
            AssertStages(updateStages, "{ $set : { X : { Y : '$Y' } } }");

            coll.UpdateOne(filter, updateError, new() { IsUpsert = true });
        }

        [Fact]
        public void Test_set_A()
        {
            var coll = GetCollection();
            var doc = new MyDocument();
            var filter = Builders<MyDocument>.Filter.Eq(x => x.Id, doc.Id);

            var pipelineError = new EmptyPipelineDefinition<MyDocument>()
                .Set(x => new MyDocument()
                {
                    A = new [] { 2, x.A[0] }
                });
            var updateError = Builders<MyDocument>.Update.Pipeline(pipelineError);

            var updateStages =
                updateError.Render(new(coll.DocumentSerializer, BsonSerializer.SerializerRegistry))
                    .AsBsonArray
                    .Cast<BsonDocument>();
            AssertStages(updateStages, "{ $set : { A : ['2', { $arrayElemAt : ['$A', 0] }] } }");

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

            public X X { get; set; }

            public int Y { get; set; }

            [BsonRepresentation(BsonType.String)]
            public int[] A { get; set; }
        }

        class MyValue
        {
            [BsonConstructor]
            public MyValue() { }
            [BsonConstructor]
            public MyValue(int value) { Value = value; }
            public int Value { get; set; }
        }

        class MyDerivedValue : MyValue
        {
            public int B { get; set; }
        }

        [BsonSerializer(typeof(XSerializer))]
        class X
        {
            public X(int y)
            {
                Y = y;
            }
            public int Y { get; }
        }

        class XSerializer : SerializerBase<X>, IBsonDocumentSerializer
        {
            public override X Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                reader.ReadStartArray();
                _ = reader.ReadName();
                var y = reader.ReadInt32();
                reader.ReadEndDocument();

                return new X(y);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, X value)
            {
                var writer = context.Writer;
                writer.WriteStartDocument();
                writer.WriteName("Y");
                writer.WriteInt32(value.Y);
                writer.WriteEndDocument();
            }

            public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
            {
                serializationInfo = memberName == "Y" ? new BsonSerializationInfo("Y", Int32Serializer.Instance, typeof(int)) : null;
                return serializationInfo != null;
            }
        }
    }
}
