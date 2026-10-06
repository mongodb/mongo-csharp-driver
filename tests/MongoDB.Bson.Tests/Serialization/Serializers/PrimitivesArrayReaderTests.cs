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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers;

public class PrimitivesArrayReaderTests
{
    [Fact]
    public void ReadBool_should_read_valid_array()
    {
        var result = Read([true, false, true], PrimitivesArrayReader.ReadBool);

        result.Should().Equal(true, false, true);
    }

    [Fact]
    public void ReadInt32_should_read_valid_array()
    {
        var result = Read([1, -2, int.MaxValue, int.MinValue], PrimitivesArrayReader.ReadInt32);

        result.Should().Equal(1, -2, int.MaxValue, int.MinValue);
    }

    [Fact]
    public void ReadInt64_should_read_valid_array()
    {
        var result = Read([1L, -2L, long.MaxValue, long.MinValue], PrimitivesArrayReader.ReadInt64);

        result.Should().Equal(1L, -2L, long.MaxValue, long.MinValue);
    }

    [Fact]
    public void ReadDoubles_should_read_valid_array()
    {
        var result = Read([1.5, -2.25, 0.0], PrimitivesArrayReader.ReadDoubles);

        result.Should().Equal(1.5, -2.25, 0.0);
    }

    [Fact]
    public void ReadDecimal128_should_read_valid_array()
    {
        var array = new BsonArray { new BsonDecimal128(1.5m), new BsonDecimal128(-2.25m) };

        var result = Read(array, PrimitivesArrayReader.ReadDecimal128);

        result.Should().Equal(1.5m, -2.25m);
    }

    [Fact]
    public void ReadInt8_should_read_valid_array()
    {
        var result = Read(new BsonArray(new[] { 0, 1, 127, -1, -128 }), PrimitivesArrayReader.ReadInt8);

        result.Should().Equal(0, 1, 127, -1, -128);
    }

    [Fact]
    public void ReadUInt8_should_read_valid_array()
    {
        var result = Read(new BsonArray(new[] { 0, 1, 127, 128, 255 }), PrimitivesArrayReader.ReadUInt8);

        result.Should().Equal(0, 1, 127, 128, 255);
    }

    [Fact]
    public void ReadInt16_should_read_valid_array()
    {
        var result = Read(new BsonArray(new[] { 0, 1, -2, 32767, -32768 }), PrimitivesArrayReader.ReadInt16);

        result.Should().Equal(0, 1, -2, short.MaxValue, short.MinValue);
    }

    [Fact]
    public void ReadUInt16_should_read_valid_array()
    {
        var result = Read(new BsonArray(new[] { 0, 1, 32768, 65535 }), PrimitivesArrayReader.ReadUInt16);

        result.Should().Equal(0, 1, 32768, ushort.MaxValue);
    }

    [Fact]
    public void ReadChar_should_read_valid_array()
    {
        // 0x20AC is the euro sign; 0xFFFF exercises the full 16 bit range
        var result = Read(new BsonArray(new[] { 65, 122, 0x20AC, 0xFFFF }), PrimitivesArrayReader.ReadChar);

        result.Should().Equal('A', 'z', (char)0x20AC, char.MaxValue);
    }

    [Fact]
    public void ReadUInt32_should_read_valid_array()
    {
        // simulate uint serialization with int, as uint is serialized as long
        var result = Read(new BsonArray(new[] { 0, 1, unchecked((int)uint.MaxValue), (int)uint.MinValue }), PrimitivesArrayReader.ReadUInt32);

        result.Should().Equal(0, 1, uint.MaxValue, uint.MinValue);
    }

    [Fact]
    public void ReadUInt64_should_read_valid_array()
    {
        var result = Read(new BsonArray(new[] { 0L, 1L, -1L, long.MaxValue, long.MinValue }), PrimitivesArrayReader.ReadUInt64);

        result.Should().Equal(0UL, 1UL, ulong.MaxValue, (ulong)long.MaxValue, unchecked((ulong)long.MinValue));
    }

    [Fact]
    public void ReadSingles_should_read_valid_array()
    {
        // Elements arrive as 8 byte doubles and are narrowed to float: 0.1 loses precision and
        // double.MaxValue saturates to infinity.
        var result = Read(new BsonArray(new[] { 1.5, -2.25, 0.0, 0.1, double.MaxValue }), PrimitivesArrayReader.ReadSingles);

        result.Should().Equal(1.5f, -2.25f, 0f, 0.1f, float.PositiveInfinity);
    }

