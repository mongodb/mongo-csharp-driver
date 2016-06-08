/* Copyright 2013-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class InterlockedInt32Tests
    {
        [Fact]
        public void Value_should_return_initial_value_after_construction()
        {
            var subject = new InterlockedInt32(3);

            subject.Value.Should().Be(3);
        }

        [Theory]
        [InlineData(0, 0, 0, false)]
        [InlineData(0, 1, 1, true)]
        [InlineData(1, 0, 0, true)]
        [InlineData(1, 1, 1, false)]
        public void TryChange_with_one_parameter(int initialValue, int toValue, int expectedValue, bool expectedResult)
        {
            var subject = new InterlockedInt32(initialValue);
            var result = subject.TryChange(toValue);
            subject.Value.Should().Be(expectedValue);
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, 0, 1, 1, true)]
        [InlineData(0, 1, 2, 0, false)]
        [InlineData(1, 0, 1, 1, false)]
        [InlineData(1, 1, 2, 2, true)]
        public void TryChange_with_two_parameters(int startingValue, int fromValue, int toValue, int expectedValue, bool expectedResult)
        {
            var subject = new InterlockedInt32(startingValue);
            var result = subject.TryChange(fromValue, toValue);
            subject.Value.Should().Be(expectedValue);
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void TryChange_with_two_parameters_should_throw_if_values_are_equal()
        {
            var subject = new InterlockedInt32(0);
            Action action = () => subject.TryChange(1, 1);
            action.ShouldThrow<ArgumentException>();
        }
    }
}
