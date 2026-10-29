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

using System.IO;
using MongoDB.Driver.Core.Misc;
#if NET6_0_OR_GREATER
using System.IO.Compression;
#else
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using SharpCompress.IO;
#endif

namespace MongoDB.Driver.Core.Compression
{
    /// <summary>
    /// Compressor according to the zlib algorithm.
    /// </summary>
    internal sealed class ZlibCompressor : ICompressor
    {
#if NET10_0_OR_GREATER
        private readonly ZLibCompressionOptions _compressionOptions;
#else
        private readonly CompressionLevel _compressionLevel;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibCompressor" /> class.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        public ZlibCompressor(int? compressionLevel = 0)
        {
            var level = Ensure.IsBetween(compressionLevel ?? -1, -1, 9, nameof(compressionLevel));
#if NET10_0_OR_GREATER
            // Compression level 6 is the zlib default.
            _compressionOptions = new ZLibCompressionOptions { CompressionLevel = level == -1 ? 6 : level };
#else
            _compressionLevel = GetCompressionLevel(level);
#endif
        }

        /// <inheritdoc />
        public CompressorType Type => CompressorType.Zlib;

        /// <inheritdoc />
        public void Compress(Stream input, Stream output)
        {
            using var zlibStream = CreateCompressionStream(output);
#if NET6_0_OR_GREATER
            input.EfficientCopyTo(zlibStream);
            zlibStream.Flush(); // sync flush
#else
            zlibStream.FlushMode = FlushType.Sync;
            input.EfficientCopyTo(zlibStream);
#endif
        }

        /// <inheritdoc />
        public void Decompress(Stream input, Stream output)
        {
            using var zlibStream = CreateDecompressionStream(input);
            zlibStream.CopyTo(output);
        }

#if NET10_0_OR_GREATER
        private ZLibStream CreateCompressionStream(Stream output) =>
            new ZLibStream(output, _compressionOptions, leaveOpen: true);

        private ZLibStream CreateDecompressionStream(Stream input) =>
            new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true);
#elif NET6_0_OR_GREATER
        private ZLibStream CreateCompressionStream(Stream output) =>
            new ZLibStream(output, _compressionLevel, leaveOpen: true);

        private ZLibStream CreateDecompressionStream(Stream input) =>
            new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true);

        // The native CompressionLevel enum only has zlib levels 0 (NoCompression), 1 (Fastest), 6 (Optimal) and 9 (SmallestSize).
        // Other levels are mapped to the nearest of those.
        private static CompressionLevel GetCompressionLevel(int level)
        {
            switch (level)
            {
                case 0:
                    return CompressionLevel.NoCompression;
                case >= 1 and <= 3:
                    return CompressionLevel.Fastest;
                case >= 8:
                    return CompressionLevel.SmallestSize;
                default: // -1 (default), 4 to 7
                    return CompressionLevel.Optimal;
            }
        }
#else
        private ZlibStream CreateCompressionStream(Stream output) =>
            new ZlibStream(SharpCompressStream.CreateNonDisposing(output), CompressionMode.Compress, _compressionLevel);

        private ZlibStream CreateDecompressionStream(Stream input) =>
            new ZlibStream(SharpCompressStream.CreateNonDisposing(input), CompressionMode.Decompress);

        private static CompressionLevel GetCompressionLevel(int level) =>
            level == -1 ? CompressionLevel.Default : (CompressionLevel)level;
#endif
    }
}
