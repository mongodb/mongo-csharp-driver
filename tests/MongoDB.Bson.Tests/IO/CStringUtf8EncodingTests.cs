/* Copyright 2016 MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class CStringUtf8EncodingTests
    {
        [Theory]
        [ParameterAttributeData]
        public void GetMaxByteCount_should_return_expected_result(
            [Values(0, 1, 2)] int charCount)
        {
            var expectedResult = charCount * 3;

            var result = CStringUtf8Encoding.GetMaxByteCount(charCount);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("abc", 0, 3, new byte[] { 97, 98, 99 })]
        [InlineData("abc", 1, 3, new byte[] { 97, 98, 99 })]
        [InlineData("\\u0080", 0, 2, new byte[] { 0xc2, 0x80 })]
        [InlineData("\\u0080", 1, 2, new byte[] { 0xc2, 0x80 })]
        [InlineData("\\u07ff", 0, 2, new byte[] { 0xdf, 0xbf })]
        [InlineData("\\u07ff", 1, 2, new byte[] { 0xdf, 0xbf })]
        [InlineData("\\u0800", 0, 3, new byte[] { 0xe0, 0xa0, 0x80 })]
        [InlineData("\\u0800", 1, 3, new byte[] { 0xe0, 0xa0, 0x80 })]
        [InlineData("\\ud7ff", 0, 3, new byte[] { 0xed, 0x9f, 0xbf })]
        [InlineData("\\ud7ff", 1, 3, new byte[] { 0xed, 0x9f, 0xbf })]
        [InlineData("\\ue000", 0, 3, new byte[] { 0xee, 0x80, 0x80 })]
        [InlineData("\\ue000", 1, 3, new byte[] { 0xee, 0x80, 0x80 })]
        [InlineData("\\uffff", 0, 3, new byte[] { 0xef, 0xbf, 0xbf })]
        [InlineData("\\uffff", 1, 3, new byte[] { 0xef, 0xbf, 0xbf })]
        [InlineData("\\ud800\\udc00", 0, 4, new byte[] { 0xf0, 0x90, 0x80, 0x80 })] // surrogate pair
        public void GetBytes_should_return_expected_result(
            string value,
            int byteIndex,
            int expectedResult,
            byte[] expectedBytes)
        {
            value = UnicodeHelper.Unescape(value);
            var bytes = new byte[CStringUtf8Encoding.GetMaxByteCount(value.Length) + byteIndex];

            var result = CStringUtf8Encoding.GetBytes(value, bytes, byteIndex, Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            bytes.Take(byteIndex).All(b => b == 0).Should().BeTrue();
            bytes.Skip(byteIndex).Take(result).Should().Equal(expectedBytes);
            bytes.Skip(byteIndex + result).All(b => b == 0).Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetBytes_should_throw_when_value_contains_null(
            [Values("\0", "a\0", "ab\0")] string value)
        {
            var bytes = new byte[CStringUtf8Encoding.GetMaxByteCount(value.Length)];

            Action action = () => CStringUtf8Encoding.GetBytes(value, bytes, 0, Utf8Encodings.Strict);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void GetBytes_should_throw_when_value_contains_null_and_fallback_encoding_is_used()
        {
            var value = "\ud800\udc00\u0000";
            var bytes = new byte[CStringUtf8Encoding.GetMaxByteCount(value.Length)];

            Action action = () => CStringUtf8Encoding.GetBytes(value, bytes, 0, Utf8Encodings.Strict);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void GetBytes_should_throw_when_value_contains_unmatched_surrogate_pair()
        {
            var value = "\ud800";
            var bytes = new byte[CStringUtf8Encoding.GetMaxByteCount(value.Length)];

            Action action = () => CStringUtf8Encoding.GetBytes(value, bytes, 0, Utf8Encodings.Strict);

            action.ShouldThrow<ArgumentException>();
        }
    }
}
