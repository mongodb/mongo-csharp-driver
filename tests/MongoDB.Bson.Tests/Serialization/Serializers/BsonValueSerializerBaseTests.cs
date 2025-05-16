/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class BsonValueSerializerBaseTests
    {
        [Fact]
        public void DeserializeValue_should_delegate_to_appropriate_serializer_based_on_bson_type()
        {
            // Since we can't directly call the protected DeserializeValue method, we'll test through the public Deserialize method
            // We'll test with a few representative BsonTypes

            // Arrange - Int32
            var int32Value = 42;
            var int32Document = new BsonDocument("value", int32Value);
            using var int32Reader = new BsonDocumentReader(int32Document);
            int32Reader.ReadStartDocument();
            int32Reader.ReadName("value");
            var int32Context = BsonDeserializationContext.CreateRoot(int32Reader);
            var int32Args = new BsonDeserializationArgs();

            // Act - Int32
            var int32Result = BsonValueSerializer.Instance.Deserialize(int32Context, int32Args);

            // Assert - Int32
            int32Result.Should().BeOfType<BsonInt32>();
            int32Result.AsInt32.Should().Be(int32Value);
            int32Reader.ReadEndDocument();

            // Arrange - String
            var stringValue = "test";
            var stringDocument = new BsonDocument("value", stringValue);
            using var stringReader = new BsonDocumentReader(stringDocument);
            stringReader.ReadStartDocument();
            stringReader.ReadName("value");
            var stringContext = BsonDeserializationContext.CreateRoot(stringReader);
            var stringArgs = new BsonDeserializationArgs();

            // Act - String
            var stringResult = BsonValueSerializer.Instance.Deserialize(stringContext, stringArgs);

            // Assert - String
            stringResult.Should().BeOfType<BsonString>();
            stringResult.AsString.Should().Be(stringValue);
            stringReader.ReadEndDocument();

            // Arrange - Document
            var documentValue = new BsonDocument("nested", "value");
            var document = new BsonDocument("value", documentValue);
            using var documentReader = new BsonDocumentReader(document);
            documentReader.ReadStartDocument();
            documentReader.ReadName("value");
            var documentContext = BsonDeserializationContext.CreateRoot(documentReader);
            var documentArgs = new BsonDeserializationArgs();

            // Act - Document
            var documentResult = BsonValueSerializer.Instance.Deserialize(documentContext, documentArgs);

            // Assert - Document
            documentResult.Should().BeOfType<BsonDocument>();
            documentResult.AsBsonDocument.Should().NotBeNull();
            documentResult.AsBsonDocument["nested"].AsString.Should().Be("value");
            documentReader.ReadEndDocument();
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);
            var y = new DerivedFromConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);
            var y = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void SerializeValue_should_delegate_to_appropriate_serializer_based_on_bson_value_type()
        {
            // We'll test a few representative BsonValue types

            // Arrange - Int32
            var int32Value = new BsonInt32(42);
            var int32Document = new BsonDocument();
            using var int32Writer = new BsonDocumentWriter(int32Document);
            int32Writer.WriteStartDocument();
            int32Writer.WriteName("value");
            var int32Context = BsonSerializationContext.CreateRoot(int32Writer);
            var int32Args = new BsonSerializationArgs();

            // Act - Int32
            BsonValueSerializer.Instance.Serialize(int32Context, int32Args, int32Value);

            // Complete the document
            int32Writer.WriteEndDocument();

            // Assert - Int32
            int32Document.Should().NotBeNull();
            int32Document["value"].Should().Be(int32Value);

            // Arrange - String
            var stringValue = new BsonString("test");
            var stringDocument = new BsonDocument();
            using var stringWriter = new BsonDocumentWriter(stringDocument);
            stringWriter.WriteStartDocument();
            stringWriter.WriteName("value");
            var stringContext = BsonSerializationContext.CreateRoot(stringWriter);
            var stringArgs = new BsonSerializationArgs();

            // Act - String
            BsonValueSerializer.Instance.Serialize(stringContext, stringArgs, stringValue);

            // Complete the document
            stringWriter.WriteEndDocument();

            // Assert - String
            stringDocument.Should().NotBeNull();
            stringDocument["value"].Should().Be(stringValue);

            // Arrange - Document
            var nestedDocument = new BsonDocument("nested", "value");
            var documentValue = nestedDocument;
            var document = new BsonDocument();
            using var documentWriter = new BsonDocumentWriter(document);
            documentWriter.WriteStartDocument();
            documentWriter.WriteName("value");
            var documentContext = BsonSerializationContext.CreateRoot(documentWriter);
            var documentArgs = new BsonSerializationArgs();

            // Act - Document
            BsonValueSerializer.Instance.Serialize(documentContext, documentArgs, documentValue);

            // Complete the document
            documentWriter.WriteEndDocument();

            // Assert - Document
            document.Should().NotBeNull();
            document["value"].Should().BeOfType<BsonDocument>();
            document["value"].AsBsonDocument["nested"].AsString.Should().Be("value");
        }

        [Fact]
        public void SerializeValue_should_throw_for_invalid_bson_value_type()
        {
            // Arrange
            var mockBsonValue = new Mock<BsonValue>();
            mockBsonValue.Setup(v => v.BsonType).Returns((BsonType)999); // Invalid BsonType

            var document = new BsonDocument();
            using var writer = new BsonDocumentWriter(document);
            writer.WriteStartDocument();
            writer.WriteName("value");
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs() { SerializeAsNominalType = true };

            // Act
            Action act = () => BsonValueSerializer.Instance.Serialize(context, args, mockBsonValue.Object);

            // Assert
            act.ShouldThrow<BsonInternalException>()
                .WithMessage("Invalid BsonType.");
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_return_true_and_correct_info()
        {
            // Arrange & Act
            var result = BsonValueSerializer.Instance.TryGetItemSerializationInfo(out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().NotBeNull();
            serializationInfo.ElementName.Should().BeNull();
            serializationInfo.Serializer.Should().BeSameAs(BsonValueSerializer.Instance);
            serializationInfo.NominalType.Should().Be(typeof(BsonValue));
        }

        [Fact]
        public void TryGetMemberSerializationInfo_should_return_true_and_correct_info()
        {
            // Arrange
            var memberName = "testMember";

            // Act
            var result = BsonValueSerializer.Instance.TryGetMemberSerializationInfo(memberName, out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().NotBeNull();
            serializationInfo.ElementName.Should().Be(memberName);
            serializationInfo.Serializer.Should().BeSameAs(BsonValueSerializer.Instance);
            serializationInfo.NominalType.Should().Be(typeof(BsonValue));
        }
        public class ConcreteBsonValueSerializerBase<TBsonValue> : BsonValueSerializerBase<TBsonValue>
            where TBsonValue : BsonValue
        {
            public ConcreteBsonValueSerializerBase(BsonType representation) : base(representation)
            {
            }

            protected override TBsonValue DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args) => throw new System.NotImplementedException();

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, TBsonValue value) => throw new System.NotImplementedException();
        }

        public class DerivedFromConcreteBsonValueSerializerBase<TBsonValue> : ConcreteBsonValueSerializerBase<TBsonValue>
            where TBsonValue : BsonValue
        {
            public DerivedFromConcreteBsonValueSerializerBase(BsonType representation) : base(representation)
            {
            }
        }
    }

    public static class BsonValueSerializerBaseReflector
    {
        public static BsonType? _bsonType<TBsonValue>(this BsonValueSerializerBase<TBsonValue> obj) where TBsonValue : BsonValue
            => (BsonType?)Reflector.GetFieldValue(obj, nameof(_bsonType));
    }
}
