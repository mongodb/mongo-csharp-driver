/* Copyright 2019-present MongoDB Inc.
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
using System.IO.Compression;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Compression
{
    /// <summary>
    /// Compressor according to the zlib algorithm.
    /// </summary>
    internal sealed class ZlibCompressor : ICompressor
    {
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibCompressor" /> class.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        public ZlibCompressor(int? compressionLevel = 0)
        {
            _compressionLevel = GetCompressionLevel(compressionLevel);
        }

        /// <inheritdoc />
        public CompressorType Type => CompressorType.Zlib;

        /// <inheritdoc />
        public void Compress(Stream input, Stream output)
        {
            // On net6.0+ delegate to the BCL System.IO.Compression.ZLibStream, which handles
            // RFC 1950 framing (header + Adler-32) natively. On older TFMs use our manual
            // implementation built on DeflateStream.
#if NET6_0_OR_GREATER
            using (var zlibStream = new System.IO.Compression.ZLibStream(output, _compressionLevel, leaveOpen: true))
#else
            using (var zlibStream = new ZlibStream(output, CompressionMode.Compress, _compressionLevel, leaveOpen: true))
#endif
            {
                input.EfficientCopyTo(zlibStream);
            }
        }

        /// <inheritdoc />
        public void Decompress(Stream input, Stream output)
        {
#if NET6_0_OR_GREATER
            using (var zlibStream = new System.IO.Compression.ZLibStream(input, CompressionMode.Decompress, leaveOpen: true))
#else
            using (var zlibStream = new ZlibStream(input, CompressionMode.Decompress, leaveOpen: true))
#endif
            {
                zlibStream.CopyTo(output);
            }
        }

        // Maps zlibCompressionLevel (RFC-style 0-9, -1=default) onto System.IO.Compression.CompressionLevel.
        // .NET only exposes 3-4 buckets so the mapping has to lose granularity. Strategy: honor the
        // zlib semantic extremes (0 = no compression, 1 = fastest, 9 = best compression) and route
        // everything else to Optimal. No asymmetric bucketing of middle values, and the only
        // TFM-divergent case is level 9.
        //
        //   0     → NoCompression
        //   1     → Fastest
        //   2-8   → Optimal
        //   9     → SmallestSize (net6.0+) / Optimal (older)
        //   -1    → Optimal
        private static CompressionLevel GetCompressionLevel(int? compressionLevel) =>
            (compressionLevel ?? -1) switch
            {
                0 => CompressionLevel.NoCompression,
                1 => CompressionLevel.Fastest,
                -1 or (>= 2 and <= 8) => CompressionLevel.Optimal,
#if NET6_0_OR_GREATER
                9 => CompressionLevel.SmallestSize,
#else
                9 => CompressionLevel.Optimal,
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel))
            };
    }
}
