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
using MongoDB.Driver.Core.Misc;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using SharpCompress.IO;

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
            using (var zlibStream = new ZlibStream(new NonDisposingStream(output), CompressionMode.Compress, _compressionLevel))
            {
                zlibStream.FlushMode = FlushType.Sync;
                input.EfficientCopyTo(zlibStream);
            }
        }

        /// <inheritdoc />
        public void Decompress(Stream input, Stream output)
        {
            using (var zlibStream = new ZlibStream(new NonDisposingStream(input), CompressionMode.Decompress))
            {
                zlibStream.CopyTo(output);
            }
        }

        private static CompressionLevel GetCompressionLevel(int? compressionLevel)
        {
            if (!compressionLevel.HasValue)
            {
                compressionLevel = -1;
            }

            switch (compressionLevel)
            {
                case -1:
                    return CompressionLevel.Default;
                case int _ when compressionLevel >= 0 && compressionLevel <= 9:
                    return (CompressionLevel)compressionLevel;
                default:
                    throw new ArgumentOutOfRangeException(nameof(compressionLevel));
            }
        }
    }
}