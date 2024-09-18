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
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class ProjectingDeserializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ProjectingDeserializer<int, int>(Int32Serializer.Instance, f => 1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ProjectingDeserializer<int, int>(Int32Serializer.Instance, f => 1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ProjectingDeserializer<int, int>(Int32Serializer.Instance, f => 1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            Func<int, int> projector = f => 1;
            var x = new ProjectingDeserializer<int, int>(Int32Serializer.Instance, projector);
            var y = new ProjectingDeserializer<int, int>(Int32Serializer.Instance, projector);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("fromSerializer")]
        [InlineData("projector")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var fromSerializer1 = new Int32Serializer(BsonType.Int32);
            var fromSerializer2 = new Int32Serializer(BsonType.String);
            Func<int, int> projector1 = f => 1;
            Func<int, int> projector2 = f => 2;
            var x = new ProjectingDeserializer<int, int>(fromSerializer1, projector1);
            var y = notEqualFieldName switch
            {
                "fromSerializer" => new ProjectingDeserializer<int, int>(fromSerializer2, projector1),
                "projector" => new ProjectingDeserializer<int, int>(fromSerializer1, projector2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ProjectingDeserializer<int, int>(Int32Serializer.Instance, f => 1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
