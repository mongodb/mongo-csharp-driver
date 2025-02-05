﻿/* Copyright 2010-present MongoDB Inc.
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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BinaryVectorSerializerTests
    {
        [Theory]
        [MemberData(nameof(TestData))]
        public void ArrayAsBinaryVectorSerializer_should_deserialize_bson_vector<T>(BinaryVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new ArrayAsBinaryVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Data.ToArray();

            var actualArray = DeserializeFromBinaryData<T[]>(vectorBson, subject);

            actualArray.ShouldAllBeEquivalentTo(expectedArray);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ArrayAsBinaryVectorSerializer_should_serialize_bson_vector<T>(BinaryVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new ArrayAsBinaryVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);
            var array = vector.Data.ToArray();

            var binaryData = SerializeToBinaryData(array, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Fact]
        public void ArrayAsBinaryVectorSerializer_should_throw_on_non_zero_padding()
        {
            var subject = new ArrayAsBinaryVectorSerializer<byte>(BinaryVectorDataType.PackedBit);

            var (vector, vectorBson) = GetTestData<byte>(BinaryVectorDataType.PackedBit, 2, 1);
            var expectedArray = vector.Data.ToArray();

            var exception = Record.Exception(() => DeserializeFromBinaryData<byte[]>(vectorBson, subject));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("padding");
        }

        [Theory]
        [MemberData(nameof(TestDataBinaryVector))]
        public void BinaryVectorSerializer_should_serialize_bson_vector<T>(BinaryVectorDataType dataType, int elementsCount, int bitsPadding, T _)
            where T : struct
        {
            var subject = CreateBinaryVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementsCount, (byte)bitsPadding);

            var binaryData = SerializeToBinaryData(vector, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestDataBinaryVector))]
        public void BinaryVectorSerializer_should_deserialize_bson_vector<T>(BinaryVectorDataType dataType, int elementsCount, int bitsPadding, T _)
           where T : struct
        {
            var subject = CreateBinaryVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementsCount, (byte)bitsPadding);
            var expectedArray = vector.Data.ToArray();
            var expectedType = (dataType) switch
            {
                BinaryVectorDataType.Float32 => typeof(BinaryVectorFloat32),
                BinaryVectorDataType.PackedBit => typeof(BinaryVectorPackedBit),
                BinaryVectorDataType.Int8 => typeof(BinaryVectorInt8),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType))
            };

            var binaryVector = DeserializeFromBinaryData<BinaryVectorBase<T>>(vectorBson, subject);

            binaryVector.Should().BeOfType(expectedType);
            binaryVector.Data.ToArray().ShouldBeEquivalentTo(expectedArray);

            if (binaryVector is BinaryVectorPackedBit vectorPackedBit)
            {
                vectorPackedBit.Padding.Should().Be((byte)bitsPadding);
            }
        }


        [Theory]
        [InlineData(BinaryVectorDataType.Int8, typeof(BinaryVectorSerializer<BinaryVectorFloat32, float>))]
        [InlineData(BinaryVectorDataType.Int8, typeof(ArrayAsBinaryVectorSerializer<int>))]
        [InlineData(BinaryVectorDataType.Int8, typeof(ReadOnlyMemoryAsBinaryVectorSerializer<float>))]
        [InlineData(BinaryVectorDataType.Int8, typeof(MemoryAsBinaryVectorSerializer<double>))]
        [InlineData(BinaryVectorDataType.PackedBit, typeof(BinaryVectorSerializer<BinaryVectorFloat32, float>))]
        [InlineData(BinaryVectorDataType.PackedBit, typeof(ArrayAsBinaryVectorSerializer<int>))]
        [InlineData(BinaryVectorDataType.PackedBit, typeof(ReadOnlyMemoryAsBinaryVectorSerializer<float>))]
        [InlineData(BinaryVectorDataType.PackedBit, typeof(MemoryAsBinaryVectorSerializer<double>))]
        [InlineData(BinaryVectorDataType.Float32, typeof(BinaryVectorSerializer<BinaryVectorInt8, byte>))]
        [InlineData(BinaryVectorDataType.Float32, typeof(ArrayAsBinaryVectorSerializer<int>))]
        [InlineData(BinaryVectorDataType.Float32, typeof(ReadOnlyMemoryAsBinaryVectorSerializer<byte>))]
        [InlineData(BinaryVectorDataType.Float32, typeof(MemoryAsBinaryVectorSerializer<double>))]
        public void BinaryVectorSerializer_should_throw_on_datatype_and_itemtype_mismatch(BinaryVectorDataType dataType, Type serializerType)
        {
            var itemType = serializerType.BaseType.GetGenericArguments().ElementAt(1);

            var exception = Record.Exception(() => Activator.CreateInstance(serializerType, dataType)).InnerException;
            exception.Should().BeOfType<NotSupportedException>();

            exception.Message.Should().Contain(itemType.ToString());
            exception.Message.Should().Contain(dataType.ToString());
        }

        [Fact]
        public void BinaryVectorSerializer_should_roundtrip_binaryvector_without_attribute()
        {
            var binaryVectorHolder = new BinaryVectorNoAttributeHolder()
            {
                ValuesFloat = new BinaryVectorFloat32(new float[] { 1.1f, 2.2f, 3.3f }),
                ValuesInt8 = new BinaryVectorInt8(new byte[] { 1, 2, 3 }),
                ValuesPackedBit = new BinaryVectorPackedBit(new byte[] { 1, 2, 3 }, 0)
            };

            var bson = binaryVectorHolder.ToBson();

            var binaryVectorHolderDehydrated = BsonSerializer.Deserialize<BinaryVectorNoAttributeHolder>(bson);

            binaryVectorHolderDehydrated.ValuesFloat.Data.ToArray().ShouldBeEquivalentTo(binaryVectorHolder.ValuesFloat.Data.ToArray());
            binaryVectorHolderDehydrated.ValuesInt8.Data.ToArray().ShouldBeEquivalentTo(binaryVectorHolder.ValuesInt8.Data.ToArray());
            binaryVectorHolderDehydrated.ValuesPackedBit.Data.ToArray().ShouldBeEquivalentTo(binaryVectorHolder.ValuesPackedBit.Data.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void MemoryAsBinaryVectorSerializer_should_serialize_bson_vector<T>(BinaryVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new MemoryAsBinaryVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);
            var memory = new Memory<T>(vector.Data.ToArray());

            var binaryData = SerializeToBinaryData(memory, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void MemoryAsBinaryVectorSerializer_should_deserialize_bson_vector<T>(BinaryVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new MemoryAsBinaryVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Data.ToArray();

            var actualMemory = DeserializeFromBinaryData<Memory<T>>(vectorBson, subject);

            actualMemory.ToArray().ShouldBeEquivalentTo(expectedArray);
        }

        [Fact]
        public void MemoryAsBinaryVectorSerializer_should_throw_on_non_zero_padding()
        {
            var subject = new ReadOnlyMemoryAsBinaryVectorSerializer<byte>(BinaryVectorDataType.PackedBit);

            var (vector, vectorBson) = GetTestData<byte>(BinaryVectorDataType.PackedBit, 2, 1);
            var expectedArray = vector.Data.ToArray();

            var exception = Record.Exception(() => DeserializeFromBinaryData<Memory<byte>>(vectorBson, subject));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("padding");
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReadonlyMemoryAsBinaryVectorSerializer_should_serialize_bson_vector<T>(BinaryVectorDataType dataType, int elementCount, T _)
           where T : struct
        {
            var subject = new ReadOnlyMemoryAsBinaryVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);

            var binaryData = SerializeToBinaryData(vector.Data, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReadonlyMemoryAsBinaryVectorSerializer_should_deserialize_bson_vector<T>(BinaryVectorDataType dataType, int elementCount, T _)
           where T : struct
        {
            var subject = new ReadOnlyMemoryAsBinaryVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Data.ToArray();

            var readonlyMemory = DeserializeFromBinaryData<ReadOnlyMemory<T>>(vectorBson, subject);

            readonlyMemory.ToArray().ShouldBeEquivalentTo(expectedArray);
        }

        [Fact]
        public void ReadonlyMemoryAsBinaryVectorSerializer_should_throw_on_non_zero_padding()
        {
            var subject = new ReadOnlyMemoryAsBinaryVectorSerializer<byte>(BinaryVectorDataType.PackedBit);

            var (vector, vectorBson) = GetTestData<byte>(BinaryVectorDataType.PackedBit, 2, 1);
            var expectedArray = vector.Data.ToArray();

            var exception = Record.Exception(() => DeserializeFromBinaryData<ReadOnlyMemory<byte>>(vectorBson, subject));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("padding");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);
            var y = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);
            var y = new ArrayAsBinaryVectorSerializer<byte>(BinaryVectorDataType.PackedBit);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ArrayAsBinaryVectorSerializer<float>(BinaryVectorDataType.Float32);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        private TCollection DeserializeFromBinaryData<TCollection>(byte[] vectorBson, IBsonSerializer serializer)
        {
            var binaryData = new BsonBinaryData(vectorBson, BsonBinarySubType.Vector);
            var bsonDocument = new BsonDocument("vector", binaryData);
            var bson = bsonDocument.ToBson();

            using var memoryStream = new MemoryStream(bson);
            using var reader = new BsonBinaryReader(memoryStream, new());
            var context = BsonDeserializationContext.CreateRoot(reader);

            reader.ReadStartDocument();
            reader.ReadName("vector");

            var result = serializer.Deserialize(context, new());

            reader.ReadEndDocument();

            return (TCollection)result;
        }

        private BsonBinaryData SerializeToBinaryData<TCollection>(TCollection collection, IBsonSerializer serializer)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BsonBinaryWriter(memoryStream, new());
            var context = BsonSerializationContext.CreateRoot(writer);

            writer.WriteStartDocument();
            writer.WriteName("vector");

            serializer.Serialize(context, new(), collection);

            writer.WriteEndDocument();

            var document = BsonSerializer.Deserialize<BsonDocument>(memoryStream.ToArray());
            var binaryData = document["vector"].AsBsonBinaryData;
            return binaryData;
        }

        public readonly static IEnumerable<object[]> TestData =
        [
            [BinaryVectorDataType.Int8, 1, byte.MaxValue],
            [BinaryVectorDataType.Int8, 55, byte.MaxValue],
            [BinaryVectorDataType.Float32, 1, float.MaxValue],
            [BinaryVectorDataType.Float32, 55, float.MaxValue],
        ];

        public readonly static IEnumerable<object[]> TestDataBinaryVector =
        [
            [BinaryVectorDataType.Int8, 1, 0, byte.MaxValue],
            [BinaryVectorDataType.Int8, 55, 0, byte.MaxValue],
            [BinaryVectorDataType.Float32, 1, 0, float.MaxValue],
            [BinaryVectorDataType.Float32, 55, 0, float.MaxValue],
            [BinaryVectorDataType.PackedBit, 1, 0, byte.MaxValue],
            [BinaryVectorDataType.PackedBit, 2, 1, byte.MaxValue],
            [BinaryVectorDataType.PackedBit, 128, 7, byte.MaxValue],
        ];

        private static (BinaryVectorBase<T> Vector, byte[] VectorBson) GetTestData<T>(BinaryVectorDataType dataType, int elementsCount, byte bitsPadding)
            where T : struct
        {
            switch (dataType)
            {
                case BinaryVectorDataType.Int8:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (byte)i).ToArray();
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. elements];

                        return (new BinaryVectorInt8(elements) as BinaryVectorBase<T>, vectorBsonData);
                    }
                case BinaryVectorDataType.PackedBit:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (byte)i).ToArray();
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. elements];

                        return (new BinaryVectorPackedBit(elements, bitsPadding) as BinaryVectorBase<T>, vectorBsonData);
                    }
                case BinaryVectorDataType.Float32:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (float)i).ToArray();
                        var vectorDataElements = elements.SelectMany(BitConverter.GetBytes);
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. vectorDataElements];

                        return (new BinaryVectorFloat32(elements) as BinaryVectorBase<T>, vectorBsonData);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType));
            }
        }

        private static IBsonSerializer CreateBinaryVectorSerializer<T>(BinaryVectorDataType dataType)
            where T : struct
        {
            IBsonSerializer serializer = dataType switch
            {
                BinaryVectorDataType.Float32 => new BinaryVectorSerializer<BinaryVectorFloat32, float>(dataType),
                BinaryVectorDataType.Int8 => new BinaryVectorSerializer<BinaryVectorInt8, byte>(dataType),
                BinaryVectorDataType.PackedBit => new BinaryVectorSerializer<BinaryVectorPackedBit, byte>(dataType),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType))
            };

            return serializer;
        }

        public class BinaryVectorNoAttributeHolder
        {
            public BinaryVectorInt8 ValuesInt8 { get; set; }
        
            public BinaryVectorPackedBit ValuesPackedBit { get; set; }

            public BinaryVectorFloat32 ValuesFloat { get; set; }
        }
    }
}
