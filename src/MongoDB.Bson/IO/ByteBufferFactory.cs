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

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a factory for IBsonBuffers.
    /// </summary>
    public static class ByteBufferFactory
    {
        /// <summary>
        /// Creates a buffer of the specified length. Depending on the length, either a SingleChunkBuffer or a MultiChunkBuffer will be created.
        /// </summary>
        /// <param name="chunkPool">The chunk pool.</param>
        /// <param name="length">The length.</param>
        /// <returns>A buffer.</returns>
        public static IByteBuffer Create(BsonChunkPool chunkPool, int length)
        {
            if (chunkPool == null)
            {
                throw new ArgumentNullException("pool");
            }
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (length < chunkPool.ChunkSize)
            {
                var chunk = chunkPool.AcquireChunk();
                return new SingleChunkBuffer(chunk, 0, length, false);
            }
            else
            {
                var chunksNeeded = ((length - 1) / chunkPool.ChunkSize) + 1;
                var chunks = new List<BsonChunk>(chunksNeeded);
                for (int i = 0; i < chunksNeeded; i++)
                {
                    chunks.Add(chunkPool.AcquireChunk());
                }
                return new MultiChunkBuffer(chunks, 0, length, false);
            }
        }

        /// <summary>
        /// Loads a byte buffer from a stream (the first 4 bytes in the stream are the length of the data).
        /// Depending on the required capacity, either a SingleChunkBuffer or a MultiChunkBuffer will be created.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A buffer.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static IByteBuffer LoadLengthPrefixedDataFrom(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var streamReader = new BsonStreamReader(stream, Utf8Helper.StrictUtf8Encoding);
            var length = streamReader.ReadInt32();

            var byteBuffer = Create(BsonChunkPool.Default, length);
            byteBuffer.Length = length;
            byteBuffer.WriteBytes(0, BitConverter.GetBytes(length), 0, 4);
            byteBuffer.LoadFrom(stream, 4, length - 4);
            byteBuffer.MakeReadOnly();

            return byteBuffer;
        }
    }
}
