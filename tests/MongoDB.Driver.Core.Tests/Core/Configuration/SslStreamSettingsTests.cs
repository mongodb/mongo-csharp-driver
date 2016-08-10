/* Copyright 2013-2016 MongoDB Inc.
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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    public class SslStreamSettingsTests
    {
        private static readonly SslStreamSettings __defaults = new SslStreamSettings();

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new SslStreamSettings();

            subject.CheckCertificateRevocation.Should().BeTrue();
            subject.ClientCertificates.Should().BeEmpty();
            subject.ClientCertificateSelectionCallback.Should().BeNull();
            subject.EnabledSslProtocols.Should().Be(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls);
            subject.ServerCertificateValidationCallback.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_clientCertificates_is_null()
        {
            Action action = () => new SslStreamSettings(clientCertificates: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("clientCertificates");
        }

        [Fact]
        public void constructor_with_checkCertificateRevocation_should_initialize_instance()
        {
            var checkCertificateRevocation = !__defaults.CheckCertificateRevocation;

            var subject = new SslStreamSettings(checkCertificateRevocation: checkCertificateRevocation);

            subject.CheckCertificateRevocation.Should().Be(checkCertificateRevocation);
            subject.ClientCertificates.Should().Equal(__defaults.ClientCertificates);
            subject.ClientCertificateSelectionCallback.Should().Be(__defaults.ClientCertificateSelectionCallback);
            subject.EnabledSslProtocols.Should().Be(__defaults.EnabledSslProtocols);
            subject.ServerCertificateValidationCallback.Should().Be(__defaults.ServerCertificateValidationCallback);
        }

        [Fact]
        public void constructor_with_clientCertificates_should_initialize_instance()
        {
            var clientCertificates = new[] { new X509Certificate() };

            var subject = new SslStreamSettings(clientCertificates: clientCertificates);

            subject.CheckCertificateRevocation.Should().Be(__defaults.CheckCertificateRevocation);
            subject.ClientCertificates.Should().Equal(clientCertificates);
            subject.ClientCertificateSelectionCallback.Should().Be(__defaults.ClientCertificateSelectionCallback);
            subject.EnabledSslProtocols.Should().Be(__defaults.EnabledSslProtocols);
            subject.ServerCertificateValidationCallback.Should().Be(__defaults.ServerCertificateValidationCallback);
        }

        [Fact]
        public void constructor_with_clientCertificateSelectionCallback_should_initialize_instance()
        {
            LocalCertificateSelectionCallback clientCertificateSelectionCallback = (s, t, l, r, a) => null;

            var subject = new SslStreamSettings(clientCertificateSelectionCallback: clientCertificateSelectionCallback);

            subject.CheckCertificateRevocation.Should().Be(__defaults.CheckCertificateRevocation);
            subject.ClientCertificates.Should().Equal(__defaults.ClientCertificates);
            subject.ClientCertificateSelectionCallback.Should().Be(clientCertificateSelectionCallback);
            subject.EnabledSslProtocols.Should().Be(__defaults.EnabledSslProtocols);
            subject.ServerCertificateValidationCallback.Should().Be(__defaults.ServerCertificateValidationCallback);
        }

        [Fact]
        public void constructor_with_enabledProtocols_should_initialize_instance()
        {
            var enabledProtocols = SslProtocols.Tls12;

            var subject = new SslStreamSettings(enabledProtocols: enabledProtocols);

            subject.CheckCertificateRevocation.Should().Be(__defaults.CheckCertificateRevocation);
            subject.ClientCertificates.Should().Equal(__defaults.ClientCertificates);
            subject.ClientCertificateSelectionCallback.Should().Be(__defaults.ClientCertificateSelectionCallback);
            subject.EnabledSslProtocols.Should().Be(enabledProtocols);
            subject.ServerCertificateValidationCallback.Should().Be(__defaults.ServerCertificateValidationCallback);
        }

        [Fact]
        public void constructor_with_serverCertificateValidationCallback_should_initialize_instance()
        {
            RemoteCertificateValidationCallback serverCertificateValidationCallback = (s, ce, ch, e) => false;

            var subject = new SslStreamSettings(serverCertificateValidationCallback: serverCertificateValidationCallback);

            subject.CheckCertificateRevocation.Should().Be(__defaults.CheckCertificateRevocation);
            subject.ClientCertificates.Should().Equal(__defaults.ClientCertificates);
            subject.ClientCertificateSelectionCallback.Should().Be(__defaults.ClientCertificateSelectionCallback);
            subject.EnabledSslProtocols.Should().Be(__defaults.EnabledSslProtocols);
            subject.ServerCertificateValidationCallback.Should().Be(serverCertificateValidationCallback);
        }

        [Fact]
        public void With_checkCertificateRevocation_should_return_expected_result()
        {
            var oldCheckCertificateRevocation = false;
            var newCheckCertificateRevocation = true;
            var subject = new SslStreamSettings(checkCertificateRevocation: oldCheckCertificateRevocation);

            var result = subject.With(checkCertificateRevocation: newCheckCertificateRevocation);

            result.CheckCertificateRevocation.Should().Be(newCheckCertificateRevocation);
            result.ClientCertificates.Should().Equal(subject.ClientCertificates);
            result.ClientCertificateSelectionCallback.Should().Be(subject.ClientCertificateSelectionCallback);
            result.EnabledSslProtocols.Should().Be(subject.EnabledSslProtocols);
            result.ServerCertificateValidationCallback.Should().Be(subject.ServerCertificateValidationCallback);
        }

        [Fact]
        public void With_clientCertificates_should_return_expected_result()
        {
            var oldClientCertificates = new[] { new X509Certificate() };
            var newClientCertificates = new[] { new X509Certificate() };
            var subject = new SslStreamSettings(clientCertificates: oldClientCertificates);

            var result = subject.With(clientCertificates: newClientCertificates);

            result.CheckCertificateRevocation.Should().Be(subject.CheckCertificateRevocation);
            result.ClientCertificates.Should().Equal(newClientCertificates);
            result.ClientCertificateSelectionCallback.Should().Be(subject.ClientCertificateSelectionCallback);
            result.EnabledSslProtocols.Should().Be(subject.EnabledSslProtocols);
            result.ServerCertificateValidationCallback.Should().Be(subject.ServerCertificateValidationCallback);
        }

        [Fact]
        public void With_clientCertificateSelectionCallback_should_return_expected_result()
        {
            LocalCertificateSelectionCallback oldClientCertificateSelectionCallback = (s, t, l, r, a) => null;
            LocalCertificateSelectionCallback newClientCertificateSelectionCallback = (s, t, l, r, a) => null;
            var subject = new SslStreamSettings(clientCertificateSelectionCallback: oldClientCertificateSelectionCallback);

            var result = subject.With(clientCertificateSelectionCallback: newClientCertificateSelectionCallback);

            result.CheckCertificateRevocation.Should().Be(subject.CheckCertificateRevocation);
            result.ClientCertificates.Should().Equal(subject.ClientCertificates);
            result.ClientCertificateSelectionCallback.Should().Be(newClientCertificateSelectionCallback);
            result.EnabledSslProtocols.Should().Be(subject.EnabledSslProtocols);
            result.ServerCertificateValidationCallback.Should().Be(subject.ServerCertificateValidationCallback);
        }

        [Fact]
        public void With_enabledProtocols_should_return_expected_result()
        {
            var oldEnabledProtocols = SslProtocols.Tls;
            var newEnabledProtocols = SslProtocols.Tls12;
            var subject = new SslStreamSettings(enabledProtocols: oldEnabledProtocols);

            var result = subject.With(enabledProtocols: newEnabledProtocols);

            result.CheckCertificateRevocation.Should().Be(subject.CheckCertificateRevocation);
            result.ClientCertificates.Should().Equal(subject.ClientCertificates);
            result.ClientCertificateSelectionCallback.Should().Be(subject.ClientCertificateSelectionCallback);
            result.EnabledSslProtocols.Should().Be(newEnabledProtocols);
            result.ServerCertificateValidationCallback.Should().Be(subject.ServerCertificateValidationCallback);
        }

        [Fact]
        public void With_serverCertificateValidationCallback_should_return_expected_result()
        {
            RemoteCertificateValidationCallback oldServerCertificateValidationCallback = (s, ce, ch, e) => false;
            RemoteCertificateValidationCallback newServerCertificateValidationCallback = (s, ce, ch, e) => false;
            var subject = new SslStreamSettings(serverCertificateValidationCallback: oldServerCertificateValidationCallback);

            var result = subject.With(serverCertificateValidationCallback: newServerCertificateValidationCallback);

            result.CheckCertificateRevocation.Should().Be(subject.CheckCertificateRevocation);
            result.ClientCertificates.Should().Equal(subject.ClientCertificates);
            result.ClientCertificateSelectionCallback.Should().Be(subject.ClientCertificateSelectionCallback);
            result.EnabledSslProtocols.Should().Be(subject.EnabledSslProtocols);
            result.ServerCertificateValidationCallback.Should().Be(newServerCertificateValidationCallback);
        }
    }
}