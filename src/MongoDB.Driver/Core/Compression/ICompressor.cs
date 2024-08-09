/* Copyright 2019–present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Compression
{
    /// <summary>
    /// Represents the compressor type.
    /// </summary>
    public enum CompressorType
    {
        // NOTE: the numeric values of the enum members MUST be kept in sync with the binary wire protocol

        /// <summary>
        /// The content of the message is uncompressed. This is realistically only used for testing.
        /// </summary>
        Noop = 0,
        /// <summary>
        /// The content of the message is compressed using snappy.
        /// </summary>
        Snappy = 1,
        /// <summary>
        /// The content of the message is compressed using zlib. 
        /// </summary>
        Zlib = 2,
        /// <summary>
        /// The content of the message is compressed using zstandard. 
        /// </summary>
        ZStandard = 3
    }

    /// <summary>
    /// Represents a compressor.
    /// </summary>
    public interface ICompressor
    {
        /// <summary>
        /// Gets the compressor type.
        /// </summary>
        CompressorType Type { get; }

        /// <summary>
        /// Compresses the specified stream.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="output">The output stream.</param>
        void Compress(Stream input, Stream output);

        /// <summary>
        /// Decompresses the specified stream.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="output">The output stream.</param>
        void Decompress(Stream input, Stream output);
    }
}
