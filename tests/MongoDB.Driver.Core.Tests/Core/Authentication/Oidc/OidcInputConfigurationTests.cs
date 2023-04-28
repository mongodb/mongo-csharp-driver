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

using System;
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Authentication;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    public class OidcInputConfigurationTests
    {
        private readonly EndPoint __endPoint = new DnsEndPoint("localhost", 27017);

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
            var exception = Record.Exception(
                () => new OidcInputConfiguration(
                    __endPoint,
                    principalName,
                    providerName,
                    requestCallbackProvider: withRequestCallback ? Mock.Of<IOidcRequestCallbackProvider>() : null,
                    refreshCallbackProvider: withRefreshCallback ? Mock.Of<IOidcRefreshCallbackProvider>() : null));

            (exception != null).Should().Be(shouldFail);
        }

        [Theory]
        // full match
        [InlineData("localhost", "localhost", true)]
        [InlineData("127.0.0.1", "127.0.0.1", true)]
        [InlineData("[::1]", "::1", true)]
        [InlineData("evilmongodb.com", "evilmongodb.com", true)]
        [InlineData("domain.subdomain", "domain.subdomain", true)]
        // not match
        [InlineData("localhost", "dummy", false)]
        [InlineData("127.0.0.1", "127.0.0.2", false)]
        [InlineData("[::1]", "::2", false)]
        [InlineData("evilmongodb.com", "dummy.com", false)]
        [InlineData("domain.subdomain", "domain.subdomain.subsubdomain", false)]
        // only one allowed
        [InlineData("localhost", "localhost;dummy", true)]
        [InlineData("127.0.0.1", "127.0.0.1;dummy", true)]
        [InlineData("[::1]", "dummy;::1", true)]
        [InlineData("evilmongodb.com", "dummy;evilmongodb.com", true)]
        [InlineData("domain.subdomain", "domain.subdomain;dummy", true)]
        // with correct *.
        [InlineData("domain.subdomain", "*.subdomain", true)]
        // with incourrect correct *.
        [InlineData("domain.subdomain", "*.subdomain.subsubdomain", false)]
        [InlineData("domain.subdomain", "*subsubdomain", false)]
        [InlineData("evilmongodb.com", "*mongodb.com", false)]
        // just in case
        [InlineData("evilmongodb.com", "*.", false)]
        public void Constructor_should_validate_allowed_hosts_for_callback_mode(
            string host,
            string allowedHosts,
            bool result)
        {
            var endPoint = EndPointHelper.Parse(host).Should().NotBeNull().And.Subject.As<EndPoint>();
            var allowedHostsList = allowedHosts?.Split(';');
            var exception = Record.Exception(
                () => new OidcInputConfiguration(
                    endPoint,
                    requestCallbackProvider: OidcTestHelper.CreateRequestCallback(validateInput: false, validateToken: false),
                    allowedHosts: allowedHostsList));

            if (result)
            {
                exception.Should().BeNull();
            }
            else
            {
                var expectedHostsList = string.Join("', '", allowedHostsList ?? OidcInputConfiguration.DefaultAllowedHostNames);
                exception
                    .Should().BeOfType<InvalidOperationException>().Which.Message
                    .Should().Be($"The used host '{host.Replace("[", "").Replace("]", "")}' doesn't match allowed hosts list ['{expectedHostsList}'].");
            }
        }
    }
}
