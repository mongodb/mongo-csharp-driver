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
        public BsonBuffer() {
            // let EnsureAvailable get the first chunk
        }
        #endregion

        #region public static properties
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
        public void Backpatch(
            int position,
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            // use local chunk variables because we're writing at a different position
            int chunkIndex = position / chunkSize;
            int chunkOffset = position % chunkSize;
            var chunk = chunks[chunkIndex];
            if (chunkSize - chunkOffset >= 4) {
                chunk[chunkOffset + 0] = (byte) (value);
                chunk[chunkOffset + 1] = (byte) (value >> 8);
                chunk[chunkOffset + 2] = (byte) (value >> 16);
                chunk[chunkOffset + 3] = (byte) (value >> 24);
            } else {
                // straddles chunk boundary
                for (int i = 0; i < 4; i++) {
                    chunk[chunkOffset++] = (byte) value;
                    if (chunkOffset == chunkSize) {
                        chunk = chunks[++chunkIndex];
                        chunkOffset = 0;
                    }
                    value >>= 8;
                }
            }
        }

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

        public void Dispose() {
            if (!disposed) {
                Clear();
                disposed = true;
            }
        }

        // this overload assumes the stream is positioned at a 4 byte length field
        public void LoadFrom(
            Stream stream
        ) {
            LoadFrom(stream, 4); // does not advance position
            int length = ReadInt32(); // advances position 4 bytes
            LoadFrom(stream, length - 4); // does not advance position
            Position -= 4; // move back to just before the length field
        }

        // loads from stream to the current position leaving position unchanged
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
                        // TODO: timeout?
                        Thread.Sleep(5); // just enough to not be busy waiting
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

        public BsonType PeekBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var bsonType = (BsonType) chunk[chunkOffset];
            if (!IsValidBsonType(bsonType)) {
                string message = string.Format("Invalid BsonType: {0}", (int) bsonType);
                throw new FileFormatException(message);
            }
            return bsonType;
        }

        public byte PeekByte() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            return chunk[chunkOffset];
        }

        public bool ReadBoolean() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var value = chunk[chunkOffset] != 0;
            Position++;
            return value;
        }

        public BsonType ReadBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var bsonType = (BsonType) chunk[chunkOffset];
            if (!IsValidBsonType(bsonType)) {
                string message = string.Format("Invalid BsonType: {0}", (int) bsonType);
                throw new FileFormatException(message);
            }
            Position++;
            return bsonType;
        }

        public byte ReadByte() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureDataAvailable(1);
            var value = chunk[chunkOffset];
            Position++;
            return value;
        }

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
                throw new FileFormatException("String is missing null terminator");
            }
            return value;
        }

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

            throw new FileFormatException("String is missing null terminator");
        }

        public void Skip(
            int count
        ) {
            // TODO: optimize this method
            Position += count;
        }

        public void SkipCString() {
            // TODO: optimize this method
            ReadCString();
        }

        public byte[] ToByteArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            var bytes = new byte[position];
            CopyTo(0, bytes, 0, position);
            return bytes;
        }

        public void WriteBoolean(
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(1);
            chunk[chunkOffset] = value ? (byte) 1 : (byte) 0;
            Position++;
        }

        public void WriteByte(
            byte value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBuffer"); }
            EnsureSpaceAvailable(1);
            chunk[chunkOffset] = value;
            Position++;
        }

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
                    "Not enough input bytes available: {0} needed but only {1} available (at position {2})",
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
