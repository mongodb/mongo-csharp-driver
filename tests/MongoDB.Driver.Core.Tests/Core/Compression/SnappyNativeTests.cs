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
using System.Linq;
using System.Text;
using FluentAssertions;
using MongoDB.Driver.Core.Compression.Snappy;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Compression
{
    public class SnappyNativeTests
    {
        [Fact]
        public void Compress_should_throw_if_parameter_null()
        {
            var exception = Record.Exception(() => SnappyCodec.Compress(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("")]
        [InlineData("!@#$%^&*()")]
        public void Compress_should_uncompress_previously_compressed_message(string input)
        {
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var compressedBytes = SnappyCodec.Compress(inputBytes);
            compressedBytes.Length.Should().BeGreaterThan(0);

            var uncompressedBytes = SnappyCodec.Uncompress(compressedBytes);
            var outputString = Encoding.ASCII.GetString(uncompressedBytes);
            outputString.Should().Be(input);
        }

        [Theory]
        [InlineData(null, 0, 3, 100, 0, typeof(ArgumentNullException))]
        [InlineData(100, 0, 3, null, 0, typeof(ArgumentNullException))]
        [InlineData(100, -1, 3, 100, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(100, 0, -1, 100, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(100, 90, 20, 100, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(100, 0, 3, 100, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(100, 0, 3, 100, 100, typeof(ArgumentOutOfRangeException))]
        [InlineData(100, 0, 3, 100, 101, typeof(ArgumentOutOfRangeException))]
        [InlineData(100, 0, 100, 3, 0, typeof(ArgumentOutOfRangeException))]
        public void Compress_with_advanced_options_should_throw_if_parameters_incorrect(int? inputCount, int inputOffset, int inputLength, int? outputCount, int outputOffset, Type expectedException)
        {
            var input = inputCount.HasValue ? new byte[inputCount.Value] : null;
            var output = outputCount.HasValue ? new byte[outputCount.Value] : null;
            var outputLength = (output?.Length ?? 0) - outputOffset;
            var exception = Record.Exception(() => SnappyCodec.Compress(input, inputOffset, inputLength, output, outputOffset, outputLength));
            exception.Should().BeOfType(expectedException);
        }

        [Theory]
        [InlineData(3, 5, 10, "Hello")]
        [InlineData(0, 11, 0, "ByeHelloBye")]
        public void Compress_with_advanced_options_should_work_as_expected(int inputOffset, int inputLength, int outputOffset, string expectedResult)
        {
            var input = Encoding.ASCII.GetBytes("ByeHelloBye");
            var output = new byte[100];

            var outputLength = output.Length - outputOffset;
            var compressedLength = SnappyCodec.Compress(input, inputOffset, inputLength, output, outputOffset, outputLength);
            var compressedOutputWithoutOffset = output.Skip(outputOffset).Take(compressedLength).ToArray();

            var uncompressed = SnappyCodec.Uncompress(compressedOutputWithoutOffset);
            var uncompressedString = Encoding.ASCII.GetString(uncompressed);
            uncompressedString.Should().Be(expectedResult);
        }

        [Fact]
        public void GetUncompressedLength_should_throw_if_parameter_null()
        {
            var exception = Record.Exception(() => SnappyCodec.GetUncompressedLength(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null, 0, 3, typeof(ArgumentNullException))]
        [InlineData(true, -1, 20, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 22 - 2, 4, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, 0, typeof(InvalidDataException))]
        [InlineData(true, 22, 0, typeof(InvalidDataException))]
        [InlineData(false, 0, 10, typeof(InvalidDataException))]
        public void GetUncompressedLength_with_advanced_options_should_throw_if_parameters_incorrect(bool? provideCorrectCompressedBytes, int inputOffset, int inputLength, Type expectedExceptionType)
        {
            var uncompressedBytes = Encoding.ASCII.GetBytes("Hello, hello, howdy?"); // 20 bytes
            byte[] compressedBytes = null;
            if (provideCorrectCompressedBytes.HasValue)
            {
                if (provideCorrectCompressedBytes.Value)
                {
                    compressedBytes = SnappyCodec.Compress(uncompressedBytes); // 22 bytes
                }
                else
                {
                    compressedBytes = Enumerable.Repeat((byte)0xff, 10).ToArray(); // 10 bytes
                }
            }

            var exception = Record.Exception(() => SnappyCodec.GetUncompressedLength(compressedBytes, inputOffset, inputLength));
            exception.Should().BeOfType(expectedExceptionType);
        }

        [Fact]
        public void Uncompress_should_throw_if_parameter_null()
        {
            var exception = Record.Exception(() => SnappyCodec.Uncompress(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null, 0, 3, 100, 0, typeof(ArgumentNullException))]
        [InlineData(true, 0, 22, null, 0, typeof(ArgumentNullException))]
        [InlineData(true, -1, 20, 100, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, -1, 100, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 22 - 2, 4, 100, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, 0, 100, 0, typeof(InvalidDataException))]
        [InlineData(true, 22, 0, 100, 0, typeof(InvalidDataException))]
        [InlineData(true, 0, 22, 100, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, 22, 100, 101, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, 22, 100, 100, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, 22, 100, 97, typeof(ArgumentOutOfRangeException))]
        [InlineData(false, 0, 22, 100, 0, typeof(InvalidDataException))]
        public void Uncompress_with_advanced_parameters_should_throw_if_parameters_incorrect(bool? provideCorrectCompressedBytes, int inputOffset, int inputLength, int? outputCount, int outputOffset, Type expectedException)
        {
            var uncompressedBytes = Encoding.ASCII.GetBytes("Hello, hello, howdy?"); // 20 bytes
            byte[] compressedBytes = null;
            if (provideCorrectCompressedBytes.HasValue)
            {
                if (provideCorrectCompressedBytes.Value)
                {
                    compressedBytes = SnappyCodec.Compress(uncompressedBytes);
                }
                else
                {
                    compressedBytes = new byte[inputLength];
                    new Random(0).NextBytes(compressedBytes);
                }
            }

            var outputBytes = outputCount.HasValue ? new byte[outputCount.Value] : null;

            var outputLength = (outputBytes?.Length ?? 0) - outputOffset;
            var exception = Record.Exception(() => SnappyCodec.Uncompress(compressedBytes, inputOffset, inputLength, outputBytes, outputOffset, outputLength));
            exception.Should().BeOfType(expectedException);
        }

        [Fact]
        public void Uncompress_with_advance_options_should_work_as_expected()
        {
            var uncompressedBytes1 = Encoding.ASCII.GetBytes("Howdy");
            var uncompressedBytes2 = Encoding.ASCII.GetBytes("Hello");

            var compressedBytes2 = SnappyCodec.Compress(uncompressedBytes2);

            var padded = uncompressedBytes1.Take(3).Concat(compressedBytes2).Concat(uncompressedBytes1.Skip(3)).ToArray();

            var output = new byte[100];

            var outputLength = output.Length - 10;
            var uncompressedLength = SnappyCodec.Uncompress(padded, 3, padded.Length - 5, output, 10, outputLength);
            uncompressedLength.Should().Be(5);

            var outputBytes2 = output.Skip(10).Take(5).ToArray();
            var outputResult2 = Encoding.ASCII.GetString(outputBytes2);
            outputResult2.Should().Be("Hello");
        }

        [Fact]
        public void Validate_should_throw_if_parameter_null()
        {
            var exception = Record.Exception(() => SnappyCodec.Validate(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Validate_should_work_as_expected()
        {
            var uncompressedBytes = Encoding.ASCII.GetBytes("Hello, hello, howdy?");

            var compressedBytes = SnappyCodec.Compress(uncompressedBytes);
            var isValid = SnappyCodec.Validate(compressedBytes);
            isValid.Should().BeTrue();

            isValid = SnappyCodec.Validate(compressedBytes, 0, 0);
            isValid.Should().BeFalse();

            isValid = SnappyCodec.Validate(compressedBytes, compressedBytes.Length, 0);
            isValid.Should().BeFalse();

            var randomBytes = new byte[10];
            new Random(0).NextBytes(randomBytes);

            var isRandomBytesValid = SnappyCodec.Validate(randomBytes, 0, randomBytes.Length);
            isRandomBytesValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(false, 0, 3, typeof(ArgumentNullException))]
        [InlineData(true, -1, 20, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(true, 22, 4, typeof(ArgumentOutOfRangeException))]
        public void Validate_with_advanced_options_should_throw_if_parameters_incorrect(bool provideCompressedBytes, int inputOffset, int inputLength, Type expectedExceptionType)
        {
            var uncompressedBytes = Encoding.ASCII.GetBytes("Hello, hello, howdy?"); // 20 bytes
            var compressedBytes = provideCompressedBytes ? SnappyCodec.Compress(uncompressedBytes) : null; // 22 bytes

            var exception = Record.Exception(() => SnappyCodec.Validate(compressedBytes, inputOffset, inputLength));
            exception.Should().BeOfType(expectedExceptionType);
        }
    }
}
