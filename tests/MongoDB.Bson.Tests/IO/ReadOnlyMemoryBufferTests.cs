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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO;

public class ReadOnlyMemoryBufferTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void AccessBackingBytes_should_return_expected_result_for_length(int length, int expectedCount)
    {
        var bytes = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
        var subject = CreateSubject(bytes);

        var result = subject.AccessBackingBytes(0);

        result.Array.Should().BeEquivalentTo(bytes);
        result.Offset.Should().Be(0);
        result.Count.Should().Be(expectedCount);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    public void AccessBackingBytes_should_return_expected_result_for_position(int position, int expectedCount)
    {
        var bytes = new byte[2];
        var subject = CreateSubject(bytes);

        var result = subject.AccessBackingBytes(position);

        result.Array.Should().BeEquivalentTo(bytes.Skip(position).ToArray());
        result.Offset.Should().Be(0);
        result.Count.Should().Be(expectedCount);
    }

    [Theory]
    [ParameterAttributeData]
    public void AccessBackingBytes_should_throw_when_position_is_invalid([Values(-1, 3)]int position)
    {
        var subject = CreateSubject(2);

        Action action = () => subject.AccessBackingBytes(position);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void Capacity_get_should_return_expected_result(int length)
    {
        var subject = CreateSubject(length);

        var result = subject.Capacity;

        result.Should().Be(length);
    }

    [Fact]
    public void Clear_should_throw()
    {
        var bytes = new byte[] { 1, 2 };
        var subject = CreateSubject(bytes);

        var e = Record.Exception(() => subject.Clear(0, 0));
        e.Should().BeOfType<InvalidOperationException>();
        e.Message.Should().Contain("is not writable.");
    }

    [Theory]
    [ParameterAttributeData]
    public void constructor_should_initialize_subject([Values(1, 2)]int length)
    {
        var bytes = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
        var subject = CreateSubject(bytes);

        subject.IsReadOnly.Should().Be(true);
        subject.Length.Should().Be(length);
        subject.Memory.ToArray().ShouldBeEquivalentTo(bytes);
    }

    [Fact]
    public void Dispose_can_be_called_multiple_times()
    {
        var subject = CreateSubject(2);

        subject.Dispose();
        subject.Dispose();
    }

    [Fact]
    public void EnsureCapacity_should_throw()
    {
        var subject = CreateSubject(2);

        ValidateWritableException(() => subject.EnsureCapacity(1));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void GetByte_should_return_expected_result(int position, byte expectedResult)
    {
        var bytes = new byte[] { 1, 2, 3 };
        var subject = CreateSubject(bytes);

        var result = subject.GetByte(position);

        result.Should().Be(expectedResult);
    }

    [Theory]
    [ParameterAttributeData]
    public void GetByte_should_throw_when_position_is_invalid(
        [Values(-1, 3)]
        int position)
    {
        var subject = CreateSubject(2);

        Action action = () => subject.GetByte(position);

        action.ShouldThrow<IndexOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, new byte[] { 0, 0 })]
    [InlineData(1, new byte[] { 1, 0 })]
    [InlineData(2, new byte[] { 1, 2 })]
    public void GetBytes_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
    {
        var bytes = new byte[] { 1, 2 };
        var subject = CreateSubject(bytes);
        var destination = new byte[2];

        subject.GetBytes(0, destination, 0, count);

        destination.Should().Equal(expectedBytes);
    }

    [Theory]
    [InlineData(1, new byte[] { 0, 1, 2, 0 })]
    [InlineData(2, new byte[] { 0, 0, 1, 2 })]
    public void GetBytes_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
    {
        var bytes = new byte[] { 1, 2 };
        var subject = CreateSubject(bytes);
        var destination = new byte[4];

        subject.GetBytes(0, destination, offset, 2);

        destination.Should().Equal(expectedBytes);
    }

    [Theory]
    [InlineData(1, new byte[] { 2, 3 })]
    [InlineData(2, new byte[] { 3, 4 })]
    public void GetBytes_should_have_expected_effect_for_position(int position, byte[] expectedBytes)
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var subject = CreateSubject(bytes);
        var destination = new byte[2];

        subject.GetBytes(position, destination, 0, 2);
        destination.Should().Equal(expectedBytes);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void GetBytes_should_throw_when_count_is_invalid_for_buffer(int position, int count)
    {
        var subject = CreateSubject(2);
        var destination = new byte[3];

        Action action = () => subject.GetBytes(position, destination, 0, count);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void GetBytes_should_throw_when_count_is_invalid_for_destination(int offset, int count)
    {
        var subject = CreateSubject([1, 2, 3]);
        var destination = new byte[2];

        Action action = () => subject.GetBytes(0, destination, offset, count);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetBytes_should_throw_when_destination_is_null()
    {
        var subject = CreateSubject([1, 2]);

        Action action = () => subject.GetBytes(0, null, 0, 2);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [ParameterAttributeData]
    public void GetBytes_should_throw_when_offset_is_invalid([Values(-1, 3)]int offset)
    {
        var subject = CreateSubject([1, 2, 3, 4]);
        var destination = new byte[2];

        Action action = () => subject.GetBytes(0, destination, offset, 0);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [ParameterAttributeData]
    public void GetBytes_should_throw_when_position_is_invalid(
        [Values(-1, 3)]
        int position)
    {
        var subject = CreateSubject([1, 2]);
        var destination = new byte[0];

        Action action = () => subject.GetBytes(position, destination, 0, 0);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    public void GetSlice_should_return_expected_result(int position, int length)
    {
        var bytes = new byte[2];
        var subject = CreateSubject(bytes);

        var result = subject.GetSlice(position, length);

        result.AccessBackingBytes(0).Offset.Should().Be(0);
        result.AccessBackingBytes(0).Count.Should().Be(length);
    }

    [Fact]
    public void GetSlice_should_return_slice_that_does_not_dispose_subject_when_slice_is_disposed()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var subject = CreateSubject(bytes);
        var slice = subject.GetSlice(1, 1);

        slice.Dispose();

        subject.GetByte(1).Should().Be(2);
    }

    [Fact]
    public void GetSlice_should_return_slice_that_is_not_disposed_when_subject_is_disposed()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var subject = CreateSubject(bytes);
        var slice = subject.GetSlice(1, 1);

        subject.Dispose();

        slice.GetByte(0).Should().Be(2);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void GetSlice_should_throw_when_length_is_invalid(int position, int length)
    {
        var subject = CreateSubject(2);

        Action action = () => subject.GetSlice(position, length);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [ParameterAttributeData]
    public void GetSlice_should_throw_when_position_is_invalid([Values(-1, 3)]int position)
    {
        var subject = CreateSubject(2);

        Action action = () => subject.GetSlice(position, 0);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Length_set_should_throw()
    {
        var subject = CreateDisposedSubject();

        ValidateWritableException(() => subject.Length = 0);
    }

    [Fact]
    public void MakeReadOnly_should_do_nothing()
    {
        var subject = CreateSubject(2);
        subject.MakeReadOnly();
    }

    [Fact]
    public void SetBytes_should_throw()
    {
        var subject = CreateSubject(2);
        var source = new byte[0];

        ValidateWritableException(() => subject.SetBytes(0, source, 0, 0));
    }

    // helper methods
    private ReadOnlyMemoryBuffer CreateDisposedSubject()
    {
        var subject = CreateSubject(2);
        subject.Dispose();
        return subject;
    }

    private ReadOnlyMemoryBuffer CreateSubject(int length) => new(Enumerable.Range(0, length).Select(i => (byte)i).ToArray());

    private ReadOnlyMemoryBuffer CreateSubject(byte[] bytes) => new(bytes);

    private void ValidateWritableException(Action action)
    {
        var e = Record.Exception(action);
        e.Should().BeOfType<InvalidOperationException>();
        e.Message.Should().Contain("is not writable.");
    }
}
