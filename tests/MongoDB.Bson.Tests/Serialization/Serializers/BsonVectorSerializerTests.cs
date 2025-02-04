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
        [MemberData(nameof(TestData))]
        public void ArrayAsBsonVectorSerializer_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
 where T : struct
        {
            var subject = new ArrayAsBsonVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Data.ToArray();

            var actualArray = DeserializeFromBinaryData<T[]>(vectorBson, subject);

            actualArray.ShouldAllBeEquivalentTo(expectedArray);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ArrayAsBsonVectorSerializer_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new ArrayAsBsonVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);
            var array = vector.Data.ToArray();

            var binaryData = SerializeToBinaryData(array, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Fact]
        public void ArrayAsBsonVectorSerializer_should_throw_on_non_zero_padding()
        {
            var subject = new ArrayAsBsonVectorSerializer<byte>(BsonVectorDataType.PackedBit);

            var (vector, vectorBson) = GetTestData<byte>(BsonVectorDataType.PackedBit, 2, 1);
            var expectedArray = vector.Data.ToArray();

            var exception = Record.Exception(() => DeserializeFromBinaryData<byte[]>(vectorBson, subject));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("padding");
        }

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
            var expectedArray = vector.Data.ToArray();
            var expectedType = (dataType) switch
            {
                BsonVectorDataType.Float32 => typeof(BsonVectorFloat32),
                BsonVectorDataType.PackedBit => typeof(BsonVectorPackedBit),
                BsonVectorDataType.Int8 => typeof(BsonVectorInt8),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType))
            };

            var bsonVector = DeserializeFromBinaryData<BsonVectorBase<T>>(vectorBson, subject);

            bsonVector.Should().BeOfType(expectedType);
            bsonVector.Data.ToArray().ShouldBeEquivalentTo(expectedArray);

            if (bsonVector is BsonVectorPackedBit vectorPackedBit)
            {
                vectorPackedBit.Padding.Should().Be((byte)bitsPadding);
            }
        }


        [Theory]
        [InlineData(BsonVectorDataType.Int8, typeof(BsonVectorSerializer<BsonVectorFloat32, float>))]
        [InlineData(BsonVectorDataType.Int8, typeof(ArrayAsBsonVectorSerializer<int>))]
        [InlineData(BsonVectorDataType.Int8, typeof(ReadOnlyMemoryAsBsonVectorSerializer<float>))]
        [InlineData(BsonVectorDataType.Int8, typeof(MemoryAsBsonVectorSerializer<double>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(BsonVectorSerializer<BsonVectorFloat32, float>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(ArrayAsBsonVectorSerializer<int>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(ReadOnlyMemoryAsBsonVectorSerializer<float>))]
        [InlineData(BsonVectorDataType.PackedBit, typeof(MemoryAsBsonVectorSerializer<double>))]
        [InlineData(BsonVectorDataType.Float32, typeof(BsonVectorSerializer<BsonVectorInt8, byte>))]
        [InlineData(BsonVectorDataType.Float32, typeof(ArrayAsBsonVectorSerializer<int>))]
        [InlineData(BsonVectorDataType.Float32, typeof(ReadOnlyMemoryAsBsonVectorSerializer<byte>))]
        [InlineData(BsonVectorDataType.Float32, typeof(MemoryAsBsonVectorSerializer<double>))]
        public void BsonVectorSerializer_should_throw_on_datatype_and_itemtype_mismatch(BsonVectorDataType dataType, Type serializerType)
        {
            var itemType = serializerType.BaseType.GetGenericArguments().ElementAt(1);

            var exception = Record.Exception(() => Activator.CreateInstance(serializerType, dataType)).InnerException;
            exception.Should().BeOfType<NotSupportedException>();

            exception.Message.Should().Contain(itemType.ToString());
            exception.Message.Should().Contain(dataType.ToString());
        }

        [Fact]
        public void BsonVectorSerializer_should_roundtrip_bsonvector_without_attribute()
        {
            var bsonVectorHolder = new BsonVectorNoAttributeHolder()
            {
                ValuesFloat = new BsonVectorFloat32(new float[] { 1.1f, 2.2f, 3.3f }),
                ValuesInt8 = new BsonVectorInt8(new byte[] { 1, 2, 3 }),
                ValuesPackedBit = new BsonVectorPackedBit(new byte[] { 1, 2, 3 }, 0)
            };

            var bson = bsonVectorHolder.ToBson();

            var bsonVectorHolderDehydrated = BsonSerializer.Deserialize<BsonVectorNoAttributeHolder>(bson);

            bsonVectorHolderDehydrated.ValuesFloat.Data.ToArray().ShouldBeEquivalentTo(bsonVectorHolder.ValuesFloat.Data.ToArray());
            bsonVectorHolderDehydrated.ValuesInt8.Data.ToArray().ShouldBeEquivalentTo(bsonVectorHolder.ValuesInt8.Data.ToArray());
            bsonVectorHolderDehydrated.ValuesPackedBit.Data.ToArray().ShouldBeEquivalentTo(bsonVectorHolder.ValuesPackedBit.Data.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void MemoryAsBsonVectorSerializer_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new MemoryAsBsonVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);
            var memory = new Memory<T>(vector.Data.ToArray());

            var binaryData = SerializeToBinaryData(memory, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void MemoryAsBsonVectorSerializer_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
            where T : struct
        {
            var subject = new MemoryAsBsonVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Data.ToArray();

            var actualMemory = DeserializeFromBinaryData<Memory<T>>(vectorBson, subject);

            actualMemory.ToArray().ShouldBeEquivalentTo(expectedArray);
        }

        [Fact]
        public void MemoryAsBsonVectorSerializer_should_throw_on_non_zero_padding()
        {
            var subject = new ReadOnlyMemoryAsBsonVectorSerializer<byte>(BsonVectorDataType.PackedBit);

            var (vector, vectorBson) = GetTestData<byte>(BsonVectorDataType.PackedBit, 2, 1);
            var expectedArray = vector.Data.ToArray();

            var exception = Record.Exception(() => DeserializeFromBinaryData<Memory<byte>>(vectorBson, subject));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("padding");
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReadonlyMemoryAsBsonVectorSerializer_should_serialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
           where T : struct
        {
            var subject = new ReadOnlyMemoryAsBsonVectorSerializer<T>(dataType);

            var (vector, expectedBson) = GetTestData<T>(dataType, elementCount, 0);

            var binaryData = SerializeToBinaryData(vector.Data, subject);

            Assert.Equal(BsonBinarySubType.Vector, binaryData.SubType);
            Assert.Equal(expectedBson, binaryData.Bytes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReadonlyMemoryAsBsonVectorSerializer_should_deserialize_bson_vector<T>(BsonVectorDataType dataType, int elementCount, T _)
           where T : struct
        {
            var subject = new ReadOnlyMemoryAsBsonVectorSerializer<T>(dataType);

            var (vector, vectorBson) = GetTestData<T>(dataType, elementCount, 0);
            var expectedArray = vector.Data.ToArray();

            var readonlyMemory = DeserializeFromBinaryData<ReadOnlyMemory<T>>(vectorBson, subject);

            readonlyMemory.ToArray().ShouldBeEquivalentTo(expectedArray);
        }

        [Fact]
        public void ReadonlyMemoryAsBsonVectorSerializer_should_throw_on_non_zero_padding()
        {
            var subject = new ReadOnlyMemoryAsBsonVectorSerializer<byte>(BsonVectorDataType.PackedBit);

            var (vector, vectorBson) = GetTestData<byte>(BsonVectorDataType.PackedBit, 2, 1);
            var expectedArray = vector.Data.ToArray();

            var exception = Record.Exception(() => DeserializeFromBinaryData<ReadOnlyMemory<byte>>(vectorBson, subject));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("padding");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);
            var y = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);
            var y = new ArrayAsBsonVectorSerializer<byte>(BsonVectorDataType.PackedBit);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ArrayAsBsonVectorSerializer<float>(BsonVectorDataType.Float32);

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

        private static (BsonVectorBase<T> Vector, byte[] VectorBson) GetTestData<T>(BsonVectorDataType dataType, int elementsCount, byte bitsPadding)
            where T : struct
        {
            switch (dataType)
            {
                case BsonVectorDataType.Int8:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (byte)i).ToArray();
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. elements];

                        return (new BsonVectorInt8(elements) as BsonVectorBase<T>, vectorBsonData);
                    }
                case BsonVectorDataType.PackedBit:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (byte)i).ToArray();
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. elements];

                        return (new BsonVectorPackedBit(elements, bitsPadding) as BsonVectorBase<T>, vectorBsonData);
                    }
                case BsonVectorDataType.Float32:
                    {
                        var elements = Enumerable.Range(0, elementsCount).Select(i => (float)i).ToArray();
                        var vectorDataElements = elements.SelectMany(BitConverter.GetBytes);
                        byte[] vectorBsonData = [(byte)dataType, bitsPadding, .. vectorDataElements];

                        return (new BsonVectorFloat32(elements) as BsonVectorBase<T>, vectorBsonData);
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

        public class BsonVectorNoAttributeHolder
        {
            public BsonVectorInt8 ValuesInt8 { get; set; }
        
            public BsonVectorPackedBit ValuesPackedBit { get; set; }

            public BsonVectorFloat32 ValuesFloat { get; set; }
        }
    }
}
