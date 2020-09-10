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

using System;
using FluentAssertions;
using MongoDB.Driver.Core.Authentication;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication
{
    public class AuthenticatorFactoryTests
    {
        [Fact]
        public void constructor_should_throw_when_delegate_is_null()
        {
            var exception = Record.Exception(() => new AuthenticatorFactory(null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Create_should_create_authenticator_based_on_the_provided_delegate()
        {
            var subject = new AuthenticatorFactory(() => new PlainAuthenticator(new UsernamePasswordCredential("source", "user", "password")));
            var authenticator = subject.Create();

            var typedAuthenticator = authenticator.Should().BeOfType<PlainAuthenticator>().Subject;
            typedAuthenticator.DatabaseName.Should().Be("source");
        }

        [Fact]
        public void Multiple_Create_calls_should_return_different_authenticators_instances()
        {
            var subject = new AuthenticatorFactory(() => new PlainAuthenticator(new UsernamePasswordCredential("source", "user", "password")));
            var authenticator1 = subject.Create();
            var authenticator2 = subject.Create();

            authenticator1.Should().NotBeSameAs(authenticator2);
        }
    }
}
