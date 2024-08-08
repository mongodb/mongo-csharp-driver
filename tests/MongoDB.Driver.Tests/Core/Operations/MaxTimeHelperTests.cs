/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class MaxTimeHelperTests
    {
        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void ToMaxTimeMS_should_return_expected_result(int maxTimeTicks, int expectedResult)
        {
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var result = MaxTimeHelper.ToMaxTimeMS(value);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void ToMaxTimeMS_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -1)] int maxTimeTicks)
        {
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => MaxTimeHelper.ToMaxTimeMS(value));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }
    }
}
