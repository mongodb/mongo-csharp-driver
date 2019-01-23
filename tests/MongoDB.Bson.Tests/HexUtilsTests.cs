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
using FluentAssertions;
using MongoDB.Shared;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class HexUtilsTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData('/')]
        [InlineData(':')]
        [InlineData('@')]
        [InlineData('G')]
        [InlineData('`')]
        [InlineData('g')]
        [InlineData(127)]
        [InlineData(255)]
        [InlineData(0xffff)]
        public void IsValidHexDigit_should_return_false_when_value_is_invalid(char c)
        {
            var result = HexUtils.IsValidHexDigit(c);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData('0')]
        [InlineData('1')]
        [InlineData('2')]
        [InlineData('3')]
        [InlineData('4')]
        [InlineData('5')]
        [InlineData('6')]
        [InlineData('7')]
        [InlineData('8')]
        [InlineData('9')]
        [InlineData('a')]
        [InlineData('b')]
        [InlineData('c')]
        [InlineData('d')]
        [InlineData('e')]
        [InlineData('f')]
        [InlineData('A')]
        [InlineData('B')]
        [InlineData('C')]
        [InlineData('D')]
        [InlineData('E')]
        [InlineData('F')]
        public void IsValidHexDigit_should_return_true_when_value_is_valid(char c)
        {
            var result = HexUtils.IsValidHexDigit(c);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("x")]
        [InlineData("0x")]
        [InlineData("x0")]
        [InlineData("x00")]
        [InlineData("0x0")]
        [InlineData("00x")]
        public void IsValidHexString_should_return_false_when_value_is_invalid(string value)
        {
            var result = HexUtils.IsValidHexString(value);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("00")]
        [InlineData("000")]
        public void IsValidHexString_should_return_true_when_value_is_valid(string value)
        {
            var result = HexUtils.IsValidHexString(value);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("11", 0x11)]
        [InlineData("111", 0x111)]
        public void ParseInt32_should_return_expected_result(string value, int expectedResult)
        {
            var result = HexUtils.ParseInt32(value);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("0x0")]
        public void ParseInt32_should_throw_when_value_is_invalid(string value)
        {
            var exception = Record.Exception(() => HexUtils.ParseInt32(value));

            exception.Should().BeOfType<FormatException>();
        }
    }
}
