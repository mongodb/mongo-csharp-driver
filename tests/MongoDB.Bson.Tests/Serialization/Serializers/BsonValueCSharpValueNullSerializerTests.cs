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
using Moq;
using Xunit;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class BsonValueCSharpNullArrayAndDocumentSerializerTests
    {
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            // Arrange
            var wrappedSerializer = new Mock<IBsonSerializer<BsonValue>>().Object;

            // Act
            var subject = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(wrappedSerializer);

            // Assert
            subject.Should().NotBeNull();
            subject.Should().BeAssignableTo<BsonValueCSharpNullSerializer<BsonValue>>();
            subject.Should().BeAssignableTo<IBsonArraySerializer>();
            subject.Should().BeAssignableTo<IBsonDocumentSerializer>();
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_delegate_to_BsonValueSerializer()
        {
            // Arrange
            var wrappedSerializer = new Mock<IBsonSerializer<BsonValue>>().Object;
            var subject = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(wrappedSerializer);

            // Get expected result directly from BsonValueSerializer for comparison
            bool expectedResult = BsonValueSerializer.Instance.TryGetItemSerializationInfo(out BsonSerializationInfo expectedInfo);

            // Act
            bool result = subject.TryGetItemSerializationInfo(out BsonSerializationInfo actualInfo);

            // Assert
            result.Should().Be(expectedResult);

            if (expectedResult)
            {
                actualInfo.Should().NotBeNull();
                actualInfo.ElementName.Should().Be(expectedInfo.ElementName);
                actualInfo.NominalType.Should().Be(expectedInfo.NominalType);
                actualInfo.Serializer.Should().BeSameAs(expectedInfo.Serializer);
            }
        }

        [Fact]
        public void TryGetMemberSerializationInfo_should_delegate_to_BsonValueSerializer()
        {
            // Arrange
            var wrappedSerializer = new Mock<IBsonSerializer<BsonValue>>().Object;
            var subject = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(wrappedSerializer);
            string memberName = "testMember";

            // Get expected result directly from BsonValueSerializer for comparison
            bool expectedResult = BsonValueSerializer.Instance.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo expectedInfo);

            // Act
            bool result = subject.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo actualInfo);

            // Assert
            result.Should().Be(expectedResult);

            if (expectedResult)
            {
                actualInfo.Should().NotBeNull();
                actualInfo.ElementName.Should().Be(expectedInfo.ElementName);
                actualInfo.NominalType.Should().Be(expectedInfo.NominalType);
                actualInfo.Serializer.Should().BeSameAs(expectedInfo.Serializer);
            }
        }

        [Fact]
        public void TryGetMemberSerializationInfo_with_different_member_names_should_behave_like_BsonValueSerializer()
        {
            // Arrange
            var wrappedSerializer = new Mock<IBsonSerializer<BsonValue>>().Object;
            var subject = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(wrappedSerializer);

            // Test with several different member names
            var memberNames = new[] { "member1", "property2", "_id", "123", string.Empty };

            foreach (var memberName in memberNames)
            {
                // Get expected result from BsonValueSerializer for comparison
                bool expectedResult = BsonValueSerializer.Instance.TryGetMemberSerializationInfo(memberName, out _);

                // Act
                bool actualResult = subject.TryGetMemberSerializationInfo(memberName, out _);

                // Assert
                actualResult.Should().Be(expectedResult, $"because result for member name '{memberName}' should match BsonValueSerializer's behavior");
            }
        }
    }

    public class BsonValueCSharpNullArraySerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullArraySerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class BsonValueCSharpNullDocumentSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullDocumentSerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class BsonValueCSharpNullSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new DerivedFromBsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullSerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class DerivedFromBsonValueCSharpNullSerializer<TBsonValue> : BsonValueCSharpNullSerializer<TBsonValue>
            where TBsonValue : BsonValue
        {
            public DerivedFromBsonValueCSharpNullSerializer(IBsonSerializer<TBsonValue> wrappedSerializer)
                : base(wrappedSerializer)
            {
            }
        }
    }
    public class BsonValueSerializer1 : SerializerBase<BsonValue>
    { }

    public class BsonValueSerializer2 : SerializerBase<BsonValue>
    { }
}
