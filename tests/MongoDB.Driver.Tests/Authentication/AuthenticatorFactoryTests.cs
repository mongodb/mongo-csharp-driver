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
using MongoDB.Driver.Authentication;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Authentication
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
        public void Create_should_call_provided_delegate()
        {
            var callback = new Mock<Func<IAuthenticator>>();
            var subject = new AuthenticatorFactory(callback.Object);
            _ = subject.Create();

            callback.Verify(x => x(), Times.Once);
        }

        [Fact] public void Multiple_Create_calls_should_call_provided_callback_multiple_times()
        {
            var callback = new Mock<Func<IAuthenticator>>();
            var subject = new AuthenticatorFactory(callback.Object);
            _ = subject.Create();
            _ = subject.Create();

            callback.Verify(x => x(), Times.Exactly(2));
        }
    }
}
