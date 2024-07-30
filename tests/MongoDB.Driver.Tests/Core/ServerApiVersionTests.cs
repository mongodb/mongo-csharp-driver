/* Copyright 2021-present MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Core.Tests
{
    public class ServerApiVersionTests
    {
        [Fact]
        public void constructor_should_throw_when_versionString_is_null()
        {
            var exception = Record.Exception(() => new ServerApiVersion(null));

            var argumentException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentException.ParamName.Should().Be("versionString");
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, "1", false)]
        [InlineData("1", null, false)]
        [InlineData("1", "1", true)]
        [InlineData("1", "2", false)]
        [InlineData("2", "1", false)]
        [InlineData("2", "2", true)]
        public void Equals_should_return_expected_result(string alphaVersionString, string betaVersionString, bool expectedResult)
        {
            var alpha = alphaVersionString == null ? null : new ServerApiVersion(alphaVersionString);
            var beta = betaVersionString == null ? null : new ServerApiVersion(betaVersionString);
            var alphaHashCode = alpha?.GetHashCode();
            var betaHashCode = beta?.GetHashCode();
            alpha?.Equals(beta).Should().Be(expectedResult);
            (alpha == beta).Should().Be(expectedResult);
            (alpha != beta).Should().Be(!expectedResult);
            (alphaHashCode == betaHashCode).Should().Be(expectedResult);
        }

        [Fact]
        public void ToString_should_return_expected_result()
        {
            var version = ServerApiVersion.V1;

            var result = version.ToString();

            result.Should().Be("1");
        }
    }
}
