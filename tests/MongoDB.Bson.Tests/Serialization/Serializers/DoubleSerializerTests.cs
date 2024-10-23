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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class DoubleSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DoubleSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DoubleSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DoubleSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DoubleSerializer();
            var y = new DoubleSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new DoubleSerializer(BsonType.Double);
            var y = new DoubleSerializer(BsonType.String);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DoubleSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Serialize_NaN_or_Infinity_to_integral_should_throw([Values(BsonType.Int64, BsonType.Int32)] BsonType representation,
            [Values(double.PositiveInfinity, double.NegativeInfinity, double.NaN)] double value)
        {
            var subject = new DoubleSerializer(representation);

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter);

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");

            var exception = Record.Exception(() => subject.Serialize(context, value));
            exception.Should().BeOfType<OverflowException>();
        }
    }
}
