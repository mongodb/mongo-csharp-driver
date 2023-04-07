/* Copyright 2010-present MongoDB Inc.
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

using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Authentication.Oidc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    public class OidcInputConfigurationTests
    {
        [Theory]
        // doesn't fail
        [InlineData(null, "providerName", false, false, false)]
        [InlineData(null, null, true, true, false)]
        [InlineData("principalName", null, true, true, false)]
        [InlineData(null, null, true, false, false)]
        // will fail
        [InlineData(null, "providerName", true, false, true)]
        [InlineData(null, "providerName", false, true, true)]
        [InlineData(null, "providerName", true, true, true)]
        [InlineData(null, "providerName", true, true, true)]
        [InlineData(null, null, false, true, true)]
        [InlineData("principalName", "providerName", false, false, true)]
        public void Constructor_should_validate_input_arguments(string principalName, string providerName, bool withRequestCallback, bool withRefreshCallback, bool shouldFail)
        {
            var endpoint = new DnsEndPoint("localhost", 27017);
            var exception = Record.Exception(
                () => new OidcInputConfiguration(
                    endpoint,
                    principalName,
                    providerName,
                    requestCallbackProvider: withRequestCallback ? Mock.Of<IOidcRequestCallbackProvider>() : null,
                    refreshCallbackProvider: withRefreshCallback ? Mock.Of<IOidcRefreshCallbackProvider>() : null));

            (exception != null).Should().Be(shouldFail);
        }
    }
}
