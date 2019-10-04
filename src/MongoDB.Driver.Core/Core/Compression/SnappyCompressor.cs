﻿/* Copyright 2019-present MongoDB Inc.
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
using System.Threading;
using MongoDB.Driver.Core.Misc;
#if NET452 || NETSTANDARD2_0
using Snappy;
#endif

namespace MongoDB.Driver.Core.Compression
{
    internal class SnappyCompressor : ICompressor
    {
        public CompressorType Type => CompressorType.Snappy;

        /// <summary>
        /// Compresses the remainder of <paramref name="input"/>, writing the compressed data to
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="input"> The input stream.</param>
        /// <param name="output">The output stream.</param>
        public void Compress(Stream input, Stream output)
        {
#if NET452 || NETSTANDARD2_0
            var uncompressedSize = (int) (input.Length - input.Position);
            var uncompressedBytes = new byte[uncompressedSize]; // does not include uncompressed message headers
            input.ReadBytes(uncompressedBytes, offset: 0, count: uncompressedSize, CancellationToken.None);
            var maxCompressedSize = SnappyCodec.GetMaxCompressedLength(uncompressedSize);
            var compressedBytes = new byte[maxCompressedSize];
            var compressedSize = SnappyCodec.Compress(uncompressedBytes, 0, uncompressedSize, compressedBytes, 0);
            output.Write(compressedBytes, 0, compressedSize);
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Decompresses the remainder of  <paramref name="input"/>, writing the uncompressed data to <paramref name="output"/>.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="output">The output stream.</param>
        public void Decompress(Stream input, Stream output)
        {
#if NET452 || NETSTANDARD2_0
            var compressedSize = (int) (input.Length - input.Position);
            var compressedBytes = new byte[compressedSize];
            input.ReadBytes(compressedBytes, offset: 0, count: compressedSize, CancellationToken.None);
            var decompressedBytes = SnappyCodec.Uncompress(compressedBytes);
            output.Write(decompressedBytes, offset: 0, count: decompressedBytes.Length);
#else
            throw new NotSupportedException();
#endif
        }
    }
}
