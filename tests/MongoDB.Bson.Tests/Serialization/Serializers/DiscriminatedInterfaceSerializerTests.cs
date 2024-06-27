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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class DiscriminatedInterfaceSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new DiscriminatedInterfaceSerializer<I>();
            var y = new DerivedFromDiscriminatedInterfaceSerializer<I>();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DiscriminatedInterfaceSerializer<I>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DiscriminatedInterfaceSerializer<I>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DiscriminatedInterfaceSerializer<I>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DiscriminatedInterfaceSerializer<I>();
            var y = new DiscriminatedInterfaceSerializer<I>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("discriminatorConvention")]
        [InlineData("interfaceSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var discriminatorConvention1 = new ScalarDiscriminatorConvention("_t");
            var discriminatorConvention2 = new ScalarDiscriminatorConvention("_u");
            var interfaceSerializer1 = new InterfaceSerializer1<I>();
            var interfaceSerializer2 = new InterfaceSerializer2<I>();

            var x = new DiscriminatedInterfaceSerializer<I>();
            var y = notEqualFieldName switch
            {
                "discriminatorConvention" => new DiscriminatedInterfaceSerializer<I>(discriminatorConvention2, interfaceSerializer1),
                "interfaceSerializer" => new DiscriminatedInterfaceSerializer<I>(discriminatorConvention1, interfaceSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DiscriminatedInterfaceSerializer<I>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class DerivedFromDiscriminatedInterfaceSerializer<TInterface> : DiscriminatedInterfaceSerializer<TInterface>
        {
            public DerivedFromDiscriminatedInterfaceSerializer() { }
        }

        public interface I { }

        public class InterfaceSerializer1<TInterface> : SerializerBase<TInterface> { }

        public class InterfaceSerializer2<TInterface> : SerializerBase<TInterface> { }
    }
}
