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
    /// An IBsonBuffer that has multiple chunks.
    /// </summary>
    public class MultiChunkBuffer : IByteBuffer
    {
        // private fields
        private readonly BsonChunkPool _chunkPool;
        private readonly int _chunkSize;
        private readonly int _origin;
        private int _capacity;
        private List<BsonChunk> _chunks;
        private bool _disposed;
        private bool _isReadOnly;
        private int _length;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChunkBuffer"/> class.
        /// </summary>
        /// <param name="chunkPool">The chunk pool.</param>
        /// <exception cref="System.ArgumentNullException">chunkPool</exception>
        public MultiChunkBuffer(BsonChunkPool chunkPool)
        {
            if (chunkPool == null)
            {
                throw new ArgumentNullException("chunkPool");
            }

            _chunkPool = chunkPool;
            _chunks = new List<BsonChunk>();
            _chunkSize = chunkPool.ChunkSize;
            _origin = 0;
            _length = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChunkBuffer"/> class.
        /// </summary>
        /// <param name="chunks">The chunks.</param>
        /// <param name="origin">The slice offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="isReadOnly">Whether the buffer is read only.</param>
        /// <exception cref="System.ArgumentNullException">chunks</exception>
        internal MultiChunkBuffer(IEnumerable<BsonChunk> chunks, int origin, int length, bool isReadOnly)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException("chunks");
            }

            _chunks = new List<BsonChunk>(chunks);
            if (_chunks.Count == 0)
            {
                throw new ArgumentException("No chunks where provided.", "chunks");
            }

            _chunkSize = _chunks[0].Bytes.Length;
            if (_chunks.Any(c => c.Bytes.Length != _chunkSize))
            {
                throw new ArgumentException("The chunks are not all the same size.");
            }
            _capacity = _chunks.Count * _chunkSize;

            if (origin < 0 || origin > _capacity)
            {
                throw new ArgumentOutOfRangeException("origin");
            }
            _origin = origin;

            if (length < 0 || _origin + length > _capacity)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            _length = length;

            _chunkPool = null;
            _isReadOnly = isReadOnly;

            foreach (var chunk in _chunks)
            {
                chunk.IncrementReferenceCount();
            }
        }

        // public properties
        /// <summary>
        /// Gets the capacity.
        /// </summary>
        /// <value>
        /// The capacity.
        /// </value>
        public int Capacity
        {
            get
            {
                ThrowIfDisposed();
                return _isReadOnly ? _length : _capacity;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">MultiChunkBuffer</exception>
        public bool IsReadOnly
        {
            get
            {
                ThrowIfDisposed();
                return _isReadOnly;
            }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">MultiChunkBuffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Length</exception>
        /// <exception cref="System.InvalidOperationException">The length of a read only buffer cannot be changed.</exception>
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
                if (value < 0 || _origin + value > _capacity)
                {
                    throw new ArgumentOutOfRangeException("Length");
                }
                EnsureIsWritable();

                _length = value;
            }
        }

        // public methods
        /// <summary>
        /// Access the backing bytes directly. The returned ArraySegment will point to the desired position and contain
        /// as many bytes as possible up to the next chunk boundary (if any). If the returned ArraySegment does not
        /// contain enough bytes for your needs you will have to call ReadBytes instead.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>
        /// An ArraySegment pointing directly to the backing bytes for the position.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside of the buffer.</exception>
        public ArraySegment<byte> AccessBackingBytes(int position)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }

            var chunkIndex = (_origin + position) / _chunkSize;
            var chunkOffset = (_origin + position) % _chunkSize;
            var chunkRemaining = _chunkSize - chunkOffset;
            return new ArraySegment<byte>(_chunks[chunkIndex].Bytes, chunkOffset, chunkRemaining);
        }

        /// <summary>
        /// Clears the specified bytes.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="count">The count.</param>
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

            var chunkIndex = (_origin + position) / _chunkSize;
            var chunkOffset = (_origin + position) % _chunkSize;
            while (count > 0)
            {
                var chunkRemaining = _chunkSize - chunkOffset;
                var toClear = count < chunkRemaining ? count : chunkRemaining;
                Array.Clear(_chunks[chunkIndex].Bytes, chunkOffset, toClear);
                chunkIndex += 1;
                chunkOffset = 0;
                count -= toClear;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Expands the buffer to at least the specified minimum length. Depending on the buffer's growth strategy
        /// it may choose to expand to a larger length.
        /// </summary>
        /// <param name="capacity">The minimum length.</param>
        /// <exception cref="System.InvalidOperationException">Capacity cannot be expanded because this buffer was created without specifying a chunk pool.</exception>
        public void EnsureCapacity(int capacity)
        {
            if (_chunkPool == null)
            {
                throw new InvalidOperationException("Capacity cannot be expanded because this buffer was created without specifying a chunk pool.");
            }

            if (_capacity < _origin + capacity)
            {
                ExpandCapacity(_origin + capacity);
            }
        }

        /// <summary>
        /// Gets a slice of this buffer.
        /// </summary>
        /// <param name="position">The position of the start of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>
        /// A slice of this buffer.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">MultiChunkBuffer</exception>
        /// <exception cref="System.InvalidOperationException">GetSlice can only be called for read only buffers.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// position
        /// or
        /// length
        /// </exception>
        public IByteBuffer GetSlice(int position, int length)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            if (length < 0)
            {
                throw new ArgumentException("Length is negative.", "count");
            }
            if (position + length > _length)
            {
                throw new ArgumentException("Length extends past the end of the buffer.", "count");
            }
            EnsureIsReadOnly();

            var firstChunk = (_origin + position) / _chunkSize;
            var lastChunk = (_origin + position + length - 1) / _chunkSize;
            var origin = (_origin + position) - (firstChunk * _chunkSize);

            if (firstChunk == lastChunk)
            {
                return new SingleChunkBuffer(_chunks[firstChunk], origin, length, isReadOnly: true);
            }
            else
            {
                var chunks = _chunks.Skip(firstChunk).Take(lastChunk - firstChunk + 1);
                return new MultiChunkBuffer(chunks, origin, length, isReadOnly: true);
            }
        }

        /// <summary>
        /// Loads the buffer from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="position">The position.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">count</exception>
        /// <exception cref="System.ArgumentException">
        /// Count is negative.;count
        /// or
        /// Count extends past the end of the buffer.;count
        /// </exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        /// <exception cref="System.ObjectDisposedException">MultiChunkBuffer</exception>
        /// <exception cref="System.InvalidOperationException">The MultiChunkBuffer is read only.</exception>
        public void LoadFrom(Stream stream, int position, int count)
        {
            ThrowIfDisposed();
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count is negative.", "count");
            }
            if (position + count > _length)
            {
                throw new ArgumentException("Count extends past the end of the buffer.", "count");
            }
            EnsureIsWritable();

            while (count > 0)
            {
                var chunkIndex = (_origin + position) / _chunkSize;
                var chunkOffset = (_origin + position) % _chunkSize;
                var chunkRemaining = _chunkSize - chunkOffset;
                var bytesToRead = (count <= chunkRemaining) ? count : chunkRemaining;
                var bytesRead = stream.Read(_chunks[chunkIndex].Bytes, chunkOffset, bytesToRead);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                position += bytesRead;
                count -= bytesRead;
            }
        }

        /// <summary>
        /// Makes this buffer read only.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">ByteArrayBuffer</exception>
        public void MakeReadOnly()
        {
            ThrowIfDisposed();
            _isReadOnly = true;
        }

        /// <summary>
        /// Reads a byte.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>
        /// A byte.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside of the buffer.</exception>
        public byte ReadByte(int position)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }

            var chunkIndex = (_origin + position) / _chunkSize;
            var chunkOffset = (_origin + position) % _chunkSize;
            return _chunks[chunkIndex].Bytes[chunkOffset];
        }

        /// <summary>
        /// Reads bytes.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="offset">The destination offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside of the buffer.</exception>
        /// <exception cref="System.ArgumentNullException">destination</exception>
        /// <exception cref="System.ArgumentException">
        /// Offset is negative.;offset
        /// or
        /// Count is negative.;count
        /// or
        /// Count extends past the end of the buffer.;count
        /// or
        /// Count extends past the end of the destination.;count
        /// </exception>
        public void ReadBytes(int position, byte[] destination, int offset, int count)
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

            var chunkIndex = (_origin + position) / _chunkSize;
            var chunkOffset = (_origin + position) % _chunkSize;
            while (count > 0)
            {
                var chunkRemaining = _chunkSize - chunkOffset;
                var bytesToCopy = (count < chunkRemaining) ? count : chunkRemaining;
                Buffer.BlockCopy(_chunks[chunkIndex].Bytes, chunkOffset, destination, offset, bytesToCopy);
                chunkIndex += 1;
                chunkOffset = 0;
                count -= bytesToCopy;
                offset += bytesToCopy;
            }
        }

        /// <summary>
        /// Writes a byte.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside of the buffer.</exception>
        public void WriteByte(int position, byte value)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            EnsureIsWritable();

            var chunkIndex = (_origin + position) / _chunkSize;
            var chunkOffset = (_origin + position) % _chunkSize;
            _chunks[chunkIndex].Bytes[chunkOffset] = value;
        }

        /// <summary>
        /// Writes bytes.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="source">The bytes (in the form of a byte array).</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// position;Position is outside of the buffer.
        /// or
        /// offset;Offset is outside of the source.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">source</exception>
        /// <exception cref="System.ArgumentException">
        /// Count is negative.;count
        /// or
        /// Count extends past the end of the buffer.;count
        /// or
        /// Count extends past the end of the source.;count
        /// </exception>
        public void WriteBytes(int position, byte[] source, int offset, int count)
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

            var chunkIndex = (_origin + position) / _chunkSize;
            var chunkOffset = (_origin + position) % _chunkSize;
            while (count > 0)
            {
                var chunkRemaining = _chunkSize - chunkOffset;
                var bytesToCopy = (count < chunkRemaining) ? count : chunkRemaining;
                Buffer.BlockCopy(source, offset, _chunks[chunkIndex].Bytes, chunkOffset, bytesToCopy);
                chunkIndex += 1;
                chunkOffset = 0;
                offset += bytesToCopy;
                count -= bytesToCopy;
            }
        }

        /// <summary>
        /// Writes Length bytes from this buffer starting at Position 0 to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ObjectDisposedException">MultiChunkBuffer</exception>
        public void WriteTo(Stream stream)
        {
            ThrowIfDisposed();

            var chunkIndex = _origin / _chunkSize;
            var chunkOffset = _origin % _chunkSize;
            var remaining = _length;
            while (remaining > 0)
            {
                var chunkRemaining = _chunkSize - chunkOffset;
                var bytesToWrite = (remaining < chunkRemaining) ? remaining : chunkRemaining;
                stream.Write(_chunks[chunkIndex].Bytes, chunkOffset, bytesToWrite);
                chunkIndex += 1;
                chunkOffset = 0;
                remaining -= bytesToWrite;
            }
        }

        // protected methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_chunks != null)
                    {
                        foreach (var chunk in _chunks)
                        {
                            chunk.DecrementReferenceCount();
                        }
                        _chunks = null;
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Throws if disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
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

        private void ExpandCapacity(int targetCapacity)
        {
            while (_capacity < targetCapacity)
            {
                var chunk = _chunkPool.AcquireChunk();
                chunk.IncrementReferenceCount();
                _chunks.Add(chunk);
                _capacity += _chunkSize;
            }
        }
    }
}
