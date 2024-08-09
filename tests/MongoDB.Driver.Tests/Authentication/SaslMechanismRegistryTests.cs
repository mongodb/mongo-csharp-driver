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
using MongoDB.Driver.Authentication;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Authentication
{
    public class SaslMechanismRegistryTests
    {
        [Fact]
        public void Register_should_add_factory()
        {
            var registry = new SaslMechanismRegistry();
            var saslContext = new SaslContext { Mechanism = "test" };

            registry.Register("test", Mock.Of<Func<SaslContext, ISaslMechanism>>());
            var created = registry.TryCreate(saslContext, out var _);

            created.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Register_throws_if_name_is_null_or_empty(string mechanismName)
        {
            var registry = new SaslMechanismRegistry();

            var exception = Record.Exception(() => registry.Register(mechanismName, Mock.Of<Func<SaslContext, ISaslMechanism>>()));

            exception.Should().BeAssignableTo<ArgumentException>();
        }

        [Fact]
        public void Register_throws_if_factory_is_null()
        {
            var registry = new SaslMechanismRegistry();

            var exception = Record.Exception(() => registry.Register("test", null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Register_throws_if_mechanism_already_registered()
        {
            var registry = new SaslMechanismRegistry();
            registry.Register("test", Mock.Of<Func<SaslContext, ISaslMechanism>>());

            var exception = Record.Exception(() => registry.Register("test", Mock.Of<Func<SaslContext, ISaslMechanism>>()));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void TryCreate_should_call_factory()
        {
            var registry = new SaslMechanismRegistry();
            var mechanismMock = Mock.Of<ISaslMechanism>();
            var factoryMock = new Mock<Func<SaslContext, ISaslMechanism>>();
            factoryMock.Setup(x => x(It.IsAny<SaslContext>())).Returns(mechanismMock);
            var saslContext = new SaslContext { Mechanism = "test" };
            registry.Register("test", factoryMock.Object);

            var created = registry.TryCreate(saslContext, out var mechanism);

            created.Should().BeTrue();
            mechanism.Should().Be(mechanismMock);
            factoryMock.Verify(x => x(It.IsAny<SaslContext>()), Times.Once);
        }

        [Fact]
        public void TryCreate_throws_on_null_context()
        {
            var registry = new SaslMechanismRegistry();

            var exception = Record.Exception(() => registry.TryCreate(null, out _));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void TryCreate_returns_false_on_unknown_mechanism()
        {
            var registry = new SaslMechanismRegistry();
            var saslContext = new SaslContext { Mechanism = "another" };

            registry.Register("test", Mock.Of<Func<SaslContext, ISaslMechanism>>());
            var created = registry.TryCreate(saslContext, out var mechanism);

            created.Should().BeFalse();
            mechanism.Should().BeNull();
        }
    }
}
