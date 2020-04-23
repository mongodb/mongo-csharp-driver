/* Copyright 2020-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Tests
{
    public class ReadPreferenceHedgeTests
    {
        [Fact]
        public void Disabled_should_return_expected_result()
        {
            var result = ReadPreferenceHedge.Disabled;

            result.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Enabled_should_return_expected_result()
        {
            var result = ReadPreferenceHedge.Enabled;

            result.IsEnabled.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(false, true)] bool isEnabled)
        {
            var subject = new ReadPreferenceHedge(isEnabled);

            subject.IsEnabled.Should().Be(isEnabled);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsEnabled_should_return_expected_result(
            [Values(false, true)] bool isEnabled)
        {
            var subject = new ReadPreferenceHedge(isEnabled);

            var result = subject.IsEnabled;

            result.Should().Be(isEnabled);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void Equals_should_return_expected_result(bool lhsIsEnabled, bool rhsIsEnabled, bool expectedResult)
        {
            var subject = new ReadPreferenceHedge(lhsIsEnabled);
            var other = new ReadPreferenceHedge(rhsIsEnabled);

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Equals_should_return_false_when_other_is_not_a_ReadPreferenceHedge(bool isEnabled)
        {
            var subject = new ReadPreferenceHedge(isEnabled);
            var other = new object();

            var result = subject.Equals(other);

            result.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetHashCode_should_return_expected_result(
            [Values(false, true)] bool isEnabled)
        {
            var subject = new ReadPreferenceHedge(isEnabled);

            var result = subject.GetHashCode();

            var expectedResult = isEnabled.GetHashCode();
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ enabled : false }")]
        [InlineData(true, "{ enabled : true }")]
        public void ToBsonDocument_should_return_expected_result(bool isEnabled, string expectedResult)
        {
            var subject = new ReadPreferenceHedge(isEnabled);

            var result = subject.ToBsonDocument();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ \"enabled\" : false }")]
        [InlineData(true, "{ \"enabled\" : true }")]
        public void ToString_should_return_expected_result(bool isEnabled, string expectedResult)
        {
            var subject = new ReadPreferenceHedge(isEnabled);

            var result = subject.ToString();

            result.Should().Be(expectedResult);
        }
    }
}
