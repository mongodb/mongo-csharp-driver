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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Serializers
{
    public class WrappedValueSerializerTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var valueSerializer = Mock.Of<IBsonSerializer<int>>();

            var subject = new WrappedValueSerializer<int>("_v", valueSerializer);

            subject.FieldName.Should().Be("_v");
            subject.ValueSerializer.Should().BeSameAs(valueSerializer);
            subject.ValueType.Should().BeSameAs(typeof(int));
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);
            var y = new DerivedFromWrappedValueSerializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);
            var y = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("fieldName")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new WrappedValueSerializer<int>("name1", itemSerializer1);
            var y = notEqualFieldName switch
            {
                "fieldName" => new WrappedValueSerializer<int>("name2", itemSerializer1),
                "valueSerializer" => new WrappedValueSerializer<int>("name1", itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new WrappedValueSerializer<int>("name", Int32Serializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromWrappedValueSerializer<TValue> : WrappedValueSerializer<TValue>
        {
            public DerivedFromWrappedValueSerializer(string fieldName, IBsonSerializer<TValue> valueSerializer) : base(fieldName, valueSerializer)
            {
            }
        }
    }
}
