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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication.Libgssapi;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Libgssapi
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "GssapiMechanism")]
    public class GssapiSecurityCredentialTests
    {
        private string _username;
        private string _password;

        public GssapiSecurityCredentialTests()
        {
            var authGssapi = Environment.GetEnvironmentVariable("AUTH_GSSAPI");
            if (!string.IsNullOrEmpty(authGssapi))
            {
                var authParts = authGssapi.Split(':');
                _username = Uri.UnescapeDataString(authParts[0]);
                _password = Uri.UnescapeDataString(authParts[1]);
            }
        }

        [Fact]
        public void Should_acquire_gssapi_security_credential_with_username_and_password()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");

            var securePassword = SecureStringHelper.ToSecureString(_password);
            var credential = GssapiSecurityCredential.Acquire(_username, securePassword);
            credential.Should().NotBeNull();
        }

        [Fact]
        public void Should_acquire_gssapi_security_credential_with_username_only()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");

            var credential = GssapiSecurityCredential.Acquire(_username);
            credential.Should().NotBeNull();
        }

        [Fact]
        public void Should_fail_to_acquire_gssapi_security_credential_with_username_and_bad_password()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");

            var securePassword = SecureStringHelper.ToSecureString("BADPASSWORD");

            var exception = Record.Exception(() => GssapiSecurityCredential.Acquire(_username, securePassword));
            exception.Should().BeOfType<LibgssapiException>();
        }
    }
}
