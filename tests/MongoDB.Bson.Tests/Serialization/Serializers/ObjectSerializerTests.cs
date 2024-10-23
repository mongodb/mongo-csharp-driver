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
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class ObjectSerializerTests
    {
        private static Func<Type, bool> __defaultAllowedDeserializationTypes = ObjectSerializer.DefaultAllowedTypes;
        private static Func<Type, bool> __defaultAllowedSerializationTypes = ObjectSerializer.DefaultAllowedTypes;
        private static IDiscriminatorConvention __defaultDiscriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
        private static GuidRepresentation __defaultGuidRepresentation = GuidRepresentation.Unspecified;

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
            var json = d.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'Obj' : true }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestBsonDecimal128()
        {
            var value = (BsonDecimal128)1.5M;
            var c = new C { Obj = value };

            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            json.Should().Be("{ \"Obj\" : { \"_t\" : \"MongoDB.Bson.BsonDecimal128, MongoDB.Bson\", \"_v\" : NumberDecimal(\"1.5\") } }");

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            rehydrated.Obj.Should().BeOfType<BsonDecimal128>();
            rehydrated.Obj.Should().Be(value);

            rehydrated.ToBson().Should().Equal(bson);
        }

        [Fact]
        public void TestDateTime()
        {
            var c = new C { Obj = BsonConstants.UnixEpoch };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'Obj' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDecimal()
        {
            var value = 1.5M;
            var c = new C { Obj = value };

            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            json.Should().Be("""{ "Obj" : { "_t" : "System.Decimal", "_v" : NumberDecimal("1.5") } }""");

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            rehydrated.Obj.Should().BeOfType<decimal>();
            rehydrated.Obj.Should().Be(value);

            rehydrated.ToBson().Should().Equal(bson);
        }

        [Fact]
        public void TestDecimal128()
        {
            var value = (Decimal128)1.5M;
            var c = new C { Obj = value };

            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            json.Should().Be("{ \"Obj\" : NumberDecimal(\"1.5\") }");

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            rehydrated.Obj.Should().BeOfType<Decimal128>();
            rehydrated.Obj.Should().Be(value);

            rehydrated.ToBson().Should().Equal(bson);
        }

        [Fact]
        public void TestDouble()
        {
            var c = new C { Obj = 1.5 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
#if NET472
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
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var subject = new ObjectSerializer();

            subject.AllowedDeserializationTypes.Should().BeSameAs(__defaultAllowedDeserializationTypes);
            subject.AllowedSerializationTypes.Should().BeSameAs(__defaultAllowedSerializationTypes);
            subject.DiscriminatorConvention.Should().BeSameAs(__defaultDiscriminatorConvention);
            subject.GuidRepresentation.Should().Be(__defaultGuidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_should_initialize_instance()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();

            var subject = new ObjectSerializer(discriminatorConvention);

            subject.AllowedDeserializationTypes.Should().BeSameAs(__defaultAllowedDeserializationTypes);
            subject.AllowedSerializationTypes.Should().BeSameAs(__defaultAllowedSerializationTypes);
            subject.DiscriminatorConvention.Should().BeSameAs(discriminatorConvention);
            subject.GuidRepresentation.Should().Be(__defaultGuidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_should_throw_when_discriminator_convention_is_null()
        {
            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention: null));

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

            subject.AllowedDeserializationTypes.Should().BeSameAs(__defaultAllowedDeserializationTypes);
            subject.AllowedSerializationTypes.Should().BeSameAs(__defaultAllowedSerializationTypes);
            subject.DiscriminatorConvention.Should().BeSameAs(discriminatorConvention);
            subject.GuidRepresentation.Should().Be(guidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_should_throw_when_discriminator_convention_is_null()
        {
            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention: null, GuidRepresentation.Unspecified));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("discriminatorConvention");
        }

        [Fact]
        public void constructor_with_allowed_types_should_initialize_instance()
        {
            Func<Type, bool> allowedTypes = t => true;

            var subject = new ObjectSerializer(allowedTypes);

            subject.AllowedDeserializationTypes.Should().BeSameAs(allowedTypes);
            subject.AllowedSerializationTypes.Should().BeSameAs(allowedTypes);
            subject.DiscriminatorConvention.Should().BeSameAs(__defaultDiscriminatorConvention);
            subject.GuidRepresentation.Should().Be(__defaultGuidRepresentation);
        }

        [Fact]
        public void constructor_with_allowed_types_should_throw_when_allowed_types_is_null()
        {
            var exception = Record.Exception(() => new ObjectSerializer(allowedTypes: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("allowedTypes");
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_allowed_types_should_initialize_instance()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();
            Func<Type, bool> allowedTypes = t => true;

            var subject = new ObjectSerializer(discriminatorConvention, allowedTypes);

            subject.AllowedDeserializationTypes.Should().BeSameAs(allowedTypes);
            subject.AllowedSerializationTypes.Should().BeSameAs(allowedTypes);
            subject.DiscriminatorConvention.Should().BeSameAs(discriminatorConvention);
            subject.GuidRepresentation.Should().Be(__defaultGuidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_allowed_types_should_throw_when_discriminator_convention_is_null()
        {
            Func<Type, bool> allowedTypes = t => true;

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention: null, allowedTypes));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("discriminatorConvention");
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_allowed_types_should_throw_when_allowed_types_is_null()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention, allowedTypes: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("allowedTypes");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_discriminator_convention_and_guid_representation_and_allowed_types_should_initialize_instance(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)] GuidRepresentation guidRepresentation)
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();
            Func<Type, bool> allowedTypes = t => true;

            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation, allowedTypes);

            subject.AllowedDeserializationTypes.Should().BeSameAs(allowedTypes);
            subject.AllowedSerializationTypes.Should().BeSameAs(allowedTypes);
            subject.DiscriminatorConvention.Should().BeSameAs(discriminatorConvention);
            subject.GuidRepresentation.Should().Be(guidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_and_allowed_types_should_throw_when_discriminator_convention_is_null()
        {
            var guidRepresentation = GuidRepresentation.Standard;
            Func<Type, bool> allowedTypes = t => true;

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention: null, guidRepresentation, allowedTypes));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("discriminatorConvention");
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_and_allowed_types_should_throw_when_allowed_types_is_null()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();
            var guidRepresentation = GuidRepresentation.Standard;

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention: null, guidRepresentation, allowedTypes: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("allowedTypes");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_discriminator_convention_and_guid_representation_and_seperate_deserialization_and_serialization_allowed_types_should_initialize_instance(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.Standard, GuidRepresentation.Unspecified)] GuidRepresentation guidRepresentation)
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();
            Func<Type, bool> allowedTypes1 = t => true;
            Func<Type, bool> allowedTypes2 = t => true;

            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation, allowedTypes1, allowedTypes2);

            subject.AllowedDeserializationTypes.Should().BeSameAs(allowedTypes1);
            subject.AllowedSerializationTypes.Should().BeSameAs(allowedTypes2);
            subject.DiscriminatorConvention.Should().BeSameAs(discriminatorConvention);
            subject.GuidRepresentation.Should().Be(guidRepresentation);
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_and_seperate_deserialization_and_serialization_allowed_types_should_throw_when_discriminator_convention_is_null()
        {
            var guidRepresentation = GuidRepresentation.Standard;
            Func<Type, bool> allowedTypes1 = t => true;
            Func<Type, bool> allowedTypes2 = t => true;

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention: null, guidRepresentation, allowedTypes1, allowedTypes2));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("discriminatorConvention");
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_and_seperate_deserialization_and_serialization_allowed_types_should_throw_when_allowed_deserialization_types_is_null()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();
            var guidRepresentation = GuidRepresentation.Standard;
            Func<Type, bool> allowedTypes2 = t => true;

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention, guidRepresentation, allowedDeserializationTypes: null, allowedTypes2));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("allowedDeserializationTypes");
        }

        [Fact]
        public void constructor_with_discriminator_convention_and_guid_representation_and_seperate_deserialization_and_serialiazation_allowed_types_should_throw_when_allowed_serialization_types_is_null()
        {
            var discriminatorConvention = Mock.Of<IDiscriminatorConvention>();
            var guidRepresentation = GuidRepresentation.Standard;
            Func<Type, bool> allowedTypes1 = t => true;

            var exception = Record.Exception(() => new ObjectSerializer(discriminatorConvention, guidRepresentation, allowedTypes1, allowedSerializationTypes: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("allowedSerializationTypes");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ObjectSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ObjectSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ObjectSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ObjectSerializer();
            var y = new ObjectSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("allowedDeserializationTypes")]
        [InlineData("allowedSerializationTypes")]
        [InlineData("discriminatorConvention")]
        [InlineData("guidRepresentation")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var discriminatorConvention1 = new ScalarDiscriminatorConvention("_t");
            var discriminatorConvention2 = new ScalarDiscriminatorConvention("_u");
            var guidRepresentation1 = GuidRepresentation.Standard;
            var guidRepresentation2 = GuidRepresentation.CSharpLegacy;
            Func<Type, bool> allowedDeserializationTypes1 = t => false;
            Func<Type, bool> allowedDeserializationTypes2 = t => true;
            Func<Type, bool> allowedSerializationTypes1 = t => false;
            Func<Type, bool> allowedSerializationTypes2 = t => true;
            var x = new ObjectSerializer(discriminatorConvention1, guidRepresentation1, allowedDeserializationTypes1, allowedSerializationTypes1);
            var y = notEqualFieldName switch
            {
                "discriminatorConvention" => new ObjectSerializer(discriminatorConvention2, guidRepresentation1, allowedDeserializationTypes1, allowedSerializationTypes1),
                "guidRepresentation" => new ObjectSerializer(discriminatorConvention1, guidRepresentation2, allowedDeserializationTypes1, allowedSerializationTypes1),
                "allowedDeserializationTypes" => new ObjectSerializer(discriminatorConvention1, guidRepresentation1, allowedDeserializationTypes2, allowedSerializationTypes1),
                "allowedSerializationTypes" => new ObjectSerializer(discriminatorConvention1, guidRepresentation1, allowedDeserializationTypes1, allowedSerializationTypes2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ObjectSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Deserialize_binary_data_should_return_expected_result_when_guid_representation_is_specified(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);
            var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var subType = GuidConverter.GetSubType(guidRepresentation);
            bytes[11] = (byte)subType;
            var readerSettings = new BsonBinaryReaderSettings();
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
        }

        [Fact]
        public void Deserialize_binary_data_should_throw_when_guid_representation_is_unspecified()
        {
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
        }

        [Theory]
        [ParameterAttributeData]
        public void Deserialize_binary_data_should_throw_when_guid_representation_is_specified_and_sub_type_is_not_expected_sub_type(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);
            var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
            var incorrectSubType = guidRepresentation == GuidRepresentation.Standard ? BsonBinarySubType.UuidLegacy : BsonBinarySubType.UuidStandard;
            bytes[11] = (byte)incorrectSubType;
            var readerSettings = new BsonBinaryReaderSettings();
            using (var memoryStream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(memoryStream, readerSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                reader.ReadStartDocument();
                reader.ReadName("x");
                var exception = Record.Exception(() => subject.Deserialize<object>(context));

                exception.Should().BeOfType<FormatException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Serialize_guid_should_have_expected_result_when_guid_representation_is_specified(
            [Values(GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard)]
            GuidRepresentation guidRepresentation)
        {
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
            var subject = new ObjectSerializer(discriminatorConvention, guidRepresentation);
            var writerSettings = new BsonBinaryWriterSettings();
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
        }

        [Fact]
        public void Serialize_guid_should_throw_when_guid_representation_is_unspecified()
        {
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
        }
    }

    public class DefaultAllowedTypesTests
    {
        [Fact]
        public void DefaultAllowedTypes_with_anonymous_type_should_return_true()
        {
            var anonymousInstance = new { X = 1 };
            var anonymousType = anonymousInstance.GetType();

            var result = ObjectSerializer.DefaultAllowedTypes(anonymousType);

            result.Should().BeTrue();
        }
    }
}
