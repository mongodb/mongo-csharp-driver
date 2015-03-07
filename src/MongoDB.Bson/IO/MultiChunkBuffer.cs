/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// An IByteBuffer that is backed by multiple chunks.
    /// </summary>
    public sealed class MultiChunkBuffer : IByteBuffer
    {
        // private fields
        private int _capacity;
        private int _chunkIndex;
        private List<IBsonChunk> _chunks;
        private readonly IBsonChunkSource _chunkSource;
        private bool _disposed;
        private bool _isReadOnly;
        private int _length;
        private List<int> _positions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChunkBuffer"/> class.
        /// </summary>
        /// <param name="chunkSource">The chunk pool.</param>
        /// <exception cref="System.ArgumentNullException">chunkPool</exception>
        public MultiChunkBuffer(IBsonChunkSource chunkSource)
        {
            if (chunkSource == null)
            {
                throw new ArgumentNullException("chunkSource");
            }

            _chunks = new List<IBsonChunk>();
            _chunkSource = chunkSource;
            _length = 0;
            _positions = new List<int> { 0 };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChunkBuffer"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="chunks">The chunks.</param>
        /// <param name="isReadOnly">Whether the buffer is read only.</param>
        /// <exception cref="System.ArgumentNullException">chunks</exception>
        internal MultiChunkBuffer(int length, IEnumerable<IBsonChunk> chunks, bool isReadOnly = false)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException("chunks");
            }

            _length = length;
            _chunks = new List<IBsonChunk>(chunks);
            _isReadOnly = isReadOnly;

            _positions = new List<int> { 0 };
            foreach (var chunk in _chunks)
            {
                _capacity += chunk.Bytes.Count;
                _positions.Add(_capacity);
            }

            if (length < 0 || length > _capacity)
            {
                throw new ArgumentOutOfRangeException("length");
            }
        }

        // public properties
        /// <inheritdoc/>
        public int Capacity
        {
            get
            {
                ThrowIfDisposed();
                return _isReadOnly ? _length : _capacity;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get
            {
                ThrowIfDisposed();
                return _isReadOnly;
            }
        }

        /// <inheritdoc/>
        public int Length
        {
            get
            {
                ThrowIfDisposed();
                return _length;
            }
            set
            {
                ThrowIfDisposed();
                if (value < 0 || value > _capacity)
                {
                    throw new ArgumentOutOfRangeException("Length");
                }
                EnsureIsWritable();

                _length = value;
            }
        }

        // public methods
        /// <inheritdoc/>
        public ArraySegment<byte> AccessBackingBytes(int position)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }

            var chunkIndex = GetChunkIndex(position);
            var chunkOffset = position - _positions[chunkIndex];
            var segment = _chunks[chunkIndex].Bytes;
            var chunkRemaining = segment.Count - chunkOffset;
            return new ArraySegment<byte>(segment.Array, segment.Offset + chunkOffset, chunkRemaining);
        }

        /// <inheritdoc/>
        public void Clear(int position, int count)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            if (position + count > _length)
            {
                throw new ArgumentException("Count extends beyond the end of the buffer.", "count");
            }

            var chunkIndex = GetChunkIndex(position);
            var chunkOffset = position - _positions[chunkIndex];
            while (count > 0)
            {
                var segment = _chunks[chunkIndex].Bytes;
                var chunkRemaining = segment.Count - chunkOffset;
                var partialCount = Math.Min(count, chunkRemaining);
                Array.Clear(segment.Array, segment.Offset + chunkOffset, partialCount);
                chunkIndex += 1;
                chunkOffset = 0;
                count -= partialCount;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var chunk in _chunks)
                {
                    chunk.Dispose();
                }
                _chunks = null;
                _positions = null;
            }
        }

        /// <inheritdoc/>
        public void EnsureCapacity(int requiredCapacity)
        {
            if (_capacity < requiredCapacity)
            {
                ExpandCapacity(requiredCapacity);
            }
        }

        /// <inheritdoc/>
        public IByteBuffer GetSlice(int position, int length)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            if (length < 0)
            {
                throw new ArgumentException("Length is negative.", "length");
            }
            if (position + length > _length)
            {
                throw new ArgumentException("Length extends past the end of the buffer.", "length");
            }
            EnsureIsReadOnly();

            var firstChunkIndex = GetChunkIndex(position);
            var lastChunkIndex = GetChunkIndex(position + length - 1);

            IByteBuffer forkedBuffer;
            if (firstChunkIndex == lastChunkIndex)
            {
                var forkedChunk = _chunks[firstChunkIndex].Fork();
                forkedBuffer = new SingleChunkBuffer(forkedChunk, forkedChunk.Bytes.Count, isReadOnly: true);
            }
            else
            {
                var forkedChunks = _chunks.Skip(firstChunkIndex).Take(lastChunkIndex - firstChunkIndex + 1).Select(c => c.Fork());
                var forkedBufferLength = _positions[lastChunkIndex + 1] - _positions[firstChunkIndex];
                forkedBuffer = new MultiChunkBuffer(forkedBufferLength, forkedChunks, isReadOnly: true);
            }

            var offset = position - _positions[firstChunkIndex];
            return new ByteBufferSlice(forkedBuffer, offset, length);
        }

        /// <inheritdoc/>
        public void MakeReadOnly()
        {
            ThrowIfDisposed();
            _isReadOnly = true;
        }

        /// <inheritdoc/>
        public byte GetByte(int position)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }

            var chunkIndex = GetChunkIndex(position);
            var chunkOffset = position - _positions[chunkIndex];
            var segment = _chunks[chunkIndex].Bytes;
            return segment.Array[segment.Offset + chunkOffset];
        }

        /// <inheritdoc/>
        public void GetBytes(int position, byte[] destination, int offset, int count)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (offset < 0)
            {
                throw new ArgumentException("Offset is negative.", "offset");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count is negative.", "count");
            }
            if (position + count > _length)
            {
                throw new ArgumentException("Count extends past the end of the buffer.", "count");
            }
            if (offset + count > destination.Length)
            {
                throw new ArgumentException("Count extends past the end of the destination.", "count");
            }

            var chunkIndex = GetChunkIndex(position);
            var chunkOffset = position - _positions[chunkIndex];
            while (count > 0)
            {
                var segment = _chunks[chunkIndex].Bytes;
                var chunkRemaining = segment.Count - chunkOffset;
                var partialCount = Math.Min(count, chunkRemaining);
                Buffer.BlockCopy(segment.Array, segment.Offset + chunkOffset, destination, offset, partialCount);
                chunkIndex += 1;
                chunkOffset = 0;
                count -= partialCount;
                offset += partialCount;
            }
        }

        /// <inheritdoc/>
        public void SetByte(int position, byte value)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            EnsureIsWritable();

            var chunkIndex = GetChunkIndex(position);
            var chunkOffset = position - _positions[chunkIndex];
            var segment = _chunks[chunkIndex].Bytes;
            segment.Array[segment.Offset + chunkOffset] = value;
        }

        /// <inheritdoc/>
        public void SetBytes(int position, byte[] source, int offset, int count)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (offset < 0 || offset > source.Length)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset is outside of the source.");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count is negative.", "count");
            }
            if (position + count > _length)
            {
                throw new ArgumentException("Count extends past the end of the buffer.", "count");
            }
            if (offset + count > source.Length)
            {
                throw new ArgumentException("Count extends past the end of the source.", "count");
            }
            EnsureIsWritable();

            var chunkIndex = GetChunkIndex(position);
            var chunkOffset = position - _positions[chunkIndex];
            while (count > 0)
            {
                var segment = _chunks[chunkIndex].Bytes;
                var chunkRemaining = segment.Count - chunkOffset;
                var partialCount = Math.Min(count, chunkRemaining);
                Buffer.BlockCopy(source, offset, segment.Array, segment.Offset + chunkOffset, partialCount);
                chunkIndex += 1;
                chunkOffset = 0;
                offset += partialCount;
                count -= partialCount;
            }
        }

        // private methods
        private void EnsureIsReadOnly()
        {
            if (!_isReadOnly)
            {
                throw new InvalidOperationException("MultiChunkBuffer is not read only.");
            }
        }

        private void EnsureIsWritable()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("MultiChunkBuffer is not writable.");
            }
        }

        private void ExpandCapacity(int requiredCapacity)
        {
            if (_chunkSource == null)
            {
                throw new InvalidOperationException("Capacity cannot be expanded because this buffer was created without specifying a chunk source.");
            }

            while (_capacity < requiredCapacity)
            {
                var chunk = _chunkSource.GetChunk(requiredCapacity);
                _chunks.Add(chunk);
                _capacity += chunk.Bytes.Count;
                _positions.Add(_capacity);
            }
        }

        private int GetChunkIndex(int position)
        {
            // locality of reference means this loop will only execute once most of the time
            while (true)
            {
                if (position >= _positions[_chunkIndex + 1])
                {
                    _chunkIndex++;
                }
                else if (position < _positions[_chunkIndex])
                {
                    _chunkIndex--;
                }
                else
                {
                    return _chunkIndex;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
