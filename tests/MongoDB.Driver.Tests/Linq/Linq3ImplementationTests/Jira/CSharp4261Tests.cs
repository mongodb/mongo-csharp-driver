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

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4261Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData("bbbbbbbbbbbbbbbbbbbbbbbb", "{ $match : { _id : ObjectId('bbbbbbbbbbbbbbbbbbbbbbbb')  } }", "Bill")]
        [InlineData("charlie", "{ $match : { _id : 'charlie'  } }", "Charlie")]
        public void Where_with_Id_with_custom_serializer_should_work(string id, string expectedStage, string expectedName)
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Select(x => x.Name).Should().Equal(expectedName);
        }

        private IMongoCollection<Person> CreateCollection()
        {
            var collection = GetCollection<Person>();

            CreateCollection(
                collection,
                new Person { Id = "aaaaaaaaaaaaaaaaaaaaaaaa", Name = "Adam" },
                new Person { Id = "bbbbbbbbbbbbbbbbbbbbbbbb", Name = "Bill" },
                new Person { Id = "charlie", Name = "Charlie" });

            return collection;
        }

        public class Person
        {
            [BsonId, AsObjectId]
            public string Id { get; set; }

            public string Name { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class AsObjectIdAttribute : BsonSerializerAttribute
        {
            public AsObjectIdAttribute() : base(typeof(ObjectIdSerializer)) { }

            private class ObjectIdSerializer : SerializerBase<string>, IRepresentationConfigurable
            {
                public BsonType Representation => BsonType.ObjectId;

                public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, string value)
                {
                    if (value == null)
                    {
                        ctx.Writer.WriteNull(); return;
                    }

                    if (value.Length == 24 && ObjectId.TryParse(value, out var oID))
                    {
                        ctx.Writer.WriteObjectId(oID); return;
                    }

                    ctx.Writer.WriteString(value);
                }

                public override string Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
                {
                    switch (ctx.Reader.CurrentBsonType)
                    {
                        case BsonType.String:
                            return ctx.Reader.ReadString();

                        case BsonType.ObjectId:
                            return ctx.Reader.ReadObjectId().ToString();

                        case BsonType.Null:
                            ctx.Reader.ReadNull();
                            return null;

                        default:
                            throw new BsonSerializationException($"'{ctx.Reader.CurrentBsonType}' values are not valid on properties decorated with an [AsObjectId] attribute!");
                    }
                }

                public IBsonSerializer WithRepresentation(BsonType representation) => throw new NotImplementedException();
            }
        }
    }
}
