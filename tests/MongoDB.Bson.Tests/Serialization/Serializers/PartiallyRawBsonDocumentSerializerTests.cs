/* Copyright 2015 MongoDB Inc.
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
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class PartiallyRawBsonDocumentSerializerTests
    {
        [Fact]
        public void constructor_should_throw_when_name_is_null()
        {
            Action action = () => new PartiallyRawBsonDocumentSerializer(null, BsonDocumentSerializer.Instance);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("name");
        }

        [Fact]
        public void constructor_should_throw_when_rawSerializer_is_null()
        {
            Action action = () => new PartiallyRawBsonDocumentSerializer("name", null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("rawSerializer");
        }

        [Fact]
        public void constructor_should_throw_when_rawSerializer_is_not_a_BsonValue_serializer()
        {
            Action action = () => new PartiallyRawBsonDocumentSerializer("name", new Int32Serializer());

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("rawSerializer");
        }

        [Fact]
        public void Deserialize_should_return_partially_raw_BsonDocument()
        {
            var document = new BsonDocument
            {
                { "a", new BsonDocument("x", 1) },
                { "b", new BsonDocument("x", 2) },
                { "c", new BsonDocument("x", 3) }
            };
            var bson = document.ToBson();
            var subject = new PartiallyRawBsonDocumentSerializer("b", RawBsonDocumentSerializer.Instance);

            var result = Deserialize(bson, subject);

            result["a"].Should().BeOfType<BsonDocument>();
            result["b"].Should().BeOfType<RawBsonDocument>();
            result["c"].Should().BeOfType<BsonDocument>();
        }

        [Fact]
        public void Deserialize_should_return_nested_partially_raw_BsonDocument()
        {
            var document = new BsonDocument
            {
                { "a", new BsonDocument("x", 1) },
                { "b", new BsonDocument
                    {
                        { "d", new BsonDocument("z", 1) },
                        { "e", new BsonDocument("z", 2) },
                        { "f", new BsonDocument("z", 3) },
                    }
                },
                { "c", new BsonDocument("x", 3) }
            };
            var bson = document.ToBson();
            var subject = new PartiallyRawBsonDocumentSerializer("b", 
                new PartiallyRawBsonDocumentSerializer("e", RawBsonDocumentSerializer.Instance));

            var result = Deserialize(bson, subject);

            result["a"].Should().BeOfType<BsonDocument>();
            result["b"].Should().BeOfType<BsonDocument>();
            result["c"].Should().BeOfType<BsonDocument>();
            result["b"]["d"].Should().BeOfType<BsonDocument>();
            result["b"]["e"].Should().BeOfType<RawBsonDocument>();
            result["b"]["f"].Should().BeOfType<BsonDocument>();
        }

        // private methods
        private BsonDocument Deserialize(byte[] bson, PartiallyRawBsonDocumentSerializer serializer)
        {
            using (var stream = new MemoryStream(bson))
            using (var reader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return serializer.Deserialize(context);
            }
        }
    }
}
