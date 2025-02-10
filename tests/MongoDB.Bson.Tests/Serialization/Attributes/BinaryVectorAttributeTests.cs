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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Attributes
{
    public class BinaryVectorAttributeTests
    {
        [Fact]
        public void BinaryVectorAttribute_should_set_binaryVectorArraySerializer_for_array()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(ArrayHolder));

            AssertSerializer<ArrayAsBinaryVectorSerializer<byte>, byte[], byte>(classMap, nameof(ArrayHolder.ValuesByte), BinaryVectorDataType.Int8);
            AssertSerializer<ArrayAsBinaryVectorSerializer<sbyte>, sbyte[], sbyte>(classMap, nameof(ArrayHolder.ValuesSByte), BinaryVectorDataType.Int8);
            AssertSerializer<ArrayAsBinaryVectorSerializer<byte>, byte[], byte>(classMap, nameof(ArrayHolder.ValuesPackedBit), BinaryVectorDataType.PackedBit);
            AssertSerializer<ArrayAsBinaryVectorSerializer<float>, float[], float>(classMap, nameof(ArrayHolder.ValuesFloat), BinaryVectorDataType.Float32);
        }

        [Fact]
        public void BinaryVectorAttribute_should_set_binaryVectorSerializer_for_binaryVector()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(BinaryVectorHolder));

            AssertSerializer<BinaryVectorSerializer<BinaryVectorInt8, sbyte>, BinaryVectorInt8, sbyte>(classMap, nameof(BinaryVectorHolder.ValuesInt8), BinaryVectorDataType.Int8);
            AssertSerializer<BinaryVectorSerializer<BinaryVectorPackedBit, byte>, BinaryVectorPackedBit, byte>(classMap, nameof(BinaryVectorHolder.ValuesPackedBit), BinaryVectorDataType.PackedBit);
            AssertSerializer<BinaryVectorSerializer<BinaryVectorFloat32, float>, BinaryVectorFloat32, float>(classMap, nameof(BinaryVectorHolder.ValuesFloat), BinaryVectorDataType.Float32);
        }

        [Fact]
        public void BinaryVectorAttribute_should_set_binaryVectorMemorySerializer_for_memory()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(MemoryHolder));

            AssertSerializer<MemoryAsBinaryVectorSerializer<byte>, Memory<byte>, byte>(classMap, nameof(MemoryHolder.ValuesByte), BinaryVectorDataType.Int8);
            AssertSerializer<MemoryAsBinaryVectorSerializer<sbyte>, Memory<sbyte>, sbyte>(classMap, nameof(MemoryHolder.ValuesSByte), BinaryVectorDataType.Int8);
            AssertSerializer<MemoryAsBinaryVectorSerializer<byte>, Memory<byte>, byte>(classMap, nameof(MemoryHolder.ValuesPackedBit), BinaryVectorDataType.PackedBit);
            AssertSerializer<MemoryAsBinaryVectorSerializer<float>, Memory<float>, float>(classMap, nameof(MemoryHolder.ValuesFloat), BinaryVectorDataType.Float32);
        }

        [Fact]
        public void BinaryVectorAttribute_should_set_binaryVectorReadonlyMemorySerializer_for_readOnlyMemory()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(ReadOnlyMemoryHolder));

            AssertSerializer<ReadOnlyMemoryAsBinaryVectorSerializer<byte>, ReadOnlyMemory<byte>, byte>(classMap, nameof(ReadOnlyMemoryHolder.ValuesByte), BinaryVectorDataType.Int8);
            AssertSerializer<ReadOnlyMemoryAsBinaryVectorSerializer<sbyte>, ReadOnlyMemory<sbyte>, sbyte>(classMap, nameof(ReadOnlyMemoryHolder.ValuesSByte), BinaryVectorDataType.Int8);
            AssertSerializer<ReadOnlyMemoryAsBinaryVectorSerializer<byte>, ReadOnlyMemory<byte>, byte>(classMap, nameof(ReadOnlyMemoryHolder.ValuesPackedBit), BinaryVectorDataType.PackedBit);
            AssertSerializer<ReadOnlyMemoryAsBinaryVectorSerializer<float>, ReadOnlyMemory<float>, float>(classMap, nameof(ReadOnlyMemoryHolder.ValuesFloat), BinaryVectorDataType.Float32);
        }

        [Theory]
        [InlineData(nameof(InvalidTypesHolder.List))]
        [InlineData(nameof(InvalidTypesHolder.Dictionary))]
        public void BinaryVectorAttribute_should_throw_on_invalid_type(string memberName)
        {
            var memberInfo = typeof(InvalidTypesHolder).GetProperty(memberName);
            var classMap = new BsonClassMap<InvalidTypesHolder>(cm => cm.MapMember(memberInfo));

            var exception = Record.Exception(() => classMap.AutoMap());

            exception.Should().BeOfType<NotSupportedException>();
            exception.Message.Should().Be($"Type {memberInfo.PropertyType} cannot be serialized as a binary vector.");
        }

        [Fact]
        public void BinaryVectorSerializer_should_be_used_for_binaryvector_without_attribute()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(BinaryVectorNoAttributeHolder));

            AssertSerializer<BinaryVectorSerializer<BinaryVectorInt8, sbyte>, BinaryVectorInt8, sbyte>(classMap, nameof(BinaryVectorNoAttributeHolder.ValuesInt8), BinaryVectorDataType.Int8);
            AssertSerializer<BinaryVectorSerializer<BinaryVectorPackedBit, byte>, BinaryVectorPackedBit, byte>(classMap, nameof(BinaryVectorNoAttributeHolder.ValuesPackedBit), BinaryVectorDataType.PackedBit);
            AssertSerializer<BinaryVectorSerializer<BinaryVectorFloat32, float>, BinaryVectorFloat32, float>(classMap, nameof(BinaryVectorNoAttributeHolder.ValuesFloat), BinaryVectorDataType.Float32);
        }

        private void AssertSerializer<TSerializer, TCollection, TITem>(BsonClassMap classMap, string memberName, BinaryVectorDataType binaryVectorDataType)
            where TSerializer : BinaryVectorSerializerBase<TCollection, TITem>
            where TITem : struct
        {
            var memberMap = classMap.GetMemberMap(memberName);
            var serializer = memberMap.GetSerializer();

            var vectorSerializer = serializer.Should().BeOfType<TSerializer>().Subject;

            vectorSerializer.VectorDataType.Should().Be(binaryVectorDataType);
        }

        public class ArrayHolder
        {
            [BinaryVector(BinaryVectorDataType.Int8)]
            public byte[] ValuesByte { get; set; }

            [BinaryVector(BinaryVectorDataType.Int8)]
            public sbyte[] ValuesSByte { get; set; }

            [BinaryVector(BinaryVectorDataType.PackedBit)]
            public byte[] ValuesPackedBit { get; set; }

            [BinaryVector(BinaryVectorDataType.Float32)]
            public float[] ValuesFloat { get; set; }
        }

        public class BinaryVectorHolder
        {
            [BinaryVector(BinaryVectorDataType.Int8)]
            public BinaryVectorInt8 ValuesInt8 { get; set; }

            [BinaryVector(BinaryVectorDataType.PackedBit)]
            public BinaryVectorPackedBit ValuesPackedBit { get; set; }

            [BinaryVector(BinaryVectorDataType.Float32)]
            public BinaryVectorFloat32 ValuesFloat { get; set; }
        }

        public class BinaryVectorNoAttributeHolder
        {
            public BinaryVectorInt8 ValuesInt8 { get; set; }

            public BinaryVectorPackedBit ValuesPackedBit { get; set; }

            public BinaryVectorFloat32 ValuesFloat { get; set; }
        }

        public class MemoryHolder
        {
            [BinaryVector(BinaryVectorDataType.Int8)]
            public Memory<byte> ValuesByte { get; set; }

            [BinaryVector(BinaryVectorDataType.Int8)]
            public Memory<sbyte> ValuesSByte { get; set; }

            [BinaryVector(BinaryVectorDataType.PackedBit)]
            public Memory<byte> ValuesPackedBit { get; set; }

            [BinaryVector(BinaryVectorDataType.Float32)]
            public Memory<float> ValuesFloat { get; set; }
        }

        public class ReadOnlyMemoryHolder
        {
            [BinaryVector(BinaryVectorDataType.Int8)]
            public ReadOnlyMemory<byte> ValuesByte { get; set; }

            [BinaryVector(BinaryVectorDataType.Int8)]
            public ReadOnlyMemory<sbyte> ValuesSByte { get; set; }

            [BinaryVector(BinaryVectorDataType.PackedBit)]
            public ReadOnlyMemory<byte> ValuesPackedBit { get; set; }

            [BinaryVector(BinaryVectorDataType.Float32)]
            public ReadOnlyMemory<float> ValuesFloat { get; set; }
        }

        public class InvalidTypesHolder
        {
            [BinaryVector(BinaryVectorDataType.Int8)]
            public List<int> List { get; set; }

            [BinaryVector(BinaryVectorDataType.Int8)]
            public Dictionary<int, string> Dictionary { get; set; }
        }
    }
}
