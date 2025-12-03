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
using System.Runtime.InteropServices;

namespace MongoDB.Bson.IO;

internal sealed class ReadOnlyMemoryBuffer : IByteBuffer
{
    private readonly ReadOnlyMemory<byte> _memory;
    private readonly IByteBufferSlicer _bufferSlicer;

    public ReadOnlyMemoryBuffer(ReadOnlyMemory<byte> memory, IByteBufferSlicer bufferSlicer)
    {
        if (bufferSlicer == null)
        {
            throw new ArgumentNullException(nameof(bufferSlicer));
        }

        _memory = memory;
        _bufferSlicer = bufferSlicer;
    }

    /// <inheritdoc/>
    public int Capacity => _memory.Length;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public int Length
    {
        get => _memory.Length;
        set => ThrowNotWritableException();
    }

    public ReadOnlyMemory<byte> Memory => _memory;

    /// <inheritdoc/>
    public ArraySegment<byte> AccessBackingBytes(int position)
    {
        var slice = _memory.Slice(position);
         if (!MemoryMarshal.TryGetArray(slice, out var segment))
         {
             segment = new(slice.ToArray());
         }

        return segment;
    }

    /// <inheritdoc/>
    public void Clear(int position, int count) =>
        ThrowNotWritableException();

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public void EnsureCapacity(int minimumCapacity) =>
        ThrowNotWritableException();

    /// <inheritdoc/>
    public byte GetByte(int position) =>
        _memory.Span[position];

    /// <inheritdoc/>
    public void GetBytes(int position, byte[] destination, int offset, int count) =>
        _memory.Span.Slice(position, count).CopyTo(new Span<byte>(destination, offset, count));

    /// <inheritdoc/>
    public IByteBuffer GetSlice(int position, int length) =>
        _bufferSlicer.GetSlice(position, length);

    /// <inheritdoc/>
    public void MakeReadOnly()
    {
    }

    /// <inheritdoc/>
    public void SetByte(int position, byte value) =>
        ThrowNotWritableException();

    /// <inheritdoc/>
    public void SetBytes(int position, byte[] source, int offset, int count) =>
        ThrowNotWritableException();

    private static void ThrowNotWritableException() =>
        throw new InvalidOperationException($"{nameof(ReadOnlyMemoryBuffer)} is not writable.");
}
