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
        /// <param name="chunkSource">The chunk pool.</param>
        /// <param name="requiredCapacity">The required capacity.</param>
        /// <returns>A buffer with at least the required capacity.</returns>
        public static IByteBuffer Create(IBsonChunkSource chunkSource, int requiredCapacity)
        {
            if (chunkSource == null)
            {
                throw new ArgumentNullException("chunkSource");
            }
            if (requiredCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException("requiredCapacity");
            }

            var capacity = 0;
            var chunks = new List<IBsonChunk>();
            while (capacity < requiredCapacity)
            {
                var chunk = chunkSource.GetChunk(requiredCapacity - capacity);
                chunks.Add(chunk);
                capacity += chunk.Bytes.Count;
            }

            if (chunks.Count == 1)
            {
                return new SingleChunkBuffer(chunks[0], 0, isReadOnly: false);
            }
            else
            {
                return new MultiChunkBuffer(0, chunks, isReadOnly: false);
            }
        }
    }
}
