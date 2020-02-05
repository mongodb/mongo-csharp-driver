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
using MongoDB.Bson.TestHelpers.XunitExtensions;
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

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void SerializeValue_should_call_WriteBinaryData(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

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
        [ResetGuidModeAfterTest]
        public void SerializeValue_should_throw_when_value_representation_is_unspecified(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode,
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
            mode.Set();

#pragma warning disable 618
            var subject = new BsonBinaryDataSerializer();

            var mockWriter = new Mock<IBsonWriter>();
            var writerSettings = new BsonBinaryWriterSettings();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                writerSettings.GuidRepresentation = writerGuidRepresentation;
            }
            mockWriter.SetupGet(m => m.Settings).Returns(writerSettings);
            var context = BsonSerializationContext.CreateRoot(mockWriter.Object);
            var args = new BsonSerializationArgs();
            var subType = valueGuidRepresentation == GuidRepresentation.Unspecified ? BsonBinarySubType.UuidLegacy : GuidConverter.GetSubType(valueGuidRepresentation);
            var value = BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2
                ? new BsonBinaryData(new byte[16], subType, valueGuidRepresentation)
                : new BsonBinaryData(new byte[16], subType);

            var exception = Record.Exception(() => subject.Serialize(context, args, value));

            var isExceptionExpected =
                BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 &&
                writerGuidRepresentation != GuidRepresentation.Unspecified &&
                valueGuidRepresentation == GuidRepresentation.Unspecified;

            if (isExceptionExpected)
            {
                var e = exception.Should().BeOfType<BsonSerializationException>().Subject;
                e.Message.Should().Contain("Cannot serialize BsonBinaryData with GuidRepresentation Unspecified");
            }
            else
            {
                exception.Should().BeNull();
            }
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void SerializeValue_should_convert_representation_when_required(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode,
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
            mode.Set();

#pragma warning disable 618
            var subject = new BsonBinaryDataSerializer();

            var mockWriter = new Mock<IBsonWriter>();
            var writerSettings = new BsonBinaryWriterSettings();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                writerSettings.GuidRepresentation = writerGuidRepresentation;
            }
            mockWriter.SetupGet(m => m.Settings).Returns(writerSettings);
            var context = BsonSerializationContext.CreateRoot(mockWriter.Object);
            var args = new BsonSerializationArgs();
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var subType = valueGuidRepresentation == GuidRepresentation.Unspecified ? BsonBinarySubType.UuidLegacy : GuidConverter.GetSubType(valueGuidRepresentation);
            var value = BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2
                ? new BsonBinaryData(bytes, subType, valueGuidRepresentation)
                : new BsonBinaryData(bytes, subType);

            var isExceptionExpected =
                BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 &&
                writerGuidRepresentation != GuidRepresentation.Unspecified &&
                valueGuidRepresentation == GuidRepresentation.Unspecified;

            if (!isExceptionExpected)
            {
                subject.Serialize(context, args, value);

                var shouldConvertRepresentation =
                    BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 &&
                    writerGuidRepresentation != GuidRepresentation.Unspecified &&
                    valueGuidRepresentation != GuidRepresentation.Unspecified &&
                    valueGuidRepresentation != writerGuidRepresentation;

                var writtenValue = value;
                if (shouldConvertRepresentation)
                {
                    var guid = GuidConverter.FromBytes(bytes, valueGuidRepresentation);
                    var convertedBytes = GuidConverter.ToBytes(guid, writerGuidRepresentation);
                    var convertedSubType = GuidConverter.GetSubType(writerGuidRepresentation);
                    writtenValue = new BsonBinaryData(convertedBytes, convertedSubType, writerGuidRepresentation);
                }

                mockWriter.Verify(m => m.WriteBinaryData(writtenValue), Times.Once);
            }
#pragma warning restore 618
        }
    }
}
