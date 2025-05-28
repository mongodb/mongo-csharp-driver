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

namespace MongoDB.Bson.IO
{
    // this class implements a few BinaryPrimitives methods we need that don't exist in some of our target frameworks
    // this class can be deleted once all our targeted frameworks have the needed methods
    internal static class BinaryPrimitivesCompat
    {
        public static double ReadDoubleLittleEndian(ReadOnlySpan<byte> source)
        {
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(source));
        }

        public static void WriteDoubleLittleEndian(Span<byte> destination, double value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(destination, BitConverter.DoubleToInt64Bits(value));
        }
        public static float ReadSingleLittleEndian(ReadOnlySpan<byte> source)
        {
            if (source.Length < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Source span is too small to contain a float.");
            }

            int intValue =
                source[0] |
                (source[1] << 8) |
                (source[2] << 16) |
                (source[3] << 24);

            return BitConverter.Int32BitsToSingle(intValue);
        }

        public static void WriteSingleLittleEndian(Span<byte> destination, float value)
        {
            if (destination.Length < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), "Destination span is too small to hold a float.");
            }

            int intValue = BitConverter.SingleToInt32Bits(value);
            destination[0] = (byte)(intValue);
            destination[1] = (byte)(intValue >> 8);
            destination[2] = (byte)(intValue >> 16);
            destination[3] = (byte)(intValue >> 24);
        }

    }
}
