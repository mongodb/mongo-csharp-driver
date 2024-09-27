/* Copyright 2019-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class BsonBinaryDataSerializerTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new BsonBinaryDataSerializer();

            subject._bsonType().Should().Be(BsonType.Binary);
        }

        [Fact]
        public void Instance_should_return_expected_result()
        {
            var subject = BsonBinaryDataSerializer.Instance;

            subject.Should().NotBeNull();
        }

        [Fact]
        public void Instance_should_return_cached_instance()
        {
            var subject1 = BsonBinaryDataSerializer.Instance;
            var subject2 = BsonBinaryDataSerializer.Instance;

            subject2.Should().BeSameAs(subject1);
        }

        [Fact]
        public void DeserializeValue_should_call_ReadBinaryData()
        {
            var subject = new BsonBinaryDataSerializer();
            var mockReader = new Mock<IBsonReader>();
            var binaryData = new BsonBinaryData(new byte[0]);
            mockReader.Setup(m => m.GetCurrentBsonType()).Returns(BsonType.Binary);
            mockReader.Setup(m => m.ReadBinaryData()).Returns(binaryData);
            var context = BsonDeserializationContext.CreateRoot(mockReader.Object);
            var args = new BsonDeserializationArgs();

            var result = subject.Deserialize(context, args);

            mockReader.Verify(m => m.ReadBinaryData(), Times.Once);
            result.Should().BeSameAs(binaryData);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonBinaryDataSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonBinaryDataSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonBinaryDataSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonBinaryDataSerializer();
            var y = new BsonBinaryDataSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonBinaryDataSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void SerializeValue_should_call_WriteBinaryData()
        {
            var subject = new BsonBinaryDataSerializer();

            var mockWriter = new Mock<IBsonWriter>();
            var context = BsonSerializationContext.CreateRoot(mockWriter.Object);
            var args = new BsonSerializationArgs();
            var value = new BsonBinaryData(new byte[0]);

            subject.Serialize(context, args, value);

            mockWriter.Verify(m => m.WriteBinaryData(value), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void SerializeValue_should_throw_when_value_representation_is_unspecified(
            [Values(
                GuidRepresentation.CSharpLegacy,
                GuidRepresentation.JavaLegacy,
                GuidRepresentation.PythonLegacy,
                GuidRepresentation.Standard,
                GuidRepresentation.Unspecified)] GuidRepresentation writerGuidRepresentation,
            [Values(
                GuidRepresentation.CSharpLegacy,
                GuidRepresentation.JavaLegacy,
                GuidRepresentation.PythonLegacy,
                GuidRepresentation.Standard,
                GuidRepresentation.Unspecified)] GuidRepresentation valueGuidRepresentation)
        {
            var subject = new BsonBinaryDataSerializer();

            var mockWriter = new Mock<IBsonWriter>();
            var writerSettings = new BsonBinaryWriterSettings();
            mockWriter.SetupGet(m => m.Settings).Returns(writerSettings);
            var context = BsonSerializationContext.CreateRoot(mockWriter.Object);
            var args = new BsonSerializationArgs();
            var subType = valueGuidRepresentation == GuidRepresentation.Unspecified ? BsonBinarySubType.UuidLegacy : GuidConverter.GetSubType(valueGuidRepresentation);
            var value = new BsonBinaryData(new byte[16], subType);

            var exception = Record.Exception(() => subject.Serialize(context, args, value));

            exception.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void SerializeValue_should_convert_representation_when_required(
            [Values(
                GuidRepresentation.CSharpLegacy,
                GuidRepresentation.JavaLegacy,
                GuidRepresentation.PythonLegacy,
                GuidRepresentation.Standard,
                GuidRepresentation.Unspecified)] GuidRepresentation writerGuidRepresentation,
            [Values(
                GuidRepresentation.CSharpLegacy,
                GuidRepresentation.JavaLegacy,
                GuidRepresentation.PythonLegacy,
                GuidRepresentation.Standard,
                GuidRepresentation.Unspecified)] GuidRepresentation valueGuidRepresentation)
        {
            var subject = new BsonBinaryDataSerializer();

            var mockWriter = new Mock<IBsonWriter>();
            var writerSettings = new BsonBinaryWriterSettings();
            mockWriter.SetupGet(m => m.Settings).Returns(writerSettings);
            var context = BsonSerializationContext.CreateRoot(mockWriter.Object);
            var args = new BsonSerializationArgs();
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var subType = valueGuidRepresentation == GuidRepresentation.Unspecified ? BsonBinarySubType.UuidLegacy : GuidConverter.GetSubType(valueGuidRepresentation);
            var value = new BsonBinaryData(bytes, subType);

            subject.Serialize(context, args, value);

            mockWriter.Verify(m => m.WriteBinaryData(value), Times.Once);
        }
    }
}
