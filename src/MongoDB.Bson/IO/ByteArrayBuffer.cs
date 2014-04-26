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
        private int _origin;
        private int _length;
        private bool _isReadOnly;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayBuffer"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="origin">The origin of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <param name="isReadOnly">Whether the buffer is read only.</param>
        /// <exception cref="System.ArgumentNullException">bytes</exception>
        public ByteArrayBuffer(byte[] bytes, int origin, int length, bool isReadOnly)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (origin < 0 || origin > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("origin", "Origin is outside the bytes.");
            }
            if (origin + length > bytes.Length)
            {
                throw new ArgumentException("Length extends past the end of the bytes.", "length");
            }

            _bytes = bytes;
            _origin = origin;
            _length = length;
            _isReadOnly = isReadOnly;
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
                return _isReadOnly ? _length : _bytes.Length - _origin;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">ByteArrayBuffer</exception>
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
        /// <exception cref="System.ArgumentException">Length is negative.;value</exception>
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
                if (_origin + value > _bytes.Length)
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

        /// <summary>
        /// Gets the origin.
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        protected int Origin
        {
            get { return _origin; }
        }

        // public methods
        /// <summary>
        /// Accesses the backing bytes.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public ArraySegment<byte> AccessBackingBytes(int position)
        {
            ThrowIfDisposed();
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside of the buffer.");
            }

            return new ArraySegment<byte>(_bytes, _origin + position, _length - position);
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
            if (count < 0)
            {
                throw new ArgumentException("Count is negative.", "count");
            }
            if (position + count > _length)
            {
                throw new ArgumentException("Count extends past the end of the buffer.", "count");
            }

            Array.Clear(_bytes, _origin + position, count);
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
        /// <exception cref="System.ArgumentException">Minimum length is negative.;minimumLength</exception>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("Capacity is negative.", "capacity");
            }

            capacity += _origin;
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

        /// <summary>
        /// Gets a slice of this buffer.
        /// </summary>
        /// <param name="position">The position of the start of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>
        /// A slice of this buffer.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">ByteArrayBuffer</exception>
        /// <exception cref="System.InvalidOperationException">GetSlice can only be called for read only buffers.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// position
        /// or
        /// length
        /// </exception>
        public virtual IByteBuffer GetSlice(int position, int length)
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

            return new ByteArrayBuffer(_bytes, _origin + position, length, isReadOnly: true);
        }

        /// <summary>
        /// Loads the buffer from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="position">The position.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside of the buffer.</exception>
        /// <exception cref="System.ArgumentException">
        /// Count is negative.;count
        /// or
        /// Count extends past the end of the buffer.;count
        /// </exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
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
                var bytesRead = stream.Read(_bytes, _origin + position, count);
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

            return _bytes[_origin + position];
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

            Buffer.BlockCopy(_bytes, _origin + position, destination, offset, count);
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

            _bytes[_origin + position] = value;
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

            Buffer.BlockCopy(source, offset, _bytes, _origin + position, count);
        }

        /// <summary>
        /// Writes Length bytes from this buffer starting at Position 0 to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ObjectDisposedException">ByteArrayBuffer</exception>
        public void WriteTo(Stream stream)
        {
            ThrowIfDisposed();
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.Write(_bytes, _origin, _length);
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
