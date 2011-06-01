/* Copyright 2010-2011 10gen Inc.
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
using System.Threading;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a buffer for BSON encoded bytes.
    /// </summary>
    public class BsonBuffer : IDisposable {
        #region private static fields
        private static Stack<byte[]> chunkPool = new Stack<byte[]>();
        private static int maxChunkPoolSize = 64;
        private const int chunkSize = 16 * 1024; // 16KiB
        private static readonly bool[] validBsonTypes = new bool[256];
        #endregion

        #region private fields
        private bool disposed = false;
        private List<byte[]> chunks = new List<byte[]>(4);
        private int capacity;
        private int length; // always use property to set so position can be adjusted if necessary
        private int position; // always use property to set so chunkIndex/chunkOffset/chunk/length will be set also
        private int chunkIndex;
        private int chunkOffset;
        private byte[] chunk;
        #endregion

        #region static constructor
        static BsonBuffer() {
            foreach (BsonType bsonType in Enum.GetValues(typeof(BsonType))) {
                validBsonTypes[(byte) bsonType] = true;
            }
        }
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBuffer class.
        /// </summary>
        public BsonBuffer() {
            // let EnsureAvailable get the first chunk
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the max chunk pool size.
        /// </summary>
        public static int MaxChunkPoolSize {
            get {
                lock (chunkPool) {
                    return maxChunkPoolSize;
                }
            }
            set {
                lock (chunkPool) {
                    maxChunkPoolSize = value;
                }
            }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the length of the data in the buffer.
        /// </summary>
        public int Length {
            get {
                if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
                return length;
            }
            set {
                if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
                EnsureSpaceAvailable(value - position);
                length = value;
                if (position > value) {
                    Position = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current position in the buffer.
        /// </summary>
        public int Position {
            get {
                if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
                return position;
            }
            set {
                if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
                if (chunkIndex != value / chunkSize) {
                    chunkIndex = value / chunkSize;
                    if (chunkIndex < chunks.Count) {
                        chunk = chunks[chunkIndex];
                    } else {
                        chunk = null; // EnsureSpaceAvailable will set this when it gets the chunk
                    }
                }
                chunkOffset = value % chunkSize;
                position = value;
                if (length < value) {
                    length = value;
                }
            }
        }
        #endregion

        #region private static methods
        private static byte[] GetChunk() {
            lock (chunkPool) {
                if (chunkPool.Count > 0) {
                    return chunkPool.Pop();
                }
            }
            return new byte[chunkSize]; // release the lock before allocating memory
        }

        private static bool IsValidBsonType(
            BsonType bsonType
        ) {
            return validBsonTypes[(byte) bsonType];
        }

        private static void ReleaseChunk(
            byte[] chunk
        ) {
            lock (chunkPool) {
                if (chunkPool.Count < maxChunkPoolSize) {
                    chunkPool.Push(chunk);
                }
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Backpatches the length of an object.
        /// </summary>
        /// <param name="position">The start position of the object.</param>
        /// <param name="length">The length of the object.</param>
        public void Backpatch(
            int position,
            int length
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            // use local chunk variables because we're writing at a different position
            int chunkIndex = position / chunkSize;
            int chunkOffset = position % chunkSize;
            var chunk = chunks[chunkIndex];
            if (chunkSize - chunkOffset >= 4) {
                chunk[chunkOffset + 0] = (byte) (length);
                chunk[chunkOffset + 1] = (byte) (length >> 8);
                chunk[chunkOffset + 2] = (byte) (length >> 16);
                chunk[chunkOffset + 3] = (byte) (length >> 24);
            } else {
                // straddles chunk boundary
                for (int i = 0; i < 4; i++) {
                    chunk[chunkOffset++] = (byte) length;
                    if (chunkOffset == chunkSize) {
                        chunk = chunks[++chunkIndex];
                        chunkOffset = 0;
                    }
                    length >>= 8;
                }
            }
        }

        /// <summary>
        /// Clears the data in the buffer.
        /// </summary>
        public void Clear() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            Position = 0;
            foreach (var localChunk in chunks) {
                ReleaseChunk(localChunk);
            }
            chunks.Clear();
            chunk = null;
            capacity = 0;
        }

        /// <summary>
        /// Copies data from the buffer to a byte array.
        /// </summary>
        /// <param name="sourceOffset">The source offset in the buffer.</param>
        /// <param name="destination">The destination byte array.</param>
        /// <param name="destinationOffset">The destination offset in the byte array.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public void CopyTo(
            int sourceOffset,
            byte[] destination,
            int destinationOffset,
            int count
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            var chunkIndex = sourceOffset / chunkSize;
            var chunkOffset = sourceOffset % chunkSize;
            while (count > 0) {
                var chunk = chunks[chunkIndex];
                var available = chunkSize - chunkOffset;
                var partialCount = (count > available) ? available : count;
                Buffer.BlockCopy(chunk, chunkOffset, destination, destinationOffset, partialCount);
                chunkIndex++;
                chunkOffset = 0;
                destinationOffset += partialCount;
                count -= partialCount;
            }
        }

        /// <summary>
        /// Disposes of any resources held by the buffer.
        /// </summary>
        public void Dispose() {
            if (!disposed) {
                Clear();
                disposed = true;
            }
        }

        /// <summary>
        /// Loads the buffer from a Stream (the Stream must be positioned at a 4 byte length field).
        /// </summary>
        /// <param name="stream">The Stream.</param>
        public void LoadFrom(
            Stream stream
        ) {
            LoadFrom(stream, 4); // does not advance position
            int length = ReadInt32(); // advances position 4 bytes
            LoadFrom(stream, length - 4); // does not advance position
            Position -= 4; // move back to just before the length field
        }

        /// <summary>
        /// Loads the buffer from a Stream (leaving the position in the buffer unchanged).
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="count">The number of bytes to load.</param>
        public void LoadFrom(
            Stream stream,
            int count
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }

            EnsureSpaceAvailable(count);
            var localChunkIndex = chunkIndex;
            var localChunkOffset = chunkOffset;
            while (count > 0) {
                var localChunk = chunks[localChunkIndex];
                int available = chunkSize - localChunkOffset;
                int partialCount = (count > available) ? available : count;
                // might take several reads, you never know with a network stream.
                int bytesPending = partialCount;
                while (bytesPending > 0) {
                    var bytesRead = stream.Read(localChunk, localChunkOffset, bytesPending);
                    if (bytesRead == 0) {
                        throw new EndOfStreamException();
                    } else {
                        localChunkOffset += bytesRead;
                        bytesPending -= bytesRead;
                    }
                }
                localChunkIndex++;
                localChunkOffset = 0;
                count -= partialCount;
                length += partialCount;
            }
        }

        /// <summary>
        /// Peeks at the next byte in the buffer and returns it as a BsonType.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public BsonType PeekBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var bsonType = (BsonType) chunk[chunkOffset];
            if (!IsValidBsonType(bsonType)) {
                string message = string.Format("Invalid BsonType {0}.", (int) bsonType);
                throw new FileFormatException(message);
            }
            return bsonType;
        }

        /// <summary>
        /// Peeks at the next byte in the buffer.
        /// </summary>
        /// <returns>A Byte.</returns>
        public byte PeekByte() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            return chunk[chunkOffset];
        }

        /// <summary>
        /// Reads a BSON Boolean from the buffer.
        /// </summary>
        /// <returns>A Boolean.</returns>
        public bool ReadBoolean() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var value = chunk[chunkOffset] != 0;
            Position++;
            return value;
        }

        /// <summary>
        /// Reads a BSON type from the buffer.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public BsonType ReadBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var bsonType = (BsonType) chunk[chunkOffset];
            if (!IsValidBsonType(bsonType)) {
                string message = string.Format("Invalid BsonType {0}.", (int) bsonType);
                throw new FileFormatException(message);
            }
            Position++;
            return bsonType;
        }

        /// <summary>
        /// Reads a byte from the buffer.
        /// </summary>
        /// <returns>A Byte.</returns>
        public byte ReadByte() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var value = chunk[chunkOffset];
            Position++;
            return value;
        }

        /// <summary>
        /// Reads bytes from the buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>A byte array.</returns>
        public byte[] ReadBytes(
            int count
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(count);
            var value = new byte[count];
            int destinationOffset = 0;
            while (count > 0) {
                // might only be reading part of the first or last chunk
                int available = chunkSize - chunkOffset;
                int partialCount = (count > available) ? available : count;
                Buffer.BlockCopy(chunk, chunkOffset, value, destinationOffset, partialCount);
                Position += partialCount;
                destinationOffset += partialCount;
                count -= partialCount;
            }
            return value;
        }

        /// <summary>
        /// Reads a BSON Double from the buffer.
        /// </summary>
        /// <returns>A Double.</returns>
        public double ReadDouble() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(8);
            if (chunkSize - chunkOffset >= 8) {
                var value = BitConverter.ToDouble(chunk, chunkOffset);
                Position += 8;
                return value;
            } else {
                return BitConverter.ToDouble(ReadBytes(8), 0); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Reads a BSON Int32 from the reader.
        /// </summary>
        /// <returns>An Int32.</returns>
        public int ReadInt32() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(4);
            if (chunkSize - chunkOffset >= 4) {
                // for int only we come out ahead with this code vs using BitConverter
                var value =
                    ((int) chunk[chunkOffset + 0]) +
                    ((int) chunk[chunkOffset + 1] << 8) +
                    ((int) chunk[chunkOffset + 2] << 16) +
                    ((int) chunk[chunkOffset + 3] << 24);
                Position += 4;
                return value;
            } else {
                return BitConverter.ToInt32(ReadBytes(4), 0); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Reads a BSON Int64 from the reader.
        /// </summary>
        /// <returns>An Int64.</returns>
        public long ReadInt64() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(8);
            if (chunkSize - chunkOffset >= 8) {
                var value = BitConverter.ToInt64(chunk, chunkOffset);
                Position += 8;
                return value;
            } else {
                return BitConverter.ToInt64(ReadBytes(8), 0); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public void ReadObjectId(
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(12);
            if (chunkSize - chunkOffset >= 12) {
                var c = chunk;
                var o = chunkOffset;
                timestamp = (c[o + 0] << 24) + (c[o + 1] << 16) + (c[o + 2] << 8) + c[o + 3];
                machine = (c[o + 4] << 16) + (c[o + 5] << 8) + c[o + 6];
                pid = (short) ((c[o + 7] << 8) + c[o + 8]);
                increment = (c[o + 9] << 16) + (c[o + 10] << 8) + c[o + 11];
                Position += 12;
            } else {
                var bytes = ReadBytes(12); // straddles chunk boundary
                ObjectId.Unpack(bytes, out timestamp, out machine, out pid, out increment);
            }
        }

        /// <summary>
        /// Reads a BSON string from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        public string ReadString() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            var length = ReadInt32();
            EnsureDataAvailable(length + 1);
            string value;
            if (chunkSize - chunkOffset >= length - 1) {
                value = Encoding.UTF8.GetString(chunk, chunkOffset, length - 1);
                Position += length - 1;
            } else {
                // straddles chunk boundary
                value = Encoding.UTF8.GetString(ReadBytes(length - 1));
            }
            byte terminator = ReadByte();
            if (terminator != 0) {
                throw new FileFormatException("String is missing null terminator.");
            }
            return value;
        }

        /// <summary>
        /// Reads a BSON CString from the reader (a null terminated string).
        /// </summary>
        /// <returns>A String.</returns>
        public string ReadCString() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            // optimize for the case where the null terminator is on the same chunk
            int partialCount;
            if (chunkIndex < chunks.Count - 1) {
                partialCount = chunkSize - chunkOffset; // remaining part of any chunk but the last
            } else {
                partialCount = length - position; // populated part of last chunk
            }
            var index = Array.IndexOf<byte>(chunk, 0, chunkOffset, partialCount);
            if (index != -1) {
                var stringLength = index - chunkOffset;
                var value = Encoding.UTF8.GetString(chunk, chunkOffset, stringLength);
                Position += stringLength + 1;
                return value;
            }

            // the null terminator is not on the same chunk so keep looking starting with the next chunk
            var localChunkIndex = chunkIndex + 1;
            var localPosition = localChunkIndex * chunkSize;
            while (localPosition < length) {
                var localChunk = chunks[localChunkIndex];
                if (localChunkIndex < chunks.Count - 1) {
                    partialCount = chunkSize; // all of any chunk but the last
                } else {
                    partialCount = length - localPosition; // populated part of last chunk
                }
                index = Array.IndexOf<byte>(localChunk, 0, 0, partialCount);
                if (index != -1) {
                    localPosition += index;
                    var stringLength = localPosition - position;
                    var value = Encoding.UTF8.GetString(ReadBytes(stringLength)); // ReadBytes advances over string
                    Position += 1; // skip over null byte at end
                    return value;
                }
                localChunkIndex++;
                localPosition += chunkSize;
            }

            throw new FileFormatException("String is missing null terminator.");
        }

        /// <summary>
        /// Skips over bytes in the buffer (advances the position).
        /// </summary>
        /// <param name="count">The number of bytes to skip.</param>
        public void Skip(
            int count
        ) {
            // TODO: optimize this method
            Position += count;
        }

        /// <summary>
        /// Skips over a CString in the buffer (advances the position).
        /// </summary>
        public void SkipCString() {
            // TODO: optimize this method
            ReadCString();
        }

        /// <summary>
        /// Converts the buffer to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            var bytes = new byte[position];
            CopyTo(0, bytes, 0, position);
            return bytes;
        }

        /// <summary>
        /// Writes a BSON Boolean to the buffer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        public void WriteBoolean(
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(1);
            chunk[chunkOffset] = value ? (byte) 1 : (byte) 0;
            Position++;
        }

        /// <summary>
        /// Writes a byte to the buffer.
        /// </summary>
        /// <param name="value">A byte.</param>
        public void WriteByte(
            byte value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(1);
            chunk[chunkOffset] = value;
            Position++;
        }

        /// <summary>
        /// Writes bytes to the buffer.
        /// </summary>
        /// <param name="value">A byte array.</param>
        public void WriteBytes(
            byte[] value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(value.Length);
            int sourceOffset = 0;
            int count = value.Length;
            while (count > 0) {
                // possibly straddles chunk boundary initially
                int available = chunkSize - chunkOffset;
                int partialCount = (count > available) ? available : count;
                Buffer.BlockCopy(value, sourceOffset, chunk, chunkOffset, partialCount);
                Position += partialCount;
                sourceOffset += partialCount;
                count -= partialCount;
            }
        }

        /// <summary>
        /// Writes a CString to the buffer.
        /// </summary>
        /// <param name="value">A string.</param>
        public void WriteCString(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            int maxLength = Encoding.UTF8.GetMaxByteCount(value.Length) + 1;
            EnsureSpaceAvailable(maxLength);
            if (chunkSize - chunkOffset >= maxLength) {
                int length = Encoding.UTF8.GetBytes(value, 0, value.Length, chunk, chunkOffset);
                chunk[chunkOffset + length] = 0;
                Position += length + 1;
            } else {
                // straddles chunk boundary
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                WriteBytes(bytes);
                WriteByte(0);
            }
        }

        /// <summary>
        /// Writes a BSON Double to the buffer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public void WriteDouble(
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(8);
            if (chunkSize - chunkOffset >= 8) {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, chunk, chunkOffset, 8);
                Position += 8;
            } else {
                WriteBytes(BitConverter.GetBytes(value)); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Writes a BSON Int32 to the buffer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        public void WriteInt32(
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(4);
            if (chunkSize - chunkOffset >= 4) {
                // for int only we come out ahead with this code vs using BitConverter
                chunk[chunkOffset + 0] = (byte) (value);
                chunk[chunkOffset + 1] = (byte) (value >> 8);
                chunk[chunkOffset + 2] = (byte) (value >> 16);
                chunk[chunkOffset + 3] = (byte) (value >> 24);
                Position += 4;
            } else {
                WriteBytes(BitConverter.GetBytes(value)); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Writes a BSON Int64 to the buffer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public void WriteInt64(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(8);
            if (chunkSize - chunkOffset >= 8) {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, chunk, chunkOffset, 8);
                Position += 8;
            } else {
                WriteBytes(BitConverter.GetBytes(value)); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Writes a BSON ObjectId to the buffer.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public void WriteObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(12);
            if (chunkSize - chunkOffset >= 12) {
                chunk[chunkOffset + 0] = (byte) (timestamp >> 24);
                chunk[chunkOffset + 1] = (byte) (timestamp >> 16);
                chunk[chunkOffset + 2] = (byte) (timestamp >> 8);
                chunk[chunkOffset + 3] = (byte) (timestamp);
                chunk[chunkOffset + 4] = (byte) (machine >> 16);
                chunk[chunkOffset + 5] = (byte) (machine >> 8);
                chunk[chunkOffset + 6] = (byte) (machine);
                chunk[chunkOffset + 7] = (byte) (pid >> 8);
                chunk[chunkOffset + 8] = (byte) (pid);
                chunk[chunkOffset + 9] = (byte) (increment >> 16);
                chunk[chunkOffset + 10] = (byte) (increment >> 8);
                chunk[chunkOffset + 11] = (byte) (increment);
                Position += 12;
            } else {
                WriteBytes(ObjectId.Pack(timestamp, machine, pid, increment)); // straddles chunk boundary
            }
        }

        /// <summary>
        /// Writes a BSON String to the buffer.
        /// </summary>
        /// <param name="value">The String value.</param>
        public void WriteString(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            int maxLength = Encoding.UTF8.GetMaxByteCount(value.Length) + 5;
            EnsureSpaceAvailable(maxLength);
            if (chunkSize - chunkOffset >= maxLength) {
                int length = Encoding.UTF8.GetBytes(value, 0, value.Length, chunk, chunkOffset + 4); // write string first
                int lengthPlusOne = length + 1;
                chunk[chunkOffset + 0] = (byte) (lengthPlusOne); // now we know the length
                chunk[chunkOffset + 1] = (byte) (lengthPlusOne >> 8);
                chunk[chunkOffset + 2] = (byte) (lengthPlusOne >> 16);
                chunk[chunkOffset + 3] = (byte) (lengthPlusOne >> 24);
                chunk[chunkOffset + 4 + length] = 0;
                Position += length + 5;
            } else {
                // straddles chunk boundary
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                WriteInt32(bytes.Length + 1);
                WriteBytes(bytes);
                WriteByte(0);
            }
        }

        /// <summary>
        /// Writes all the data in the buffer to a Stream.
        /// </summary>
        /// <param name="stream">The Stream.</param>
        public void WriteTo(
            Stream stream
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            if (position > 0) {
                var wholeChunks = position / chunkSize;
                for (int chunkIndex = 0; chunkIndex < wholeChunks; chunkIndex++) {
                    stream.Write(chunks[chunkIndex], 0, chunkSize);
                }

                var partialChunkSize = position % chunkSize;
                if (partialChunkSize != 0) {
                    stream.Write(chunks[wholeChunks], 0, partialChunkSize);
                }
            }
        }

        /// <summary>
        /// Writes a 32-bit zero the the buffer.
        /// </summary>
        public void WriteZero() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(4);
            if (chunkSize - chunkOffset >= 4) {
                // for int only we come out ahead with this code vs using BitConverter
                chunk[chunkOffset + 0] = 0;
                chunk[chunkOffset + 1] = 0;
                chunk[chunkOffset + 2] = 0;
                chunk[chunkOffset + 3] = 0;
                Position += 4;
            } else {
                WriteBytes(BitConverter.GetBytes((int) 0)); // straddles chunk boundary
            }
        }
        #endregion

        #region private methods
        private void EnsureDataAvailable(
            int needed
        ) {
            if (length - position < needed) {
                var available = length - position;
                var message = string.Format(
                    "Not enough input bytes available. Needed {0}, but only {1} are available (at position {2}).",
                    needed,
                    available,
                    position
                );
                throw new EndOfStreamException(message);
            }
        }

        private void EnsureSpaceAvailable(
            int needed
        ) {
            if (capacity - position < needed) {
                // either we have no chunks or we just crossed a chunk boundary landing at chunkOffset 0
                if (chunk == null) {
                    chunk = GetChunk();
                    chunks.Add(chunk);
                    capacity += chunkSize;
                }

                while (capacity - position < needed) {
                    chunks.Add(GetChunk());
                    capacity += chunkSize;
                }
            }
        }
        #endregion
    }
}
