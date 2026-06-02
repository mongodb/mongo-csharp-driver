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
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public class EqualsMethodToFilterTranslatorTests
    {
        private static readonly RenderArgs<TestClass> __args = new(
            BsonSerializer.SerializerRegistry.GetSerializer<TestClass>(),
            BsonSerializer.SerializerRegistry);

        [Fact]
        public void Equals_with_uint64_and_nullable_int_should_translate()
        {
            ulong value = 2;

            var filter = Builders<TestClass>.Filter.Where(e => e.NullableIntegerProperty.Equals(value));

            filter.Render(__args).Should().Be("{ NullableIntegerProperty : 2 }");
        }

        [Fact]
        public void Equals_with_int_and_nullable_int_should_translate()
        {
            int value = 2;

            var filter = Builders<TestClass>.Filter.Where(e => e.NullableIntegerProperty.Equals(value));

            filter.Render(__args).Should().Be("{ NullableIntegerProperty : 2 }");
        }

        [Fact]
        public void Equals_with_null_should_translate()
        {
            var filter = Builders<TestClass>.Filter.Where(e => e.NullableIntegerProperty.Equals(null));

            filter.Render(__args).Should().Be("{ NullableIntegerProperty : null }");
        }

        [Fact]
        public void Equals_with_uint64_and_int_should_translate()
        {
            ulong value = 1;

            var filter = Builders<TestClass>.Filter.Where(e => e.IntegerProperty.Equals(value));

            filter.Render(__args).Should().Be("{ IntegerProperty : 1 }");
        }

        [Fact]
        public void Equals_with_constant_as_receiver_should_translate()
        {
            const int value = 1;

            var filter = Builders<TestClass>.Filter.Where(e => value.Equals(e.IntegerProperty));

            filter.Render(__args).Should().Be("{ IntegerProperty : 1 }");
        }

        [Fact]
        public void Equals_with_string_and_nullable_int_should_translate()
        {
            var value = "2";

            var filter = Builders<TestClass>.Filter.Where(e => e.NullableIntegerProperty.Equals(value));

            filter.Render(__args).Should().Be("{ NullableIntegerProperty : 2 }");
        }

        [Fact]
        public void Equals_with_overflowing_uint64_and_nullable_int_should_translate()
        {
            ulong value = (ulong)int.MaxValue + 1;

            var filter = Builders<TestClass>.Filter.Where(e => e.NullableIntegerProperty.Equals(value));

            filter.Render(__args).Should().Be("{ NullableIntegerProperty : 2147483648 }");
        }

        public class TestClass
        {
            public int IntegerProperty { get; set; }
            public int? NullableIntegerProperty { get; set; }
        }
    }
}
