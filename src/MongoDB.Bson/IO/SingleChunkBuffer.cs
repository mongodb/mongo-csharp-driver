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
using System.IO;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// An IByteBuffer that is backed by a single chunk.
    /// </summary>
    public sealed class SingleChunkBuffer : IByteBuffer
    {
        // private fields
        private IBsonChunk _chunk;
        private bool _disposed;
        private bool _isReadOnly;
        private int _length;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleChunkBuffer"/> class.
        /// </summary>
        /// <param name="chunk">The chuns.</param>
        /// <param name="length">The length.</param>
        /// <param name="isReadOnly">Whether the buffer is read only.</param>
        internal SingleChunkBuffer(IBsonChunk chunk, int length, bool isReadOnly)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException("chunk");
            }
            if (length < 0 || length > chunk.Bytes.Count)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            _chunk = chunk;
            _length = length;
            _isReadOnly = isReadOnly;
        }

        // public properties
        /// <inheritdoc/>
        public int Capacity
        {
            get
            {
                ThrowIfDisposed();
                return _isReadOnly ? _length : _chunk.Bytes.Count;
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
                if (value < 0 || value > _chunk.Bytes.Count)
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

            return new ArraySegment<byte>(_chunk.Bytes.Array, _chunk.Bytes.Offset + position, _chunk.Bytes.Count - position);
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

            Array.Clear(_chunk.Bytes.Array, _chunk.Bytes.Offset + position, count);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _chunk.Dispose();
                _chunk = null;
            }
        }

        /// <inheritdoc/>
        public void EnsureCapacity(int capacity)
        {
            throw new NotSupportedException("Capacity cannot be expanded for a SingleChunkBuffer.");
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

            var forkedBuffer = new SingleChunkBuffer(_chunk.Fork(), _length, isReadOnly: true);

            return new ByteBufferSlice(forkedBuffer, position, length);
        }

        /// <inheritdoc/>
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
                var bytesRead = stream.Read(_chunk.Bytes.Array, _chunk.Bytes.Offset + position, count);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                position += bytesRead;
                count -= bytesRead;
            }
        }
      
        /// <inheritdoc/>
        public void MakeReadOnly()
        {
            ThrowIfDisposed();
            _isReadOnly = true;
        }

        /// <inheritdoc/>
        public byte ReadByte(int position)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }

            return _chunk.Bytes.Array[_chunk.Bytes.Offset + position];
        }

        /// <inheritdoc/>
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

            Buffer.BlockCopy(_chunk.Bytes.Array, _chunk.Bytes.Offset + position, destination, offset, count);
        }

        /// <inheritdoc/>
        public void WriteByte(int position, byte value)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }
            EnsureIsWritable();

            _chunk.Bytes.Array[_chunk.Bytes.Offset + position] = value;
        }

        /// <inheritdoc/>
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

            Buffer.BlockCopy(source, offset, _chunk.Bytes.Array, _chunk.Bytes.Offset + position, count);
        }

        /// <inheritdoc/>
        public void WriteTo(Stream stream, int position, int count)
        {
            ThrowIfDisposed();

            stream.Write(_chunk.Bytes.Array, _chunk.Bytes.Offset + position, count);
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
