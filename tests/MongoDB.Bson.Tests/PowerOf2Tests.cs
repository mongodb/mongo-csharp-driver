/* Copyright 2010-2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class PowerOf2Tests
    {
        [Theory]
        [ParameterAttributeData]
        public void IsPowerOf2_should_return_false_when_not_a_power_of_2(
            [Values(3, 5, 6, 7, 9, 127, 129, 0x37ffffff)]
            int n)
        {
            var result = PowerOf2.IsPowerOf2(n);

            result.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void IsPowerOf2_should_return_true_when_a_power_of_2(
            [Values(0, 1, 2, 4, 8, 128, 0x40000000)]
            int n)
        {
            var result = PowerOf2.IsPowerOf2(n);

            result.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void IsPowerOf2_should_throw_when_n_is_invalid(
            [Values(-1, 0x40000001)]
            int n)
        {
            Action action = () => PowerOf2.IsPowerOf2(n);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("n");
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(5, 8)]
        [InlineData(6, 8)]
        [InlineData(7, 8)]
        [InlineData(8, 8)]
        [InlineData(9, 16)]
        [InlineData(127, 128)]
        [InlineData(128, 128)]
        [InlineData(129, 256)]
        [InlineData(0x37ffffff, 0x40000000)]
        [InlineData(0x40000000, 0x40000000)]
        public void RoundUpToPowerOf2_should_return_expected_result(int n, int expectedResult)
        {
            var result = PowerOf2.RoundUpToPowerOf2(n);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void RoundUpToPowerOf2_should_throw_when_n_is_invalid(
            [Values(-1, 0x40000001)]
            int n)
        {
            Action action = () => PowerOf2.RoundUpToPowerOf2(n);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("n");
        }
    }
}
