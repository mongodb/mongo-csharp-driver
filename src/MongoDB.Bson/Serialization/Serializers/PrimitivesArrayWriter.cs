using System;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    internal static class PrimitivesArrayWriter
    {
        public static void WriteBool(IBsonWriter bsonWriter, Span<bool> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteBoolean(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteInt8(IBsonWriter bsonWriter, Span<sbyte> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteUInt8(IBsonWriter bsonWriter, Span<byte> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteInt16(IBsonWriter bsonWriter, Span<short> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteUInt16(IBsonWriter bsonWriter, Span<ushort> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteChar(IBsonWriter bsonWriter, Span<char> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteInt32(IBsonWriter bsonWriter, Span<int> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteUInt32(IBsonWriter bsonWriter, Span<uint> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt32((int)span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteInt64(IBsonWriter bsonWriter, Span<long> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt64(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteUInt64(IBsonWriter bsonWriter, Span<ulong> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteInt64((long)span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteSingles(IBsonWriter bsonWriter, Span<float> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteDouble(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteDoubles(IBsonWriter bsonWriter, Span<double> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteDouble(span[i]);
            }
            bsonWriter.WriteEndArray();
        }

        public static void WriteDecimal128(IBsonWriter bsonWriter, Span<decimal> span)
        {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < span.Length; i++)
            {
                bsonWriter.WriteDecimal128(span[i]);
            }
            bsonWriter.WriteEndArray();
        }
    }
}
