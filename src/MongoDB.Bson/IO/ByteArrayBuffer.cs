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
    /// An IByteBuffer that is backed by a contiguous byte array.
    /// </summary>
    public class ByteArrayBuffer : IByteBuffer
    {
        // private fields
        private bool _disposed;
        private byte[] _bytes;
        private int _length;
        private bool _isReadOnly;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayBuffer"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="isReadOnly">Whether the buffer is read only.</param>
        public ByteArrayBuffer(byte[] bytes, bool isReadOnly = false)
            : this(bytes, bytes.Length, isReadOnly)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayBuffer"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="length">The length.</param>
        /// <param name="isReadOnly">Whether the buffer is read only.</param>
        public ByteArrayBuffer(byte[] bytes, int length, bool isReadOnly = false)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (length < 0 || length > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            _bytes = bytes;
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
                return _isReadOnly ? _length : _bytes.Length;
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
                if (value < 0)
                {
                    throw new ArgumentException("Length is negative.", "value");
                }
                if (value > _bytes.Length)
                {
                    throw new ArgumentException("Length extends beyond the end of the buffer.", "value");
                }
                EnsureIsWritable();

                _length = value;
            }
        }

        // protected properties
        /// <summary>
        /// Gets a value indicating whether this <see cref="ByteArrayBuffer"/> is disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disposed; otherwise, <c>false</c>.
        /// </value>
        protected bool Disposed
        {
            get { return _disposed; }
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

            return new ArraySegment<byte>(_bytes, position, _length - position);
        }

        /// <inheritdoc/>
        public void Clear(int position, int count)
        {
            ThrowIfDisposed();
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

            Array.Clear(_bytes, position, count);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("Capacity is negative.", "capacity");
            }

            if (capacity > _bytes.Length)
            {
                var powerOf2 = 32;
                while (powerOf2 < capacity)
                {
                    powerOf2 <<= 1;
                }
                SetCapacity(powerOf2);
            }
        }

        /// <inheritdoc/>
        public virtual IByteBuffer GetSlice(int position, int length)
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

            var forkedBuffer = new ByteArrayBuffer(_bytes, _length, isReadOnly: true);
            return new ByteBufferSlice(forkedBuffer, position, length);
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

            return _bytes[position];
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

            Buffer.BlockCopy(_bytes, position, destination, offset, count);
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

            _bytes[position] = value;
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

            Buffer.BlockCopy(source, offset, _bytes, position, count);
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // subclasses override this method if they have anything to Dispose
            _disposed = true;
        }

        /// <summary>
        /// Ensures the buffer is read only.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">ByteArrayBuffer is not read only.</exception>
        protected void EnsureIsReadOnly()
        {
            if (!_isReadOnly)
            {
                var message = string.Format("{0} is not read only.", GetType().Name);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Ensures the buffer is writable.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">ByteArrayBuffer is not writable.</exception>
        protected void EnsureIsWritable()
        {
            if (_isReadOnly)
            {
                var message = string.Format("{0} is not writable.", GetType().Name);
                throw new InvalidOperationException(message);
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
        private void SetCapacity(int capacity)
        {
            var oldBytes = _bytes;
            _bytes = new byte[capacity];
            var bytesToCopy = capacity < oldBytes.Length ? capacity : oldBytes.Length;
            Buffer.BlockCopy(oldBytes, 0, _bytes, 0, bytesToCopy);
        }
    }
}
