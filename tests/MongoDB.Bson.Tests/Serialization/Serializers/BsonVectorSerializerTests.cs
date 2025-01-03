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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.ObjectModel;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonVectorSerializerTests
    {
        [Theory]
        [MemberData(nameof(TestDataBsonVector))]
        public void BsonVectorSerializer_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementsCount, int bitsPadding, T _)
            where T : struct
        {
            var subject = CreateBsonVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementsCount, (byte)bitsPadding);

            var binaryData = SerializeToBinaryData(vector, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestDataBsonVector))]
        public void BsonVectorSerializer_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementsCount, int bitsPadding, T _)
           where T : struct
        {
            var subject = CreateBsonVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementsCount, (byte)bitsPadding);
            var expectedArray = vector.Vector.ToArray();
            var expectedType = (dataType) switch
            {
                BsonVectorDataType.Float32 => typeof(BsonVectorFloat32),
                BsonVectorDataType.PackedBit => typeof(BsonVectorPackedBit),
                BsonVectorDataType.Int8 => typeof(BsonVectorInt8),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType))
            };

            var bsonVector = DeserializeFromBinaryData<BsonVector<T>>(vectorBson, subject);

            bsonVector.Should().BeOfType(expectedType);
            bsonVector.Vector.ToArray().ShouldBeEquivalentTo(expectedArray);

            if (bsonVector is BsonVectorPackedBit vectorPackedBit)
            {
                vectorPackedBit.Padding.Should().Be((byte)bitsPadding);
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void BsonVectorSerializerArray_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
         where T : struct
        {
            var subject = new BsonVectorArraySerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Vector.ToArray();

            var actualArray = DeserializeFromBinaryData<T[]>(vectorBson, subject);

            actualArray.ShouldAllBeEquivalentTo(expectedArray);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void BsonVectorSerializerArray_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new BsonVectorArraySerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);
            var array = vector.Vector.ToArray();

            var binaryData = SerializeToBinaryData(array, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void BsonVectorSerializerMemory_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new BsonVectorMemorySerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);
            var memory = new Memory<T>(vector.Vector.ToArray());

            var binaryData = SerializeToBinaryData(memory, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void BsonVectorSerializerMemory_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new BsonVectorMemorySerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Vector.ToArray();

            var actualMemory = DeserializeFromBinaryData<Memory<T>>(vectorBson, subject);

            actualMemory.ToArray().ShouldBeEquivalentTo(expectedArray);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void BsonVectorSerializerReadonlyMemory_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
           where T : struct
        {
            var subject = new BsonVectorReadOnlyMemorySerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);

            var binaryData = SerializeToBinaryData(vector.Vector, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void BsonVectorSerializerReadonlyMemory_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
           where T : struct
        {
            var subject = new BsonVectorReadOnlyMemorySerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Vector.ToArray();

            var readonlyMemory = DeserializeFromBinaryData<ReadOnlyMemory<T>>(vectorBson, subject);

            readonlyMemory.ToArray().ShouldBeEquivalentTo(expectedArray);
        }

        [Theory]
        [InlineData(BsonVectorDataType.Int8, typeof(BsonVectorSerializer<BsonVectorFloat32, float>))]
        [InlineData(BsonVectorDataType.Int8, typeof(BsonVectorArraySerializer<int>))]
        [InlineData(BsonVectorDataType.Int8, typeof(BsonVectorReadOnlyMemorySerializer<float>))]
        [InlineData(BsonVectorDataType.Int8, typeof(BsonVectorMemorySerializer<double>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(BsonVectorSerializer<BsonVectorFloat32, float>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(BsonVectorArraySerializer<int>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(BsonVectorReadOnlyMemorySerializer<float>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(BsonVectorMemorySerializer<double>))]
        [InlineData(BsonVectorDataType.Float32, typeof(BsonVectorSerializer<BsonVectorInt8, byte>))]
        [InlineData(BsonVectorDataType.Float32, typeof(BsonVectorArraySerializer<int>))]
        [InlineData(BsonVectorDataType.Float32, typeof(BsonVectorReadOnlyMemorySerializer<byte>))]
        [InlineData(BsonVectorDataType.Float32, typeof(BsonVectorMemorySerializer<double>))]
        public void BsonVectorSerializer_should_throw_on_datatype_and_itemtype_mismatch(BsonVectorDataType dataType, Type serializerType)
        {
            var itemType = serializerType.BaseType.GetGenericArguments().ElementAt(1);

            var exception = Record.Exception(() => Activator.CreateInstance(serializerType, dataType)).InnerException;
            exception.Should().BeOfType<InvalidOperationException>();

            exception.Message.Should().Contain(itemType.ToString());
            exception.Message.Should().Contain(dataType.ToString());
        }

        [Theory]
        [InlineData((BsonVectorDataType)10)]
        [InlineData((BsonVectorDataType)100)]
        public void BsonVectorSerializer_should_throw_on_wrong_datatype(BsonVectorDataType dataType)
        {
            var exception = Record.Exception(() => new BsonVectorArraySerializer<byte>(dataType));
            exception.Should().BeOfType<ArgumentOutOfRangeException>();

            exception.Message.Should().Contain("Unsupported vector datatype.");
            exception.Message.Should().Contain(dataType.ToString());
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
            [BsonVectorDataType.Int8, 1, byte.MaxValue],
            [BsonVectorDataType.Int8, 55, byte.MaxValue],
            [BsonVectorDataType.Float32, 1, float.MaxValue],
            [BsonVectorDataType.Float32, 55, float.MaxValue],
        ];

        public readonly static IEnumerable<object[]> TestDataBsonVector =
        [
            [BsonVectorDataType.Int8, 1, 0, byte.MaxValue],
            [BsonVectorDataType.Int8, 55, 0, byte.MaxValue],
            [BsonVectorDataType.Float32, 1, 0, float.MaxValue],
            [BsonVectorDataType.Float32, 55, 0, float.MaxValue],
            [BsonVectorDataType.PackedBit, 1, 0, byte.MaxValue],
            [BsonVectorDataType.PackedBit, 2, 1, byte.MaxValue],
            [BsonVectorDataType.PackedBit, 128, 7, byte.MaxValue],
        ];

        private static (BsonVector<T> Vector, byte[] VectorBson) GetTestData<T>(BsonVectorDataType dataType, int elementsCount, byte bitsPadding)
            where T : struct
        {
            switch (dataType)
            {
                case BsonVectorDataType.Int8:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (byte)i).ToArray();
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. elements];

                        return (new BsonVectorInt8(elements) as BsonVector<T>, vectorBsonData);
                    }
                case BsonVectorDataType.PackedBit:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (byte)i).ToArray();
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. elements];

                        return (new BsonVectorPackedBit(elements, bitsPadding) as BsonVector<T>, vectorBsonData);
                    }
                case BsonVectorDataType.Float32:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (float)i).ToArray();
                        var vectorDataElements = elements.SelectMany(BitConverter.GetBytes);
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. vectorDataElements];

                        return (new BsonVectorFloat32(elements) as BsonVector<T>, vectorBsonData);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType));
            }
        }

        private static IBsonSerializer CreateBsonVectorSerializer<T>(BsonVectorDataType dataType)
            where T : struct
        {
            IBsonSerializer serializer = dataType switch
            {
                BsonVectorDataType.Float32 => new BsonVectorSerializer<BsonVectorFloat32, float>(dataType),
                BsonVectorDataType.Int8 => new BsonVectorSerializer<BsonVectorInt8, byte>(dataType),
                BsonVectorDataType.PackedBit => new BsonVectorSerializer<BsonVectorPackedBit, byte>(dataType),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType))
            };

            return serializer;
        }
    }
}
