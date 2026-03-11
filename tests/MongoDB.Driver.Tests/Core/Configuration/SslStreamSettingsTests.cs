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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
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

            subject.CheckCertificateRevocation.Should().BeFalse();
            subject.ClientCertificates.Should().BeEmpty();
            subject.ClientCertificateSelectionCallback.Should().BeNull();
            subject.EnabledSslProtocols.Should().Be(SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12);
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
            var clientCertificates = new [] { X509CertificateLoader.LoadCertificate(__testCert) };

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
            var oldClientCertificates = new[] { X509CertificateLoader.LoadCertificate(__testCert) };
            var newClientCertificates = new[] { X509CertificateLoader.LoadCertificate(__testCert) };
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
            var oldEnabledProtocols = SslProtocols.Tls12;
            var newEnabledProtocols = SslStreamSettings.SslProtocolsTls13;
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

        private static readonly byte[] __testCert =
        [
            48, 130, 2, 120, 48, 130, 1, 225, 160, 3, 2, 1, 2, 2, 9, 0, 206, 136, 148, 86, 218, 120, 139, 228, 48, 13,
            6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 5, 5, 0, 48, 85, 49, 11, 48, 9, 6, 3, 85, 4, 6, 19, 2, 85, 83, 49,
            16, 48, 14, 6, 3, 85, 4, 8, 12, 7, 71, 101, 111, 114, 103, 105, 97, 49, 16, 48, 14, 6, 3, 85, 4, 7, 12, 7,
            65, 116, 108, 97, 110, 116, 97, 49, 17, 48, 15, 6, 3, 85, 4, 10, 12, 8, 84, 101, 115, 116, 32, 73, 110, 99,
            49, 15, 48, 13, 6, 3, 85, 4, 3, 12, 6, 84, 101, 115, 116, 101, 114, 48, 30, 23, 13, 49, 51, 48, 49, 50, 52,
            50, 50, 51, 49, 53, 55, 90, 23, 13, 52, 48, 48, 54, 49, 48, 50, 50, 51, 49, 53, 55, 90, 48, 85, 49, 11, 48,
            9, 6, 3, 85, 4, 6, 19, 2, 85, 83, 49, 16, 48, 14, 6, 3, 85, 4, 8, 12, 7, 71, 101, 111, 114, 103, 105, 97,
            49, 16, 48, 14, 6, 3, 85, 4, 7, 12, 7, 65, 116, 108, 97, 110, 116, 97, 49, 17, 48, 15, 6, 3, 85, 4, 10, 12,
            8, 84, 101, 115, 116, 32, 73, 110, 99, 49, 15, 48, 13, 6, 3, 85, 4, 3, 12, 6, 84, 101, 115, 116, 101, 114,
            48, 129, 159, 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 1, 5, 0, 3, 129, 141, 0, 48, 129, 137, 2, 129,
            129, 0, 232, 50, 71, 90, 149, 7, 66, 154, 146, 17, 101, 153, 240, 201, 205, 17, 59, 156, 61, 172, 41, 163,
            80, 81, 177, 1, 14, 50, 152, 220, 19, 52, 114, 60, 93, 140, 66, 234, 182, 65, 56, 206, 53, 40, 67, 46, 69,
            120, 51, 245, 144, 87, 56, 115, 177, 152, 173, 157, 2, 44, 91, 53, 32, 128, 97, 145, 37, 68, 109, 122, 31,
            161, 19, 141, 73, 202, 231, 201, 251, 237, 201, 100, 104, 200, 174, 94, 50, 176, 101, 223, 70, 34, 22, 172,
            46, 171, 254, 90, 63, 56, 242, 75, 66, 31, 208, 99, 48, 144, 47, 118, 205, 76, 100, 230, 44, 28, 240, 2,
            149, 8, 21, 34, 221, 130, 204, 31, 64, 115, 2, 3, 1, 0, 1, 163, 80, 48, 78, 48, 29, 6, 3, 85, 29, 14, 4, 22,
            4, 20, 176, 240, 6, 4, 223, 189, 160, 18, 104, 18, 37, 81, 177, 25, 156, 225, 223, 154, 251, 188, 48, 31, 6,
            3, 85, 29, 35, 4, 24, 48, 22, 128, 20, 176, 240, 6, 4, 223, 189, 160, 18, 104, 18, 37, 81, 177, 25, 156,
            225, 223, 154, 251, 188, 48, 12, 6, 3, 85, 29, 19, 4, 5, 48, 3, 1, 1, 255, 48, 13, 6, 9, 42, 134, 72, 134,
            247, 13, 1, 1, 5, 5, 0, 3, 129, 129, 0, 125, 197, 226, 141, 46, 105, 97, 45, 124, 3, 78, 240, 183, 242, 135,
            46, 163, 108, 116, 43, 13, 140, 99, 162, 16, 163, 139, 110, 46, 46, 210, 140, 243, 52, 11, 37, 221, 96, 97,
            210, 147, 235, 98, 212, 72, 62, 195, 67, 209, 144, 74, 31, 187, 93, 102, 214, 132, 153, 150, 206, 32, 157,
            233, 124, 210, 12, 248, 64, 62, 65, 32, 18, 111, 211, 78, 51, 231, 117, 205, 93, 80, 41, 8, 190, 22, 236,
            50, 245, 140, 56, 54, 17, 12, 58, 56, 78, 33, 102, 200, 32, 134, 70, 223, 253, 226, 161, 221, 125, 203, 177,
            119, 225, 144, 250, 197, 202, 165, 142, 200, 144, 209, 170, 84, 179, 15, 56, 10, 194
        ];
    }
}
