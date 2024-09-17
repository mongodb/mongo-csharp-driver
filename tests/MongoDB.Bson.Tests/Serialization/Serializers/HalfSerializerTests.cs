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
using System.Globalization;
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
        [InlineData(BsonType.Decimal128, "18.5", """{ "x" : { "$numberDecimal" : "18.5" } }""")]
        [InlineData(BsonType.Double, "18.5", """{ "x" : { "$numberDouble" : "18.5" } }""")]
        [InlineData(BsonType.Int64, "18.5", """{ "x" : { "$numberLong" : "18" } }""")]
        [InlineData(BsonType.Int32, "18.5", """{ "x" : { "$numberInt" : "18" } }""")]
        [InlineData(BsonType.String, "18.5", """{ "x" : "18.5" }""")]
        public void Serialize_should_have_expected_result(BsonType representation, string value,
            string expectedResult)
        {
            var subject = new HalfSerializer(representation, new RepresentationConverter(true, true));
            var halfValue = Half.Parse(value, CultureInfo.InvariantCulture);

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter,
                new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");
            subject.Serialize(context, halfValue);
            writer.WriteEndDocument();
            var result = textWriter.ToString();

            result.Should().Be(expectedResult);
        }

        /** To test:
         * - Serialization/Deserialization from MaxValue / MinValue
         * - Serialization/Deserialization for PositiveInfinity / NegativeInfinity
         */

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
    }
#endif
}
