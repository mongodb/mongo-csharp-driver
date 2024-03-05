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
using MongoDB.Driver.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Operations
{
    public class ElementDeserializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new ElementDeserializer<int>("name", Int32Serializer.Instance);
            var y = new DerivedFromElementDeserializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ElementDeserializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ElementDeserializer<int>("name", Int32Serializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ElementDeserializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ElementDeserializer<int>("name", Int32Serializer.Instance);
            var y = new ElementDeserializer<int>("name", Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("deserializeNull")]
        [InlineData("elementName")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var valueSerializer1 = new Int32Serializer(BsonType.Int32);
            var valueSerializer2 = new Int32Serializer(BsonType.String);
            var deserializeNull1 = true;
            var deserializeNull2 = false;
            var x = new ElementDeserializer<int>("name1", valueSerializer1, deserializeNull1);
            var y = notEqualFieldName switch
            {
                "elementName" => new ElementDeserializer<int>("name2", valueSerializer1, deserializeNull1),
                "valueSerializer" => new ElementDeserializer<int>("name1", valueSerializer2, deserializeNull1),
                "deserializeNull" => new ElementDeserializer<int>("name1", valueSerializer1, deserializeNull2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ElementDeserializer<int>("name", Int32Serializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromElementDeserializer<TValue> : ElementDeserializer<TValue>
        {
            public DerivedFromElementDeserializer(string elementName, IBsonSerializer<TValue> valueSerializer) : base(elementName, valueSerializer)
            {
            }
        }
    }
}
