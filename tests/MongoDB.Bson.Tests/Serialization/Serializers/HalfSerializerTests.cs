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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
#if NET6_0_OR_GREATER
    public class HalfSerializerTests
    {
        [Fact]
        public void Constructor_with_no_arguments_should_return_expected_result()
        {
            var subject = new HalfSerializer();

            subject.Representation.Should().Be(BsonType.Double);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_representation_should_return_expected_result(
            [Values(BsonType.Decimal128, BsonType.Double, BsonType.Int64, BsonType.Int32, BsonType.String)]
            BsonType representation)
        {
            var subject = new HalfSerializer(representation);

            subject.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "18" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "18" } }""")]
        [InlineData("""{ "x" : { "$numberLong" : "18" } }""")]
        [InlineData("""{ "x" : { "$numberInt" : "18" } }""")]
        [InlineData("""{ "x" : "18" }""")]
        public void Deserialize_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(false, false));
            var expectedResult = (Half)18;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "18.5" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "18.5" } }""")]
        [InlineData("""{ "x" : "18.5" }""")]
        public void Deserialize_with_floating_point_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(false, false));
            var expectedResult = (Half)18.5;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "9.999999999999999999999999999999999E+6144" } }""")] //Decimal128.MaxValue
        [InlineData("""{ "x" : { "$numberDouble" : "1.7976931348623157E+308" } }""")] //double.MaxValue
        [InlineData("""{ "x" : { "$numberLong" : "65504" } }""")]
        [InlineData("""{ "x" : { "$numberInt" : "65504" } }""")]
        [InlineData("""{ "x" : "65504" }""")]
        public void Deserialize_of_max_value_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(false, false));
            var expectedResult = Half.MaxValue;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "-9.999999999999999999999999999999999E+6144" } }""")] //Decimal128.MinValue
        [InlineData("""{ "x" : { "$numberDouble" : "-1.7976931348623157E+308" } }""")] //double.MinValue
        [InlineData("""{ "x" : { "$numberLong" : "-65504" } }""")]
        [InlineData("""{ "x" : { "$numberInt" : "-65504" } }""")]
        [InlineData("""{ "x" : "-65504" }""")]
        public void Deserialize_of_min_value_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(true, true));
            var expectedResult = Half.MinValue;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "NaN" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "NaN" } }""")]
        [InlineData("""{ "x" : "NaN" }""")]
        public void Deserialize_of_nan_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(true, true));
            var expectedResult = Half.NaN;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "-Infinity" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "-Infinity" } }""")]
        [InlineData("""{ "x" : "-Infinity" }""")]
        public void Deserialize_of_negative_infinity_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(true, true));
            var expectedResult = Half.NegativeInfinity;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "Infinity" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "Infinity" } }""")]
        [InlineData("""{ "x" : "Infinity" }""")]
        public void Deserialize_of_positive_infinity_should_have_expected_result(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(true, true));
            var expectedResult = Half.PositiveInfinity;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "9.759" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "9.759" } }""")]
        public void Deserialize_without_truncation_allowed_and_enough_digits_should_throw(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(false, false));

            TestDeserializeWithException<TruncationException>(subject, json);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "9.759" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "9.759" } }""")]
        public void Deserialize_with_truncation_allowed_should_have_expected_results(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(true, true));
            var expectedResult = (Half)9.76;

            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDecimal" : "75000" } }""")]
        [InlineData("""{ "x" : { "$numberDouble" : "75000" } }""")]
        [InlineData("""{ "x" : { "$numberLong" : "75000" } }""")]
        [InlineData("""{ "x" : { "$numberInt" : "75000" } }""")]
        public void Deserialize_without_overflow_allowed_and_over_range_values_should_throw(string json)
        {
            var subject = new HalfSerializer(BsonType.Decimal128, new RepresentationConverter(false, true));

            TestDeserializeWithException<OverflowException>(subject, json);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new HalfSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new HalfSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new HalfSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new HalfSerializer();
            var y = new HalfSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }


        [Theory]
        [InlineData(BsonType.Decimal128)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.String)]
        public void Equals_with_not_equal_fields_should_return_true(BsonType representation)
        {
            var x = new HalfSerializer();
            var y = new HalfSerializer(representation);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Instance_should_return_default_serializer()
        {
            var subject = HalfSerializer.Instance;

            subject.Should().Be(new HalfSerializer());
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new HalfSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "18.5" } }""")]
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "18.5" } }""")]
        [InlineData(BsonType.Int64, """{ "x" : { "$numberLong" : "18" } }""")]
        [InlineData(BsonType.Int32, """{ "x" : { "$numberInt" : "18" } }""")]
        [InlineData(BsonType.String, """{ "x" : "18.5" }""")]
        public void Serialize_should_have_expected_result(BsonType representation, string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = (Half)18.5;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "0" } }""")]
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int64, """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Int32, """{ "x" : { "$numberInt" : "0" } }""")]
        [InlineData(BsonType.String, """{ "x" : "0" }""")]
        public void Serialize_of_zero_should_have_expected_result(BsonType representation, string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = (Half)0;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.Int32)]
        public void Serialize_without_truncation_allowed_and_floating_point_should_throw(BsonType representation)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, false));
            var halfValue = (Half)18.5;

            TestSerializeWithException<TruncationException>(subject, halfValue);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "9.999999999999999999999999999999999E+6144" } }""")] //Decimal128.MaxValue
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "1.7976931348623157E+308" } }""")] //double.MaxValue
        [InlineData(BsonType.Int64, """{ "x" : { "$numberLong" : "65504" } }""")]
        [InlineData(BsonType.Int32, """{ "x" : { "$numberInt" : "65504" } }""")]
        [InlineData(BsonType.String, """{ "x" : "65504" }""")]
        public void Serialize_of_max_value_should_have_expected_result(BsonType representation,
            string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = Half.MaxValue;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "-9.999999999999999999999999999999999E+6144" } }""")] //Decimal128.MinValue
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "-1.7976931348623157E+308" } }""")] //double.MinValue
        [InlineData(BsonType.Int64, """{ "x" : { "$numberLong" : "-65504" } }""")]
        [InlineData(BsonType.Int32, """{ "x" : { "$numberInt" : "-65504" } }""")]
        [InlineData(BsonType.String, """{ "x" : "-65504" }""")]
        public void Serialize_of_min_value_should_have_expected_result(BsonType representation,
            string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = Half.MinValue;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "NaN" } }""")]
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "NaN" } }""")]
        [InlineData(BsonType.String, """{ "x" : "NaN" }""")]
        public void Serialize_of_nan_should_have_expected_result(BsonType representation,
            string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = Half.NaN;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "-Infinity" } }""")]
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "-Infinity" } }""")]
        [InlineData(BsonType.String, """{ "x" : "-Infinity" }""")]
        public void Serialize_of_negative_infinity_should_have_expected_result(BsonType representation,
            string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = Half.NegativeInfinity;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Decimal128, """{ "x" : { "$numberDecimal" : "Infinity" } }""")]
        [InlineData(BsonType.Double, """{ "x" : { "$numberDouble" : "Infinity" } }""")]
        [InlineData(BsonType.String, """{ "x" : "Infinity" }""")]
        public void Serialize_of_positive_infinity_should_have_expected_result(BsonType representation,
            string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = Half.PositiveInfinity;

            TestSerialize(subject, halfValue, expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Serialize_of_positive_infinity_with_integer_representation_should_throw(
            [Values(BsonType.Int64, BsonType.Int32)] BsonType representation,
            [Values(true, false)] bool allowOverflow, [Values(true, false)] bool allowTruncation)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(allowOverflow, allowTruncation));
            var halfValue = Half.PositiveInfinity;

            TestSerializeWithException<OverflowException>(subject, halfValue);
        }

        [Theory]
        [ParameterAttributeData]
        public void Serialize_of_negative_infinity_with_integer_representation_should_throw(
            [Values(BsonType.Int64, BsonType.Int32)] BsonType representation,
            [Values(true, false)] bool allowOverflow, [Values(true, false)] bool allowTruncation)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(allowOverflow, allowTruncation));
            var halfValue = Half.NegativeInfinity;

            TestSerializeWithException<OverflowException>(subject, halfValue);
        }

        [Theory]
        [ParameterAttributeData]
        public void Serialize_of_nan_with_integer_representation_should_throw(
            [Values(BsonType.Int64, BsonType.Int32)] BsonType representation,
            [Values(true, false)] bool allowOverflow, [Values(true, false)] bool allowTruncation)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(allowOverflow, allowTruncation));
            var halfValue = Half.NaN;

            TestSerializeWithException<OverflowException>(subject, halfValue);
        }

        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result(
            [Values(BsonType.Decimal128, BsonType.Double, BsonType.Int64, BsonType.Int32, BsonType.String)] BsonType oldRepresentation,
            [Values(BsonType.Decimal128, BsonType.Double, BsonType.Int64, BsonType.Int32, BsonType.String)] BsonType newRepresentation)
        {
            var subject = new HalfSerializer(oldRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            result.Representation.Should().Be(newRepresentation);
            if (newRepresentation == oldRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
        }

        private static void TestDeserialize(HalfSerializer subject, string json, Half expectedResult)
        {
            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(expectedResult);
        }

        private static void TestDeserializeWithException<T>(HalfSerializer subject, string json) where T : Exception
        {
            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<T>();
        }

        private static void TestSerialize(HalfSerializer subject, Half value, string expectedResult)
        {
            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter,
                new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");
            subject.Serialize(context, value);
            writer.WriteEndDocument();
            var result = textWriter.ToString();

            result.Should().Be(expectedResult);
        }

        private static void TestSerializeWithException<T>(HalfSerializer subject, Half value) where T : Exception
        {
            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter,
                new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");
            var action = () => subject.Serialize(context, value);

            action.ShouldThrow<T>();
        }
    }
#endif
}
