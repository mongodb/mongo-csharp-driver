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
using System.Buffers.Binary;
using System.Runtime.InteropServices;

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
            if (BitConverter.IsLittleEndian)
            {
                var vectorDataBytes = MemoryMarshal.Cast<TItem, byte>(vectorData);
                byte[] result = [(byte)binaryVectorDataType, padding, .. vectorDataBytes];
                return result;
            }

            byte[] resultBytes;
            switch (binaryVectorDataType)
            {
                case BinaryVectorDataType.Float32:
                    int length = vectorData.Length * sizeof(float);
                    resultBytes = new byte[2 + length]; 				          // Allocate output buffer:
                    resultBytes[0] = (byte)binaryVectorDataType; 			      // - [0]: vector type
                    resultBytes[1] = padding;						              // - [1]: padding
                    var floatSpan = MemoryMarshal.Cast<TItem, float>(vectorData);	
                    Span<byte> floatOutput = resultBytes.AsSpan(2);			      // - [2...]: actual float data , skipping header
                    foreach (var value in floatSpan)
                    {
			            // Each float is 4 bytes - write in Big Endian format
                        BinaryPrimitives.WriteSingleBigEndian(floatOutput, value);
                        floatOutput = floatOutput.Slice(4); // advance to next 4-byte block
                    }
                    return resultBytes;

                case BinaryVectorDataType.Int8:
                case BinaryVectorDataType.PackedBit:
                    var vectorDataBytes = MemoryMarshal.Cast<TItem, byte>(vectorData);
                    resultBytes = new byte[2 + vectorDataBytes.Length];
                    resultBytes[0] = (byte)binaryVectorDataType;
                    resultBytes[1] = padding;
                    vectorDataBytes.CopyTo(resultBytes.AsSpan(2));
                    return resultBytes;

                default:
                    throw new NotSupportedException($"Binary vector serialization is not supported for {binaryVectorDataType} on Big Endian architecture yet.");
            }
        }
    }
}

