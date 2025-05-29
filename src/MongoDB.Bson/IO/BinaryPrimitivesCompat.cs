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
#if NET6_0_OR_GREATER
            return BinaryPrimitives.ReadSingleLittleEndian(source);
#else
            if (source.Length < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Source span is too small to contain a float.");
            }

            // Manually construct a 32-bit integer from 4 bytes in Little Endian order.
            // BSON mandates that all multibyte values-including float32-must be encoded
            // using Little Endian byte order, regardless of the system architecture.
            //
            // This method ensures platform-agnostic behavior by explicitly assembling
            // the bytes in the correct order, rather than relying on the system's native endianness.
            //
            // Given a byte sequence [a, b, c, d], representing a float encoded in Little Endian,
            // the expression below constructs the 32-bit integer as:
            //     intValue = a + (b << 8) + (c << 16) + (d << 24)
            //
            // This preserves the intended bit pattern when converting back to float using
            // BitConverter.Int32BitsToSingle.
            //
            // Example:
            //   A float value of 1.0f is represented in IEEE-754 binary32 format as:
            //       [0x00, 0x00, 0x80, 0x3F]  (Little Endian)
            //   On a Big Endian system, naive interpretation would yield an incorrect value,
            //   but this method assembles the int as:
            //       0x00 + (0x00 << 8) + (0x80 << 16) + (0x3F << 24) = 0x3F800000,
            //   which correctly maps to 1.0f.
            //
            // This guarantees BSON-compliant serialization across all platforms.
            int intValue =
                source[0] |
                (source[1] << 8) |
                (source[2] << 16) |
                (source[3] << 24);

            // This struct emulates BitConverter.Int32BitsToSingle for platforms like net472.
            return new FloatIntUnion { IntValue = intValue }.FloatValue;
#endif
        }

        public static void WriteSingleLittleEndian(Span<byte> destination, float value)
        {
#if NET6_0_OR_GREATER
            BinaryPrimitives.WriteSingleLittleEndian(destination, value);
#else
            if (destination.Length < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), "Destination span is too small to hold a float.");
            }

            // This struct emulates BitConverter.SingleToInt32Bits for platforms like net472.
            int intValue = new FloatIntUnion { FloatValue = value }.IntValue;

            destination[0] = (byte)(intValue);
            destination[1] = (byte)(intValue >> 8);
            destination[2] = (byte)(intValue >> 16);
            destination[3] = (byte)(intValue >> 24);
#endif
        }

        // This layout trick allows safely reinterpreting float as int and vice versa.
        // It ensures identical memory layout for both fields, used for low-level bit conversion
        // in environments like net472 which lack BitConverter.SingleToInt32Bits and its inverse.
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)] public float FloatValue;
            [FieldOffset(0)] public int IntValue;
        }
    }
}
