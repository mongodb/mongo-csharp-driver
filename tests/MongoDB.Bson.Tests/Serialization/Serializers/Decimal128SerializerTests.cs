﻿/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class Decimal128SerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new Decimal128Serializer();
            var y = new DerivedFromDecimal128Serializer();

            var result = x.Equals(y);

            result.Should().Be(false);
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

        public class DerivedFromDecimal128Serializer : Decimal128Serializer
        {
        }
    }
}
