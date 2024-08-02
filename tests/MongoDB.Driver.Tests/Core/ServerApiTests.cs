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
    public class ServerApiTests
    {
        [Fact]
        public void constructor_should_throw_when_serverApiVersion_is_null()
        {
            var exception = Record.Exception(() => new ServerApi(null));

            var argumentException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentException.ParamName.Should().Be("version");
        }

        [Fact]
        public void constructor_should_throw_when_serverApiVersion_is_null_and_other_fields_are_not_null()
        {
            var exception = Record.Exception(() => new ServerApi(null, true, true));

            var argumentException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentException.ParamName.Should().Be("version");
        }

        [Theory]
        [InlineData(null, null, null, null, null, null, true)]
        [InlineData(null, null, null, "1", null, null, false)]
        [InlineData("1", null, null, null, null, null, false)]
        [InlineData("1", null, null, "1", null, null, true)]
        [InlineData("1", null, null, "2", null, null, false)]
        [InlineData("2", null, null, "1", null, null, false)]
        [InlineData("2", null, null, "2", null, null, true)]
        [InlineData("1", true, true, "1", true, true, true)]
        [InlineData("1", false, false, "1", false, false, true)]
        [InlineData("1", false, false, "1", false, true, false)]
        [InlineData("1", false, null, "1", false, false, false)]
        public void Equals_should_return_expected_result(
            string alphaApiVersionString, bool? alphaStrict, bool? alphaDeprecationErrors,
            string betaApiVersionString, bool? betaStrict, bool? betaDeprecationErrors,
            bool expectedResult)
        {
            var alpha = alphaApiVersionString == null ? null : new ServerApi(new ServerApiVersion(alphaApiVersionString), alphaStrict, alphaDeprecationErrors);
            var beta = betaApiVersionString == null ? null : new ServerApi(new ServerApiVersion(betaApiVersionString), betaStrict, betaDeprecationErrors);
            var alphaHashCode = alpha?.GetHashCode();
            var betaHashCode = beta?.GetHashCode();
            alpha?.Equals(beta).Should().Be(expectedResult);
            (alpha == beta).Should().Be(expectedResult);
            (alpha != beta).Should().Be(!expectedResult);
            (alphaHashCode == betaHashCode).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("1", null, null, "{ Version : 1 }")]
        [InlineData("1", null, false, "{ Version : 1, DeprecationErrors : False }")]
        [InlineData("1", null, true, "{ Version : 1, DeprecationErrors : True }")]
        [InlineData("1", false, null, "{ Version : 1, Strict : False }")]
        [InlineData("1", true, null, "{ Version : 1, Strict : True }")]
        [InlineData("1", true, true, "{ Version : 1, Strict : True, DeprecationErrors : True }")]
        public void ToString_should_return_expected_result(string apiVersionString, bool? strict, bool? deprecationErrors, string expectedResult)
        {
            var serverApi = new ServerApi(new ServerApiVersion(apiVersionString), strict, deprecationErrors);

            var result = serverApi.ToString();

            result.Should().Be(expectedResult);
        }
    }
}
