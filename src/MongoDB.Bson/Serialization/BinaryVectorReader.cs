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
using System.Runtime.InteropServices;

namespace MongoDB.Bson.Serialization
{
    internal static class BinaryVectorReader
    {
        public static BinaryVector<TItem> ReadBinaryVector<TItem>(ReadOnlyMemory<byte> vectorData)
            where TItem : struct
        {
            var (items, padding, vectorDataType) = ReadBinaryVectorAsArray<TItem>(vectorData);

            return CreateBinaryVector(items, padding, vectorDataType);
        }

        public static (TItem[] Items, byte Padding, BinaryVectorDataType VectorDataType) ReadBinaryVectorAsArray<TItem>(ReadOnlyMemory<byte> vectorData)
            where TItem : struct
        {
            var (vectorDataBytes, padding, vectorDataType) = ReadBinaryVectorAsBytes(vectorData);
            ValidateItemType<TItem>(vectorDataType);

            TItem[] items;

            switch (vectorDataType)
            {
                case BinaryVectorDataType.Float32:

                    if ((vectorDataBytes.Span.Length & 3) != 0)
                    {
                        throw new FormatException("Data length of binary vector of type Float32 must be a multiple of 4 bytes.");
                    }

                    if (BitConverter.IsLittleEndian)
                    {
                        var singles = MemoryMarshal.Cast<byte, float>(vectorDataBytes.Span);
                        items = (TItem[])(object)singles.ToArray();
                    }
                    else
                    {
                        throw new NotSupportedException("Binary vector data is not supported on Big Endian architecture yet.");
                    }
                    break;
                case BinaryVectorDataType.Int8:
                    var itemsSpan = MemoryMarshal.Cast<byte, TItem>(vectorDataBytes.Span);
                    items = (TItem[])(object)itemsSpan.ToArray();
                    break;
                case BinaryVectorDataType.PackedBit:
                    items = (TItem[])(object)vectorDataBytes.ToArray();
                    break;
                default:
                    throw new NotSupportedException($"Binary vector data type {vectorDataType} is not supported.");
            }

            return (items, padding, vectorDataType);
        }

        public static (ReadOnlyMemory<byte> Bytes, byte Padding, BinaryVectorDataType VectorDataType) ReadBinaryVectorAsBytes(ReadOnlyMemory<byte> vectorData)
        {
            if (vectorData.Length < 2)
            {
                throw new ArgumentException($"Invalid {nameof(vectorData)} size {vectorData.Length}.", nameof(vectorData));
            }

            var vectorDataSpan = vectorData.Span;
            var vectorDataType = (BinaryVectorDataType)vectorDataSpan[0];

            var padding = vectorDataSpan[1];
            if (padding > 7)
            {
                throw new FormatException($"Invalid padding size {padding}.");
            }

            if (padding != 0 && vectorData.Length == 2)
            {
                throw new FormatException($"Vector data elements expected with padding size {padding}, but no elements found.");
            }

            return (vectorData.Slice(2), padding, vectorDataType);
        }

        private static BinaryVector<TItem> CreateBinaryVector<TItem>(TItem[] items, byte padding, BinaryVectorDataType vectorDataType)
            where TItem : struct
        {
            switch (vectorDataType)
            {
                case BinaryVectorDataType.Float32:
                    ValidateItemTypeForBinaryVector<TItem, float, BinaryVectorFloat32>();
                    return new BinaryVectorFloat32(AsTypedArrayOrThrow<float>()) as BinaryVector<TItem>;
                case BinaryVectorDataType.Int8:
                    ValidateItemTypeForBinaryVector<TItem, sbyte, BinaryVectorInt8>();
                    return new BinaryVectorInt8(AsTypedArrayOrThrow<sbyte>()) as BinaryVector<TItem>;
                case BinaryVectorDataType.PackedBit:
                    ValidateItemTypeForBinaryVector<TItem, byte, BinaryVectorPackedBit>();
                    return new BinaryVectorPackedBit(AsTypedArrayOrThrow<byte>(), padding) as BinaryVector<TItem>;
                default:
                    throw new NotSupportedException($"Vector data type {vectorDataType} is not supported.");
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

        public static void ValidateItemType<TItem>(BinaryVectorDataType binaryVectorDataType)
        {
            IEnumerable<Type> expectedItemTypes = binaryVectorDataType switch
            {
                BinaryVectorDataType.Float32 => [typeof(float)],
                BinaryVectorDataType.Int8 => [typeof(byte), typeof(sbyte)],
                BinaryVectorDataType.PackedBit => [typeof(byte)],
                _ => throw new ArgumentException(nameof(binaryVectorDataType), "Unsupported vector datatype.")
            };

            if (!expectedItemTypes.Contains(typeof(TItem)))
            {
                throw new NotSupportedException($"Item type {typeof(TItem)} is not supported with {binaryVectorDataType} vector type, expected item type to be {string.Join(",", expectedItemTypes)}.");
            }
        }

        private static void ValidateItemTypeForBinaryVector<TItem, TItemExpectedType, TBinaryVectorType>()
        {
            if (typeof(TItem) != typeof(TItemExpectedType))
            {
                throw new NotSupportedException($"Expected {typeof(TItemExpectedType)} for {typeof(TBinaryVectorType)}, but found {typeof(TItem)}.");
            }
        }
    }
}
