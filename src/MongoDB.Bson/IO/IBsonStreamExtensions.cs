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
    /// Represents extension methods on IBsonStream.
    /// </summary>
    public static class IBsonStreamExtensions
    {
        // static fields
        private static readonly bool[] __validBsonTypes = new bool[256];

        // static constructor
        static IBsonStreamExtensions()
        {
            foreach (BsonType bsonType in Enum.GetValues(typeof(BsonType)))
            {
                __validBsonTypes[(byte)bsonType] = true;
            }
        }

        // static methods
        /// <summary>
        /// Backpatches the size.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="startPosition">The start position.</param>
        public static void BackpatchSize(this IBsonStream stream, long startPosition)
        {
            var endPosition = stream.Position;
            var size = (int)(endPosition - startPosition);
            stream.Position = startPosition;
            stream.WriteInt32(size);
            stream.Position = endPosition;
        }

        /// <summary>
        /// Reads the binary sub type.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The binary sub type.</returns>
        public static BsonBinarySubType ReadBinarySubType(this IBsonStream stream)
        {
            var b = stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }
            return (BsonBinarySubType)b;
        }

        /// <summary>
        /// Reads a boolean from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A boolean.</returns>
        public static bool ReadBoolean(this IBsonStream stream)
        {
            var b = stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }
            return b != 0;
        }

        /// <summary>
        /// Reads the BSON type.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The BSON type.</returns>
        public static BsonType ReadBsonType(this IBsonStream stream)
        {
            var b = stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }
            if (!__validBsonTypes[b])
            {
                string message = string.Format("Invalid BsonType: {0}.", b);
                throw new FormatException(message);
            }
            return (BsonType)b;
        }

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public static void ReadBytes(this IBsonStream stream, byte[] buffer, int offset, int count)
        {
            if (count == 1)
            {
                var b = stream.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                buffer[offset] = (byte)b;
            }
            else
            {
                while (count > 0)
                {
                    var bytesRead = stream.Read(buffer, offset, count);
                    if (bytesRead == 0)
                    {
                        throw new EndOfStreamException();
                    }
                    offset += bytesRead;
                    count -= bytesRead;
                }
            }
        }

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="count">The count.</param>
        /// <returns>The bytes.</returns>
        public static byte[] ReadBytes(this IBsonStream stream, int count)
        {
            var bytes = new byte[count];
            stream.ReadBytes(bytes, 0, count);
            return bytes;
        }

        /// <summary>
        /// Writes a binary sub type to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteBinarySubType(this IBsonStream stream, BsonBinarySubType value)
        {
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a boolean to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteBoolean(this IBsonStream stream, bool value)
        {
            stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        /// <summary>
        /// Writes a BsonType to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteBsonType(this IBsonStream stream, BsonType value)
        {
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public static void WriteBytes(this IBsonStream stream, byte[] buffer, int offset, int count)
        {
            if (count == 1)
            {
                stream.WriteByte(buffer[offset]);
            }
            else
            {
                stream.Write(buffer, offset, count);
            }
        }

        /// <summary>
        /// Writes a slice to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="slice">The slice.</param>
        public static void WriteSlice(this IBsonStream stream, IByteBuffer slice)
        {
            var position = 0;
            var count = slice.Length;

            while (count > 0)
            {
                var segment = slice.AccessBackingBytes(position);
                var partialCount = Math.Min(count, segment.Count);
                stream.WriteBytes(segment.Array, segment.Offset, partialCount);
                position += count;
                count -= partialCount;
            }
        }
    }
}
