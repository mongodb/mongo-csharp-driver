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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FilterDefinitionBuilderIntArrayComparedToEnumerableIntTests
    {
        private static IBsonSerializerRegistry __registry = BsonSerializer.SerializerRegistry;
        private static IBsonSerializer<C> __serializer = BsonSerializer.SerializerRegistry.GetSerializer<C>();
        private static FilterDefinitionBuilder<C> __subject = Builders<C>.Filter;

        public class C
        {
            public int[] A { get; set; }
        }

        [Theory]
        [InlineData(new int[] { 1, 2 }, "{ A : [1, 2] }")]
        public void Eq_with_field_name_should_render_correctly(IEnumerable<int> values, string expectedFilter)
        {
            var filter = __subject.Eq("A", values);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 }, "{ A : [1, 2] }")]
        public void Eq_with_lambda_should_render_correctly(IEnumerable<int> values, string expectedFilter)
        {
            var filter = __subject.Eq(x => x.A, values);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 }, "{ A : { $ne : [1, 2] } }")]
        public void Ne_with_field_name_should_render_correctly(IEnumerable<int> values, string expectedFilter)
        {
            var filter = __subject.Ne("A", values);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 }, "{ A : { $ne : [1, 2] } }")]
        public void Ne_with_lambda_should_render_correctly(IEnumerable<int> values, string expectedFilter)
        {
            var filter = __subject.Ne(x => x.A, values);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 }, "{ A : [1, 2] }")]
        public void Where_operator_equals_should_render_correctly(IEnumerable<int> values, string expectedFilter)
        {
            var filter = __subject.Where(x => x.A == values);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 }, "{ A : { $ne : [1, 2] } }")]
        public void Where_operator_not_equals_should_render_correctly(IEnumerable<int> values, string expectedFilter)
        {
            var filter = __subject.Where(x => x.A != values);

            filter.Render(__serializer, __registry).Should().Be(expectedFilter);
        }
    }
}
