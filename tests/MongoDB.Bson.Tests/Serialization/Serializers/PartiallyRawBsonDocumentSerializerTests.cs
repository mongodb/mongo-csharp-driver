/* Copyright 2015-present MongoDB Inc.
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

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new PartiallyRawBsonDocumentSerializer("name", BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new PartiallyRawBsonDocumentSerializer("name", BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new PartiallyRawBsonDocumentSerializer("name", BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new PartiallyRawBsonDocumentSerializer("name", BsonValueSerializer.Instance);
            var y = new PartiallyRawBsonDocumentSerializer("name", BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("rawSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var rawSerializer1 = new RawBsonArraySerializer();
            var rawSerializer2 = new RawBsonDocumentSerializer();
            var x = new PartiallyRawBsonDocumentSerializer("name1", rawSerializer1);
            var y = notEqualFieldName switch
            {
                "name" => new PartiallyRawBsonDocumentSerializer("name2", rawSerializer1),
                "rawSerializer" => new PartiallyRawBsonDocumentSerializer("name1", rawSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new PartiallyRawBsonDocumentSerializer("name", BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
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
