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
    public class FilterDefinitionBuilderIntComparedToDoubleWithStringRepresentationTests
    {
        private static IBsonSerializerRegistry __registry = BsonSerializer.SerializerRegistry;
        private static IBsonSerializer<C> __serializer = BsonSerializer.SerializerRegistry.GetSerializer<C>();
        private static FilterDefinitionBuilder<C> __subject = Builders<C>.Filter;

        public class C
        {
            [BsonRepresentation(BsonType.String)]
            public int I { get; set; }
        }

        [Theory]
        [InlineData(1.5, "{ I : \"1.5\" }")]
        public void Eq_with_field_name_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Eq("I", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : \"1.5\" }")]
        public void Eq_with_lambda_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Eq(x => x.I, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $gt : \"1.5\" } }")]
        public void Gt_with_field_name_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Gt("I", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $gt : \"1.5\" } }")]
        public void Gt_with_lambda_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Gt(x => x.I, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $gte : \"1.5\" } }")]
        public void Gte_with_field_name_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Gte("I", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $gte : \"1.5\" } }")]
        public void Gte_with_lambda_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Gte(x => x.I, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $lt : \"1.5\" } }")]
        public void Lt_with_field_name_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Lt("I", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $lt : \"1.5\" } }")]
        public void Lt_with_lambda_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Lt(x => x.I, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $lte : \"1.5\" } }")]
        public void Lte_with_field_name_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Lte("I", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $lte : \"1.5\" } }")]
        public void Lte_with_lambda_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Lte(x => x.I, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $ne : \"1.5\" } }")]
        public void Ne_with_field_name_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Ne("I", value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $ne : \"1.5\" } }")]
        public void Ne_with_lambda_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Ne(x => x.I, value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : \"1.5\" }")]
        public void Where_operator_equals_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I == value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $gt : \"1.5\" } }")]
        public void Where_operator_greater_than_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I > value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $gte : \"1.5\" } }")]
        public void Where_operator_greater_than_or_equal_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I >= value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $lt : \"1.5\" } }")]
        public void Where_operator_less_than_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I < value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $lte : \"1.5\" } }")]
        public void Where_operator_less_than_or_equal_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I <= value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1.5, "{ I : { $ne : \"1.5\" } }")]
        public void Where_operator_not_equal_should_render_correctly(double value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I != value);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }
    }
}
