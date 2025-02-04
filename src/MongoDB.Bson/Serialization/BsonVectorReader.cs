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
using System.Runtime.InteropServices;
using MongoDB.Bson.ObjectModel;

namespace MongoDB.Bson.Serialization
{
    internal static class BsonVectorReader
    {
        public static BsonVectorBase<TItem> ReadBsonVector<TItem>(ReadOnlyMemory<byte> vectorData)
            where TItem : struct
        {
            var (items, padding, vectorDataType) = ReadBsonVectorAsArray<TItem>(vectorData);

            return CreateBsonVector(items, padding, vectorDataType);
        }

        public static (TItem[] Items, byte Padding, BsonVectorDataType VectorDataType) ReadBsonVectorAsArray<TItem>(ReadOnlyMemory<byte> vectorData)
            where TItem : struct
        {
            var (vectorDataBytes, padding, vectorDataType) = ReadBsonVectorAsBytes(vectorData);
            ValidateItemType<TItem>(vectorDataType);

            TItem[] items;

            switch (vectorDataType)
            {
                case BsonVectorDataType.Float32:
                    if (BitConverter.IsLittleEndian)
                    {
                        var singles = MemoryMarshal.Cast<byte, float>(vectorDataBytes.Span);
                        items = (TItem[])(object)singles.ToArray();
                    }
                    else
                    {
                        throw new NotSupportedException("Bson Vector data is not supported on Big Endian architecture yet.");
                    }
                    break;
                case BsonVectorDataType.Int8:
                case BsonVectorDataType.PackedBit:
                    items = (TItem[])(object)vectorDataBytes.ToArray();
                    break;
                default:
                    throw new NotSupportedException($"Vector data type {vectorDataType} is not supported");
            }

            return (items, padding, vectorDataType);
        }

        public static (ReadOnlyMemory<byte> Bytes, byte Padding, BsonVectorDataType VectorDataType) ReadBsonVectorAsBytes(ReadOnlyMemory<byte> vectorData)
        {
            if (vectorData.Length < 2)
            {
                throw new ArgumentException($"Invalid {nameof(vectorData)} size {vectorData.Length}", nameof(vectorData));
            }

            var vectorDataSpan = vectorData.Span;
            var vectorDataType = (BsonVectorDataType)vectorDataSpan[0];

            var padding = vectorDataSpan[1];
            if (padding > 7)
            {
                throw new FormatException($"Invalid padding size {padding}");
            }

            return (vectorData.Slice(2), padding, vectorDataType);
        }

        private static BsonVectorBase<TItem> CreateBsonVector<TItem>(TItem[] items, byte padding, BsonVectorDataType vectorDataType)
            where TItem : struct
        {
            switch (vectorDataType)
            {
                case BsonVectorDataType.Float32:
                    return new BsonVectorFloat32(AsTypedArrayOrThrow<float>()) as BsonVectorBase<TItem>;
                case BsonVectorDataType.Int8:
                    return new BsonVectorInt8(AsTypedArrayOrThrow<byte>()) as BsonVectorBase<TItem>;
                case BsonVectorDataType.PackedBit:
                    return new BsonVectorPackedBit(AsTypedArrayOrThrow<byte>(), padding) as BsonVectorBase<TItem>;
                default:
                    throw new NotSupportedException($"Vector data type {vectorDataType} is not supported");
            }

            TExpectedItem[] AsTypedArrayOrThrow<TExpectedItem>()
            {
                if (items is not TExpectedItem[] result)
                {
                    throw new ArgumentException($"Item type {typeof(TItem)} is not supported with {vectorDataType} vector type, expected {typeof(TExpectedItem)}.");
                }

                return result;
            }
        }

        public static void ValidateItemType<TItem>(BsonVectorDataType bsonVectorDataType)
        {
            var expectedItemType = bsonVectorDataType switch
            {
                BsonVectorDataType.Float32 => typeof(float),
                BsonVectorDataType.Int8 => typeof(byte),
                BsonVectorDataType.PackedBit => typeof(byte),
                _ => throw new ArgumentException(nameof(bsonVectorDataType), "Unsupported vector datatype.")
            };

            if (expectedItemType != typeof(TItem))
            {
                throw new NotSupportedException($"Item type {typeof(TItem)} is not supported with {bsonVectorDataType} vector type, expected item type to be {expectedItemType}.");
            }
        }
    }
}
