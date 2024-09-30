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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
#if NET6_0_OR_GREATER
    public class TimeOnlySerializerTests
    {
        [Fact]
        public void Constructor_with_no_arguments_should_return_expected_result()
        {
            var subject = new TimeOnlySerializer();

            subject.Representation.Should().Be(BsonType.Int64);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_representation_should_return_expected_result(
            [Values(BsonType.String, BsonType.Int64)]
            BsonType representation)
        {
            var subject = new TimeOnlySerializer(representation);

            subject.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDouble" : "907255698" } }""","00:01:30.7255698" )]
        [InlineData("""{ "x" : { "$numberDecimal" : "907255698" } }""","00:01:30.7255698" )]
        [InlineData("""{ "x" : { "$numberInt" : "907255698" } }""","00:01:30.7255698" )]
        public void Deserialize_should_be_forgiving_of_actual_numeric_types(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(TimeOnly.ParseExact(expectedResult, "o"));
        }

        [Theory]
        [InlineData("""{ "x" : "08:32:05.5946583" }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : "00:00:00.0000000" }""","00:00:00.0000000")]
        [InlineData("""{ "x" : "23:59:59.9999999" }""","23:59:59.9999999" )]
        [InlineData("""{ "x" : { "$numberLong" : "307255946583" } }""","08:32:05.5946583" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","00:00:00.0000000" )]
        [InlineData("""{ "x" : { "$numberLong" : "863999999999" } }""","23:59:59.9999999" )]
        public void Deserialize_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new TimeOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(TimeOnly.ParseExact(expectedResult, "o"));
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
        public void Serialize_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new TimeOnlySerializer(representation);
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

        [Fact]
        public void Serializer_should_be_registered()
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(TimeOnly));

            serializer.Should().Be(new TimeOnlySerializer());
        }

        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result(
            [Values(BsonType.Int64, BsonType.String)] BsonType oldRepresentation,
            [Values(BsonType.Int64, BsonType.String)] BsonType newRepresentation)
        {
            var subject = new TimeOnlySerializer(oldRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            result.Representation.Should().Be(newRepresentation);
            if (newRepresentation == oldRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
        }
    }
#endif
}