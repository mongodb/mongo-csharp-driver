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

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class GuidSerializerTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new GuidSerializer();

            subject.GuidRepresentation.Should().Be(GuidRepresentation.Unspecified);
            subject.Representation.Should().Be(BsonType.Binary);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_representation_should_initialize_instance(
            [Values(BsonType.Binary, BsonType.String)]
            BsonType representation)
        {
            var subject = new GuidSerializer(representation);

            subject.GuidRepresentation.Should().Be(GuidRepresentation.Unspecified);
            subject.Representation.Should().Be(representation);
        }

        [Fact]
        public void constructor_with_representation_should_throw_when_representation_is_invalid()
        {
            var exception = Record.Exception(() => new GuidSerializer(BsonType.Int32));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("representation");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_guid_representation_should_initialize_instance(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation guidRepresentation)
        {
            var subject = new GuidSerializer(guidRepresentation);

            subject.GuidRepresentation.Should().Be(guidRepresentation);
            subject.Representation.Should().Be(BsonType.Binary);
        }

        [Theory]
        [ParameterAttributeData]
        public void GuidRepresentation_should_return_expected_result(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation guidRepresentation)
        {
            var subject = new GuidSerializer(guidRepresentation);

            var result = subject.GuidRepresentation;

            result.Should().Be(guidRepresentation);
        }

        [Theory]
        [ParameterAttributeData]
        public void Representation_should_return_expected_result(
            [Values(BsonType.Binary, BsonType.String)]
            BsonType representation)
        {
            var subject = new GuidSerializer(representation);

            var result = subject.Representation;

            result.Should().Be(representation);
        }

        public static IEnumerable<object[]> Deserialize_should_return_expected_result_when_representation_is_binary_MemberData()
        {
            var data = new TheoryData<GuidRepresentation>();

            foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
            {
                if (serializerGuidRepresentation == GuidRepresentation.Unspecified)
                {
                    continue;
                }

                data.Add(serializerGuidRepresentation);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Deserialize_should_return_expected_result_when_representation_is_binary_MemberData))]
        public void Deserialize_should_return_expected_result_when_representation_is_binary(
            GuidRepresentation serializerGuidRepresentation)
        {
            var subject = new GuidSerializer(serializerGuidRepresentation);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var documentSubType = GuidConverter.GetSubType(serializerGuidRepresentation);
            documentBytes[11] = (byte)documentSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var result = subject.Deserialize(context, args);

            var guidBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var expectedGuid = GuidConverter.FromBytes(guidBytes, serializerGuidRepresentation);
            result.Should().Be(expectedGuid);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void Deserialize_should_throw_when_representation_is_binary_and_length_is_invalid(int length)
        {
            var subject = new GuidSerializer(GuidRepresentation.Standard);
            var document = new BsonDocument("x", new BsonBinaryData(new byte[length]));
            var documentBytes = document.ToBson();
            var readerSettings = new BsonBinaryReaderSettings();
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var exception = Record.Exception(() => subject.Deserialize(context, args));

            exception.Should().BeOfType<FormatException>();
        }

        [Theory]
        [InlineData(BsonBinarySubType.Binary)]
        [InlineData(BsonBinarySubType.Encrypted)]
        [InlineData(BsonBinarySubType.Function)]
        [InlineData(BsonBinarySubType.MD5)]
#pragma warning disable 618
        [InlineData(BsonBinarySubType.OldBinary)]
#pragma warning restore 0618
        [InlineData(BsonBinarySubType.UserDefined)]
        public void Deserialize_should_throw_when_representation_is_binary_and_sub_type_is_invalid(BsonBinarySubType documentSubType)
        {
            var subject = new GuidSerializer(GuidRepresentation.Standard);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            documentBytes[11] = (byte)documentSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var exception = Record.Exception(() => subject.Deserialize(context, args));

            exception.Should().BeOfType<FormatException>();
        }

        [Fact]
        public void Deserialize_should_throw_when_representation_is_binary_and_guid_representation_is_unspecified()
        {
            var subject = new GuidSerializer(GuidRepresentation.Unspecified);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var readerSettings = new BsonBinaryReaderSettings();
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var exception = Record.Exception(() => subject.Deserialize(context, args));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        public static IEnumerable<object[]> Deserialize_should_throw_when_representation_is_binary_and_sub_type_does_not_match_MemberData()
        {
            var data = new TheoryData<GuidRepresentation, BsonBinarySubType>();

            foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
            {
                if (serializerGuidRepresentation == GuidRepresentation.Unspecified)
                {
                    continue;
                }
                var expectedSubType = GuidConverter.GetSubType(serializerGuidRepresentation);

                data.Add(serializerGuidRepresentation, expectedSubType);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Deserialize_should_throw_when_representation_is_binary_and_sub_type_does_not_match_MemberData))]
        public void Deserialize_should_throw_when_representation_is_binary_and_sub_type_does_not_match(
            GuidRepresentation serializerGuidRepresentation,
            BsonBinarySubType expectedSubType)
        {
            var subject = new GuidSerializer(serializerGuidRepresentation);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var nonMatchingSubType = expectedSubType == BsonBinarySubType.UuidLegacy ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
            documentBytes[11] = (byte)nonMatchingSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var exception = Record.Exception(() => subject.Deserialize(context, args));

            exception.Should().BeOfType<FormatException>();
        }

        [Fact]
        public void Deserialize_should_return_expected_result_when_representation_is_string()
        {
            var subject = new GuidSerializer(BsonType.String);
            var json = "\"01020304-0506-0708-090a-0b0c0d0e0f10\"";
            var reader = new JsonReader(json, new JsonReaderSettings());
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var result = subject.Deserialize(context, args);

            result.Should().Be(new Guid("01020304-0506-0708-090a-0b0c0d0e0f10"));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1.0")]
        public void Deserialize_should_throw_when_bson_type_is_invalid(string json)
        {
            var subject = new GuidSerializer(BsonType.String);
            var reader = new JsonReader(json, new JsonReaderSettings());
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var exception = Record.Exception(() => subject.Deserialize(context, args));

            exception.Should().BeOfType<FormatException>();
        }

        public static IEnumerable<object[]> Serialize_should_write_expected_bytes_MemberData()
        {
            var data = new TheoryData<GuidRepresentation, BsonBinarySubType>();

            foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
            {
                if (serializerGuidRepresentation == GuidRepresentation.Unspecified)
                {
                    continue;
                }

                var expectedSubType = GuidConverter.GetSubType(serializerGuidRepresentation);

                data.Add(serializerGuidRepresentation, expectedSubType);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Serialize_should_write_expected_bytes_MemberData))]
        public void Serialize_should_write_expected_bytes(
            GuidRepresentation serializerGuidRepresentation,
            BsonBinarySubType expectedSubType)
        {
            var subject = new GuidSerializer(serializerGuidRepresentation);
            var memoryStream = new MemoryStream();
            var writerSettings = new BsonBinaryWriterSettings();
            var writer = new BsonBinaryWriter(memoryStream, writerSettings);
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();
            var value = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("x");
            subject.Serialize(context, args, value);
            writer.WriteEndDocument();
            var result = memoryStream.ToArray();

            var expectedBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            expectedBytes[11] = (byte)expectedSubType;
            Array.Copy(GuidConverter.ToBytes(value, serializerGuidRepresentation), 0, expectedBytes, 12, 16);
            result.Should().Equal(expectedBytes);
        }

        [Fact]
        public void Serialize_should_throw_when_guidRepresentation_is_Unspecified()
        {
            var subject = new GuidSerializer(GuidRepresentation.Unspecified);
            var memoryStream = new MemoryStream();
            var writerSettings = new BsonBinaryWriterSettings();
            var writer = new BsonBinaryWriter(memoryStream, writerSettings);
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();
            var value = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("x");
            var exception = Record.Exception(() => subject.Serialize(context, args, value));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        [Fact]
        public void Serialize_should_write_expected_string_when_representation_is_string()
        {
            var subject = new GuidSerializer(BsonType.String);
            var stringWriter = new StringWriter();
            var writer = new JsonWriter(stringWriter);
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();
            var value = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");

            subject.Serialize(context, args, value);
            var result = stringWriter.ToString();

            result.Should().Be("\"01020304-0506-0708-090a-0b0c0d0e0f10\"");
        }

        [Fact]
        public void WithGuidRepresentation_should_return_expected_result()
        {
            var subject = new GuidSerializer(GuidRepresentation.CSharpLegacy);

            var result = subject.WithGuidRepresentation(GuidRepresentation.JavaLegacy);

            result.Representation.Should().Be(BsonType.Binary);
            result.GuidRepresentation.Should().Be(GuidRepresentation.JavaLegacy);
        }

        [Fact]
        public void WithRepresentation_should_return_expected_result()
        {
            var subject = new GuidSerializer(BsonType.Binary);

            var result = subject.WithRepresentation(BsonType.String);

            result.Representation.Should().Be(BsonType.String);
            result.GuidRepresentation.Should().Be(GuidRepresentation.Unspecified);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new GuidSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new GuidSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new GuidSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new GuidSerializer();
            var y = new GuidSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("guidRepresentation")]
        [InlineData("representation")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var x = new GuidSerializer();
            var y = notEqualFieldName switch
            {
                "guidRepresentation" => new GuidSerializer(GuidRepresentation.JavaLegacy),
                "representation" => new GuidSerializer(BsonType.String),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new GuidSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
