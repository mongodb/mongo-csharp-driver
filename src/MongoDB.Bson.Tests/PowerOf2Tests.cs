/* Copyright 2010-2015 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Bson.Tests
{
    [TestFixture]
    public class PowerOf2Tests
    {
        [Test]
        public void IsPowerOf2_should_return_false_when_not_a_power_of_2(
            [Values(3, 5, 6, 7, 9, 127, 129, 0x37ffffff)]
            int n)
        {
            var result = PowerOf2.IsPowerOf2(n);

            result.Should().BeFalse();
        }

        [Test]
        public void IsPowerOf2_should_return_true_when_a_power_of_2(
            [Values(0, 1, 2, 4, 8, 128, 0x40000000)]
            int n)
        {
            var result = PowerOf2.IsPowerOf2(n);

            result.Should().BeTrue();
        }

        [Test]
        public void IsPowerOf2_should_throw_when_n_is_invalid(
            [Values(-1, 0x40000001)]
            int n)
        {
            Action action = () => PowerOf2.IsPowerOf2(n);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("n");
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 8)]
        [TestCase(6, 8)]
        [TestCase(7, 8)]
        [TestCase(8, 8)]
        [TestCase(9, 16)]
        [TestCase(127, 128)]
        [TestCase(128, 128)]
        [TestCase(129, 256)]
        [TestCase(0x37ffffff, 0x40000000)]
        [TestCase(0x40000000, 0x40000000)]
        public void RoundUpToPowerOf2_should_return_expected_result(int n, int expectedResult)
        {
            var result = PowerOf2.RoundUpToPowerOf2(n);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void RoundUpToPowerOf2_should_throw_when_n_is_invalid(
            [Values(-1, 0x40000001)]
            int n)
        {
            Action action = () => PowerOf2.RoundUpToPowerOf2(n);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("n");
        }
    }
}
