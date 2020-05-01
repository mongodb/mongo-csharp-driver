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
using MongoDB.Bson.TestHelpers.XunitExtensions;
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
            var data = new TheoryData<GuidRepresentationMode, GuidRepresentation, GuidRepresentation, GuidRepresentation, GuidRepresentation>();

            foreach (var defaultGuidRepresentationMode in EnumHelper.GetValues<GuidRepresentationMode>())
            {
                foreach (var defaultGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                {
                    if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && defaultGuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        continue;
                    }

                    foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                    {
                        if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && serializerGuidRepresentation == GuidRepresentation.Unspecified)
                        {
                            continue;
                        }

                        foreach (var readerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                        {
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 &&
                                serializerGuidRepresentation != GuidRepresentation.Unspecified &&
                                readerGuidRepresentation != GuidRepresentation.Unspecified &&
                                GuidConverter.GetSubType(serializerGuidRepresentation) != GuidConverter.GetSubType(readerGuidRepresentation))
                            {
                                continue;
                            }
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && readerGuidRepresentation != GuidRepresentation.Unspecified)
                            {
                                continue;
                            }

                            var expectedGuidRepresentation = serializerGuidRepresentation;
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && expectedGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                expectedGuidRepresentation = readerGuidRepresentation;
                            }
                            if (expectedGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                continue;
                            }

                            data.Add(defaultGuidRepresentationMode, defaultGuidRepresentation, serializerGuidRepresentation, readerGuidRepresentation, expectedGuidRepresentation);
                        }
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Deserialize_should_return_expected_result_when_representation_is_binary_MemberData))]
        [ResetGuidModeAfterTest]
        public void Deserializer_should_return_expected_result_when_representation_is_binary(
            GuidRepresentationMode defaultGuidRepresentationMode,
            GuidRepresentation defaultGuidRepresentation,
            GuidRepresentation serializerGuidRepresentation,
            GuidRepresentation readerGuidRepresentation,
            GuidRepresentation expectedGuidRepresentation)
        {
            GuidMode.Set(defaultGuidRepresentationMode, defaultGuidRepresentation);

            var subject = new GuidSerializer(serializerGuidRepresentation);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var documentSubType = GuidConverter.GetSubType(expectedGuidRepresentation);
            documentBytes[11] = (byte)documentSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2)
            {
#pragma warning disable 618
                readerSettings.GuidRepresentation = readerGuidRepresentation;
#pragma warning restore 618
            }
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var result = subject.Deserialize(context, args);

            var guidBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var expectedGuid = GuidConverter.FromBytes(guidBytes, expectedGuidRepresentation);
            result.Should().Be(expectedGuid);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void Deserialize_should_throw_when_representation_is_binary_and_length_is_invalid(int length)
        {
            var subject = new GuidSerializer(BsonType.Binary);
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
            var subject = new GuidSerializer(BsonType.Binary);
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

        public static IEnumerable<object[]> Deserialize_should_throw_when_representation_is_binary_and_guid_representation_is_unspecified_MemberData()
        {
            var data = new TheoryData<GuidRepresentationMode, GuidRepresentation, GuidRepresentation?>();

            foreach (var defaultGuidRepresentationMode in EnumHelper.GetValues<GuidRepresentationMode>())
            {
                foreach (var defaultGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                {
                    if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && defaultGuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        continue;
                    }

                    foreach (var readerGuidRepresentation in EnumHelper.GetValuesAndNull<GuidRepresentation>())
                    {
                        var effectiveGuidRepresentation = GuidRepresentation.Unspecified;
                        if (defaultGuidRepresentationMode == GuidRepresentationMode.V2)
                        {
                            effectiveGuidRepresentation = readerGuidRepresentation ?? defaultGuidRepresentation;
                        }
                        if (effectiveGuidRepresentation != GuidRepresentation.Unspecified)
                        {
                            continue;
                        }

                        data.Add(defaultGuidRepresentationMode, defaultGuidRepresentation, readerGuidRepresentation);
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Deserialize_should_throw_when_representation_is_binary_and_guid_representation_is_unspecified_MemberData))]
        [ResetGuidModeAfterTest]
        public void Deserialize_should_throw_when_representation_is_binary_and_guid_representation_is_unspecified(
            GuidRepresentationMode defaultGuidRepresentationMode,
            GuidRepresentation defaultGuidRepresentation,
            GuidRepresentation? readerGuidRepresentation)
        {
            GuidMode.Set(defaultGuidRepresentationMode, defaultGuidRepresentation);

            var subject = new GuidSerializer(GuidRepresentation.Unspecified);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var readerSettings = new BsonBinaryReaderSettings();
            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && readerGuidRepresentation.HasValue)
            {
#pragma warning disable 618
                readerSettings.GuidRepresentation = readerGuidRepresentation.Value;
#pragma warning restore 618
            }
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
            var data = new TheoryData<GuidRepresentationMode, GuidRepresentation, GuidRepresentation, GuidRepresentation?, BsonBinarySubType>();

            foreach (var defaultGuidRepresentationMode in EnumHelper.GetValues<GuidRepresentationMode>())
            {
                if (defaultGuidRepresentationMode == GuidRepresentationMode.V2)
                {
                    continue; // for backward compatibility GuidSerializer only enforces this constraint in V3 mode
                }

                foreach (var defaultGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                {
                    if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && defaultGuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        continue;
                    }

                    foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                    {
                        foreach (var readerGuidRepresentation in EnumHelper.GetValuesAndNull<GuidRepresentation>())
                        {
                            var effectiveGuidRepresentation = serializerGuidRepresentation;
#pragma warning disable 618
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && serializerGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                effectiveGuidRepresentation = readerGuidRepresentation ?? defaultGuidRepresentation;
                            }
#pragma warning restore 618
                            if (effectiveGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                continue;
                            }
                            var expectedSubType = GuidConverter.GetSubType(effectiveGuidRepresentation);

                            data.Add(defaultGuidRepresentationMode, defaultGuidRepresentation, serializerGuidRepresentation, readerGuidRepresentation, expectedSubType);
                        }
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Deserialize_should_throw_when_representation_is_binary_and_sub_type_does_not_match_MemberData))]
        [ResetGuidModeAfterTest]
        public void Deserialize_should_throw_when_representation_is_binary_and_sub_type_does_not_match(
            GuidRepresentationMode defaultGuidRepresentationMode,
            GuidRepresentation defaultGuidRepresentation,
            GuidRepresentation serializerGuidRepresentation,
            GuidRepresentation? readerGuidRepresentation,
            BsonBinarySubType expectedSubType)
        {
            GuidMode.Set(defaultGuidRepresentationMode, defaultGuidRepresentation);

            var subject = new GuidSerializer(serializerGuidRepresentation);
            var documentBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var nonMatchingSubType = expectedSubType == BsonBinarySubType.UuidLegacy ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
            documentBytes[11] = (byte)nonMatchingSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && readerGuidRepresentation.HasValue)
            {
#pragma warning disable 618
                readerSettings.GuidRepresentation = readerGuidRepresentation.Value;
#pragma warning restore 618
            }
            var reader = new BsonBinaryReader(new MemoryStream(documentBytes), readerSettings);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            var exception = Record.Exception(() => subject.Deserialize(context, args));

            exception.Should().BeOfType<FormatException>();
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Deserialize_should_return_expected_result_when_representation_is_string(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

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
            var data = new TheoryData<GuidRepresentationMode, GuidRepresentation, GuidRepresentation, GuidRepresentation?, BsonBinarySubType, GuidRepresentation>();

            foreach (var defaultGuidRepresentationMode in EnumHelper.GetValues<GuidRepresentationMode>())
            {
                foreach (var defaultGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                {
                    if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && defaultGuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        continue;
                    }

                    foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                    {
                        foreach (var writerGuidRepresentation in EnumHelper.GetValuesAndNull<GuidRepresentation>())
                        {
                            var effectiveGuidRepresentation = serializerGuidRepresentation;
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && serializerGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                effectiveGuidRepresentation = writerGuidRepresentation ?? defaultGuidRepresentation;
                            }
                            if (effectiveGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                continue;
                            }
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2)
                            {
                                var effectiveWriterGuidRepresentation = writerGuidRepresentation ?? defaultGuidRepresentation;
                                if (effectiveWriterGuidRepresentation != GuidRepresentation.Unspecified && effectiveGuidRepresentation != effectiveWriterGuidRepresentation)
                                {
                                    continue;
                                }
                            }

                            var expectedSubType = GuidConverter.GetSubType(effectiveGuidRepresentation);

                            data.Add(defaultGuidRepresentationMode, defaultGuidRepresentation, serializerGuidRepresentation, writerGuidRepresentation, expectedSubType, effectiveGuidRepresentation);
                        }
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Serialize_should_write_expected_bytes_MemberData))]
        [ResetGuidModeAfterTest]
        public void Serialize_should_write_expected_bytes(
            GuidRepresentationMode defaultGuiRepresentationMode,
            GuidRepresentation defaultGuidRepresentation,
            GuidRepresentation serializerGuidRepresentation,
            GuidRepresentation? writerGuidRepresentation,
            BsonBinarySubType expectedSubType,
            GuidRepresentation effectiveGuidRepresentation)
        {
            GuidMode.Set(defaultGuiRepresentationMode, defaultGuidRepresentation);

            var subject = new GuidSerializer(serializerGuidRepresentation);
            var memoryStream = new MemoryStream();
            var writerSettings = new BsonBinaryWriterSettings();
            if (defaultGuiRepresentationMode == GuidRepresentationMode.V2 && writerGuidRepresentation.HasValue)
            {
#pragma warning disable 618
                writerSettings.GuidRepresentation = writerGuidRepresentation.Value;
#pragma warning restore 618
            }
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
            Array.Copy(GuidConverter.ToBytes(value, effectiveGuidRepresentation), 0, expectedBytes, 12, 16);
            result.Should().Equal(expectedBytes);
        }

        public static IEnumerable<object[]> Serialize_should_throw_when_effectiveGuidRepresentation_is_Unspecified_MemberData()
        {
            var data = new TheoryData<GuidRepresentationMode, GuidRepresentation, GuidRepresentation, GuidRepresentation?>();

            foreach (var defaultGuidRepresentationMode in EnumHelper.GetValues<GuidRepresentationMode>())
            {
                foreach (var defaultGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                {
                    if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && defaultGuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        continue;
                    }

                    foreach (var serializerGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                    {
                        foreach (var writerGuidRepresentation in EnumHelper.GetValuesAndNull<GuidRepresentation>())
                        {
                            var effectiveGuidRepresentation = serializerGuidRepresentation;
                            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && serializerGuidRepresentation == GuidRepresentation.Unspecified)
                            {
                                effectiveGuidRepresentation = writerGuidRepresentation ?? defaultGuidRepresentation;
                            }
                            if (effectiveGuidRepresentation != GuidRepresentation.Unspecified)
                            {
                                continue;
                            }

                            data.Add(defaultGuidRepresentationMode, defaultGuidRepresentation, serializerGuidRepresentation, writerGuidRepresentation);
                        }
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Serialize_should_throw_when_effectiveGuidRepresentation_is_Unspecified_MemberData))]
        [ResetGuidModeAfterTest]
        public void Serialize_should_throw_when_effectiveGuidRepresentation_is_Unspecified(
            GuidRepresentationMode defaultGuidRepresentationMode,
            GuidRepresentation defaultGuidRepresentation,
            GuidRepresentation serializerGuidRepresentation,
            GuidRepresentation? writerGuidRepresentation)
        {
            GuidMode.Set(defaultGuidRepresentationMode, defaultGuidRepresentation);

            var subject = new GuidSerializer(serializerGuidRepresentation);
            var memoryStream = new MemoryStream();
            var writerSettings = new BsonBinaryWriterSettings();
            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 && writerGuidRepresentation.HasValue)
            {
#pragma warning disable 618
                writerSettings.GuidRepresentation = writerGuidRepresentation.Value;
#pragma warning restore 618
            }
            var writer = new BsonBinaryWriter(memoryStream, writerSettings);
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();
            var value = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("x");
            var exception = Record.Exception(() => subject.Serialize(context, args, value));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Serialize_shoud_write_expected_string_when_representation_is_string(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

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
    }
}
