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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
#if NET6_0_OR_GREATER
    public class TimeOnlySerializerTests
    {
        [Fact]
        public void Attribute_should_set_correct_units()
        {
            var timeOnly = new TimeOnly(13, 24, 53);

            var testObj = new TestClass
            {
                Hours = timeOnly,
                Minutes = timeOnly,
                Seconds = timeOnly,
                Milliseconds = timeOnly,
                Microseconds = timeOnly,
                Ticks = timeOnly,
                Nanoseconds = timeOnly,
            };

            var json = testObj.ToJson();

            var expected = "{ \"Hours\" : 13, "
                           + "\"Minutes\" : 804, "
                           + "\"Seconds\" : 48293, "
                           + "\"Milliseconds\" : 48293000, "
                           + "\"Microseconds\" : 48293000000, "
                           + "\"Ticks\" : 482930000000, "
                           + "\"Nanoseconds\" : 48293000000000 }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void Constructor_with_no_arguments_should_return_expected_result()
        {
            var subject = new TimeOnlySerializer();

            subject.Representation.Should().Be(BsonType.Int64);
            subject.Units.Should().Be(TimeOnlyUnits.Ticks);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_representation_should_return_expected_result(
            [Values(BsonType.String, BsonType.Int64, BsonType.Int32, BsonType.Double)]
            BsonType representation,
            [Values(TimeOnlyUnits.Ticks, TimeOnlyUnits.Hours, TimeOnlyUnits.Minutes, TimeOnlyUnits.Seconds,
                TimeOnlyUnits.Milliseconds, TimeOnlyUnits.Microseconds, TimeOnlyUnits.Ticks, TimeOnlyUnits.Nanoseconds)]
            TimeOnlyUnits units)
        {
            var subject = new TimeOnlySerializer(representation, units);

            subject.Representation.Should().Be(representation);
            subject.Units.Should().Be(units);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : "23:59:59.9999999" }""","23:59:59.9999999" )]
        [InlineData("""{ "x" : { "$numberLong" : "307255946583" } }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "863999999999" } }""","23:59:59.9999999" )]
        [InlineData("""{ "x" : { "$numberDouble" : "307255946583" } }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "863999999999" } }""","23:59:59.9999999" )]
        [InlineData("""{ "x" : { "$numberInt" : "27624525" } }""","00:00:02.7624525" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "2147483647" } }""","00:03:34.7483647" )] //int.MaxValue
        public void Deserialize_with_ticks_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer();
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : { "$numberLong" : "14" } }""","14:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "14" } }""","14:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "15.54" } }""","15:32:24.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "14" } }""","14:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        public void Deserialize_with_hours_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer(BsonType.Int64, TimeOnlyUnits.Hours);
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : { "$numberLong" : "145" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "145" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "145.5" } }""","02:25:30.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "145" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        public void Deserialize_with_minutes_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer(BsonType.Int64, TimeOnlyUnits.Minutes);
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : { "$numberLong" : "8700" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700.25" } }""","02:25:00.2500000" )]
        [InlineData("""{ "x" : { "$numberInt" : "8700" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        public void Deserialize_with_seconds_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer(BsonType.Int64, TimeOnlyUnits.Seconds);
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : { "$numberLong" : "8700000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700250.43" } }""","02:25:00.2504300" )]
        [InlineData("""{ "x" : { "$numberInt" : "8700000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        public void Deserialize_with_milliseconds_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer(BsonType.Int64, TimeOnlyUnits.Milliseconds);
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : { "$numberLong" : "8700000000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700000000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700250430.5" } }""","02:25:00.2504305" )]
        [InlineData("""{ "x" : { "$numberInt" : "8700000" } }""","00:00:08.7000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        public void Deserialize_with_microseconds_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer(BsonType.Int64, TimeOnlyUnits.Microseconds);
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : { "$numberLong" : "8700000000000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700000000000" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberDouble" : "8700000000000.5" } }""","02:25:00.0000000" )]
        [InlineData("""{ "x" : { "$numberInt" : "870000000" } }""","00:00:00.8700000" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","00:00:00.0000000" )]
        public void Deserialize_with_nanoseconds_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer(BsonType.Int64, TimeOnlyUnits.Nanoseconds);
            TestDeserialize(subject, json, expectedResult);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberLong" : "-23" } }""" )]
        [InlineData("""{ "x" : { "$numberLong" : "963999999999" } }""")]
        public void Deserialize_should_throw_when_time_as_int64_is_out_of_range(string json)
        {
            var subject = new TimeOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<ArgumentOutOfRangeException>();
            exception.Message.Should().Be("Ticks must be between 0 and and TimeOnly.MaxValue.Ticks. (Parameter 'ticks')");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TimeOnlySerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TimeOnlySerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TimeOnlySerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TimeOnlySerializer();
            var y = new TimeOnlySerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Instance_should_return_default_serializer()
        {
            var subject = TimeOnlySerializer.Instance;

            subject.Should().Be(new TimeOnlySerializer());
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TimeOnlySerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.String, "23:59:59.9999999", """{ "x" : "23:59:59.9999999" }""")]
        [InlineData(BsonType.Int64, "08:32:05.5946583", """{ "x" : { "$numberLong" : "307255946583" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Int64, "23:59:59.9999999", """{ "x" : { "$numberLong" : "863999999999" } }""")]
        [InlineData(BsonType.Double, "08:32:05.5946583", """{ "x" : { "$numberDouble" : "307255946583.0" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Double, "23:59:59.9999999", """{ "x" : { "$numberDouble" : "863999999999.0" } }""")]
        [InlineData(BsonType.Int32, "00:00:02.7624525", """{ "x" : { "$numberInt" : "27624525" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        [InlineData(BsonType.Int32, "00:03:34.7483647", """{ "x" : { "$numberInt" : "2147483647" } }""")] //int.MaxValue
        public void Serialize_with_ticks_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Ticks);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.Int64, "14:32:24.0000000", """{ "x" : { "$numberLong" : "14" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Double, "14:32:24.0000000", """{ "x" : { "$numberDouble" : "14.539999999999999" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int32, "14:32:24.0000000", """{ "x" : { "$numberInt" : "14" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        public void Serialize_with_hours_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Hours);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.Int64, "02:25:30.0000000", """{ "x" : { "$numberLong" : "145" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Double, "02:25:30.0000000", """{ "x" : { "$numberDouble" : "145.5" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int32, "02:25:30.0000000", """{ "x" : { "$numberInt" : "145" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        public void Serialize_with_minutes_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Minutes);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.Int64, "02:25:00.2500000", """{ "x" : { "$numberLong" : "8700" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Double, "02:25:00.2500000", """{ "x" : { "$numberDouble" : "8700.25" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int32, "02:25:00.2500000", """{ "x" : { "$numberInt" : "8700" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        public void Serialize_with_seconds_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Seconds);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.Int64, "02:25:00.2504305", """{ "x" : { "$numberLong" : "8700250430500" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Double, "02:25:00.2504305", """{ "x" : { "$numberDouble" : "8700250430500.0" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.8700000", """{ "x" : { "$numberInt" : "870000000" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        public void Serialize_with_nanoseconds_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Nanoseconds);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.Int64, "02:25:00.2504300", """{ "x" : { "$numberLong" : "8700250" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Double, "02:25:00.2504300", """{ "x" : { "$numberDouble" : "8700250.4299999997" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int32, "02:25:00.2504300", """{ "x" : { "$numberInt" : "8700250" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        public void Serialize_with_milliseconds_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Milliseconds);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Theory]
        [InlineData(BsonType.String, "08:32:05.5946583", """{ "x" : "08:32:05.5946583" }""")]
        [InlineData(BsonType.String, "00:00:00.0000000", """{ "x" : "00:00:00.0000000" }""")]
        [InlineData(BsonType.Int64, "02:25:00.2504305", """{ "x" : { "$numberLong" : "8700250430" } }""")]
        [InlineData(BsonType.Int64, "00:00:00.0000000", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Double, "02:25:00.2504305", """{ "x" : { "$numberDouble" : "8700250430.5" } }""")]
        [InlineData(BsonType.Double, "00:00:00.0000000", """{ "x" : { "$numberDouble" : "0.0" } }""")]
        [InlineData(BsonType.Int32, "00:00:08.7000000", """{ "x" : { "$numberInt" : "8700000" } }""")]
        [InlineData(BsonType.Int32, "00:00:00.0000000", """{ "x" : { "$numberInt" : "0" } }""")]
        public void Serialize_with_microseconds_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation, TimeOnlyUnits.Microseconds);

            TestSerialize(subject, valueString, expectedResult);
        }

        [Fact]
        public void Serializer_should_be_registered()
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(TimeOnly));

            serializer.Should().Be(new TimeOnlySerializer());
        }

        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result(
            [Values(BsonType.String, BsonType.Int64, BsonType.Int32, BsonType.Double)] BsonType oldRepresentation,
            [Values(BsonType.String, BsonType.Int64, BsonType.Int32, BsonType.Double)] BsonType newRepresentation)
        {
            var subject = new TimeOnlySerializer(oldRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            result.Representation.Should().Be(newRepresentation);
            if (newRepresentation == oldRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
        }

        private static void TestDeserialize(TimeOnlySerializer subject, string json, string expectedResult)
        {
            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(TimeOnly.ParseExact(expectedResult, "o"));
        }

        private static void TestSerialize(TimeOnlySerializer subject, string valueString, string expectedResult)
        {
            var value = TimeOnly.ParseExact(valueString, "o");

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

        private class TestClass
        {
            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Hours )]
            public TimeOnly Hours { get; set; }

            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Minutes )]
            public TimeOnly Minutes { get; set; }

            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Seconds )]
            public TimeOnly Seconds { get; set; }

            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Milliseconds )]
            public TimeOnly Milliseconds { get; set; }

            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Microseconds )]
            public TimeOnly Microseconds { get; set; }

            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Ticks )]
            public TimeOnly Ticks { get; set; }

            [BsonTimeOnlyOptions(BsonType.Int64, TimeOnlyUnits.Nanoseconds )]
            public TimeOnly Nanoseconds { get; set; }
        }
    }
#endif
}