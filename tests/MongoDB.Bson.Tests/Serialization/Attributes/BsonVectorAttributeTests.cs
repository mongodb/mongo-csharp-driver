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
using MongoDB.Bson.ObjectModel;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Attributes
{
    public class BsonVectorAttributeTests
    {
        [Fact]
        public void BsonVectorAttribute_should_set_bsonVectorArraySerializer_for_array()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(ArrayHolder));

            AssertSerializer<BsonVectorArraySerializer<byte>, byte[], byte>(classMap, nameof(ArrayHolder.ValuesByte), BsonVectorDataType.Int8);
            AssertSerializer<BsonVectorArraySerializer<byte>, byte[], byte>(classMap, nameof(ArrayHolder.ValuesPackedBit), BsonVectorDataType.PackedBit);
            AssertSerializer<BsonVectorArraySerializer<float>, float[], float>(classMap, nameof(ArrayHolder.ValuesFloat), BsonVectorDataType.Float32);
        }

        [Fact]
        public void BsonVectorAttribute_should_set_bsonVectorSerializer_for_bsonVector()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(BsonVectorHolder));

            AssertSerializer<BsonVectorSerializer<BsonVectorInt8, byte>, BsonVectorInt8, byte>(classMap, nameof(BsonVectorHolder.ValuesInt8), BsonVectorDataType.Int8);
            AssertSerializer<BsonVectorSerializer<BsonVectorPackedBit, byte>, BsonVectorPackedBit, byte>(classMap, nameof(BsonVectorHolder.ValuesPackedBit), BsonVectorDataType.PackedBit);
            AssertSerializer<BsonVectorSerializer<BsonVectorFloat32, float>, BsonVectorFloat32, float>(classMap, nameof(BsonVectorHolder.ValuesFloat), BsonVectorDataType.Float32);
        }

        [Fact]
        public void BsonVectorAttribute_should_set_bsonVectorMemorySerializer_for_memory()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(MemoryHolder));

            AssertSerializer<BsonVectorMemorySerializer<byte>, Memory<byte>, byte>(classMap, nameof(MemoryHolder.ValuesByte), BsonVectorDataType.Int8);
            AssertSerializer<BsonVectorMemorySerializer<byte>, Memory<byte>, byte>(classMap, nameof(MemoryHolder.ValuesPackedBit), BsonVectorDataType.PackedBit);
            AssertSerializer<BsonVectorMemorySerializer<float>, Memory<float>, float>(classMap, nameof(MemoryHolder.ValuesFloat), BsonVectorDataType.Float32);
        }

        [Fact]
        public void BsonVectorAttribute_should_set_bsonVectorReadonlyMemorySerializer_for_readOnlyMemory()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(ReadOnlyMemoryHolder));

            AssertSerializer<BsonVectorReadOnlyMemorySerializer<byte>, ReadOnlyMemory<byte>, byte>(classMap, nameof(ReadOnlyMemoryHolder.ValuesByte), BsonVectorDataType.Int8);
            AssertSerializer<BsonVectorReadOnlyMemorySerializer<byte>, ReadOnlyMemory<byte>, byte>(classMap, nameof(ReadOnlyMemoryHolder.ValuesPackedBit), BsonVectorDataType.PackedBit);
            AssertSerializer<BsonVectorReadOnlyMemorySerializer<float>, ReadOnlyMemory<float>, float>(classMap, nameof(ReadOnlyMemoryHolder.ValuesFloat), BsonVectorDataType.Float32);
        }

        [Theory]
        [InlineData(nameof(InvalidTypesHolder.List))]
        [InlineData(nameof(InvalidTypesHolder.Dictionary))]
        public void BsonVectorAttribute_should_throw_on_invalid_type(string memberName)
        {
            var memberInfo = typeof(InvalidTypesHolder).GetProperty(memberName);
            var classMap = new BsonClassMap<InvalidTypesHolder>(cm => cm.MapMember(memberInfo));

            var exception = Record.Exception(() => classMap.AutoMap());

            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Be($"Type {memberInfo.PropertyType} is not supported for a binary vector.");
        }

        private void AssertSerializer<TSerializer, TCollection, TITem>(BsonClassMap classMap, string memberName, BsonVectorDataType bsonVectorDataType)
            where TSerializer : BsonVectorSerializerBase<TCollection, TITem>
            where TITem : struct
        {
            var memberMap = classMap.GetMemberMap(memberName);
            var serializer = memberMap.GetSerializer();

            var vectorSerializer = serializer.Should().BeOfType<TSerializer>().Subject;

            vectorSerializer.VectorDataType.Should().Be(bsonVectorDataType);
        }

        public class ArrayHolder
        {
            [BsonVector(BsonVectorDataType.Int8)]
            public byte[] ValuesByte { get; set; }

            [BsonVector(BsonVectorDataType.PackedBit)]
            public byte[] ValuesPackedBit { get; set; }

            [BsonVector(BsonVectorDataType.Float32)]
            public float[] ValuesFloat { get; set; }
        }

        public class BsonVectorHolder
        {
            [BsonVector(BsonVectorDataType.Int8)]
            public BsonVectorInt8 ValuesInt8 { get; set; }

            [BsonVector(BsonVectorDataType.PackedBit)]
            public BsonVectorPackedBit ValuesPackedBit { get; set; }

            [BsonVector(BsonVectorDataType.Float32)]
            public BsonVectorFloat32 ValuesFloat { get; set; }
        }

        public class MemoryHolder
        {
            [BsonVector(BsonVectorDataType.Int8)]
            public Memory<byte> ValuesByte { get; set; }

            [BsonVector(BsonVectorDataType.PackedBit)]
            public Memory<byte> ValuesPackedBit { get; set; }

            [BsonVector(BsonVectorDataType.Float32)]
            public Memory<float> ValuesFloat { get; set; }
        }

        public class ReadOnlyMemoryHolder
        {
            [BsonVector(BsonVectorDataType.Int8)]
            public ReadOnlyMemory<byte> ValuesByte { get; set; }

            [BsonVector(BsonVectorDataType.PackedBit)]
            public ReadOnlyMemory<byte> ValuesPackedBit { get; set; }

            [BsonVector(BsonVectorDataType.Float32)]
            public ReadOnlyMemory<float> ValuesFloat { get; set; }
        }

        public class InvalidTypesHolder
        {
            [BsonVector(BsonVectorDataType.Int8)]
            public List<int> List { get; set; }

            [BsonVector(BsonVectorDataType.Int8)]
            public Dictionary<int, string> Dictionary { get; set; }
        }
    }
}
