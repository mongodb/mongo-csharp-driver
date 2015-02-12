/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Shared;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a source of chunks optimized for input buffers.
    /// </summary>
    public sealed class InputBufferChunkSource : IBsonChunkSource
    {
        // constants
        const int DefaultMaxChunkSize = 1 * 1024 * 1024;
        const int DefaultMaxUnpooledChunkSize = 4 * 1024;
        const int DefaultMinChunkSize = 16 * 1024;

        // fields
        private readonly IBsonChunkSource _chunkSource;
        private bool _disposed;
        private readonly int _maxChunkSize;
        private readonly int _maxUnpooledChunkSize;
        private readonly int _minChunkSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InputBufferChunkSource"/> class.
        /// </summary>
        /// <param name="chunkSource">The chunk source.</param>
        /// <param name="maxUnpooledSize">The maximum size of an unpooled chunk.</param>
        /// <param name="minChunkSize">The minimum size of a chunk.</param>
        /// <param name="maxChunkSize">The maximum size of a chunk.</param>
        public InputBufferChunkSource(
            IBsonChunkSource chunkSource,
            int maxUnpooledSize = DefaultMaxUnpooledChunkSize,
            int minChunkSize = DefaultMinChunkSize,
            int maxChunkSize = DefaultMaxChunkSize)
        {
            if (chunkSource == null)
            {
                throw new ArgumentNullException("chunkSource");
            }
            if (maxUnpooledSize < 0)
            {
                throw new ArgumentException("maxUnpooledSize");
            }
            if (minChunkSize <= 0 || !PowerOf2.IsPowerOf2(minChunkSize))
            {
                throw new ArgumentException("minChunkSize");
            }
            if (maxChunkSize <= 0 || !PowerOf2.IsPowerOf2(maxChunkSize))
            {
                throw new ArgumentException("maxChunkSize");
            }
            if (maxChunkSize < minChunkSize)
            {
                throw new ArgumentException("maxChunkSize");
            }

            _chunkSource = chunkSource;
            _maxUnpooledChunkSize = maxUnpooledSize;
            _minChunkSize = minChunkSize;
            _maxChunkSize = maxChunkSize;
        }

        // methods        
        /// <inheritdoc/>
        public void Dispose()
        {
            _disposed = true;
        }

        /// <inheritdoc/>
        public IBsonChunk GetChunk(int requestedSize)
        {
            if (requestedSize < 0)
            {
                throw new ArgumentException("requestedSize");
            }
            ThrowIfDisposed();

            if (requestedSize <= _maxUnpooledChunkSize)
            {
                return new ByteArrayChunk(requestedSize);
            }

            var powerOf2Size = PowerOf2.RoundUpToPowerOf2(requestedSize);
            if (powerOf2Size - requestedSize > _minChunkSize)
            {
                powerOf2Size = powerOf2Size / 2;
            }
            var chunkSize = Math.Max(Math.Min(powerOf2Size, _maxChunkSize), _minChunkSize);

            return _chunkSource.GetChunk(chunkSize);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
