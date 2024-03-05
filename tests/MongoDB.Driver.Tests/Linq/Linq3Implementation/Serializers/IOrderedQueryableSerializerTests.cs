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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Serializers
{
    public class IOrderedQueryableSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);
            var y = new DerivedFromIOrderedQueryableSerializer<int>(Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);
            var y = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new IOrderedQueryableSerializer<int>(itemSerializer1);
            var y = new IOrderedQueryableSerializer<int>(itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new IOrderedQueryableSerializer<int>(Int32Serializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromIOrderedQueryableSerializer<TItem> : IOrderedQueryableSerializer<TItem>
        {
            public DerivedFromIOrderedQueryableSerializer(IBsonSerializer<TItem> itemSerializer) : base(itemSerializer)
            {
            }
        }
    }
}
