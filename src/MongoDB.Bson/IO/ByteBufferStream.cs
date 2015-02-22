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
    /// Represents a Stream backed by an IByteBuffer. Similar to MemoryStream but backed by an IByteBuffer
    /// instead of a byte array and also implements the IBsonStream interface for higher performance BSON I/O.
    /// </summary>
    public class ByteBufferStream : Stream, IBsonStream
    {
        // private fields
        private  IByteBuffer _byteBuffer;
        private readonly bool _ownsByteBuffer;
        private bool _disposed;
        private int _length;
        private int _position;
        private readonly byte[] _temp = new byte[12];
        private readonly byte[] _tempUtf8 = new byte[128];

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
        /// <inheritdoc/>
        public Stream BaseStream
        {
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        /// <inheritdoc/>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get { return !_byteBuffer.IsReadOnly; }
        }

        /// <inheritdoc/>
        public int Capacity
        {
            get
            {
                ThrowIfDisposed();
                return _byteBuffer.Capacity;
            }
        }

        /// <inheritdoc/>
        public override long Length
        {
            get { return _length; }
        }

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override void Flush()
        {
            // do nothing
        }

        /// <inheritdoc/>
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
                throw new ArgumentException("Length extends beyond the end of the stream.", "length");
            }

            return _byteBuffer.GetSlice(position, length);
        }

        /// <inheritdoc/>
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

            _byteBuffer.GetBytes(_position, buffer, offset, count);
            _position += count;

            return count;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            if (_position >= _length)
            {
                return -1;
            }
            return _byteBuffer.GetByte(_position++);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
            _byteBuffer.SetBytes(_position, buffer, offset, count);
            SetPositionAfterWrite(_position + count);
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            PrepareToWrite(1);
            _byteBuffer.SetByte(_position, value);
            SetPositionAfterWrite(_position + 1);
        }

        // protected methods
        /// <inheritdoc/>
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
            ThrowIfEndOfStream(count);
            var bytes = new byte[count];
            _byteBuffer.GetBytes(_position, bytes, 0, count);
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

        /// <inheritdoc/>
        public string ReadCString(UTF8Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var bytes = ReadCStringBytes();
            return Utf8Helper.DecodeUtf8String(bytes.Array, bytes.Offset, bytes.Count, encoding);
        }

        /// <inheritdoc/>
        public ArraySegment<byte> ReadCStringBytes()
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

        /// <inheritdoc/>
        public double ReadDouble()
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
                this.ReadBytes(_temp, 0, 8);
                return BitConverter.ToDouble(_temp, 0);
            }
        }

        /// <inheritdoc/>
        public int ReadInt32()
        {
            ThrowIfDisposed();
            ThrowIfEndOfStream(4);

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
                this.ReadBytes(_temp, 0, 4);
                return _temp[0] | (_temp[1] << 8) | (_temp[2] << 16) | (_temp[3] << 24);
            }
        }

        /// <inheritdoc/>
        public long ReadInt64()
        {
            ThrowIfDisposed();
            ThrowIfEndOfStream(8);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 8)
            {
                _position += 8;
                return BitConverter.ToInt64(segment.Array, segment.Offset);
            }
            else
            {
                this.ReadBytes(_temp, 0, 8);
                return BitConverter.ToInt64(_temp, 0);
            }
        }

        /// <inheritdoc/>
        public ObjectId ReadObjectId()
        {
            ThrowIfDisposed();
            ThrowIfEndOfStream(12);

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= 12)
            {
                _position += 12;
                return new ObjectId(segment.Array, segment.Offset);
            }
            else
            {
                this.ReadBytes(_temp, 0, 12);
                return new ObjectId(_temp, 0);
            }
        }

        /// <inheritdoc/>
        public IByteBuffer ReadSlice()
        {
            ThrowIfDisposed();

            var position = _position;
            var length = ReadInt32();
            Position = position + length;
            return _byteBuffer.GetSlice(position, length);
        }

        /// <inheritdoc/>
        public string ReadString(UTF8Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            ThrowIfDisposed();

            var length = ReadInt32();
            if (length <= 0)
            {
                var message = string.Format("Invalid string length: {0}.", length);
                throw new FormatException(message);
            }

            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= length)
            {
                ThrowIfEndOfStream(length);
                if (segment.Array[segment.Offset + length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }
                _position += length;
                return Utf8Helper.DecodeUtf8String(segment.Array, segment.Offset, length - 1, encoding);
            }
            else
            {
                var bytes = length <= _tempUtf8.Length ? _tempUtf8 : new byte[length];
                this.ReadBytes(bytes, 0, length);
                if (bytes[length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }
                return Utf8Helper.DecodeUtf8String(bytes, 0, length - 1, encoding);
            }
        }

        /// <inheritdoc/>
        public void SkipCString()
        {
            var nullPosition = FindNullByte();
            _position = nullPosition + 1;
        }

        /// <inheritdoc/>
        public void WriteCString(string value)
        {
            var maxLength = Utf8Encodings.Strict.GetMaxByteCount(value.Length) + 1;
            PrepareToWrite(maxLength);

            int actualLength;
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= maxLength)
            {
                actualLength = Utf8Encodings.Strict.GetBytes(value, 0, value.Length, segment.Array, segment.Offset);
                if (Array.IndexOf<byte>(segment.Array, 0, segment.Offset, actualLength) != -1)
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }

                segment.Array[segment.Offset + actualLength] = 0;
            }
            else
            {
                byte[] bytes;
                if (maxLength <= _tempUtf8.Length)
                {
                    bytes = _tempUtf8;
                    actualLength = Utf8Encodings.Strict.GetBytes(value, 0, value.Length, bytes, 0);
                }
                else
                {
                    bytes = Utf8Encodings.Strict.GetBytes(value);
                    actualLength = bytes.Length;
                }

                if (Array.IndexOf<byte>(bytes, 0, 0, actualLength) != -1)
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }

                _byteBuffer.SetBytes(_position, bytes, 0, actualLength);
                _byteBuffer.SetByte(_position + actualLength, 0);
            }

            SetPositionAfterWrite(_position + actualLength + 1);
        }

        /// <inheritdoc/>
        public void WriteCStringBytes(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (Array.IndexOf<byte>(value, 0) != -1)
            {
                throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
            }

            var length = value.Length;

            PrepareToWrite(length + 1);

            _byteBuffer.SetBytes(_position, value, 0, length);
            _byteBuffer.SetByte(_position + length, 0);

            SetPositionAfterWrite(_position + length + 1);
        }

        /// <inheritdoc/>
        public void WriteDouble(double value)
        {
            PrepareToWrite(8);

            var bytes = BitConverter.GetBytes(value);
            _byteBuffer.SetBytes(_position, bytes, 0, 8);

            SetPositionAfterWrite(_position + 8);
        }

        /// <inheritdoc/>
        public void WriteInt32(int value)
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
                _temp[0] = (byte)(value);
                _temp[1] = (byte)(value >> 8);
                _temp[2] = (byte)(value >> 16);
                _temp[3] = (byte)(value >> 24);
                _byteBuffer.SetBytes(_position, _temp, 0, 4);
            }

            SetPositionAfterWrite(_position + 4);
        }

        /// <inheritdoc/>
        public void WriteInt64(long value)
        {
            PrepareToWrite(8);

            var bytes = BitConverter.GetBytes(value);
            _byteBuffer.SetBytes(_position, bytes, 0, 8);

            SetPositionAfterWrite(_position + 8);
        }

        /// <inheritdoc/>
        public void WriteObjectId(ObjectId value)
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
                _byteBuffer.SetBytes(_position, bytes, 0, 12);
            }

            SetPositionAfterWrite(_position + 12);
        }

        /// <inheritdoc/>
        public void WriteString(string value, UTF8Encoding encoding)
        {
            var maxLength = encoding.GetMaxByteCount(value.Length) + 5;
            PrepareToWrite(maxLength);

            int actualLength;
            var segment = _byteBuffer.AccessBackingBytes(_position);
            if (segment.Count >= maxLength)
            {
                actualLength = encoding.GetBytes(value, 0, value.Length, segment.Array, segment.Offset + 4);

                var lengthPlusOne = actualLength + 1;
                segment.Array[segment.Offset] = (byte)lengthPlusOne;
                segment.Array[segment.Offset + 1] = (byte)(lengthPlusOne >> 8);
                segment.Array[segment.Offset + 2] = (byte)(lengthPlusOne >> 16);
                segment.Array[segment.Offset + 3] = (byte)(lengthPlusOne >> 24);
                segment.Array[segment.Offset + 4 + actualLength] = 0;
            }
            else
            {
                byte[] bytes;
                if (maxLength <= _tempUtf8.Length)
                {
                    bytes = _tempUtf8;
                    actualLength = encoding.GetBytes(value, 0, value.Length, bytes, 0);
                }
                else
                {
                    bytes = encoding.GetBytes(value);
                    actualLength = bytes.Length;
                }

                var lengthPlusOneBytes = BitConverter.GetBytes(actualLength + 1);

                _byteBuffer.SetBytes(_position, lengthPlusOneBytes, 0, 4);
                _byteBuffer.SetBytes(_position + 4, bytes, 0, actualLength);
                _byteBuffer.SetByte(_position + 4 + actualLength, 0);
            }

            SetPositionAfterWrite(_position + actualLength + 5);
        }
    }
}
