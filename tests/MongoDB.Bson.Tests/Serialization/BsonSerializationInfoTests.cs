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

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonSerializationInfoTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));
            var y = new DerivedFromBsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));
            var y = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("elementName")]
        [InlineData("elementPath")]
        [InlineData("serializer")]
        [InlineData("nominalType")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var elementPath1 = new[] { "elementName1" };
            var elementPath2 = new[] { "elementName2" };
            var serializer1 = new Int32Serializer(BsonType.Int32);
            var serializer2 = new Int32Serializer(BsonType.String);
            var nominalType1 = typeof(int);
            var nominalType2 = typeof(object);
            var x = notEqualFieldName == "elementPath" ?
                BsonSerializationInfo.CreateWithPath(elementPath1, serializer1, nominalType1) :
                new BsonSerializationInfo("elementName1", serializer1, nominalType1);
            var y = notEqualFieldName switch
            {
                "elementName" => new BsonSerializationInfo("elementName2", serializer1, nominalType1),
                "elementPath" => BsonSerializationInfo.CreateWithPath(elementPath2, serializer1, nominalType1),
                "serializer" => new BsonSerializationInfo("elementName1", serializer2, nominalType1),
                "nominalType" => new BsonSerializationInfo("elementName1", serializer1, nominalType2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(notEqualFieldName == null ? true : false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonSerializationInfo("elementName", Int32Serializer.Instance, typeof(int));

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        private class DerivedFromBsonSerializationInfo : BsonSerializationInfo
        {
            public DerivedFromBsonSerializationInfo(string elementName, IBsonSerializer serializer, Type nominalType) : base(elementName, serializer, nominalType)
            {
            }
        }
    }
}
