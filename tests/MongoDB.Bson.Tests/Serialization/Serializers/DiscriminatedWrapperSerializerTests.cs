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
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class DiscriminatedWrapperSerializerTests
    {
        private static readonly IDiscriminatorConvention __discriminatorConvention1 = new ScalarDiscriminatorConvention("_t");
        private static readonly IDiscriminatorConvention __discriminatorConvention2 = new ScalarDiscriminatorConvention("_u");
        private static readonly IBsonSerializer<C> __wrappedSerializer1 = new CSerializer1();
        private static readonly IBsonSerializer<C> __wrappedSerializer2 = new CSerializer2();

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);
            var y = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("discriminatorConvention")]
        [InlineData("wrappedSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var x = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);
            var y = notEqualFieldName switch
            {
                "discriminatorConvention" => new DiscriminatedWrapperSerializer<C>(__discriminatorConvention2, __wrappedSerializer1),
                "wrappedSerializer" => new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DiscriminatedWrapperSerializer<C>(__discriminatorConvention1, __wrappedSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class C { }

        public class CSerializer1 : SerializerBase<C> { }

        public class CSerializer2 : SerializerBase<C> { }
    }
}
