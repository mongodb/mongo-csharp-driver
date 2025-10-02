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
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Serializers
{
    public class AsEnumUnderlyingTypeSerializerTests
    {
        private static readonly IBsonSerializer<E> __enumSerializer1 = new ESerializer1();
        private static readonly IBsonSerializer<E> __enumSerializer2 = new ESerializer2();

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);
            var y = new DerivedFromAsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);
            var y = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);
            var y = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new AsEnumUnderlyingTypeSerializer<E, int>(__enumSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromAsEnumUnderlyingTypeSerializer<TEnum, TEnumUnderlyingType> : AsEnumUnderlyingTypeSerializer<TEnum, TEnumUnderlyingType>
            where TEnum : Enum
            where TEnumUnderlyingType : struct
        {
            public DerivedFromAsEnumUnderlyingTypeSerializer(IBsonSerializer<TEnum> enumSerializer) : base(enumSerializer)
            {
            }
        }

        internal enum E { }

        internal class ESerializer1 : StructSerializerBase<E> { }

        internal class ESerializer2 : StructSerializerBase<E> { }
    }
}
