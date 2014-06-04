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
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a Stream backed by memory. Similar to MemoryStream but has a configurable buffer
    /// provider and also implements the IBsonStream interface for higher performance BSON I/O.
    /// </summary>
    public class BsonMemoryStream : Stream, IBsonStream, ISliceableStream
    {
        // private fields
        private byte[] _buffer;
        private int _capacity;
        private bool _expandable;
        private bool _isOpen;
        private int _length;
        private int _origin;
        private int _position;
        private bool _writeable;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        public BsonMemoryStream()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="segment">The backing array segment.</param>
        public BsonMemoryStream(ArraySegment<byte> segment)
            : this(segment.Array, segment.Offset, segment.Count, writeable: true, publiclyVisible: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="segment">The backing array segment.</param>
        /// <param name="writeable">Whether the stream is writeable.</param>
        public BsonMemoryStream(ArraySegment<byte> segment, bool writeable)
            : this(segment.Array, segment.Offset, segment.Count, writeable, publiclyVisible: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="buffer">The backing buffer.</param>
        public BsonMemoryStream(byte[] buffer)
            : this(buffer, writeable: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="buffer">The backing buffer.</param>
        /// <param name="writeable">Whether the stream is writeable.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public BsonMemoryStream(byte[] buffer, bool writeable)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            var length = buffer.Length;
            _buffer = buffer;
            _capacity = length;
            _expandable = false;
            _isOpen = true;
            _length = length;
            _origin = 0;
            _position = 0;
            _writeable = writeable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="buffer">The backing buffer.</param>
        /// <param name="origin">The origin of the slice in the backing buffer.</param>
        /// <param name="count">The count.</param>
        public BsonMemoryStream(byte[] buffer, int origin, int count)
            : this(buffer, origin, count, writeable: true, publiclyVisible: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="buffer">The backing buffer.</param>
        /// <param name="origin">The origin of the slice in the backing buffer.</param>
        /// <param name="count">The count.</param>
        /// <param name="writeable">Whether the stream is writeable.</param>
        public BsonMemoryStream(byte[] buffer, int origin, int count, bool writeable)
            : this(buffer, origin, count, writeable, publiclyVisible: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="buffer">The backing buffer.</param>
        /// <param name="origin">The origin of the slice in the backing buffer.</param>
        /// <param name="count">The count.</param>
        /// <param name="writeable">Whether the stream is writeable.</param>
        /// <param name="publiclyVisible">Whether the backing bytes are publicly visible (via GetBuffer).</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public BsonMemoryStream(byte[] buffer, int origin, int count, bool writeable, bool publiclyVisible)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            var length = origin + count;
            _buffer = buffer;
            _capacity = length;
            _expandable = false;
            _isOpen = true;
            _length = length;
            _origin = origin;
            _position = origin;
            _writeable = writeable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonMemoryStream"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity.</param>
        /// <exception cref="System.ArgumentException">Capacity cannot be negative.;capacity</exception>
        public BsonMemoryStream(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("Capacity cannot be negative.", "capacity");
            }

            _buffer = new byte[capacity];
            _capacity = capacity;
            _expandable = true;
            _isOpen = true;
            _length = 0;
            _origin = 0;
            _position = 0;
            _writeable = true;
        }

        // public properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return _isOpen; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get { return _isOpen; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get { return _writeable; }
        }

        /// <summary>
        /// Gets or sets the capacity.
        /// </summary>
        /// <value>
        /// The capacity.
        /// </value>
        /// <exception cref="System.ArgumentOutOfRangeException">value;Capacity is smaller than current Length.</exception>
        /// <exception cref="System.NotSupportedException">Stream Capacity is fixed.</exception>
        public int Capacity
        {
            get
            {
                if (!_isOpen)
                {
                    StreamIsClosed();
                }
                return _capacity - _origin;
            }
            set
            {
                if (value < Length)
                {
                    throw new ArgumentOutOfRangeException("value", "Capacity is smaller than current Length.");
                }
                if (!_isOpen)
                {
                    StreamIsClosed();
                }
                if (!_expandable && value != Capacity)
                {
                    throw new NotSupportedException("Stream Capacity is fixed.");
                }
                if (_expandable && value != _capacity)
                {
                    if (value > 0)
                    {
                        var newBuffer = new byte[value];
                        if (_length > 0)
                        {
                            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
                        }
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = null;
                    }
                    _capacity = value;
                }
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get
            {
                if (!_isOpen)
                {
                    StreamIsClosed();
                }
                return (long)(_length - _origin);
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// value;Position is negative.
        /// or
        /// value;Position is greater than Int32.MaxValue.
        /// </exception>
        public override long Position
        {
            get
            {
                if (!_isOpen)
                {
                    StreamIsClosed();
                }
                return (long)(_position - _origin);
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Position is negative.");
                }
                if (!_isOpen)
                {
                    StreamIsClosed();
                }
                if (value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("value", "Position is greater than Int32.MaxValue.");
                }
                _position = _origin + (int)value;
            }
        }

        // public methods
        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            // nothing to do
        }

        /// <summary>
        /// Gets the slice.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside of the buffer.</exception>
        /// <exception cref="System.ArgumentException">
        /// Length is negative.;length
        /// or
        /// Length extends beyond the end of the buffer.;length
        /// </exception>
        /// <exception cref="System.NotSupportedException">GetSlice is not supported for writeable streams.</exception>
        public IByteBuffer GetSlice(int position, int length)
        {
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
                throw new ArgumentException("Length extends beyond the end of the buffer.", "length");
            }
            if (_writeable)
            {
                throw new NotSupportedException("GetSlice is not supported for writeable streams.");
            }

            return new ByteArrayBuffer(_buffer, _origin + position, length, isReadOnly: true);
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// offset;Offset is negative.
        /// or
        /// count;Count is negative.
        /// </exception>
        /// <exception cref="System.ArgumentException">Offset plus count exceed buffer length.;count</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset is negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Count is negative.");
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Offset plus count exceed buffer length.", "count");
            }
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var available = _length - _position;
            if (count > available)
            {
                count = available; // Stream API returns available bytes instead of throwing EndOfStreamException
            }

            if (count <= 8)
            {
                var remaining = count;
                while (--remaining >= 0)
                {
                    buffer[offset + remaining] = _buffer[_position + remaining];
                }
            }
            else
            {
                Buffer.BlockCopy(_buffer, _position, buffer, offset, count);
            }

            _position += count;
            return count;
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>
        /// The unsigned byte cast to an Int32, or -1 if at the end of the stream.
        /// </returns>
        public override int ReadByte()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            if (_position >= _length)
            {
                return -1; // Stream API returns -1 instead of throwing EndOfStreamException
            }

            return _buffer[_position++];
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">offset;Offset must be a 32-bit value.</exception>
        /// <exception cref="System.ArgumentException">origin</exception>
        /// <exception cref="System.IO.IOException">
        /// Attempt to Seek before the beginning of the stream.
        /// or
        /// Attempt to Seek to a position greater than Int32.MaxValue.
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            if (offset > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset must be a 32-bit value.");
            }

            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = _origin + offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = _position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _length + offset;
                    break;
                default:
                    throw new ArgumentException("origin");
            }
            if (newPosition < _origin)
            {
                throw new IOException("Attempt to Seek before the beginning of the stream.");
            }
            if (newPosition > int.MaxValue)
            {
                throw new IOException("Attempt to Seek to a position greater than Int32.MaxValue.");
            }

            _position = (int)newPosition;
            return newPosition;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// value;Length is not a positive 32-bit integer value.
        /// or
        /// value
        /// </exception>
        public override void SetLength(long value)
        {
            if (value < 0 || value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("value", "Length is not a positive 32-bit integer value.");
            }
            if (value > (int.MaxValue - _origin))
            {
                throw new ArgumentOutOfRangeException("value");
            }
            EnsureWriteable();

            var newLength = _origin + (int)value;
            if (!EnsureCapacity(newLength) && newLength > _length)
            {
                Array.Clear(_buffer, _length, newLength - _length);
            }

            _length = newLength;
            if (_position > newLength)
            {
                _position = newLength;
            }
        }

        /// <summary>
        /// Writes the stream contents to a byte array, regardless of the Position property.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToArray()
        {
            var length = _length - _origin;
            var array = new byte[length];
            Buffer.BlockCopy(_buffer, _origin, array, 0, length);
            return array;
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// offset;Offset is negative.
        /// or
        /// count;Count is negative.
        /// </exception>
        /// <exception cref="System.ArgumentException">Offset plus count exceed buffer length.;count</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset is negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Count is negative.");
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Offset plus count exceed buffer length.", "count");
            }
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var newPosition = _position + count;
            EnsurePosition(newPosition);

            if (count <= 8 && buffer != _buffer)
            {
                var counter = count;
                while (--counter >= 0)
                {
                    _buffer[_position + counter] = buffer[offset + counter];
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }

            SetPositionAfterWrite(newPosition);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var newPosition = _position + 1;
            EnsurePosition(newPosition);

            _buffer[_position] = value;
            SetPositionAfterWrite(_position + 1);
        }

        /// <summary>
        /// Writes the entire contents of the memory stream to another stream.
        /// </summary>
        /// <param name="stream">The stream to write this memory stream to.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            stream.Write(_buffer, _origin, _length - _origin);
        }

        // protected methods
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isOpen = false;
                _writeable = false;
                _expandable = false;
            }
            base.Dispose(disposing);
        }

        // private methods
        private bool EnsureCapacity(int value)
        {
            if (value < 0)
            {
                throw new IOException("Stream too long.");
            }
            if (value < _capacity)
            {
                return false;
            }

            if (value < 0x100)
            {
                value = 0x100;
            }
            if (value < _capacity * 2)
            {
                value = _capacity * 2;
            }

            Capacity = value;
            return true;
        }

        private void EnsurePosition(int position)
        {
            if (position < 0)
            {
                throw new IOException("Stream too long");
            }

            if (position > _length)
            {
                var zeroFill = position > _length;
                if (position > _capacity && EnsureCapacity(position))
                {
                    zeroFill = false;
                }
                if (zeroFill)
                {
                    Array.Clear(_buffer, _length, _position - _length);
                }
            }
        }

        private void EnsureWriteable()
        {
            if (!CanWrite)
            {
                throw new NotSupportedException("Stream is not writeable.");
            }
        }

        private int FindNullByte()
        {
            for (var index = _position; index < _length; index++)
            {
                if (_buffer[index] == 0)
                {
                    return index;
                }
            }

            throw new EndOfStreamException();
        }

        private void SetPositionAfterWrite(int position)
        {
            _position = position;
            if (_length < position)
            {
                _length = position;
            }
        }

        private void StreamIsClosed()
        {
            throw new ObjectDisposedException("BsonMemoryStream");
        }

        // explicit interface implementations
        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <returns>An ArraySegment containing the CString bytes (without the null byte).</returns>
        ArraySegment<byte> IBsonStream.ReadBsonCStringBytes()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var startPosition = _position;
            var nullPosition = FindNullByte();
            var length = nullPosition - startPosition;
            _position = nullPosition + 1; // advance over the null byte

            return new ArraySegment<byte>(_buffer, startPosition, length); // without the null byte
        }

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <returns>
        /// A double.
        /// </returns>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        double IBsonStream.ReadBsonDouble()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var position = _position;
            if ((_position += 8) > _length)
            {
                _position = _length;
                throw new EndOfStreamException();
            }

            return BitConverter.ToDouble(_buffer, position);
        }

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>
        /// An int.
        /// </returns>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        int IBsonStream.ReadBsonInt32()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var position = _position;
            if ((_position += 4) > _length)
            {
                _position = _length;
                throw new EndOfStreamException();
            }

            return _buffer[position] | (_buffer[position + 1] << 8) | (_buffer[position + 2] << 16) | (_buffer[position + 3] << 24);
        }

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>
        /// A long.
        /// </returns>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        long IBsonStream.ReadBsonInt64()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var position = _position;
            if ((_position += 8) > _length)
            {
                _position = _length;
                throw new EndOfStreamException();
            }

            var lo = (uint)(_buffer[position] | (_buffer[position + 1] << 8) | (_buffer[position + 2] << 16) | (_buffer[position + 3] << 24));
            var hi = (uint)(_buffer[position + 4] | (_buffer[position + 5] << 8) | (_buffer[position + 6] << 16) | (_buffer[position + 7] << 24));
            return (long)(((ulong)hi << 32) | (ulong)lo);
        }

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>
        /// An ObjectId.
        /// </returns>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        ObjectId IBsonStream.ReadBsonObjectId()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var position = _position;
            if ((_position += 12) > _length)
            {
                _position = _length;
                throw new EndOfStreamException();
            }

            return new ObjectId(_buffer, position);
        }

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>
        /// A string.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">encoding</exception>
        /// <exception cref="System.FormatException"></exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        string IBsonStream.ReadBsonString(UTF8Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var length = ((IBsonStream)this).ReadBsonInt32();
            if (length <= 0)
            {
                var message = string.Format("Invalid string length: {0}.", length);
                throw new FormatException(message);
            }
            if (_buffer[_position + length - 1] != 0)
            {
                throw new FormatException("String is missing terminating null byte.");
            }

            var position = _position;
            if ((_position += length) > _length)
            {
                _position = _length;
                throw new EndOfStreamException();
            }

            return Utf8Helper.DecodeUtf8String(_buffer, position, length - 1, encoding);
        }

        /// <summary>
        /// Skips over a BSON CString leaving the stream positioned just after the terminating null byte.
        /// </summary>
        void IBsonStream.SkipBsonCString()
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }

            var nullPosition = FindNullByte();
            _position = nullPosition + 1;
        }

        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonCString(string value)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var maxLength = Utf8Helper.StrictUtf8Encoding.GetMaxByteCount(value.Length) + 1;
            var maxNewPosition = _position + maxLength;
            EnsurePosition(maxNewPosition);

            var length = Utf8Helper.StrictUtf8Encoding.GetBytes(value, 0, value.Length, _buffer, _position);
            _buffer[_position + length] = 0;

            SetPositionAfterWrite(length + 1);
        }

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonDouble(double value)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var newPosition = _position + 8;
            EnsurePosition(newPosition);

            var bytes = BitConverter.GetBytes(value);
            _buffer[_position] = bytes[0];
            _buffer[_position + 1] = bytes[1];
            _buffer[_position + 2] = bytes[2];
            _buffer[_position + 3] = bytes[3];
            _buffer[_position + 4] = bytes[4];
            _buffer[_position + 5] = bytes[5];
            _buffer[_position + 6] = bytes[6];
            _buffer[_position + 7] = bytes[7];

            SetPositionAfterWrite(newPosition);
        }

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonInt32(int value)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var newPosition = _position + 4;
            EnsurePosition(newPosition);

            _buffer[_position] = (byte)value;
            _buffer[_position + 1] = (byte)(value >> 8);
            _buffer[_position + 2] = (byte)(value >> 16);
            _buffer[_position + 3] = (byte)(value >> 24);

            SetPositionAfterWrite(newPosition);
        }

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonInt64(long value)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var newPosition = _position + 8;
            EnsurePosition(newPosition);

            _buffer[_position] = (byte)value;
            _buffer[_position + 1] = (byte)(value >> 8);
            _buffer[_position + 2] = (byte)(value >> 16);
            _buffer[_position + 3] = (byte)(value >> 24);
            _buffer[_position + 4] = (byte)(value >> 32);
            _buffer[_position + 5] = (byte)(value >> 40);
            _buffer[_position + 6] = (byte)(value >> 48);
            _buffer[_position + 7] = (byte)(value >> 56);

            SetPositionAfterWrite(newPosition);
        }

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonObjectId(ObjectId value)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var newPosition = _position + 12;
            EnsurePosition(newPosition);

            value.GetBytes(_buffer, _position);

            SetPositionAfterWrite(newPosition);
        }

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        void IBsonStream.WriteBsonString(string value, UTF8Encoding encoding)
        {
            if (!_isOpen)
            {
                StreamIsClosed();
            }
            EnsureWriteable();

            var maxLength = encoding.GetMaxByteCount(value.Length) + 5;
            var maxNewPosition = _position + maxLength;
            EnsurePosition(maxNewPosition);

            var length = encoding.GetBytes(value, 0, value.Length, _buffer, _position + 4);
            var lengthPlusOne = length + 1;
            _buffer[_position] = (byte)lengthPlusOne;
            _buffer[_position + 1] = (byte)(lengthPlusOne >> 8);
            _buffer[_position + 2] = (byte)(lengthPlusOne >> 16);
            _buffer[_position + 3] = (byte)(lengthPlusOne >> 24);
            _buffer[_position + 4 + length] = 0;

            SetPositionAfterWrite(length + 5);
        }
    }
}
