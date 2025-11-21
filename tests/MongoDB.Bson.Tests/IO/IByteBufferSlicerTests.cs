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

using FluentAssertions;
using MongoDB.Bson.IO;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO;

public class ByteBufferSlicerTests
{
    [Fact]
    public void ByteBufferSlicer_should_use_underlying_buffer_slice()
    {
        using var byteBufferExpected = new ByteArrayBuffer([1, 2, 3]);

        var byteBufferMock = new Mock<IByteBuffer>();
        byteBufferMock.Setup(b => b.GetSlice(0, 3)).Returns(byteBufferExpected);

        var slicer = new ByteBufferSlicer(byteBufferMock.Object);

        using var slice = slicer.GetSlice(0, 3);

        slice.Should().Be(byteBufferExpected);
        byteBufferMock.Verify(b => b.GetSlice(0, 3), Times.Once);
    }

    [Fact]
    public void ReadOnlyMemorySlicer_should_return_ReadOnlyMemoryBuffer()
    {
        byte[] bytes = [1, 2, 3];

        var slicer = new ReadOnlyMemorySlicer(bytes);

        using var slice = slicer.GetSlice(0, 1);

        var buffer = slice.Should().BeOfType<ReadOnlyMemoryBuffer>().Subject;
        buffer.Memory.Length.Should().Be(1);
        buffer.Memory.Span[0].Should().Be(1);
    }
}
