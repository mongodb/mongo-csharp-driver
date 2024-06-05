using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    internal static class PrimitivesArrayReader
    {
        private enum ConversionType
        {
            BoolToBool,
            DoubleToSingle,
            DoubleToDouble,
            Decimal128ToDecimal128,
            Int32ToInt8,
            Int32ToUInt8,
            Int32ToInt16,
            Int32ToUInt16,
            Int32ToChar,
            Int32ToInt32,
            Int32ToUInt32,
            Int64ToInt64,
            Int64ToUInt64
        }

        public static bool[] ReadBool(IBsonReader bsonReader) =>
            ReadBsonArray<bool>(bsonReader, ConversionType.BoolToBool);

        public static byte[] ReadInt8(IBsonReader bsonReader) =>
            ReadBsonArray<byte>(bsonReader, ConversionType.Int32ToInt8);

        public static sbyte[] ReadUInt8(IBsonReader bsonReader) =>
            ReadBsonArray<sbyte>(bsonReader, ConversionType.Int32ToUInt8);

        public static short[] ReadInt16(IBsonReader bsonReader) =>
            ReadBsonArray<short>(bsonReader, ConversionType.Int32ToInt16);

        public static ushort[] ReadUInt16(IBsonReader bsonReader) =>
            ReadBsonArray<ushort>(bsonReader, ConversionType.Int32ToUInt16);

        public static char[] ReadChar(IBsonReader bsonReader) =>
            ReadBsonArray<char>(bsonReader, ConversionType.Int32ToChar);

        public static int[] ReadInt32(IBsonReader bsonReader) =>
            ReadBsonArray<int>(bsonReader, ConversionType.Int32ToInt32);

        public static int[] ReadUInt32(IBsonReader bsonReader) =>
            ReadBsonArray<int>(bsonReader, ConversionType.Int32ToUInt32);

        public static float[] ReadSingles(IBsonReader bsonReader) =>
            ReadBsonArray<float>(bsonReader, ConversionType.DoubleToSingle);

        public static double[] ReadDoubles(IBsonReader bsonReader) =>
            ReadBsonArray<double>(bsonReader, ConversionType.DoubleToDouble);

        public static decimal[] ReadDecimal128(IBsonReader bsonReader) =>
            ReadBsonArray<decimal>(bsonReader, ConversionType.Decimal128ToDecimal128);

        public static long[] ReadInt64(IBsonReader bsonReader) =>
            ReadBsonArray<long>(bsonReader, ConversionType.Int64ToInt64);

        public static ulong[] ReadUInt64(IBsonReader bsonReader) =>
            ReadBsonArray<ulong>(bsonReader, ConversionType.Int64ToUInt64);

        private static T[] ReadBsonArray<T>(
            IBsonReader bsonReader,
            ConversionType conversionType)
        {
            var (bsonDataType, bsonDataSize) = GetBsonDataTypeAndSize(conversionType);

            var array = bsonReader.ReadRawBsonArray();
            using var buffer = ThreadStaticBuffer.RentBuffer(array.Length);

            var bytes = buffer.Bytes;
            array.GetBytes(0, bytes, 0, array.Length);

            var result = new List<T>();

            var index = 4; // 4 first bytes are array object size in bytes
            var maxIndex = array.Length - 1;

            while (index < maxIndex)
            {
                ValidateBsonType(bsonDataType);

                // Skip name
                while (bytes[index] != 0) { index++; };
                index++; // Skip string terminating 0

                T value = default;

                // Read next item
                switch (conversionType)
                {
                    case ConversionType.DoubleToSingle:
                        {
                            var v = (float)BitConverter.ToDouble(bytes, index);

                            value = Unsafe.As<float, T>(ref v);
                            break;
                        }
                    case ConversionType.DoubleToDouble:
                        {
                            var v = BitConverter.ToDouble(bytes, index);
                            value = Unsafe.As<double, T>(ref v);
                            break;
                        }
                    case ConversionType.Decimal128ToDecimal128:
                        {
                            var lowBits = (ulong)BitConverter.ToInt64(bytes, index);
                            var highBits = (ulong)BitConverter.ToInt64(bytes, index + 8);
                            var v = Decimal128.ToDecimal(Decimal128.FromIEEEBits(highBits, lowBits));

                            value = Unsafe.As<decimal, T>(ref v);
                            break;
                        }
                    case ConversionType.BoolToBool:
                        {
                            var v = bytes[index] != 0;

                            value = Unsafe.As<bool, T>(ref v);
                            break;
                        }
                    case ConversionType.Int32ToInt8:
                        {
                            var v = (sbyte)BitConverter.ToInt32(bytes, index);
                            value = Unsafe.As<sbyte, T>(ref v);

                            break;
                        }
                    case ConversionType.Int32ToUInt8:
                        {
                            var v = (byte)BitConverter.ToInt32(bytes, index);
                            value = Unsafe.As<byte, T>(ref v);
                            break;
                        }
                    case ConversionType.Int32ToInt16:
                        {
                            var v = (short)BitConverter.ToInt32(bytes, index);
                            value = Unsafe.As<short, T>(ref v);
                            break;
                        }
                    case ConversionType.Int32ToUInt16:
                        {
                            var v = (ushort)BitConverter.ToInt32(bytes, index);
                            value = Unsafe.As<ushort, T>(ref v);
                            break;
                        }
                    case ConversionType.Int32ToChar:
                        {
                            var v = BitConverter.ToChar(bytes, index);
                            value = Unsafe.As<char, T>(ref v);
                            break;
                        }
                    case ConversionType.Int32ToInt32:
                        {
                            var v = BitConverter.ToInt32(bytes, index);
                            value = Unsafe.As<int, T>(ref v);
                            break;
                        }
                    case ConversionType.Int32ToUInt32:
                        {
                            var v = BitConverter.ToUInt32(bytes, index);
                            value = Unsafe.As<uint, T>(ref v);
                            break;
                        }
                    case ConversionType.Int64ToInt64:
                        {
                            var v = BitConverter.ToInt64(bytes, index);
                            value = Unsafe.As<long, T>(ref v);
                            break;
                        }
                    case ConversionType.Int64ToUInt64:
                        {
                            var v = BitConverter.ToUInt64(bytes, index);
                            value = Unsafe.As<ulong, T>(ref v);
                            break;
                        }
                    default:
                        throw new InvalidOperationException();
                }

                result.Add(value);

                index += bsonDataSize;
            }

            ValidateBsonType(BsonType.EndOfDocument);

            return result.ToArray();

            void ValidateBsonType(BsonType bsonType)
            {
                if ((BsonType)bytes[index] != bsonType)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static (BsonType, int) GetBsonDataTypeAndSize(ConversionType conversionType) =>
            conversionType switch
            {
                ConversionType.BoolToBool => (BsonType.Boolean, 1),

                ConversionType.DoubleToSingle or
                ConversionType.DoubleToDouble => (BsonType.Double, 8),

                ConversionType.Int32ToUInt8 or
                ConversionType.Int32ToInt8 or
                ConversionType.Int32ToUInt16 or
                ConversionType.Int32ToInt16 or
                ConversionType.Int32ToInt32 or
                ConversionType.Int32ToChar or
                ConversionType.Int32ToUInt32 => (BsonType.Int32, 4),

                ConversionType.Int64ToInt64 or
                ConversionType.Int64ToUInt64 => (BsonType.Int64, 8),

                ConversionType.Decimal128ToDecimal128 => (BsonType.Decimal128, 16),

                _ => throw new NotSupportedException()
            };
    }
}
