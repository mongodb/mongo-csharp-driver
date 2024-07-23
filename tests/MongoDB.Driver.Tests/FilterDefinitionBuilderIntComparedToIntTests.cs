﻿/* Copyright 2017-present MongoDB Inc.
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

namespace MongoDB.Driver.Tests
{
    public class FilterDefinitionBuilderIntComparedToIntTests
    {
        private static RenderArgs<C> __args = new(BsonSerializer.SerializerRegistry.GetSerializer<C>(), BsonSerializer.SerializerRegistry);
        private static FilterDefinitionBuilder<C> __subject = Builders<C>.Filter;

        public class C
        {
            public int I { get; set; }
        }

        [Theory]
        [InlineData(1, "{ I : 1 }")]
        public void Eq_with_field_name_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Eq("I", value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : 1 }")]
        public void Eq_with_lambda_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Eq(x => x.I, value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $gt : 1 } }")]
        public void Gt_with_field_name_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Gt("I", value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $gt : 1 } }")]
        public void Gt_with_lambda_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Gt(x => x.I, value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $gte : 1 } }")]
        public void Gte_with_field_name_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Gte("I", value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $gte : 1 } }")]
        public void Gte_with_lambda_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Gte(x => x.I, value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $lt : 1 } }")]
        public void Lt_with_field_name_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Lt("I", value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $lt : 1 } }")]
        public void Lt_with_lambda_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Lt(x => x.I, value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $lte : 1 } }")]
        public void Lte_with_field_name_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Lte("I", value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $lte : 1 } }")]
        public void Lte_with_lambda_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Lte(x => x.I, value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $ne : 1 } }")]
        public void Ne_with_field_name_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Ne("I", value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $ne : 1 } }")]
        public void Ne_with_lambda_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Ne(x => x.I, value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : 1 }")]
        public void Where_operator_equals_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I == value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $gt : 1 } }")]
        public void Where_operator_greater_than_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I > value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $gte : 1 } }")]
        public void Where_operator_greater_than_or_equal_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I >= value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $lt : 1 } }")]
        public void Where_operator_less_than_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I < value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $lte : 1 } }")]
        public void Where_operator_less_than_or_equal_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I <= value);

            filter.Render(__args).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ I : { $ne : 1 } }")]
        public void Where_operator_not_equal_should_render_correctly(int value, string expectedFilter)
        {
            var filter = __subject.Where(x => x.I != value);

            filter.Render(__args).Should().Be(expectedFilter);
        }
    }
}
