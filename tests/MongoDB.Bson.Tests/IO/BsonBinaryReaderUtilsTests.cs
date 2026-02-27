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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO;

public class BsonBinaryReaderUtilsTests
{
    [Fact]
    public void CreateBinaryReader_should_create_ReadOnlyMemoryBsonReader_for_ReadOnlyMemoryBuffer()
    {
        var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };
        using var buffer = new ReadOnlyMemoryBuffer(bytes, new ReadOnlyMemorySlicer(bytes));
        using var reader = BsonBinaryReaderUtils.CreateBinaryReader(buffer, new());

        reader.Should().BeOfType<ReadOnlyMemoryBsonReader>();

        ValidateSliceType<ReadOnlyMemoryBuffer>(reader, bytes.Length);
    }

    [Fact]
    public void CreateBinaryReader_should_create_ReadOnlyMemoryBsonReader_for_SingleChunkBuffer()
    {
        var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };

        var buffer = new SingleChunkBuffer(new ByteArrayChunk(bytes), bytes.Length, true);
        var reader = BsonBinaryReaderUtils.CreateBinaryReader(buffer, new());

        reader.Should().BeOfType<ReadOnlyMemoryBsonReader>();

        ValidateSliceType<ByteBufferSlice>(reader, bytes.Length);
    }

    [Fact]
    public void CreateBinaryReader_should_create_ReadOnlyMemoryBsonReader_for_MultiChunkBuffer_with_single_chunk()
    {
        var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };

        var buffer = new MultiChunkBuffer([new ByteArrayChunk(bytes)], bytes.Length, true);
        var reader = BsonBinaryReaderUtils.CreateBinaryReader(buffer, new());

        reader.Should().BeOfType<ReadOnlyMemoryBsonReader>();

        ValidateSliceType<ByteBufferSlice>(reader, bytes.Length);
    }

    [Fact]
    public void CreateBinaryReader_should_create_BsonBinaryReader_for_MultiChunkBuffer_with_multiple_chunks()
    {
        var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };

        var buffer = new MultiChunkBuffer([new ByteArrayChunk(bytes.Take(10).ToArray()), new ByteArrayChunk(bytes.Skip(10).ToArray())], bytes.Length, true);
        var reader = BsonBinaryReaderUtils.CreateBinaryReader(buffer, new());

        reader.Should().BeOfType<BsonBinaryReader>();

        ValidateSliceType<ByteBufferSlice>(reader, bytes.Length);
    }

    private static void ValidateSliceType<T>(IBsonReader reader, int expectedSliceSize)
        where T : IByteBuffer
    {
        // Check the usage of the correct slicer by reading a slice
        var slice = reader.ReadRawBsonDocument();

        slice.Length.Should().Be(expectedSliceSize);
        slice.Should().BeOfType<T>();
    }
}
