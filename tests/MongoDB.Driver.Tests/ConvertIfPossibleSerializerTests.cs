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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ConvertIfPossibleSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);
            var y = new DerivedFromConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);
            var y = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("serializer")]
        [InlineData("serializerRegistry")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var serializer1 = new Int32Serializer(Bson.BsonType.Int32);
            var serializer2 = new Int32Serializer(Bson.BsonType.String);
            var serializerRegistry1 = new BsonSerializerRegistry();
            var serializerRegistry2 = new BsonSerializerRegistry();
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(serializer1, serializerRegistry1);
            var y = notEqualFieldName switch
            {
                "serializer" => new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(serializer2, serializerRegistry1),
                "serializerRegistry" => new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(serializer1, serializerRegistry2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new FieldValueSerializerHelper.ConvertIfPossibleSerializer<int, int>(Int32Serializer.Instance, BsonSerializer.SerializerRegistry);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromConvertIfPossibleSerializer<TFrom, TTo> : FieldValueSerializerHelper.ConvertIfPossibleSerializer<TFrom, TTo>
        {
            public DerivedFromConvertIfPossibleSerializer(IBsonSerializer<TTo> serializer, IBsonSerializerRegistry serializerRegistry) : base(serializer, serializerRegistry)
            {
            }
        }
    }
}
