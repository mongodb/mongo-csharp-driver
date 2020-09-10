/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Compression;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    public class ConnectionSettingsTests
    {
        private static readonly ConnectionSettings __defaults = new ConnectionSettings();

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new ConnectionSettings();

            subject.ApplicationName.Should().BeNull();
            subject.AuthenticatorFactories.Should().BeEmpty();
            subject.Compressors.Should().BeEmpty();
            subject.MaxIdleTime.Should().Be(TimeSpan.FromMinutes(10));
            subject.MaxLifeTime.Should().Be(TimeSpan.FromMinutes(30));
        }

        [Fact]
        public void constructor_should_throw_when_applicationName_is_too_long()
        {
            var applicationName = new string('x', 129);

            var exception = Record.Exception(() => new ConnectionSettings(applicationName: applicationName));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("applicationName");
        }

        [Fact]
        public void constructor_should_throw_when_authenticators_is_null()
        {
            Action action = () => new ConnectionSettings(authenticatorFactories: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("authenticatorFactories");
        }

        [Fact]
        public void constructor_should_throw_when_compressors_is_null()
        {
            var exception = Record.Exception(() => new ConnectionSettings(compressors: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("compressors");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_maxIdleTime_is_negative_or_zero(
            [Values(-1, 0)]
            int maxIdleTime)
        {
            Action action = () => new ConnectionSettings(maxIdleTime: TimeSpan.FromSeconds(maxIdleTime));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxIdleTime");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_maxLifeTime_is_negative_or_zero(
            [Values(-1, 0)]
            int maxLifeTime)
        {
            Action action = () => new ConnectionSettings(maxLifeTime: TimeSpan.FromSeconds(maxLifeTime));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxLifeTime");
        }

        [Fact]
        public void constructor_with_applicationName_should_initialize_instance()
        {
            var subject = new ConnectionSettings(applicationName: "app");

            subject.ApplicationName.Should().Be("app");
            subject.AuthenticatorFactories.Should().Equal(__defaults.AuthenticatorFactories);
            subject.Compressors.Should().Equal(__defaults.Compressors);
            subject.MaxIdleTime.Should().Be(__defaults.MaxIdleTime);
            subject.MaxLifeTime.Should().Be(__defaults.MaxLifeTime);
        }

        [Fact]
        public void constructor_with_authenticatorFactories_should_initialize_instance()
        {
            var authenticatorFactories = new[] { new AuthenticatorFactory(() => null) };

            var subject = new ConnectionSettings(authenticatorFactories: authenticatorFactories);

            subject.ApplicationName.Should().BeNull();
            subject.AuthenticatorFactories.Should().Equal(authenticatorFactories);
            subject.Compressors.Should().BeEquivalentTo(__defaults.Compressors);
            subject.MaxIdleTime.Should().Be(__defaults.MaxIdleTime);
            subject.MaxLifeTime.Should().Be(__defaults.MaxLifeTime);
        }

        [Fact]
        public void constructor_with_compressors_should_initialize_instance()
        {
            var compressors = new[] { new CompressorConfiguration(CompressorType.Zlib) };

            var subject = new ConnectionSettings(compressors: compressors);

            subject.ApplicationName.Should().BeNull();
            subject.AuthenticatorFactories.Should().BeEmpty();
            subject.Compressors.Should().Equal(compressors);
            subject.MaxIdleTime.Should().Be(__defaults.MaxIdleTime);
            subject.MaxLifeTime.Should().Be(__defaults.MaxLifeTime);
        }

        [Fact]
        public void constructor_with_maxIdleTime_should_initialize_instance()
        {
            var maxIdleTime = TimeSpan.FromSeconds(123);

            var subject = new ConnectionSettings(maxIdleTime: maxIdleTime);

            subject.ApplicationName.Should().BeNull();
            subject.AuthenticatorFactories.Should().BeEmpty();
            subject.Compressors.Should().Equal(__defaults.Compressors);
            subject.MaxIdleTime.Should().Be(maxIdleTime);
            subject.MaxLifeTime.Should().Be(__defaults.MaxLifeTime);
        }

        [Fact]
        public void constructor_with_maxLifeTime_should_initialize_instance()
        {
            var maxLifeTime = TimeSpan.FromSeconds(123);

            var subject = new ConnectionSettings(maxLifeTime: maxLifeTime);

            subject.ApplicationName.Should().BeNull();
            subject.AuthenticatorFactories.Should().BeEmpty();
            subject.Compressors.Should().Equal(__defaults.Compressors);
            subject.MaxIdleTime.Should().Be(__defaults.MaxIdleTime);
            subject.MaxLifeTime.Should().Be(maxLifeTime);
        }

        [Fact]
        public void With_applicationName_should_return_expected_result()
        {
            var oldApplicationName = "app1";
            var newApplicationName = "app2";
            var subject = new ConnectionSettings(applicationName: oldApplicationName);

            var result = subject.With(applicationName: newApplicationName);

            result.ApplicationName.Should().Be(newApplicationName);
            result.AuthenticatorFactories.Should().Equal(subject.AuthenticatorFactories);
            subject.Compressors.Should().Equal(__defaults.Compressors);
            result.MaxIdleTime.Should().Be(subject.MaxIdleTime);
            result.MaxLifeTime.Should().Be(subject.MaxLifeTime);
        }

        [Fact]
        public void With_authenticatorFactories_should_return_expected_result()
        {
            var oldAuthenticatorFactories = new[] { new AuthenticatorFactory(() => null) };
            var newAuthenticatorFactories = new[] { new AuthenticatorFactory(() => null) };

            var subject = new ConnectionSettings(authenticatorFactories: oldAuthenticatorFactories);

            var result = subject.With(authenticatorFactories: newAuthenticatorFactories);

            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.AuthenticatorFactories.Should().Equal(newAuthenticatorFactories);
            subject.Compressors.Should().Equal(subject.Compressors);
            result.MaxIdleTime.Should().Be(subject.MaxIdleTime);
            result.MaxLifeTime.Should().Be(subject.MaxLifeTime);
        }

        [Fact]
        public void With_compressors_should_return_expected_result()
        {
            var oldCompressors = new[] { new CompressorConfiguration(CompressorType.Zlib) };
            var newCompressors = new[] { new CompressorConfiguration(CompressorType.Snappy) };
            var subject = new ConnectionSettings(compressors: oldCompressors);

            var result = subject.With(compressors: newCompressors);

            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.AuthenticatorFactories.Should().Equal(subject.AuthenticatorFactories);
            result.Compressors.Should().Equal(newCompressors);
            result.MaxIdleTime.Should().Be(subject.MaxIdleTime);
            result.MaxLifeTime.Should().Be(subject.MaxLifeTime);
        }

        [Fact]
        public void With_maxIdleTime_should_return_expected_result()
        {
            var oldMaxIdleTime = TimeSpan.FromSeconds(1);
            var newMaxIdleTime = TimeSpan.FromSeconds(2);
            var subject = new ConnectionSettings(maxIdleTime: oldMaxIdleTime);

            var result = subject.With(maxIdleTime: newMaxIdleTime);

            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.AuthenticatorFactories.Should().Equal(subject.AuthenticatorFactories);
            result.Compressors.Should().Equal(subject.Compressors);
            result.MaxIdleTime.Should().Be(newMaxIdleTime);
            result.MaxLifeTime.Should().Be(subject.MaxLifeTime);
        }

        [Fact]
        public void With_maxLifeTime_should_return_expected_result()
        {
            var oldMaxLifeTime = TimeSpan.FromSeconds(1);
            var newMaxLifeTime = TimeSpan.FromSeconds(2);
            var subject = new ConnectionSettings(maxLifeTime: oldMaxLifeTime);

            var result = subject.With(maxLifeTime: newMaxLifeTime);

            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.AuthenticatorFactories.Should().Equal(subject.AuthenticatorFactories);
            result.Compressors.Should().Equal(subject.Compressors);
            result.MaxIdleTime.Should().Be(subject.MaxIdleTime);
            result.MaxLifeTime.Should().Be(newMaxLifeTime);
        }
    }
}
