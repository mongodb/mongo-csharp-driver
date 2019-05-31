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
#if NET452 || NETSTANDARD2_0
using System.IO.Compression;
using Snappy;
#endif

namespace MongoDB.Driver.Core.Compression
{
    internal class SnappyCompressor : ICompressor
    {
        public CompressorType Type => CompressorType.Snappy;

        public void Compress(Stream input, Stream output)
        {
#if NET452 || NETSTANDARD2_0
            using (var snappyStream = new SnappyStream(output, CompressionMode.Compress, true))
            {
                input.EfficientCopyTo(snappyStream);
            }
#else
            throw new NotSupportedException();
#endif
        }

        public void Decompress(Stream input, Stream output)
        {
#if NET452 || NETSTANDARD2_0
            using (var snappyStream = new SnappyStream(input, CompressionMode.Decompress, true))
            {
                snappyStream.CopyTo(output);
            }
#else
            throw new NotSupportedException();
#endif
        }
    }
}
