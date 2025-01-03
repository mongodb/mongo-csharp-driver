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
        public static BsonVector<T> ReadBsonVector<T>(ReadOnlyMemory<byte> vectorData)
            where T : struct
        {
            var (elements, padding, vectorDataType) = ReadBsonVectorAsArray<T>(vectorData);

            return CreateBsonVector(elements, padding, vectorDataType);
        }

        public static (T[] Elements, byte Padding, BsonVectorDataType vectorDataType) ReadBsonVectorAsArray<T>(ReadOnlyMemory<byte> vectorData)
            where T : struct
        {
            var (vectorDataBytes, padding, vectorDataType) = ReadBsonVectorAsBytes(vectorData);
            ValidateDataType<T>(vectorDataType);

            T[] elements;

            switch (vectorDataType)
            {
                case BsonVectorDataType.Float32:
                    if (BitConverter.IsLittleEndian)
                    {
                        var singles = MemoryMarshal.Cast<byte, float>(vectorDataBytes.Span);
                        elements = singles.ToArray() as T[];
                    }
                    else
                    {
                        throw new NotSupportedException("Bson Vector data is not supported on Big Endian architecture yet.");
                    }
                    break;
                case BsonVectorDataType.Int8:
                case BsonVectorDataType.PackedBit:
                    elements = vectorDataBytes.ToArray() as T[];
                    break;
                default:
                    throw new NotSupportedException($"Vector data type {vectorDataType} is not supported");
            }

            return (elements, padding, vectorDataType);
        }

        public static (ReadOnlyMemory<byte> VectorDataBytes, byte Padding, BsonVectorDataType VectorDataType) ReadBsonVectorAsBytes(ReadOnlyMemory<byte> vectorData)
        {
            if (vectorData.Length < 2)
            {
                throw new InvalidOperationException($"Invalid {nameof(vectorData)} size {vectorData.Length}");
            }

            var vectorDataSpan = vectorData.Span;
            var vectorDataType = (BsonVectorDataType)vectorDataSpan[0];

            var paddingSizeBits = vectorDataSpan[1];
            if (paddingSizeBits > 7)
            {
                throw new InvalidOperationException($"Invalid padding size {paddingSizeBits}");
            }

            return (vectorData.Slice(2), paddingSizeBits, vectorDataType);
        }

        private static BsonVector<T> CreateBsonVector<T>(T[] elements, byte padding, BsonVectorDataType vectorDataType)
            where T : struct
        {
            switch (vectorDataType)
            {
                case BsonVectorDataType.Float32:
                    {
                        return new BsonVectorFloat32(AsTypeOrThrow<float>()) as BsonVector<T>;
                    }
                case BsonVectorDataType.Int8:
                    {
                        return new BsonVectorInt8(AsTypeOrThrow<byte>()) as BsonVector<T>;
                    }
                case BsonVectorDataType.PackedBit:
                    {
                        return new BsonVectorPackedBit(AsTypeOrThrow<byte>(), padding) as BsonVector<T>;
                    }
                default:
                    throw new NotSupportedException($"Vector data type {vectorDataType} is not supported");
            }

            R[] AsTypeOrThrow<R>()
            {
                if (elements is not R[] result)
                {
                    throw new InvalidOperationException($"Type {typeof(T)} is not supported with {vectorDataType} vector type.");
                }

                return result;
            }
        }

        public static void ValidateDataType<T>(BsonVectorDataType bsonVectorDataType)
        {
            var supportedType = bsonVectorDataType switch
            {
                BsonVectorDataType.Float32 => typeof(float),
                BsonVectorDataType.Int8 => typeof(byte),
                BsonVectorDataType.PackedBit => typeof(byte),
                _ => throw new ArgumentOutOfRangeException(nameof(bsonVectorDataType), bsonVectorDataType, "Unsupported vector datatype.")
            };

            if (supportedType != typeof(T))
            {
                throw new InvalidOperationException($"Type {typeof(T)} is not supported with {bsonVectorDataType} vector type. Supported types are [{supportedType}].");
            }
        }
    }
}
