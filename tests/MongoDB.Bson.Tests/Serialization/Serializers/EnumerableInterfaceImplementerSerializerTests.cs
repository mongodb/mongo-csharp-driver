/* Copyright 2015-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class EnumerableInterfaceImplementerSerializerTests
    {
        private static readonly IBsonSerializer __itemSerializer1;
        private static readonly IBsonSerializer __itemSerializer2;

        static EnumerableInterfaceImplementerSerializerTests()
        {
            __itemSerializer1 = new Int32Serializer(BsonType.Int32);
            __itemSerializer2 = new Int32Serializer(BsonType.String);
        }

        [Fact]
        public void Constructor_should_throw_when_serializer_is_null()
        {
            // Act
            Action act = () => new EnumerableInterfaceImplementerSerializer<TestEnumerable>((IBsonSerializer)null);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("itemSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_serializer_registry_is_null()
        {
            // Act
            Action act = () => new EnumerableInterfaceImplementerSerializer<TestEnumerable>((IBsonSerializerRegistry)null);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("serializerRegistry");
        }

        [Fact]
        public void Constructor_with_item_serializer_should_initialize_instance_with_specified_serializer()
        {
            // Arrange
            var itemSerializer = new StringSerializer();

            // Act
            var serializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>(itemSerializer);

            // Assert
            serializer.Should().NotBeNull();
            GetItemSerializer(serializer).Should().BeSameAs(itemSerializer);
        }

        [Fact]
        public void Constructor_with_no_arguments_should_initialize_instance()
        {
            // Arrange & Act
            var serializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>();

            // Assert
            serializer.Should().NotBeNull();
            GetItemSerializer(serializer).Should().NotBeNull();
        }

        [Fact]
        public void Constructor_with_serializer_registry_should_initialize_instance()
        {
            // Arrange
            var itemSerializer = new ObjectSerializer();
            var mockRegistry = new Mock<IBsonSerializerRegistry>();
            mockRegistry.Setup(r => r.GetSerializer(typeof(object))).Returns(itemSerializer);

            // Act
            var serializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>(mockRegistry.Object);

            // Assert
            serializer.ItemSerializer.Should().Be(itemSerializer);
        }

        [Fact]
        public void CreateAccumulator_should_return_new_instance_of_TValue()
        {
            // Arrange
            var serializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>();

            // Act
            var result = InvokeCreateAccumulator(serializer);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestEnumerable>();
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void IChildSerializerConfigurable_ChildSerializer_should_return_ItemSerializer()
        {
            // Arrange
            var itemSerializer = new StringSerializer();
            var serializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>(itemSerializer);
            var childSerializerConfigurable = (IChildSerializerConfigurable)serializer;

            // Act
            var result = childSerializerConfigurable.ChildSerializer;

            // Assert
            result.Should().BeSameAs(itemSerializer);
        }

        [Fact]
        public void IChildSerializerConfigurable_WithChildSerializer_should_return_serializer_with_specified_child_serializer()
        {
            // Arrange
            var originalItemSerializer = new StringSerializer();
            var originalSerializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>(originalItemSerializer);
            var newItemSerializer = new Int32Serializer();
            var childSerializerConfigurable = (IChildSerializerConfigurable)originalSerializer;

            // Act
            var result = childSerializerConfigurable.WithChildSerializer(newItemSerializer);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeSameAs(originalSerializer);
            result.Should().BeOfType<EnumerableInterfaceImplementerSerializer<TestEnumerable>>();
            var resultSerializer = (EnumerableInterfaceImplementerSerializer<TestEnumerable>)result;
            GetItemSerializer(resultSerializer).Should().BeSameAs(newItemSerializer);
        }

        [Fact]
        public void WithItemSerializer_should_return_new_serializer_with_specified_item_serializer()
        {
            // Arrange
            var originalItemSerializer = new StringSerializer();
            var originalSerializer = new EnumerableInterfaceImplementerSerializer<TestEnumerable>(originalItemSerializer);
            var newItemSerializer = new Int32Serializer();

            // Act
            var result = originalSerializer.WithItemSerializer(newItemSerializer);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeSameAs(originalSerializer);
            result.Should().BeOfType<EnumerableInterfaceImplementerSerializer<TestEnumerable>>();
            GetItemSerializer(result).Should().BeSameAs(newItemSerializer);
        }

        // Helper method to get the ItemSerializer property via reflection
        private IBsonSerializer GetItemSerializer<TValue>(EnumerableInterfaceImplementerSerializer<TValue> serializer)
            where TValue : IEnumerable, new()
        {
            var propertyInfo = typeof(EnumerableInterfaceImplementerSerializerBase<>)
                .MakeGenericType(typeof(TValue))
                .GetProperty("ItemSerializer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (IBsonSerializer)propertyInfo.GetValue(serializer);
        }

        // Helper method to invoke protected CreateAccumulator method via reflection
        private object InvokeCreateAccumulator<TValue>(EnumerableInterfaceImplementerSerializer<TValue> serializer)
            where TValue : IEnumerable, new()
        {
            var methodInfo = typeof(EnumerableInterfaceImplementerSerializer<TValue>)
                .GetMethod("CreateAccumulator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return methodInfo.Invoke(serializer, null);
        }

        // Test implementation of IEnumerable for testing
        private class TestEnumerable : IEnumerable
        {
            private readonly List<object> _items = new();

            public void Add(object item)
            {
                _items.Add(item);
            }

            public IEnumerator GetEnumerator()
            {
                return _items.GetEnumerator();
            }
        }
    }

    public class EnumerableInterfaceImplementerSerializerGenericTests
    {
        private static readonly IBsonSerializer<int> __itemSerializer1;
        private static readonly IBsonSerializer<int> __itemSerializer2;

        static EnumerableInterfaceImplementerSerializerGenericTests()
        {
            __itemSerializer1 = new Int32Serializer(BsonType.Int32);
            __itemSerializer2 = new Int32Serializer(BsonType.String);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);
            var y = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new EnumerableInterfaceImplementerSerializer<List<int>, int>(__itemSerializer1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void LookupSerializer_should_not_throw_StackOverflowException()
        {
            var serializer = BsonSerializer.LookupSerializer<C>();

            serializer.Should().BeOfType<EnumerableInterfaceImplementerSerializer<C, C>>();
            var itemSerializer = ((EnumerableInterfaceImplementerSerializer<C, C>)serializer).ItemSerializer;
            itemSerializer.Should().BeSameAs(serializer);
        }

        [Fact]
        public void Serialize_should_return_expected_result()
        {
            var subject = CreateSubject();

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var context = BsonSerializationContext.CreateRoot(jsonWriter);
                var value = new C { Id = 1, Children = new List<C> { new C { Id = 2, Children = new List<C>() } } };

                subject.Serialize(context, value);

                var json = stringWriter.ToString();
                json.Should().Be("[[]]");
            }
        }

        private IBsonSerializer<C> CreateSubject()
        {
            // create subject without using the global serializer registry
            var serializerRegistry = new BsonSerializerRegistry();
            var subject = new EnumerableInterfaceImplementerSerializer<C, C>(serializerRegistry);
            serializerRegistry.RegisterSerializer(typeof(C), subject);
            return subject;
        }

        public class C : IEnumerable<C>
        {
            public List<C> Children;
            public int Id;

            public IEnumerator<C> GetEnumerator()
            {
                return Children.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
