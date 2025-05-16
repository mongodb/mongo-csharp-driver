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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class IOrderedEnumerableSerializerTests
    {
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";

            // Act
            var subject = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Assert
            subject.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_should_throw_when_itemSerializer_is_null()
        {
            // Arrange
            IBsonSerializer<string> itemSerializer = null;
            var thenByExceptionMessage = "ThenBy is not supported";

            // Act
            Action act = () => new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("itemSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_thenByExceptionMessage_is_null()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            string thenByExceptionMessage = null;

            // Act
            Action act = () => new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("thenByExceptionMessage");
        }

        [Fact]
        public void Create_should_create_serializer_with_correct_item_type()
        {
            // Arrange
            var itemSerializer = new Int32Serializer();
            string exceptionMessage = "ThenBy is not supported";

            // Act
            var result = IOrderedEnumerableSerializer.Create(itemSerializer, exceptionMessage);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType(typeof(IOrderedEnumerableSerializer<int>));
        }

        [Fact]
        public void Create_should_throw_when_itemSerializer_is_null()
        {
            // Arrange
            IBsonSerializer itemSerializer = null;
            string exceptionMessage = "ThenBy is not supported";

            // Act
            Action act = () => IOrderedEnumerableSerializer.Create(itemSerializer, exceptionMessage);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("itemSerializer");
        }

        [Fact]
        public void Deserialize_should_create_ordered_enumerable_list_wrapper()
        {
            // Arrange
            var itemSerializer = new Int32Serializer();
            var subject = new IOrderedEnumerableSerializer<int>(itemSerializer, "ThenBy is not supported");

            var document = new BsonDocument("x", new BsonArray(new[] { 1, 2, 3 }));
            using var reader = new BsonDocumentReader(document);
            var context = BsonDeserializationContext.CreateRoot(reader);
            reader.ReadStartDocument();
            reader.ReadName("x");

            // Act
            var result = subject.Deserialize(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IOrderedEnumerable<int>>();
            result.Should().BeOfType<OrderedEnumerableListWrapper<int>>();
            result.Count().Should().Be(3);
            result.Should().Equal([1, 2, 3]);
        }

        [Fact]
        public void Deserialize_should_return_ordered_enumerable()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            var document = new BsonDocument("items", new BsonArray(new[] { "a", "b", "c" }));
            using var reader = new BsonDocumentReader(document);
            var context = BsonDeserializationContext.CreateRoot(reader);

            reader.ReadStartDocument();
            reader.ReadName("items");

            // Act
            var result = subject.Deserialize(context, new BsonDeserializationArgs());

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IOrderedEnumerable<string>>();
            result.Should().ContainInOrder("a", "b", "c");

            // Verify ThenBy throws with the expected message
            Action act = () => result.ThenBy(x => x.Length);
            act.ShouldThrow<InvalidOperationException>().WithMessage(thenByExceptionMessage);

            reader.ReadEndDocument();
        }

        [Fact]
        public void Equals_different_item_serializer_should_return_false()
        {
            // Arrange
            var itemSerializer1 = new StringSerializer();
            var itemSerializer2 = new StringSerializer(BsonType.ObjectId);
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject1 = new IOrderedEnumerableSerializer<string>(itemSerializer1, thenByExceptionMessage);
            var subject2 = new IOrderedEnumerableSerializer<string>(itemSerializer2, thenByExceptionMessage);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_different_then_by_exception_message_should_return_false()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage1 = "ThenBy is not supported 1";
            var thenByExceptionMessage2 = "ThenBy is not supported 2";
            var subject1 = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage1);
            var subject2 = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage2);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_different_type_should_return_false()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Act
            var result = subject.Equals(new object());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new IOrderedEnumerableSerializer<int>(Int32Serializer.Instance, "message");

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new IOrderedEnumerableSerializer<int>(Int32Serializer.Instance, "message");
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_same_instance_should_return_true()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Act
            var result = subject.Equals(subject);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void Equals_same_values_should_return_true()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject1 = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);
            var subject2 = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new IOrderedEnumerableSerializer<int>(Int32Serializer.Instance, "message");

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new IOrderedEnumerableSerializer<int>(Int32Serializer.Instance, "message");
            var y = new IOrderedEnumerableSerializer<int>(Int32Serializer.Instance, "message");

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("itemSerializer")]
        [InlineData("message")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new IOrderedEnumerableSerializer<int>(itemSerializer1, "message1");
            var y = notEqualFieldName switch
            {
                "itemSerializer" => new IOrderedEnumerableSerializer<int>(itemSerializer2, "message1"),
                "message" => new IOrderedEnumerableSerializer<int>(itemSerializer1, "message2"),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new IOrderedEnumerableSerializer<int>(Int32Serializer.Instance, "message");

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void Serialize_should_write_array_of_items()
        {
            // Arrange
            var itemSerializer = new Int32Serializer();
            var subject = new IOrderedEnumerableSerializer<int>(itemSerializer, "ThenBy is not supported");
            var list = new OrderedEnumerableListWrapper<int>([1, 2, 3], "ThenBy is not supported");

            var document = new BsonDocument();
            using var writer = new BsonDocumentWriter(document);
            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");

            // Act
            subject.Serialize(context, list);
            writer.WriteEndDocument();

            // Assert
            document["x"].Should().Be(new BsonArray(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void Serialize_should_write_array_with_items()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            var orderedEnumerable = new List<string> { "a", "b", "c" }
                .OrderBy(x => x);

            var document = new BsonDocument();
            using var writer = new BsonDocumentWriter(document);
            var context = BsonSerializationContext.CreateRoot(writer);

            writer.WriteStartDocument();
            writer.WriteName("items");

            // Act
            subject.Serialize(context, new BsonSerializationArgs(), orderedEnumerable);

            // Assert
            writer.WriteEndDocument();

            document.Should().NotBeNull();
            document.ElementCount.Should().Be(1);
            document["items"].Should().BeOfType<BsonArray>();
            document["items"].AsBsonArray.Count.Should().Be(3);
            document["items"].AsBsonArray[0].AsString.Should().Be("a");
            document["items"].AsBsonArray[1].AsString.Should().Be("b");
            document["items"].AsBsonArray[2].AsString.Should().Be("c");
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_return_correct_info()
        {
            // Arrange
            var itemSerializer = new Int32Serializer();
            var subject = new IOrderedEnumerableSerializer<int>(itemSerializer, "ThenBy is not supported");

            // Act
            var success = subject.TryGetItemSerializationInfo(out BsonSerializationInfo info);

            // Assert
            success.Should().BeTrue();
            info.Should().NotBeNull();
            info.Serializer.Should().BeSameAs(itemSerializer);
            info.NominalType.Should().Be(typeof(int));
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_return_true_and_serialization_info()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var thenByExceptionMessage = "ThenBy is not supported";
            var subject = new IOrderedEnumerableSerializer<string>(itemSerializer, thenByExceptionMessage);

            // Act
            var result = subject.TryGetItemSerializationInfo(out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().NotBeNull();
            serializationInfo.ElementName.Should().BeNull();
            serializationInfo.Serializer.Should().BeSameAs(itemSerializer);
            serializationInfo.NominalType.Should().Be(typeof(string));
        }
    }
}
