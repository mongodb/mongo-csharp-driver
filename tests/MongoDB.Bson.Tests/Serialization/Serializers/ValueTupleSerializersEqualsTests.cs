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
using Xunit;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class ValueTupleWith1ItemSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int>();
            var y = new ValueTupleSerializer<int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int>(itemSerializer1);
            var y = new ValueTupleSerializer<int>(itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith2ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int>();
            var y = new ValueTupleSerializer<int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int, int>(itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int>(itemSerializer2, itemSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int>(itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith3ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int>();
            var y = new ValueTupleSerializer<int, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        [InlineData("item3Serializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1),
                "item3Serializer" => new ValueTupleSerializer<int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith4ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int>();
            var y = new ValueTupleSerializer<int, int, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        [InlineData("item3Serializer")]
        [InlineData("item4Serializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item3Serializer" => new ValueTupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item4Serializer" => new ValueTupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith5ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int>();
            var y = new ValueTupleSerializer<int, int, int, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        [InlineData("item3Serializer")]
        [InlineData("item4Serializer")]
        [InlineData("item5Serializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item3Serializer" => new ValueTupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item4Serializer" => new ValueTupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item5Serializer" => new ValueTupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith6ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int>();
            var y = new ValueTupleSerializer<int, int, int, int, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        [InlineData("item3Serializer")]
        [InlineData("item4Serializer")]
        [InlineData("item5Serializer")]
        [InlineData("item6Serializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item3Serializer" => new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item4Serializer" => new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item5Serializer" => new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item6Serializer" => new ValueTupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith7ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int>();
            var y = new ValueTupleSerializer<int, int, int, int, int, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        [InlineData("item3Serializer")]
        [InlineData("item4Serializer")]
        [InlineData("item5Serializer")]
        [InlineData("item6Serializer")]
        [InlineData("item7Serializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item3Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item4Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item5Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item6Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item7Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class ValueTupleWith7ItemsAndRestSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>();
            var y = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("item1Serializer")]
        [InlineData("item2Serializer")]
        [InlineData("item3Serializer")]
        [InlineData("item4Serializer")]
        [InlineData("item5Serializer")]
        [InlineData("item6Serializer")]
        [InlineData("item7Serializer")]
        [InlineData("restSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var restSerializer1 = new ValueTupleSerializer<int>(itemSerializer1);
            var restSerializer2 = new ValueTupleSerializer<int>(itemSerializer2);
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item2Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item3Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item4Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item5Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, restSerializer1),
                "item6Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, restSerializer1),
                "item7Serializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, restSerializer1),
                "restSerializer" => new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ValueTupleSerializer<int, int, int, int, int, int, int, ValueTuple<int>>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
