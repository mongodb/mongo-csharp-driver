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
using System.Collections.ObjectModel;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class ReadOnlyDictionaryInterfaceImplementerSerializerTests
    {
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            // Act
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>();

            // Assert
            serializer.Should().NotBeNull();
            serializer.DictionaryRepresentation.Should().Be(DictionaryRepresentation.Document);
        }

        [Fact]
        public void Constructor_with_dictionary_representation_and_serializers_should_initialize_instance()
        {
            // Arrange
            var dictionaryRepresentation = DictionaryRepresentation.ArrayOfDocuments;
            var keySerializer = new StringSerializer();
            var valueSerializer = new Int32Serializer();

            // Act
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                dictionaryRepresentation, keySerializer, valueSerializer);

            // Assert
            serializer.Should().NotBeNull();
            serializer.DictionaryRepresentation.Should().Be(dictionaryRepresentation);
            serializer.KeySerializer.Should().BeSameAs(keySerializer);
            serializer.ValueSerializer.Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void Constructor_with_dictionary_representation_should_initialize_instance()
        {
            // Arrange
            var dictionaryRepresentation = DictionaryRepresentation.ArrayOfDocuments;

            // Act
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(dictionaryRepresentation);

            // Assert
            serializer.Should().NotBeNull();
            serializer.DictionaryRepresentation.Should().Be(dictionaryRepresentation);
        }
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();
            var y = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("dictionaryRepresentation")]
        [InlineData("keySerializer")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var dictionaryRepresentation1 = DictionaryRepresentation.Document;
            var dictionaryRepresentation2 = DictionaryRepresentation.ArrayOfArrays;
            var keySerializer1 = new Int32Serializer(BsonType.Int32);
            var keySerializer2 = new Int32Serializer(BsonType.String);
            var valueSerializer1 = new Int32Serializer(BsonType.Int32);
            var valueSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation1, keySerializer1, valueSerializer1);
            var y = notEqualFieldName switch
            {
                "dictionaryRepresentation" => new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation2, keySerializer1, valueSerializer1),
                "keySerializer" => new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation1, keySerializer2, valueSerializer1),
                "valueSerializer" => new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>(dictionaryRepresentation1, keySerializer1, valueSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ReadOnlyDictionaryInterfaceImplementerSerializer<Dictionary<int, int>, int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void IChildSerializerConfigurable_ChildSerializer_should_return_ValueSerializer()
        {
            // Arrange
            var keySerializer = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer, valueSerializer);

            // Act
            var childSerializer = ((IChildSerializerConfigurable)serializer).ChildSerializer;

            // Assert
            childSerializer.Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void IChildSerializerConfigurable_WithChildSerializer_should_return_new_instance()
        {
            // Arrange
            var keySerializer = new StringSerializer();
            var valueSerializer1 = new Int32Serializer();
            var valueSerializer2 = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer, valueSerializer1);
            var configurableSerializer = (IChildSerializerConfigurable)serializer;

            // Act
            var result = configurableSerializer.WithChildSerializer(valueSerializer2);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.Should().BeOfType<ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>>();
            var typedResult = (ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>)result;
            typedResult.KeySerializer.Should().BeSameAs(keySerializer);
            typedResult.ValueSerializer.Should().BeSameAs(valueSerializer2);
        }

        [Fact]
        public void IDictionaryRepresentationConfigurable_WithDictionaryRepresentation_should_return_new_instance()
        {
            // Arrange
            var representation1 = DictionaryRepresentation.Document;
            var representation2 = DictionaryRepresentation.ArrayOfDocuments;
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(representation1);
            var configurableSerializer = (IDictionaryRepresentationConfigurable)serializer;

            // Act
            var result = configurableSerializer.WithDictionaryRepresentation(representation2);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.Should().BeOfType<ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>>();
            var typedResult = (ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>)result;
            typedResult.DictionaryRepresentation.Should().Be(representation2);
        }

        [Fact]
        public void IMultipleChildSerializersConfigurable_ChildSerializers_should_return_key_and_value_serializers()
        {
            // Arrange
            var keySerializer = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer, valueSerializer);
            var configurableSerializer = (IMultipleChildSerializersConfigurable)serializer;

            // Act
            var childSerializers = configurableSerializer.ChildSerializers;

            // Assert
            childSerializers.Should().NotBeNull();
            childSerializers.Should().HaveCount(2);
            childSerializers[0].Should().BeSameAs(keySerializer);
            childSerializers[1].Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void IMultipleChildSerializersConfigurable_WithChildSerializers_should_return_new_instance_when_serializers_are_different()
        {
            // Arrange
            var keySerializer1 = new StringSerializer();
            var valueSerializer1 = new Int32Serializer();
            var keySerializer2 = new StringSerializer();
            var valueSerializer2 = new Int32Serializer(BsonType.String);
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer1, valueSerializer1);
            var configurableSerializer = (IMultipleChildSerializersConfigurable)serializer;

            // Act
            var result = configurableSerializer.WithChildSerializers([keySerializer2, valueSerializer2]);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.Should().BeOfType<ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>>();
            var typedResult = (ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>)result;
            typedResult.KeySerializer.Should().BeSameAs(keySerializer2);
            typedResult.ValueSerializer.Should().BeSameAs(valueSerializer2);
        }

        [Fact]
        public void IMultipleChildSerializersConfigurable_WithChildSerializers_should_return_same_instance_when_serializers_are_equal()
        {
            // Arrange
            var keySerializer = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer, valueSerializer);
            var configurableSerializer = (IMultipleChildSerializersConfigurable)serializer;

            // Act
            var result = configurableSerializer.WithChildSerializers([keySerializer, valueSerializer]);

            // Assert
            result.Should().BeSameAs(serializer);
        }

        [Fact]
        public void IMultipleChildSerializersConfigurable_WithChildSerializers_should_throw_when_array_length_is_not_2()
        {
            // Arrange
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>();
            var configurableSerializer = (IMultipleChildSerializersConfigurable)serializer;

            // Act
            Action act = () => configurableSerializer.WithChildSerializers([new StringSerializer()]);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<Exception>()
                .Which.Message.Should().Be("Wrong number of child serializers passed.");
        }

        [Fact]
        public void WithDictionaryRepresentation_should_return_new_instance_when_representation_is_different()
        {
            // Arrange
            var representation1 = DictionaryRepresentation.Document;
            var representation2 = DictionaryRepresentation.ArrayOfDocuments;
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(representation1);

            // Act
            var result = serializer.WithDictionaryRepresentation(representation2);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.DictionaryRepresentation.Should().Be(representation2);
            result.KeySerializer.Should().BeSameAs(serializer.KeySerializer);
            result.ValueSerializer.Should().BeSameAs(serializer.ValueSerializer);
        }

        [Fact]
        public void WithDictionaryRepresentation_should_return_same_instance_when_representation_is_same()
        {
            // Arrange
            var representation = DictionaryRepresentation.Document;
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(representation);

            // Act
            var result = serializer.WithDictionaryRepresentation(representation);

            // Assert
            result.Should().BeSameAs(serializer);
        }
        [Fact]
        public void WithDictionaryRepresentation_with_serializers_should_return_new_instance_when_key_serializer_is_different()
        {
            // Arrange
            var representation = DictionaryRepresentation.Document;
            var keySerializer1 = new StringSerializer();
            var keySerializer2 = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                representation, keySerializer1, valueSerializer);

            // Act
            var result = serializer.WithDictionaryRepresentation(representation, keySerializer2, valueSerializer);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.DictionaryRepresentation.Should().Be(representation);
            result.KeySerializer.Should().BeSameAs(keySerializer2);
            result.ValueSerializer.Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void WithDictionaryRepresentation_with_serializers_should_return_new_instance_when_representation_is_different()
        {
            // Arrange
            var representation1 = DictionaryRepresentation.Document;
            var representation2 = DictionaryRepresentation.ArrayOfDocuments;
            var keySerializer = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                representation1, keySerializer, valueSerializer);

            // Act
            var result = serializer.WithDictionaryRepresentation(representation2, keySerializer, valueSerializer);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.DictionaryRepresentation.Should().Be(representation2);
            result.KeySerializer.Should().BeSameAs(keySerializer);
            result.ValueSerializer.Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void WithDictionaryRepresentation_with_serializers_should_return_new_instance_when_value_serializer_is_different()
        {
            // Arrange
            var representation = DictionaryRepresentation.Document;
            var keySerializer = new StringSerializer();
            var valueSerializer1 = new Int32Serializer();
            var valueSerializer2 = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                representation, keySerializer, valueSerializer1);

            // Act
            var result = serializer.WithDictionaryRepresentation(representation, keySerializer, valueSerializer2);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.DictionaryRepresentation.Should().Be(representation);
            result.KeySerializer.Should().BeSameAs(keySerializer);
            result.ValueSerializer.Should().BeSameAs(valueSerializer2);
        }

        [Fact]
        public void WithDictionaryRepresentation_with_serializers_should_return_same_instance_when_all_parameters_are_same()
        {
            // Arrange
            var representation = DictionaryRepresentation.Document;
            var keySerializer = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                representation, keySerializer, valueSerializer);

            // Act
            var result = serializer.WithDictionaryRepresentation(representation, keySerializer, valueSerializer);

            // Assert
            result.Should().BeSameAs(serializer);
        }
        [Fact]
        public void WithKeySerializer_should_return_new_instance_when_serializer_is_different()
        {
            // Arrange
            var keySerializer1 = new StringSerializer();
            var keySerializer2 = new StringSerializer();
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer1, valueSerializer);

            // Act
            var result = serializer.WithKeySerializer(keySerializer2);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.DictionaryRepresentation.Should().Be(DictionaryRepresentation.Document);
            result.KeySerializer.Should().BeSameAs(keySerializer2);
            result.ValueSerializer.Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void WithKeySerializer_should_return_same_instance_when_serializer_is_same()
        {
            // Arrange
            var keySerializer = new StringSerializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer, new Int32Serializer());

            // Act
            var result = serializer.WithKeySerializer(keySerializer);

            // Assert
            result.Should().BeSameAs(serializer);
        }
        [Fact]
        public void WithValueSerializer_should_return_new_instance_when_serializer_is_different()
        {
            // Arrange
            var keySerializer = new StringSerializer();
            var valueSerializer1 = new Int32Serializer();
            var valueSerializer2 = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, keySerializer, valueSerializer1);

            // Act
            var result = serializer.WithValueSerializer(valueSerializer2);

            // Assert
            result.Should().NotBeSameAs(serializer);
            result.DictionaryRepresentation.Should().Be(DictionaryRepresentation.Document);
            result.KeySerializer.Should().BeSameAs(keySerializer);
            result.ValueSerializer.Should().BeSameAs(valueSerializer2);
        }

        [Fact]
        public void WithValueSerializer_should_return_same_instance_when_serializer_is_same()
        {
            // Arrange
            var valueSerializer = new Int32Serializer();
            var serializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, int>, string, int>(
                DictionaryRepresentation.Document, new StringSerializer(), valueSerializer);

            // Act
            var result = serializer.WithValueSerializer(valueSerializer);

            // Assert
            result.Should().BeSameAs(serializer);
        }
    }
}
