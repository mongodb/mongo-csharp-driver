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
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a Stream backed by an IByteBuffer. Similar to MemoryStream but backed by an IByteBuffer
    /// instead of a byte array and also implements the IBsonStream interface for higher performance BSON I/O.
    /// </summary>
    public class ByteBufferStream : Stream, IBsonStream, ISliceableStream
    {
        // private fields
        private readonly IByteBuffer _byteBuffer;
        private readonly bool _ownsByteBuffer;
        private bool _disposed;
        private int _length;
        private int _position;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteBufferStream"/> class.
        /// </summary>
        /// <param name="byteBuffer">The byte buffer.</param>
        public ByteBufferStream(IByteBuffer byteBuffer)
            : this(byteBuffer, ownsByteBuffer: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteBufferStream"/> class.
        /// </summary>
        /// <param name="byteBuffer">The byte buffer.</param>
        /// <param name="ownsByteBuffer">Whether the stream owns the byteBuffer and should Dispose it when done.</param>
        public ByteBufferStream(IByteBuffer byteBuffer, bool ownsByteBuffer)
        {
            _byteBuffer = byteBuffer;
            _ownsByteBuffer = ownsByteBuffer;
            _length = byteBuffer.Length;
        }

        // public properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        /// <returns>A value that determines whether the current stream can time out.</returns>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get { return !_byteBuffer.IsReadOnly; }
        }

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
                return _byteBuffer.Capacity;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get { return _length; }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get { return _position; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Position is negative.", "position");
                }
                _position = (int)value;
            }
        }

        // public methods
        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            // do nothing
        }

        /// <summary>
        /// Gets the slice.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">position;Position is outside the stream.</exception>
        /// <exception cref="System.ArgumentException">
        /// Length is negative.;length
        /// or
        /// Length extends beyond the end of the stream.
        /// </exception>
        public IByteBuffer GetSlice(int position, int length)
        {
            if (position < 0 || position > _length)
            {
                throw new ArgumentOutOfRangeException("position", "Position is outside the stream.");
            }
            if (length < 0)
            {
                throw new ArgumentException("Length is negative.", "length");
            }
            if (position + length > _length)
            {
                throw new ArgumentException("Length extends beyond the end of the stream.");
            }

            return _byteBuffer.GetSlice(position, length);
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentException("Offset is negative.", "offset");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count is negative", "count");
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Count extends beyond the end of the buffer.", "count");
            }

            if (_position >= _length)
            {
                return 0;
            }

            var available = _length - _position;
            if (count > available)
            {
                count = available;
            }

            _byteBuffer.ReadBytes(_position, buffer, offset, count);
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
            if (_position >= _length)
            {
                return -1;
            }
            return _byteBuffer.ReadByte(_position++);
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.ArgumentException">origin</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long position;
            switch (origin)
            {
                case SeekOrigin.Begin: position = offset; break;
                case SeekOrigin.Current: position = _position + offset; break;
                case SeekOrigin.End: position = _length + offset; break;
                default: throw new ArgumentException(string.Format("Invalid origin: {0}.", origin), "origin");
            }
            if (position < 0)
            {
                throw new InvalidOperationException("Attempted to seek before the beginning of the stream.");
            }
            if (position > int.MaxValue)
            {
                throw new InvalidOperationException("Attempted to seek beyond the maximum value that can be represented using 32 bits.");
            }

            _position = (int)position;
            return position;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="length">The desired length of the current stream in bytes.</param>
        public override void SetLength(long length)
        {
            if (length < 0)
            {
                throw new ArgumentException("Length is negative.", "length");
            }
            if (length > Capacity)
            {
                throw new ArgumentException("Length exceeds capacity.", "length");
            }
            EnsureWriteable();          

            _length = (int)length;
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentException("Offset is negative.", "offset");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count is negative.", "count");
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Count extends beyond the end of the buffer.", "count");
            }
            ThrowIfDisposed();
            EnsureWriteable();

            PrepareToWrite(count);
            _byteBuffer.WriteBytes(_position, buffer, offset, count);
            SetPositionAfterWrite(_position + count);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            PrepareToWrite(1);
            _byteBuffer.WriteByte(_position, value);
            SetPositionAfterWrite(_position + 1);
        }

        // protected methods
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_ownsByteBuffer)
                {
                    _byteBuffer.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        // private methods
        private void EnsureWriteable()
        {
            if (!CanWrite)
            {
                throw new NotSupportedException("Stream is not writeable.");
            }
        }

        private int FindNullByte()
        {
            var position = _position;
            while (position < _length)
            {
                var segment = _byteBuffer.AccessBackingBytes(position);
                var endOfSegmentIndex = segment.Offset + segment.Count;
                for (var index = segment.Offset; index < endOfSegmentIndex; index++)
                {
                    if (segment.Array[index] == 0)
                    {
                        return position + (index - segment.Offset);
                    }
                }
                position += segment.Count;
            }

            throw new EndOfStreamException();
        }

        private void PrepareToWrite(int count)
        {
            _byteBuffer.EnsureCapacity(_position + count);
            _byteBuffer.Length = _byteBuffer.Capacity;
            if (_length < _position)
            {
                _byteBuffer.Clear(_length, _position - _length);
            }
        }

        private byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            _byteBuffer.ReadBytes(_position, bytes, 0, count);
            _position += count;
            return bytes;
        }

        private void SetPositionAfterWrite(int position)
        {
            _position = position;
            if (_length < position)
            {
                _length = position;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("ByteBufferStream");
            }
        }

        private void ThrowIfEndOfStream(int count)
        {
            if (_position + count > _length)
            {
                if (_position < _length)
                {
                    _position = _length;
                }
                throw new EndOfStreamException();
            }
        }

        // explicit interface implementations
        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <returns>An ArraySegment containing the CString bytes (without the null byte).</returns>
        ArraySegment<byte> IBsonStream.ReadBsonCStringBytes()
        {
            ThrowIfDisposed();
            ThrowIfEndOfStream(1);

            var startPosition = _position;
            var nullPosition = FindNullByte();
            var length = nullPosition - startPosition;

            var segment = _byteBuffer.AccessBackingBytes(startPosition);
            if (segment.Count >= length)
            {
                _position = nullPosition + 1; // advance over null byte
                return new ArraySegment<byte>(segment.Array, segment.Offset, length); // without the null byte
            }
            else
            {
                var cstring = ReadBytes(length + 1); // read null byte also
                return new ArraySegment<byte>(cstring, 0, length); // without the null byte
            }
        }

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <returns>
        /// A double.
        /// </returns>
        double IBsonStream.ReadBsonDouble()
        {
            ThrowIfDisposed();
            ThrowIfEndOfStream(8);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 8)
            {
                _position += 8;
                return BitConverter.ToDouble(segment.Array, segment.Offset);
            }
            else
            {
                var bytes = ReadBytes(8);
                return BitConverter.ToDouble(bytes, 0);
            }
        }

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>
        /// An int.
        /// </returns>
        int IBsonStream.ReadBsonInt32()
        {
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 4)
            {
                _position += 4;
                var bytes = segment.Array;
                var offset = segment.Offset;
                return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
            }
            else
            {
                var bytes = ReadBytes(4);
                return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
            }
        }

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>
        /// A long.
        /// </returns>
        long IBsonStream.ReadBsonInt64()
        {
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 8)
            {
                _position += 8;
                var bytes = segment.Array;
                var offset = segment.Offset;
                var lo = (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
                var hi = (uint)(bytes[offset + 4] | (bytes[offset + 5] << 8) | (bytes[offset + 6] << 16) | (bytes[offset + 7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
            else
            {
                var bytes = ReadBytes(8);
                var lo = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
                var hi = (uint)(bytes[4] | (bytes[5] << 8) | (bytes[6] << 16) | (bytes[7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
        }

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>
        /// An ObjectId.
        /// </returns>
        ObjectId IBsonStream.ReadBsonObjectId()
        {
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 12)
            {
                _position += 12;
                return new ObjectId(segment.Array, segment.Offset);
            }
            else
            {
                var bytes = ReadBytes(12);
                return new ObjectId(bytes);
            }
        }

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>
        /// A string.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">encoding</exception>
        /// <exception cref="System.FormatException">
        /// String is missing terminating null byte.
        /// or
        /// String is missing terminating null byte.
        /// </exception>
        string IBsonStream.ReadBsonString(UTF8Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var length = ((IBsonStream)this).ReadBsonInt32();
            if (length <= 0)
            {
                var message = string.Format("Invalid string length: {0}.", length);
                throw new FormatException(message);
            }

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= length)
            {
                if (segment.Array[segment.Offset + length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }
                _position += length;
                return Utf8Helper.DecodeUtf8String(segment.Array, segment.Offset, length - 1, encoding);
            }
            else
            {
                var bytes = ReadBytes(length);
                if (bytes[length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }
                return Utf8Helper.DecodeUtf8String(bytes, 0, length - 1, encoding);
            }
        }

        /// <summary>
        /// Skips over a BSON CString leaving the stream positioned just after the terminating null byte.
        /// </summary>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        void IBsonStream.SkipBsonCString()
        {
            var nullPosition = FindNullByte();
            _position = nullPosition + 1;
        }

        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentException">
        /// UTF8 representation of a CString cannot contain null bytes.
        /// or
        /// UTF8 representation of a CString cannot contain null bytes.
        /// </exception>
        void IBsonStream.WriteBsonCString(string value)
        {
            var maxLength = Utf8Helper.StrictUtf8Encoding.GetMaxByteCount(value.Length) + 1;
            PrepareToWrite(maxLength);

            int actualLength;
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= maxLength)
            {
                actualLength = Utf8Helper.StrictUtf8Encoding.GetBytes(value, 0, value.Length, segment.Array, segment.Offset);
                if (Array.IndexOf<byte>(segment.Array, 0, segment.Offset, actualLength) != -1)
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }

                segment.Array[segment.Offset + actualLength] = 0;
            }
            else
            {
                var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(value);
                if (bytes.Contains<byte>(0))
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }
                actualLength = bytes.Length;

                _byteBuffer.WriteBytes(_position, bytes, 0, actualLength);
                _byteBuffer.WriteByte(_position + actualLength, 0);
            }

            SetPositionAfterWrite(_position + actualLength + 1);
        }

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            PrepareToWrite(8);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 8)
            {
                segment.Array[segment.Offset] = bytes[0];
                segment.Array[segment.Offset + 1] = bytes[1];
                segment.Array[segment.Offset + 2] = bytes[2];
                segment.Array[segment.Offset + 3] = bytes[3];
                segment.Array[segment.Offset + 4] = bytes[4];
                segment.Array[segment.Offset + 5] = bytes[5];
                segment.Array[segment.Offset + 6] = bytes[6];
                segment.Array[segment.Offset + 7] = bytes[7];
            }
            else
            {
                _byteBuffer.WriteBytes(_position, bytes, 0, 8);
            }

            SetPositionAfterWrite(_position + 8);
        }

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonInt32(int value)
        {
            PrepareToWrite(4);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 4)
            {
                segment.Array[segment.Offset] = (byte)value;
                segment.Array[segment.Offset + 1] = (byte)(value >> 8);
                segment.Array[segment.Offset + 2] = (byte)(value >> 16);
                segment.Array[segment.Offset + 3] = (byte)(value >> 24);
            }
            else
            {
                var bytes = new byte[4];
                bytes[0] = (byte)value;
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                _byteBuffer.WriteBytes(_position, bytes, 0, 4);
            }

            SetPositionAfterWrite(_position + 4);
        }

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonInt64(long value)
        {
            PrepareToWrite(8);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 8)
            {
                segment.Array[segment.Offset] = (byte)value;
                segment.Array[segment.Offset + 1] = (byte)(value >> 8);
                segment.Array[segment.Offset + 2] = (byte)(value >> 16);
                segment.Array[segment.Offset + 3] = (byte)(value >> 24);
                segment.Array[segment.Offset + 4] = (byte)(value >> 32);
                segment.Array[segment.Offset + 5] = (byte)(value >> 40);
                segment.Array[segment.Offset + 6] = (byte)(value >> 48);
                segment.Array[segment.Offset + 7] = (byte)(value >> 56);
            }
            else
            {
                var bytes = new byte[8];
                bytes[0] = (byte)value;
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                bytes[4] = (byte)(value >> 32);
                bytes[5] = (byte)(value >> 40);
                bytes[6] = (byte)(value >> 48);
                bytes[7] = (byte)(value >> 56);
                _byteBuffer.WriteBytes(_position, bytes, 0, 8);
            }

            SetPositionAfterWrite(_position + 8);
        }

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void IBsonStream.WriteBsonObjectId(ObjectId value)
        {
            PrepareToWrite(12);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 12)
            {
                value.GetBytes(segment.Array, segment.Offset);
            }
            else
            {
                var bytes = value.ToByteArray();
                _byteBuffer.WriteBytes(_position, bytes, 0, 12);
            }

            SetPositionAfterWrite(_position + 12);
        }

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentException">
        /// UTF8 representation of a CString cannot contain null bytes.
        /// or
        /// UTF8 representation of a CString cannot contain null bytes.
        /// </exception>
        void IBsonStream.WriteBsonString(string value, UTF8Encoding encoding)
        {
            var maxLength = encoding.GetMaxByteCount(value.Length) + 5;
            PrepareToWrite(maxLength);

            int actualLength;
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= maxLength)
            {
                actualLength = encoding.GetBytes(value, 0, value.Length, segment.Array, segment.Offset + 4);
                if (Array.IndexOf<byte>(segment.Array, 0, segment.Offset, actualLength) != -1)
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }

                var lengthPlusOne = actualLength + 1;
                segment.Array[segment.Offset] = (byte)lengthPlusOne;
                segment.Array[segment.Offset + 1] = (byte)(lengthPlusOne >> 8);
                segment.Array[segment.Offset + 2] = (byte)(lengthPlusOne >> 16);
                segment.Array[segment.Offset + 3] = (byte)(lengthPlusOne >> 24);
                segment.Array[segment.Offset + 4 + actualLength] = 0;
            }
            else
            {
                var bytes = encoding.GetBytes(value);
                if (bytes.Contains<byte>(0))
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }
                actualLength = bytes.Length;
                var lengthPlusOneBytes = BitConverter.GetBytes(actualLength + 1);

                _byteBuffer.WriteBytes(_position, lengthPlusOneBytes, 0, 4);
                _byteBuffer.WriteBytes(_position, bytes, 4, actualLength);
                _byteBuffer.WriteByte(_position + actualLength, 0);
            }

            SetPositionAfterWrite(_position + actualLength + 5);
        }
    }
}
