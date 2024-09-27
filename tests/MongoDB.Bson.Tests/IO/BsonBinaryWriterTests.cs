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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.IO;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonBinaryWriterTests
    {
        [Theory]
        [ParameterAttributeData]
        public void BsonBinaryWriter_should_support_writing_multiple_documents(
            [Range(0, 3)]
            int numberOfDocuments)
        {
            var document = new BsonDocument("x", 1);
            var bson = document.ToBson();
            var expectedResult = Enumerable.Repeat(bson, numberOfDocuments).Aggregate(Enumerable.Empty<byte>(), (a, b) => a.Concat(b)).ToArray();

            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                for (var n = 0; n < numberOfDocuments; n++)
                {
                    binaryWriter.WriteStartDocument();
                    binaryWriter.WriteName("x");
                    binaryWriter.WriteInt32(1);
                    binaryWriter.WriteEndDocument();
                }

                var result = stream.ToArray();
                result.Should().Equal(expectedResult);
            }
        }

        [Fact]
        public void BsonBinaryWriter_should_support_writing_more_than_2GB()
        {
            RequireProcess.Check().Bits(64);

            using (var stream = new NullBsonStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var bigBinaryData = new BsonBinaryData(new byte[int.MaxValue / 2 - 1000]);
                for (var i = 0; i < 3; i++)
                {
                    binaryWriter.WriteStartDocument();
                    binaryWriter.WriteName("x");
                    binaryWriter.WriteBinaryData(bigBinaryData);
                    binaryWriter.WriteEndDocument();
                }

                var smallBinaryData = new BsonBinaryData(new byte[2000]);
                binaryWriter.WriteStartDocument();
                binaryWriter.WriteName("x");
                binaryWriter.WriteBinaryData(smallBinaryData);
                binaryWriter.WriteEndDocument();
            }
        }

        [Fact]
        public void BackpatchSize_should_throw_when_size_is_larger_than_2GB()
        {
            RequireProcess.Check().Bits(64);

            using (var stream = new NullBsonStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var bytes = new byte[int.MaxValue / 2]; // 1GB
                var binaryData = new BsonBinaryData(bytes);

                binaryWriter.WriteStartDocument();
                binaryWriter.WriteName("array");
                binaryWriter.WriteStartArray();
                binaryWriter.WriteBinaryData(binaryData);
                binaryWriter.WriteBinaryData(binaryData);

                Action action = () => binaryWriter.WriteEndArray(); // indirectly calls private BackpatchSize method

                action.ShouldThrow<FormatException>();
            }
        }

        [Fact]
        public void WriteGuid_should_work()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BsonBinaryWriter(memoryStream);
            var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("v");
            writer.WriteGuid(guid);
            writer.WriteEndDocument();

            var documentBytes = memoryStream.ToArray();
            var subType = (BsonBinarySubType)documentBytes[11];
            var guidBytes = documentBytes.Skip(12).Take(16).ToArray();

            subType.Should().Be(BsonBinarySubType.UuidStandard);
            guidBytes.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        }

        [Theory]
        [InlineData(GuidRepresentation.Standard, BsonBinarySubType.UuidStandard, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 })]
        [InlineData(GuidRepresentation.CSharpLegacy, BsonBinarySubType.UuidLegacy, new byte[] { 4, 3, 2, 1, 6, 5, 8, 7, 9, 10, 11, 12, 13, 14, 15, 16 })]
        [InlineData(GuidRepresentation.JavaLegacy, BsonBinarySubType.UuidLegacy, new byte[] { 8, 7, 6, 5, 4, 3, 2, 1, 16, 15, 14, 13, 12, 11, 10, 9 })]
        [InlineData(GuidRepresentation.PythonLegacy, BsonBinarySubType.UuidLegacy, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 })]
        public void WriteGuid_with_guidRepresentation_should_work(GuidRepresentation guidRepresentation, BsonBinarySubType expectedSubType, byte[] expectedGuidBytes)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BsonBinaryWriter(memoryStream);
            var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("v");
            writer.WriteGuid(guid, guidRepresentation);
            writer.WriteEndDocument();

            var documentBytes = memoryStream.ToArray();
            var subType = (BsonBinarySubType)documentBytes[11];
            var guidBytes = documentBytes.Skip(12).Take(16).ToArray();

            subType.Should().Be(expectedSubType);
            guidBytes.Should().Equal(expectedGuidBytes);
        }
    }
}
