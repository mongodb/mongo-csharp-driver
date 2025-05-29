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
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization
{
    internal static class BinaryVectorWriter
    {
        public static byte[] WriteToBytes<TItem>(BinaryVector<TItem> binaryVector)
            where TItem : struct
        {
            byte padding = 0;
            if (binaryVector is BinaryVectorPackedBit binaryVectorPackedBit)
            {
                padding = binaryVectorPackedBit.Padding;
            }

            return WriteToBytes(binaryVector.Data.Span, binaryVector.DataType, padding);
        }

        public static byte[] WriteToBytes<TItem>(ReadOnlySpan<TItem> vectorData, BinaryVectorDataType binaryVectorDataType, byte padding)
            where TItem : struct
        {
            byte[] resultBytes;

            switch (binaryVectorDataType)
            {
                case BinaryVectorDataType.Float32:
                    var length = vectorData.Length * sizeof(float);
                    resultBytes = new byte[2 + length];
                    resultBytes[0] = (byte)binaryVectorDataType;
                    resultBytes[1] = padding;

                    var floatSpan = MemoryMarshal.Cast<TItem, float>(vectorData);
                    Span<byte> floatOutput = resultBytes.AsSpan(2);

                    if (BitConverter.IsLittleEndian)
                    {
                        MemoryMarshal.Cast<float, byte>(floatSpan).CopyTo(floatOutput);
                    }
                    else
                    {
                        for (int i = 0; i < floatSpan.Length; i++)
                        {
                            BinaryPrimitivesCompat.WriteSingleLittleEndian(floatOutput.Slice(i * 4, 4), floatSpan[i]);
                        }
                    }

                    return resultBytes;

                case BinaryVectorDataType.Int8:
                case BinaryVectorDataType.PackedBit:
                    var vectorDataBytes = MemoryMarshal.Cast<TItem, byte>(vectorData);
                    return [(byte)binaryVectorDataType, padding, .. vectorDataBytes];

                default:
                    throw new NotSupportedException($"Binary vector serialization is not supported for {binaryVectorDataType}.");
            }
        }
    }
}
