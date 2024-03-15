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
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Authentication.Oidc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    public class OidcConfigurationTests
    {
        private static readonly IOidcKnownCallbackProviders __buildInProvidersMock;
        private static readonly IOidcCallback __callbackMock = new Mock<IOidcCallback>().Object;

        static OidcConfigurationTests()
        {
            var buildInProvidersMock = new Mock<IOidcKnownCallbackProviders>();
            buildInProvidersMock.Setup(p => p.Aws)
                .Returns(__callbackMock);

            __buildInProvidersMock = buildInProvidersMock.Object;
        }

        [Theory]
        [MemberData(nameof(ValidConfigurationTestCases))]
        public void Constructor_should_accept_valid_arguments(
            IReadOnlyList<EndPoint> endPoints,
            string principalName,
            IReadOnlyDictionary<string, object> mechanismProperties)
        {
            var configuration = new OidcConfiguration(endPoints, principalName, mechanismProperties, __buildInProvidersMock);

            configuration.PrincipalName.Should().Be(principalName);
            configuration.Callback.Should().NotBeNull();
        }

        public static IEnumerable<object[]> ValidConfigurationTestCases = new[]
        {
            new object[] { new[] { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" } },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" } },
            new object[] { new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" } },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" } },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" } },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock } },
        };

        [Theory]
        [MemberData(nameof(InvalidConfigurationTestCases))]
        public void Constructor_throws_on_invalid_arguments(
            IReadOnlyList<EndPoint> endPoints,
            string principalName,
            IReadOnlyDictionary<string, object> mechanismProperties,
            string paramName)
        {
            var exception = Record.Exception(() =>
                new OidcConfiguration(endPoints, principalName, mechanismProperties, __buildInProvidersMock));

            exception.Should().BeAssignableTo<ArgumentException>()
                .Subject.ParamName.Should().Be(paramName);
        }

        public static IEnumerable<object[]> InvalidConfigurationTestCases = new[]
        {
            new object[] { null, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }, "endPoints" },
            new object[] { Array.Empty<EndPoint>(), null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }, "endPoints" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, null, null, "authMechanismProperties" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", null, "authMechanismProperties" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object>(), null },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object>(), null },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["unknown_property"] = 42 }, "unknown_property" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = null }, "PROVIDER_NAME" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "" }, "PROVIDER_NAME" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = 1 }, "PROVIDER_NAME" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "unknown provider" }, "PROVIDER_NAME" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = null }, "OIDC_CALLBACK" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = "invalid type" }, "OIDC_CALLBACK" },
            new object[] { new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock, ["PROVIDER_NAME"] = "aws" }, null },
        };

        [Theory]
        [MemberData(nameof(ComparisonTestCases))]
        public void Equals_should_compare_by_values(
            bool expectedResult,
            IReadOnlyList<EndPoint> endPoints1,
            string principalName1,
            IReadOnlyDictionary<string, object> mechanismProperties1,
            IReadOnlyList<EndPoint> endPoints2,
            string principalName2,
            IReadOnlyDictionary<string, object> mechanismProperties2)
        {
            var configuration1 = new OidcConfiguration(endPoints1, principalName1, mechanismProperties1, __buildInProvidersMock);
            var configuration2 = new OidcConfiguration(endPoints2, principalName2, mechanismProperties2, __buildInProvidersMock);

            var result = configuration1.Equals(configuration2);

            result.Should().Be(expectedResult);
        }

        public static IEnumerable<object[]> ComparisonTestCases = new[]
        {
            new object[]
            {
                true,
                new[] { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                true,
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" },
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }
            },
            new object[]
            {
                true,
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" },
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }
            },
            new object[]
            {
                true,
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" },
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018)}, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }
            },
            new object[]
            {
                true,
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("other-host", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" },
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("other-host", 27017)}, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }
            },
            new object[]
            {
                true,
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("otherhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new DnsEndPoint("localhost", 27018) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("otherhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("otherhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017) }, "name", new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" },
                new[] { new DnsEndPoint("localhost", 27017) }, "name", new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["PROVIDER_NAME"] = "aws" },
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock }
            },
            new object[]
            {
                false,
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["OIDC_CALLBACK"] = __callbackMock },
                new[] { new DnsEndPoint("localhost", 27017) }, null, new Dictionary<string, object> { ["OIDC_CALLBACK"] = new Mock<IOidcCallback>().Object }
            },
        };
    }
}
