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
using System.Dynamic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class ObjectSerializerTests
    {
        public class C
        {
            public object Obj;
        }

        public class D
        {
            public object[] Array;
        }

        [Fact]
        public void TestArray()
        {
            var d = new D
            {
                Array = new object[]
                {
                    (Decimal128)1.5M,
                    1.5,
                    "abc",
                    new object(),
                    true,
                    BsonConstants.UnixEpoch,
                    null,
                    123,
                    123L
                }
            };
            var json = d.ToJson();
            var expected = "{ 'Array' : [#A] }";
            expected = expected.Replace("#A",
                string.Join(", ", new string[]
                    {
                        "NumberDecimal('1.5')",
                        "1.5",
                        "'abc'",
                        "{ }",
                        "true",
                        "ISODate('1970-01-01T00:00:00Z')",
                        "null",
                        "123",
                        "NumberLong(123)"
                    }));
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestBoolean()
        {
            var c = new C { Obj = true };
            var json = c.ToJson();
            var expected = "{ 'Obj' : true }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDateTime()
        {
            var c = new C { Obj = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = "{ 'Obj' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDecimal128()
        {
            var c = new C { Obj = (Decimal128)1.5M };
            var json = c.ToJson();
            var expected = "{ 'Obj' : NumberDecimal('1.5') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDouble()
        {
            var c = new C { Obj = 1.5 };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 1.5 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt32()
        {
            var c = new C { Obj = 123 };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 123 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt64()
        {
            var c = new C { Obj = 123L };
            var json = c.ToJson();
            var expected = "{ 'Obj' : NumberLong(123) }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Theory]
        [InlineData("{ Obj : {  _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\" } }")]
        public void TestMissingDiscriminator(string json)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.IsType<ExpandoObject>(result.Obj);
        }

        [Theory]
        [InlineData("{ Obj : { _t : \"System.Guid\" } }")]
        public void TestMissingValue(string json)
        {
            Assert.Throws<FormatException>(() => BsonSerializer.Deserialize<C>(json));
        }

        [Fact]
        public void TestNull()
        {
            var c = new C { Obj = null };
            var json = c.ToJson();
            var expected = "{ 'Obj' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestObject()
        {
            var c = new C { Obj = new object() };
            var json = c.ToJson();
            var expected = "{ 'Obj' : { } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Theory]
        [InlineData("{ Obj : { _t : \"System.Object[]\", _v : [] } }")]
        [InlineData("{ Obj : { _v : [], _t : \"System.Object[]\" } }")]
        public void TestOrderOfElementsDoesNotMatter(string json)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.IsType<object[]>(result.Obj);
        }

        [Theory]
        [InlineData("{ Obj : { _t : \"System.Guid\", _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\" } }", "01020304-0506-0708-090a-0b0c0d0e0f10")]
        [InlineData("{ Obj : { _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\", _t : \"System.Guid\" } }", "01020304-0506-0708-090a-0b0c0d0e0f10")]
        public void TestOrderOfElementsDoesNotMatter_with_Guids(string json, string expectedGuid)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.Equal(Guid.Parse(expectedGuid), result.Obj);
        }

        [Fact]
        public void TestString()
        {
            var c = new C { Obj = "abc" };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 'abc' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUsesDiscriminatorWhenTypeIsNotABsonPrimitive()
        {
            var c = new C { Obj = new ExpandoObject() };
            var json = c.ToJson(configurator: config => config.IsDynamicType = t => false);
#if NET452
            var discriminator = "System.Dynamic.ExpandoObject";
#else
            var discriminator = typeof(ExpandoObject).AssemblyQualifiedName;
#endif
            var expected = ("{ 'Obj' : { '_t' : '" + discriminator + "', '_v' : { } } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson(configurator: config => config.IsDynamicType = t => false);
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(configurator: config => config.IsDynamicType = t => false)));
        }

        [Fact]
        public void TestDoesNotUseDiscriminatorForDynamicTypes()
        {
            // explicitly setting the IsDynamicType and DynamicDocumentSerializer properties
            // in case we change the dynamic defaults in BsonDefaults.

            var c = new C { Obj = new ExpandoObject() };
            var json = c.ToJson(configurator: b => b.IsDynamicType = t => t == typeof(ExpandoObject));
            var expected = "{ 'Obj' : { } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson(configurator: b => b.IsDynamicType = t => t == typeof(ExpandoObject));
            var rehydrated = BsonSerializer.Deserialize<C>(bson, b => b.DynamicDocumentSerializer = BsonSerializer.LookupSerializer<ExpandoObject>());
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(configurator: b => b.IsDynamicType = t => t == typeof(ExpandoObject))));
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new ObjectSerializer();

            subject._discriminatorConvention().Should().Be(BsonSerializer.LookupDiscriminatorConvention(typeof(object)));
            subject._guidRepresentation().Should().Be(GuidRepresentation.Unspecified);
        }

        [Fact]
        public void constructor_with_discriminator_convention_should_initialize_instance()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();

            var subject = new ObjectSerializer(discriminatorConvention);

            subject._discriminatorConvention().Should().BeSameAs(discriminatorConvention);
            subject._guidRepresentation().Should().Be(GuidRepresentation.Unspecified);
        }

        [Fact]
        public void constructor_with_discriminator_convention_should_throw_when_discriminator_convention_is_null()
        {
            var exception = Record.Exception(() => new ObjectSerializer(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("discriminatorConvention");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_discriminator_convention_and_guid_representation_should_initialize_instance(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)] GuidRepresentation guidRepresentation)
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();

            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);

            subject._discriminatorConvention().Should().BeSameAs(discriminatorConvention);
            subject._guidRepresentation().Should().Be(guidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_should_throw_when_discriminator_convention_is_null()
        {
            var exception = Record.Exception(() => new ObjectSerializer(null, GuidRepresentation.Unspecified));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("discriminatorConvention");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Deserialize_binary_data_should_return_expected_result_when_guid_representation_is_unspecified_and_mode_is_v2(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation defaultGuidRepresentation,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation readerGuidRepresentation)
        {
#pragma warning disable 618
            var expectedGuidRepresentation = readerGuidRepresentation == GuidRepresentation.Unspecified ? defaultGuidRepresentation : readerGuidRepresentation;
            if (expectedGuidRepresentation == GuidRepresentation.Unspecified)
            {
                throw new SkipException("Skipped because expected GuidRepresentation is Unspecified.");
            }
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V2;
            BsonDefaults.GuidRepresentation = defaultGuidRepresentation;
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, GuidRepresentation.Unspecified);
            var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var subType = GuidConverter.GetSubType(expectedGuidRepresentation);
            bytes[11] = (byte)subType;
            var readerSettings = new BsonBinaryReaderSettings();
            if (readerGuidRepresentation != GuidRepresentation.Unspecified)
            {
                readerSettings.GuidRepresentation = readerGuidRepresentation;
            }
            using (var memoryStream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(memoryStream, readerSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                reader.ReadStartDocument();
                reader.ReadName("x");
                var result = subject.Deserialize<object>(context);

                var guidBytes = bytes.Skip(12).Take(16).ToArray();
                var expectedResult = GuidConverter.FromBytes(guidBytes, expectedGuidRepresentation);
                result.Should().Be(expectedResult);
            }
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Deserialize_binary_data_should_return_expected_result_when_guid_representation_is_specified(
            [ClassValues(typeof(GuidModeValues))]
            GuidMode mode,
            [Values(-1, GuidRepresentation.Unspecified)]
            GuidRepresentation readerGuidRepresentation,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
#pragma warning disable 618
            mode.Set();
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);
            var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var subType = GuidConverter.GetSubType(guidRepresentation);
            bytes[11] = (byte)subType;
            var readerSettings = new BsonBinaryReaderSettings();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                readerSettings.GuidRepresentation = readerGuidRepresentation == (GuidRepresentation)(-1) ? guidRepresentation : GuidRepresentation.Unspecified;
            }
            using (var memoryStream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(memoryStream, readerSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                reader.ReadStartDocument();
                reader.ReadName("x");
                var result = subject.Deserialize<object>(context);

                var guidBytes = bytes.Skip(12).Take(16).ToArray();
                var expectedResult = GuidConverter.FromBytes(guidBytes, guidRepresentation);
                result.Should().Be(expectedResult);
            }
#pragma warning restore 618
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Deserialize_binary_data_should_throw_when_guid_representation_is_unspecified_and_mode_is_v3()
        {
#pragma warning disable 618
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, GuidRepresentation.Unspecified);
            var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            using (var memoryStream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(memoryStream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                reader.ReadStartDocument();
                reader.ReadName("x");
                var exception = Record.Exception(() => subject.Deserialize<object>(context));

                exception.Should().BeOfType<BsonSerializationException>();
            }
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Deserialize_binary_data_should_throw_when_guid_representation_is_specified_and_sub_type_is_not_expected_sub_type(
            [ClassValues(typeof(GuidModeValues))]
            GuidMode mode,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation readerGuidRepresentation,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
#pragma warning disable 618
            mode.Set();
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);
            var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var incorrectSubType = guidRepresentation == GuidRepresentation.Standard ? BsonBinarySubType.UuidLegacy : BsonBinarySubType.UuidStandard;
            bytes[11] = (byte)incorrectSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                readerSettings.GuidRepresentation = readerGuidRepresentation;
            }
            using (var memoryStream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(memoryStream, readerSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                reader.ReadStartDocument();
                reader.ReadName("x");
                var exception = Record.Exception(() => subject.Deserialize<object>(context));

                exception.Should().BeOfType<FormatException>();
            }
#pragma warning restore 618
        }

        [SkippableTheory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Serialize_guid_should_have_expected_result_when_guid_representation_is_unspecified_and_mode_is_v2(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation defaultGuidRepresentation,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation writerGuidRepresentation)
        {
#pragma warning disable 618
            var expectedGuidRepresentation = writerGuidRepresentation != GuidRepresentation.Unspecified ? writerGuidRepresentation : defaultGuidRepresentation;
            if (expectedGuidRepresentation == GuidRepresentation.Unspecified)
            {
                throw new SkipException("Test skipped because expectedGuidRepresentation is Unspecified.");
            }
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V2;
            BsonDefaults.GuidRepresentation = defaultGuidRepresentation;
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, GuidRepresentation.Unspecified);
            var writerSettings = new BsonBinaryWriterSettings();
            if (writerGuidRepresentation != GuidRepresentation.Unspecified)
            {
                writerSettings.GuidRepresentation = writerGuidRepresentation;
            }
            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(memoryStream, writerSettings))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

                writer.WriteStartDocument();
                writer.WriteName("x");
                subject.Serialize(context, guid);
                writer.WriteEndDocument();

                var bytes = memoryStream.ToArray();
                var expectedBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
                var expectedSubType = GuidConverter.GetSubType(expectedGuidRepresentation);
                var expectedGuidBytes = GuidConverter.ToBytes(guid, expectedGuidRepresentation);
                expectedBytes[11] = (byte)expectedSubType;
                Array.Copy(expectedGuidBytes, 0, expectedBytes, 12, 16);
                bytes.Should().Equal(expectedBytes);
            }
#pragma warning restore 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Serialize_guid_should_have_expected_result_when_guid_representation_is_specified(
            [ClassValues(typeof(GuidModeValues))]
            GuidMode mode,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)]
            GuidRepresentation writerGuidRepresentation,
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
#pragma warning disable 618
            mode.Set();
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);
            var writerSettings = new BsonBinaryWriterSettings();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                writerSettings.GuidRepresentation = writerGuidRepresentation;
            }
            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(memoryStream, writerSettings))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

                writer.WriteStartDocument();
                writer.WriteName("x");
                subject.Serialize(context, guid);
                writer.WriteEndDocument();

                var bytes = memoryStream.ToArray();
                var expectedBytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
                var expectedSubType = GuidConverter.GetSubType(guidRepresentation);
                var expectedGuidBytes = GuidConverter.ToBytes(guid, guidRepresentation);
                expectedBytes[11] = (byte)expectedSubType;
                Array.Copy(expectedGuidBytes, 0, expectedBytes, 12, 16);
                bytes.Should().Equal(expectedBytes);
            }
#pragma warning restore 618
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Serialize_guid_should_throw_when_guid_representation_is_unspecified_and_mode_is_v3()
        {
#pragma warning disable 618
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, GuidRepresentation.Unspecified);
            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(memoryStream))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

                writer.WriteStartDocument();
                writer.WriteName("x");
                var exception = Record.Exception(() => subject.Serialize(context, guid));

                exception.Should().BeOfType<BsonSerializationException>();
            }
#pragma warning restore 618
        }
    }

    internal static class ObjectSerializerReflector
    {
        public static IDiscriminatorConvention _discriminatorConvention(this ObjectSerializer obj) => (IDiscriminatorConvention)Reflector.GetFieldValue(obj, nameof(_discriminatorConvention));
        public static GuidRepresentation _guidRepresentation(this ObjectSerializer obj) => (GuidRepresentation)Reflector.GetFieldValue(obj, nameof(_guidRepresentation));
    }
}
