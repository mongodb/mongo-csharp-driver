/* Original work:
 *   Copyright (c) 2016 - David Rouyer rouyer.david@gmail.com Copyright (c) 2011 - 2014 Robert Važan, Google Inc.
 *
 *   Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 *
 *   * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 *
 *   * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the * documentation and/or other materials provided with the distribution.
 *
 *   * Neither the name of Robert Važan nor the names of contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Modified work: 
 *   Copyright 2020–present MongoDB Inc.
 *
 *   Licensed under the Apache License, Version 2.0 (the "License");
 *   you may not use this file except in compliance with the License.
 *   You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 *   Unless required by applicable law or agreed to in writing, software
 *   distributed under the License is distributed on an "AS IS" BASIS,
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *   See the License for the specific language governing permissions and
 *   limitations under the License.
 */

using System;
using System.IO;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Compression.Snappy
{
    internal static class SnappyCodec
    {
        public static int Compress(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(output, nameof(output));
            EnsureBufferRangeIsValid(inputOffset, inputLength, input.Length);
            EnsureBufferRangeIsValid(outputOffset, outputLength, output.Length);

            var status = SnappyAdapter.snappy_compress(input, inputOffset, inputLength, output, outputOffset, ref outputLength);
            switch (status)
            {
                case SnappyStatus.Ok:
                    return outputLength;
                case SnappyStatus.BufferTooSmall:
                    throw new ArgumentOutOfRangeException("Output array is too small.");
                default:
                    throw new InvalidDataException("Invalid input.");
            }
        }

        public static byte[] Compress(byte[] input)
        {
            Ensure.IsNotNull(input, nameof(input));

            var max = GetMaxCompressedLength(input.Length);

            var output = new byte[max];
            var outputLength = Compress(input, 0, input.Length, output, 0, output.Length);
            if (outputLength == max)
                return output;
            var truncated = new byte[outputLength];
            Array.Copy(output, truncated, outputLength);
            return truncated;
        }

        public static int GetMaxCompressedLength(int inputLength)
        {
            return SnappyAdapter.snappy_max_compressed_length(inputLength);
        }

        public static int GetUncompressedLength(byte[] input, int inputOffset, int inputLength)
        {
            Ensure.IsNotNull(input, nameof(input));
            EnsureBufferRangeIsValid(inputOffset, inputLength, input.Length);
            if (inputLength == 0)
            {
                throw new InvalidDataException("Compressed block cannot be empty.");
            }

            var status = SnappyAdapter.snappy_uncompressed_length(input, inputOffset, inputLength, out var outputLength);
            switch (status)
            {
                case SnappyStatus.Ok:
                    return outputLength;
                default:
                    throw new InvalidDataException("Input is not a valid snappy-compressed block.");
            }
        }

        public static int GetUncompressedLength(byte[] input)
        {
            Ensure.IsNotNull(input, nameof(input));

            return GetUncompressedLength(input, 0, input.Length);
        }

        public static int Uncompress(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(output, nameof(output));
            EnsureBufferRangeIsValid(inputOffset, inputLength, input.Length);
            EnsureBufferRangeIsValid(outputOffset, outputLength, output.Length);

            if (inputLength == 0)
            {
                throw new InvalidDataException("Compressed block cannot be empty.");
            }

            var status = SnappyAdapter.snappy_uncompress(input, inputOffset, inputLength, output, outputOffset, ref outputLength);
            switch (status)
            {
                case SnappyStatus.Ok:
                    return outputLength;
                case SnappyStatus.BufferTooSmall:
                    throw new ArgumentOutOfRangeException("Output array is too small.");
                default:
                    throw new InvalidDataException("Input is not a valid snappy-compressed block.");
            }
        }

        public static byte[] Uncompress(byte[] input)
        {
            var max = GetUncompressedLength(input);
            var output = new byte[max];
            var outputLength = Uncompress(input, 0, input.Length, output, 0, output.Length);
            if (outputLength == max)
            {
                return output;
            }

            var truncated = new byte[outputLength];
            Array.Copy(output, truncated, outputLength);
            return truncated;
        }

        public static bool Validate(byte[] input, int inputOffset, int inputLength)
        {
            Ensure.IsNotNull(input, nameof(input));
            EnsureBufferRangeIsValid(inputOffset, inputLength, input.Length);
            if (inputLength == 0)
            {
                return false;
            }

            return SnappyAdapter.snappy_validate_compressed_buffer(input, inputOffset, inputLength) == SnappyStatus.Ok;
        }

        public static bool Validate(byte[] input)
        {
            Ensure.IsNotNull(input, nameof(input));

            return Validate(input, 0, input.Length);
        }

        // private static methods
        private static void EnsureBufferRangeIsValid(int offset, int length, int bufferLength)
        {
            if (offset < 0 || length < 0 || offset + length > bufferLength)
            {
                throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the buffer.");
            }
        }
    }
}
