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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Serializers
{
    public class ConvertEnumToIntegralTypeToSerializerTests
    {
        private static readonly IBsonSerializer<int> __intSerializer1 = new Int32Serializer(BsonType.Int32);
        private static readonly IBsonSerializer<int> __intSerializer2 = new Int32Serializer(BsonType.String);

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);
            var y = new DerivedFromConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);
            var y = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);
            var y = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ConvertEnumToIntegralTypeSerializer<E, int, int>(__intSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromConvertEnumToIntegralTypeSerializer<TEnum, TEnumUnderlyingType, TIntegralType> : ConvertEnumToIntegralTypeSerializer<TEnum, TEnumUnderlyingType, TIntegralType>
            where TEnum : struct, Enum
            where TEnumUnderlyingType : struct
            where TIntegralType : struct
        {
            public DerivedFromConvertEnumToIntegralTypeSerializer(IBsonSerializer<TIntegralType> intSerializer) : base(intSerializer)
            {
            }
        }

        internal enum E { }
    }
}
