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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4809Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_by_Id_with_custom_serializer_should_work()
        {
            var collection = GetCollection();
            var id = "111111111111111111111111";

            var filter = Builders<RootDocument>.Filter.Where(x => x.Id == id);

            var renderedFilter = filter.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry));
            renderedFilter.Should().Be("{ _id : ObjectId('111111111111111111111111' ) }");

            var result = collection.FindSync(filter).Single();
            result.X.Should().Be(1);
        }

        private IMongoCollection<RootDocument> GetCollection()
        {
            var collection = GetCollection<RootDocument>("test");
            CreateCollection(
                collection,
                new RootDocument { Id = "111111111111111111111111", X = 1 },
                new RootDocument { Id = "222222222222222222222222", X = 2 });
            return collection;
        }

        private class RootDocument
        {
            [BsonSerializer(typeof(CustomStringRepresentedAsObjectIdSerializer))]
            public string Id { get; set; }
            public int X { get; set; }
        }

        // This custom serializer is a slightly modified version of the custom serializer in the JIRA ticket
        // note: normally this serializer would implement IHasRepresentationSerializer
        // but for testing purposes we deliberately chose not to implement it
        public class CustomStringRepresentedAsObjectIdSerializer : SerializerBase<string>
        {
            public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                BsonType type = context.Reader.GetCurrentBsonType();
                switch (type)
                {
                    case BsonType.ObjectId: return context.Reader.ReadObjectId().ToString();
                    default:
                        var message = $"Cannot convert a {type} to a String.";
                        throw new NotSupportedException(message);
                }
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
            {
                context.Writer.WriteObjectId(ToObjectId(value));
            }

            private static ObjectId ToObjectId(string source)
            {
                if (ObjectId.TryParse(source, out ObjectId returnId))
                {
                    return returnId;
                }
                else
                {
                    return ObjectId.Empty;
                }
            }
        }
    }
}
