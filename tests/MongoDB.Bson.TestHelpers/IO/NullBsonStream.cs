/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.IO;

namespace MongoDB.Bson.TestHelpers.IO
{
    public class NullBsonStream : BsonStream
    {
        private long _length;
        private long _position;

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                if (_length < _position)
                {
                    _length = _position;
                }
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override string ReadCString(UTF8Encoding encoding)
        {
            throw new NotSupportedException();
        }

        public override ArraySegment<byte> ReadCStringBytes()
        {
            throw new NotSupportedException();
        }

        public override Decimal128 ReadDecimal128()
        {
            throw new NotSupportedException();
        }

        public override double ReadDouble()
        {
            throw new NotSupportedException();
        }

        public override int ReadInt32()
        {
            throw new NotSupportedException();
        }

        public override long ReadInt64()
        {
            throw new NotSupportedException();
        }

        public override ObjectId ReadObjectId()
        {
            throw new NotSupportedException();
        }

        public override IByteBuffer ReadSlice()
        {
            throw new NotSupportedException();
        }

        public override string ReadString(UTF8Encoding encoding)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: Position = offset; break;
                case SeekOrigin.Current: Position = _position + offset; break;
                case SeekOrigin.End: Position = _length + offset; break;
                default: throw new ArgumentException("Invalid origin.", "origin");
            }
            return _position;
        }

        public override void SetLength(long value)
        {
            _length = value;
        }

        public override void SkipCString()
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Position += count;
        }

        public override void WriteByte(byte value)
        {
            Position += 1;
        }

        public override void WriteCString(string value)
        {
            var length = Utf8Encodings.Strict.GetByteCount(value);
            Position += length + 1;
        }

        public override void WriteCStringBytes(byte[] value)
        {
            Position += value.Length + 1;
        }

        public override void WriteDecimal128(Decimal128 value)
        {
            Position += 16;
        }

        public override void WriteDouble(double value)
        {
            Position += 8;
        }

        public override void WriteInt32(int value)
        {
            Position += 4;
        }

        public override void WriteInt64(long value)
        {
            Position += 8;
        }

        public override void WriteObjectId(ObjectId value)
        {
            Position += 12;
        }

        public override void WriteString(string value, UTF8Encoding encoding)
        {
            var length = encoding.GetByteCount(value);
            Position += 4 + length + 1;
        }
    }
}