    [Fact]
    public void ReadInt32_should_read_empty_array()
    {
        var result = Read(new BsonArray(), PrimitivesArrayReader.ReadInt32);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadInt32_should_read_array_with_multidigit_element_names()
    {
        // element names "0".."149" exercise the name-skip loop with 1-, 2- and 3-byte names
        var expected = Enumerable.Range(0, 150).ToArray();

        var result = Read(new BsonArray(expected), PrimitivesArrayReader.ReadInt32);

        result.Should().Equal(expected);
    }

    [Fact]
    public void ReadInt32_should_throw_when_element_type_is_not_the_expected_one()
    {
        var array = new BsonArray { 1, "not an int32" };

        var exception = Record.Exception(() => Read(array, PrimitivesArrayReader.ReadInt32));
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void ReadInt32_should_throw_when_last_element_value_is_truncated()
    {
        // The stale bytes planted below sit immediately after the array's 10 bytes. Before the
        // fix the truncated Int32 read absorbs stale byte 10, the end-of-document check is
        // satisfied by stale byte 11, and the leaked value is returned to the caller.
        PrimeThreadStaticBuffer(buffer =>
        {
            buffer[10] = 0x53; // 'S' - stands in for a byte of another document's content
            buffer[11] = 0x00; // makes the end-of-document check at index 11 pass silently
        });

        var malformedArray = new byte[]
        {
            0x0A, 0x00, 0x00, 0x00, // declared array length: 10 bytes
            0x10,                   // element type: Int32
            (byte)'0', 0x00,        // element name: "0"
            0x00, 0x00, 0x00        // value: only 3 of the 4 Int32 bytes are present
        };

        var exception = Record.Exception(() => Read(malformedArray, PrimitivesArrayReader.ReadInt32));
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReadDecimal128_should_throw_when_last_element_value_is_truncated()
    {
        // Decimal128 is the widest element the reader handles: its value is read as two 8 byte
        // halves, so it can reach the furthest past the end of a truncated array. Start from a
        // well formed single element array - its last byte is the array's end-of-document
        // marker, and the byte before it is the most significant byte of the 16 byte value.
        var completeArray = SerializeArray(new BsonArray { new BsonDecimal128(123.45m) });
        var truncatedLength = completeArray.Length - 2;

        // Plant that one value byte in the slack of the thread static buffer, standing in for a
        // byte another document left behind. Before the fix the read of the high half absorbs it
        // and the caller gets a fully reconstructed 123.45; the rest of the buffer stays zeroed,
        // so the end-of-document check that follows passes silently as well.
        PrimeThreadStaticBuffer(buffer => buffer[truncatedLength] = completeArray[truncatedLength]);

        // Drop the end-of-document byte and the final value byte, and shrink the declared length
        // to match, so the array ends one byte into the high half of the Decimal128 value.
        var malformedArray = completeArray.Take(truncatedLength).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(malformedArray, truncatedLength);

        var exception = Record.Exception(() => Read(malformedArray, PrimitivesArrayReader.ReadDecimal128));
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReadInt32_should_throw_when_element_name_is_not_terminated()
    {
        // All slack bytes are non-zero, so the unbounded name-skip loop runs off the end of the
        // buffer and throws IndexOutOfRangeException before the fix.
        PrimeThreadStaticBuffer(buffer =>
        {
            for (var i = 8; i < buffer.Length; i++)
            {
                buffer[i] = 0xFF;
            }
        });

        var malformedArray = new byte[]
        {
            0x08, 0x00, 0x00, 0x00,        // declared array length: 8 bytes
            0x10,                          // element type: Int32
            (byte)'0', (byte)'1', (byte)'2' // element name, never terminated
        };

        var exception = Record.Exception(() => Read(malformedArray, PrimitivesArrayReader.ReadInt32));

        exception.Should().BeOfType<IndexOutOfRangeException>();
    }

    [Fact]
    public void ReadBool_should_throw_when_array_is_missing_its_end_of_document_byte()
    {
        PrimeThreadStaticBuffer(_ => { }); // Clear the buffer

        var malformedArray = new byte[]
        {
            0x08, 0x00, 0x00, 0x00, // declared array length: 8 bytes
            0x08,                   // element type: Boolean
            (byte)'0', 0x00,        // element name: "0"
            0x01                    // value: true, and no end-of-document byte follows
        };

        var exception = Record.Exception(() => Read(malformedArray, PrimitivesArrayReader.ReadBool));
        exception.Should().BeOfType<IndexOutOfRangeException>();
    }

    private static void PrimeThreadStaticBuffer(Action<byte[]> fill)
    {
        using var rented = ThreadStaticBuffer.RentBuffer(256);
        Array.Clear(rented.Bytes, 0, rented.Bytes.Length);
        fill(rented.Bytes);
    }

    private static T[] Read<T>(BsonArray array, Func<IBsonReader, T[]> read) =>
        Read(SerializeArray(array), read);

    private static T[] Read<T>(byte[] arrayBytes, Func<IBsonReader, T[]> read)
    {
        var documentBytes = WrapInDocument(arrayBytes);

        using var stream = new MemoryStream(documentBytes);
        using var reader = new BsonBinaryReader(stream);

        reader.ReadStartDocument();
        reader.ReadBsonType();
        reader.ReadName();

        return read(reader);
    }

    private static byte[] SerializeArray(BsonArray array)
    {
        var documentBytes = new BsonDocument("a", array).ToBson();
        var arrayLength = documentBytes.Length - 4 - 3 - 1;

        return documentBytes.Skip(7).Take(arrayLength).ToArray();
    }

    private static byte[] WrapInDocument(byte[] arrayBytes)
    {
        var documentLength = 4 + 1 + 2 + arrayBytes.Length + 1;

        return
        [
            (byte)documentLength,
            (byte)(documentLength >> 8),
            (byte)(documentLength >> 16),
            (byte)(documentLength >> 24),
            (byte)BsonType.Array,
            (byte)'a',
            0x00,
            .. arrayBytes,
            0x00
        ];
    }
}
