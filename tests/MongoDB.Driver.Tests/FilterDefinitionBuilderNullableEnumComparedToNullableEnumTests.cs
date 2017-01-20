/* Copyright 2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FilterDefinitionBuilderNullableEnumComparedToNullableEnumTests
    {
        private static IBsonSerializerRegistry __registry = BsonSerializer.SerializerRegistry;
        private static IBsonSerializer<C> __serializer = BsonSerializer.SerializerRegistry.GetSerializer<C>();
        private static FilterDefinitionBuilder<C> __subject = Builders<C>.Filter;

        public enum E { A, B }

        public class C
        {
            public E? E { get; set; }
        }

        [Theory]
        [InlineData(E.A, "{ E : 0 }")]
        [InlineData(null, "{ E : null }")]
        public void Eq_with_field_name_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Eq("E", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : 0 }")]
        [InlineData(null, "{ E : null }")]
        public void Eq_with_lambda_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Eq(x => x.E, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $gt : 0 } }")]
        [InlineData(null, "{ E : { $gt : null } }")]
        public void Gt_with_field_name_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Gt("E", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $gt : 0 } }")]
        [InlineData(null, "{ E : { $gt : null } }")]
        public void Gt_with_lambda_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Gt(x => x.E, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $gte : 0 } }")]
        [InlineData(null, "{ E : { $gte : null } }")]
        public void Gte_with_field_name_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Gte("E", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $gte : 0 } }")]
        [InlineData(null, "{ E : { $gte : null } }")]
        public void Gte_with_lambda_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Gte(x => x.E, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $lt : 0 } }")]
        [InlineData(null, "{ E : { $lt : null } }")]
        public void Lt_with_field_name_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Lt("E", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $lt : 0 } }")]
        [InlineData(null, "{ E : { $lt : null } }")]
        public void Lt_with_lambda_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Lt(x => x.E, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $lte : 0 } }")]
        [InlineData(null, "{ E : { $lte : null } }")]
        public void Lte_with_field_name_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Lte("E", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $lte : 0 } }")]
        [InlineData(null, "{ E : { $lte : null } }")]
        public void Lte_with_lambda_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Lte(x => x.E, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $ne : 0 } }")]
        [InlineData(null, "{ E : { $ne : null } }")]
        public void Ne_with_field_name_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Ne("E", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $ne : 0 } }")]
        [InlineData(null, "{ E : { $ne : null } }")]
        public void Ne_with_lambda_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Ne(x => x.E, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : 0 }")]
        [InlineData(null, "{ E : null }")]
        public void Where_operator_equals_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.E == value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $gt : 0 } }")]
        [InlineData(null, "{ E : { $gt : null } }")]
        public void Where_operator_greater_than_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.E > value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $gte : 0 } }")]
        [InlineData(null, "{ E : { $gte : null } }")]
        public void Where_operator_greater_than_or_equal_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.E >= value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $lt : 0 } }")]
        [InlineData(null, "{ E : { $lt : null } }")]
        public void Where_operator_less_than_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.E < value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $lte : 0 } }")]
        [InlineData(null, "{ E : { $lte : null } }")]
        public void Where_operator_less_than_or_equal_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.E <= value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(E.A, "{ E : { $ne : 0 } }")]
        [InlineData(null, "{ E : { $ne : null } }")]
        public void Where_operator_not_equal_should_render_correctly(E? value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.E != value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }
    }
}
