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
using FluentAssertions;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    public class KmsProviderRegistryTests
    {
        [Fact]
        public void Register_should_add_factory()
        {
            var registry = new KmsProviderRegistry();

            registry.Register("test", Mock.Of<Func<IKmsProvider>>());
            var created = registry.TryCreate("test", out var _);

            created.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Register_throws_if_name_is_null_or_empty(string mechanismName)
        {
            var registry = new KmsProviderRegistry();

            var exception = Record.Exception(() => registry.Register(mechanismName, Mock.Of<Func<IKmsProvider>>()));

            exception.Should().BeAssignableTo<ArgumentException>();
        }

        [Fact]
        public void Register_throws_if_factory_is_null()
        {
            var registry = new KmsProviderRegistry();

            var exception = Record.Exception(() => registry.Register("test", null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Register_throws_if_provider_already_registered()
        {
            var registry = new KmsProviderRegistry();
            registry.Register("test", Mock.Of<Func<IKmsProvider>>());

            var exception = Record.Exception(() => registry.Register("test", Mock.Of<Func<IKmsProvider>>()));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void TryCreate_should_call_factory()
        {
            var registry = new KmsProviderRegistry();
            var providerMock = Mock.Of<IKmsProvider>();
            var factoryMock = new Mock<Func<IKmsProvider>>();
            factoryMock.Setup(x => x()).Returns(providerMock);
            registry.Register("test", factoryMock.Object);

            var created = registry.TryCreate("test", out var provider);

            created.Should().BeTrue();
            provider.Should().Be(providerMock);
            factoryMock.Verify(x => x(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryCreate_throws_on_empty_provider_name(string providerName)
        {
            var registry = new KmsProviderRegistry();

            var exception = Record.Exception(() => registry.TryCreate(providerName, out _));

            exception.Should().BeAssignableTo<ArgumentException>();
        }

        [Fact]
        public void TryCreate_returns_false_on_unknown_provider()
        {
            var registry = new KmsProviderRegistry();

            registry.Register("test", Mock.Of<Func<IKmsProvider>>());
            var created = registry.TryCreate("another", out var mechanism);

            created.Should().BeFalse();
            mechanism.Should().BeNull();
        }
    }
}
