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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class Decimal128SerializerTests
    {
        [Fact]
        public void Constructor_with_no_arguments_should_return_expected_result()
        {
            var subject = new Decimal128Serializer();

            subject.Representation.Should().Be(BsonType.Decimal128);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_representation_should_return_expected_result(
            [Values(BsonType.Decimal128, BsonType.Int32, BsonType.Int64, BsonType.String, BsonType.Double)] BsonType representation)
        {
            var subject = new Decimal128Serializer(representation);

            subject.Representation.Should().Be(representation);
        }

        [Fact]
        public void Constructor_with_representation_should_throw_when_representation_is_invalid()
        {
            var exception = Record.Exception(() => new Decimal128Serializer(BsonType.Null));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new Decimal128Serializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new Decimal128Serializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new Decimal128Serializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new Decimal128Serializer();
            var y = new Decimal128Serializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("converter")]
        [InlineData("representation")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var converter1 = new RepresentationConverter(false, false);
            var converter2 = new RepresentationConverter(true, true);
            var representation1 = BsonType.String;
            var representation2 = BsonType.Decimal128;
            var x = new Decimal128Serializer(representation1, converter1);
            var y = notEqualFieldName switch
            {
                "representation" => new Decimal128Serializer(representation2, converter1),
                "converter" => new Decimal128Serializer(representation1, converter2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new Decimal128Serializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public static IEnumerable<object[]> SerializeSpecialValuesData()
        {
            return from bsonType in new[] { BsonType.Int64, BsonType.Int32 }
                from val in new [] { Decimal128.PositiveInfinity, Decimal128.NegativeInfinity, Decimal128.QNaN }
                select new object[] { bsonType, val };
        }

        [Theory]
        [MemberData(nameof(SerializeSpecialValuesData))]
        public void Serialize_NaN_or_Infinity_to_integral_should_throw(BsonType representation, Decimal128 value)
        {
            var subject = new Decimal128Serializer(representation);

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter);

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");

            var exception = Record.Exception(() => subject.Serialize(context, value));
            exception.Should().BeOfType<OverflowException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result(
            [Values(BsonType.Decimal128, BsonType.Int32, BsonType.Int64, BsonType.String, BsonType.Double)] BsonType oldRepresentation,
            [Values(BsonType.Decimal128, BsonType.Int32, BsonType.Int64, BsonType.String, BsonType.Double)] BsonType newRepresentation)
        {
            var subject = new Decimal128Serializer(oldRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            result.Representation.Should().Be(newRepresentation);
            if (newRepresentation == oldRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
        }
    }
}
