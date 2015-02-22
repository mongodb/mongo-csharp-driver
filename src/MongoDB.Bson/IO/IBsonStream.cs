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
    /// Represents specialized performance critial reading and writing methods for BSON values. Classes
    /// that implement Stream can choose to also implement this interface to improve performance when
    /// reading and writing BSON values.
    /// </summary>
    public interface IBsonStream
    {
        // properties
        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        long Length { get; }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        long Position { get; set; }

        /// <summary>
        /// Gets the base stream.
        /// </summary>
        /// <value>
        /// The base stream.
        /// </value>
        Stream BaseStream { get; }

        // methods
        /// <summary>
        /// Flushes the stream.
        /// </summary>
        void Flush();

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns>The number of bytes read (0 if at end of stream).</returns>
        int Read(byte[] buffer, int offset, int count);

        /// <summary>
        /// Reads a byte.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        int ReadByte();

        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string.</returns>
        string ReadCString(UTF8Encoding encoding);

        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <returns>An ArraySegment containing the CString bytes (without the null byte).</returns>
        ArraySegment<byte> ReadCStringBytes();

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <returns>A double.</returns>
        double ReadDouble();

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>An int.</returns>
        int ReadInt32();

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>A long.</returns>
        long ReadInt64();

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        ObjectId ReadObjectId();

        /// <summary>
        /// Reads a raw length prefixed slice from the stream.
        /// </summary>
        /// <returns>A slice.</returns>
        IByteBuffer ReadSlice();

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string.</returns>
        string ReadString(UTF8Encoding encoding);

        /// <summary>
        /// Sets the position within the stream.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="origin">The origin.</param>
        /// <returns>The new position.</returns>
        long Seek(long offset, SeekOrigin origin);

        /// <summary>
        /// Sets the length.
        /// </summary>
        /// <param name="value">The value.</param>
        void SetLength(long value);

        /// <summary>
        /// Skips over a BSON CString leaving the stream positioned just after the terminating null byte.
        /// </summary>
        void SkipCString();

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        void Write(byte[] buffer, int offset, int count);

        /// <summary>
        /// Writes a byte to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteByte(byte value);

        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteCString(string value);

        /// <summary>
        /// Writes the CString bytes to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteCStringBytes(byte[] value);

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteDouble(double value);

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteInt32(int value);

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteInt64(long value);

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteObjectId(ObjectId value);

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        void WriteString(string value, UTF8Encoding encoding);
    }
}
