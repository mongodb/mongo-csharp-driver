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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO;

public class RawBsonSliceHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void GetValidatedLength_should_return_declared_length(int trailingByteCount)
    {
        var bytes = new BsonDocument("x", 1).ToBson();
        var slice = new ByteArrayBuffer(bytes.Concat(new byte[trailingByteCount]).ToArray(), isReadOnly: true);

        var result = RawBsonSliceHelper.GetValidatedLength(slice, "slice");

        result.Should().Be(bytes.Length);
    }

    [Fact]
    public void GetValidatedLength_should_read_prefix_that_spans_chunks()
    {
        var bytes = new BsonDocument("x", 1).ToBson();
        var chunks = new IBsonChunk[]
        {
            new ByteArrayChunk(bytes.Take(2).ToArray()),
            new ByteArrayChunk(bytes.Skip(2).ToArray())
        };
        var slice = new MultiChunkBuffer(chunks, bytes.Length, isReadOnly: true);

        var result = RawBsonSliceHelper.GetValidatedLength(slice, "slice");

        result.Should().Be(bytes.Length);
    }

    [Fact]
    public void GetValidatedLength_should_throw_when_slice_is_null()
    {
        var exception = Record.Exception(() => RawBsonSliceHelper.GetValidatedLength(null, "slice"));

        exception.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("slice");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void GetValidatedLength_should_throw_when_slice_is_too_short(int sliceLength)
    {
        var slice = new ByteArrayBuffer(new byte[sliceLength], isReadOnly: true);

        var exception = Record.Exception(() => RawBsonSliceHelper.GetValidatedLength(slice, "slice"));

        exception.Should().BeOfType<ArgumentException>().Which.ParamName.Should().Be("slice");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(13)]
    [InlineData(int.MaxValue)]
    public void GetValidatedLength_should_throw_when_declared_length_is_invalid(int declaredLength)
    {
        var bytes = new BsonDocument("x", 1).ToBson();
        BitConverter.GetBytes(declaredLength).CopyTo(bytes, 0);
        var slice = new ByteArrayBuffer(bytes, isReadOnly: true);

        var exception = Record.Exception(() => RawBsonSliceHelper.GetValidatedLength(slice, "slice"));

        exception.Should().BeOfType<ArgumentException>().Which.ParamName.Should().Be("slice");
    }

    [Fact]
    public void GetValidatedLength_should_throw_when_terminating_null_is_missing()
    {
        var bytes = new BsonDocument("x", 1).ToBson();
        bytes[bytes.Length - 1] = 1;
        var slice = new ByteArrayBuffer(bytes, isReadOnly: true);

        var exception = Record.Exception(() => RawBsonSliceHelper.GetValidatedLength(slice, "slice"));

        exception.Should().BeOfType<ArgumentException>().Which.ParamName.Should().Be("slice");
    }
}
