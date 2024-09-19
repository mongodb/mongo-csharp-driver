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
    public class TupleWith1ItemSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int>();
            var y = new TupleSerializer<int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new TupleSerializer<int>(itemSerializer1);
            var y = new TupleSerializer<int>(itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith2ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int>();
            var y = new TupleSerializer<int, int>();

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
            var x = new TupleSerializer<int, int>(itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int>(itemSerializer2, itemSerializer1),
                "item2Serializer" => new TupleSerializer<int, int>(itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith3ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int, int>();
            var y = new TupleSerializer<int, int, int>();

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
            var x = new TupleSerializer<int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1),
                "item2Serializer" => new TupleSerializer<int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1),
                "item3Serializer" => new TupleSerializer<int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith4ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int>();
            var y = new TupleSerializer<int, int, int, int>();

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
            var x = new TupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new TupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item3Serializer" => new TupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item4Serializer" => new TupleSerializer<int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith5ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int>();
            var y = new TupleSerializer<int, int, int, int, int>();

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
            var x = new TupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new TupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item3Serializer" => new TupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item4Serializer" => new TupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item5Serializer" => new TupleSerializer<int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith6ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int, int>();
            var y = new TupleSerializer<int, int, int, int, int, int>();

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
            var x = new TupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int, int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new TupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item3Serializer" => new TupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item4Serializer" => new TupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item5Serializer" => new TupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item6Serializer" => new TupleSerializer<int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int, int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith7ItemsSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int>();
            var y = new TupleSerializer<int, int, int, int, int, int, int>();

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
            var x = new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item2Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item3Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1),
                "item4Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1),
                "item5Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1),
                "item6Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1),
                "item7Serializer" => new TupleSerializer<int, int, int, int, int, int, int>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class TupleWith7ItemsAndRestSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>();
            var y = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>();

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
            var restSerializer1 = new TupleSerializer<int>(itemSerializer1);
            var restSerializer2 = new TupleSerializer<int>(itemSerializer2);
            var x = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1);
            var y = notEqualFieldName switch
            {
                "item1Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item2Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item3Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item4Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer1),
                "item5Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, itemSerializer1, restSerializer1),
                "item6Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, itemSerializer1, restSerializer1),
                "item7Serializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer2, restSerializer1),
                "restSerializer" => new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>(itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, itemSerializer1, restSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new TupleSerializer<int, int, int, int, int, int, int, Tuple<int>>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
