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
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a writer that writes primitive BSON types to a Stream.
    /// </summary>
    public class BsonStreamWriter
    {
        // private fields
        private readonly Stream _stream;
        private readonly IBsonStream _bsonStream;
        private readonly UTF8Encoding _encoding;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonStreamWriter" /> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public BsonStreamWriter(Stream stream, UTF8Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            _stream = stream;
            _bsonStream = stream as IBsonStream;
            _encoding = encoding;
        }

        // public properties
        /// <summary>
        /// Gets the base stream.
        /// </summary>
        /// <value>
        /// The base stream.
        /// </value>
        public Stream BaseStream
        {
            get { return _stream; }
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        // public methods
        /// <summary>
        /// Writes a BSON boolean to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteBoolean(bool value)
        {
            _stream.WriteByte((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// Writes a BSON type code to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteBsonType(BsonType value)
        {
            _stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a byte to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBytes(byte[] value)
        {
            _stream.Write(value, 0, value.Length);
        }
        
        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        /// <exception cref="System.ArgumentException">UTF8 representation cannot contain null bytes when writing a BSON CString.;value</exception>
        public void WriteCString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (_bsonStream != null)
            {
                _bsonStream.WriteBsonCString(value);
            }
            else
            {
                var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(value);
                if (bytes.Contains<byte>(0))
                {
                    throw new ArgumentException("UTF8 representation cannot contain null bytes when writing a BSON CString.", "value");
                }
                _stream.Write(bytes, 0, bytes.Length);
                _stream.WriteByte(0);
            }
        }

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteDouble(double value)
        {
            if (_bsonStream != null)
            {
                _bsonStream.WriteBsonDouble(value);
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                _stream.Write(bytes, 0, 8);
            }
        }

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteInt32(int value)
        {
            if (_bsonStream != null)
            {
                _bsonStream.WriteBsonInt32(value);
            }
            else
            {
                var bytes = new byte[4];
                bytes[0] = (byte)value;
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                _stream.Write(bytes, 0, 4);
            }
        }

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteInt64(long value)
        {
            if (_bsonStream != null)
            {
                _bsonStream.WriteBsonInt64(value);
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
                _stream.Write(bytes, 0, 8);
            }
        }

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteObjectId(ObjectId value)
        {
            if (_bsonStream != null)
            {
                _bsonStream.WriteBsonObjectId(value);
            }
            else
            {
                var bytes = value.ToByteArray();
                _stream.Write(bytes, 0, 12);
            }
        }

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        public void WriteString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (_bsonStream != null)
            {
                _bsonStream.WriteBsonString(value, _encoding);
            }
            else
            {
                var bytes = _encoding.GetBytes(value);
                WriteInt32(bytes.Length + 1);
                WriteBytes(bytes);
                WriteByte(0);
            }
        }
    }
}
