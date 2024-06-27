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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateFacetResultsSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });
            var y = new DerivedFromAggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });
            var y = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("names")]
        [InlineData("serializers")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var serializer1 = new Int32Serializer(Bson.BsonType.Int32);
            var serializer2 = new Int32Serializer(Bson.BsonType.String);
            var x = new AggregateFacetResultsSerializer(new[] { "name1" }, new[] { serializer1 });
            var y = notEqualFieldName switch
            {
                "names" => new AggregateFacetResultsSerializer(new[] { "name2" }, new[] { serializer1 }),
                "serializers" => new AggregateFacetResultsSerializer(new[] { "name1" }, new[] { serializer2 }),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new AggregateFacetResultsSerializer(new[] { "name" }, new[] { Int32Serializer.Instance });

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromAggregateFacetResultsSerializer : AggregateFacetResultsSerializer
        {
            public DerivedFromAggregateFacetResultsSerializer(IEnumerable<string> names, IEnumerable<IBsonSerializer> serializers)
                : base(names, serializers)
            {
            }
        }
    }
}
