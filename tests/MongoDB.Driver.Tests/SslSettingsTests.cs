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
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;

namespace MongoDB.Driver.Tests
{
    public class SslSettingsTests
    {
        private X509Certificate ClientCertificateSelectionCallback(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return null;
        }

        private bool ServerCertificateValidationCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyError)
        {
            return true;
        }

        [Fact]
        public void TestCheckCertificateRevocation()
        {
            var settings = new SslSettings();
            settings.CheckCertificateRevocation.Should().BeFalse();

            var checkCertificateRevocation = !settings.CheckCertificateRevocation;
            settings.CheckCertificateRevocation = checkCertificateRevocation;
            Assert.Equal(checkCertificateRevocation, settings.CheckCertificateRevocation);

            settings.Freeze();
            Assert.Equal(checkCertificateRevocation, settings.CheckCertificateRevocation);
            Assert.Throws<InvalidOperationException>(() => { settings.CheckCertificateRevocation = checkCertificateRevocation; });
        }

        [SkippableFact]
        public void TestClientCertificates()
        {
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard15);

            var settings = new SslSettings();
            Assert.Equal(null, settings.ClientCertificates);

            var certificateFileName = GetTestCertificateFileName();
            var clientCertificates = new[] { new X509Certificate2(certificateFileName, "password"), new X509Certificate2(certificateFileName, "password") };
            settings.ClientCertificates = clientCertificates;

            Assert.True(clientCertificates.SequenceEqual(settings.ClientCertificates));
            Assert.True(settings.ClientCertificates.Cast<X509Certificate2>().All(cert => cert.HasPrivateKey));

            settings.Freeze();
            Assert.True(clientCertificates.SequenceEqual(settings.ClientCertificates));
            Assert.Throws<InvalidOperationException>(() => { settings.ClientCertificates = clientCertificates; });
        }

        [Fact]
        public void TestClientCertificateSelectionCallback()
        {
            var settings = new SslSettings();
            Assert.Equal(null, settings.ClientCertificateSelectionCallback);

            var callback = (LocalCertificateSelectionCallback)ClientCertificateSelectionCallback;
            settings.ClientCertificateSelectionCallback = callback;
            Assert.Equal(callback, settings.ClientCertificateSelectionCallback);

            settings.Freeze();
            Assert.Equal(callback, settings.ClientCertificateSelectionCallback);
            Assert.Throws<InvalidOperationException>(() => { settings.ClientCertificateSelectionCallback = callback; });
        }

        [SkippableFact]
        public void TestClone()
        {
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard15);

            var certificateFileName = GetTestCertificateFileName();
            var settings = new SslSettings
            {
                CheckCertificateRevocation = false,
                ClientCertificates = new[] { new X509Certificate2(certificateFileName, "password") },
                ClientCertificateSelectionCallback = ClientCertificateSelectionCallback,
                EnabledSslProtocols = SslProtocols.Tls,
                ServerCertificateValidationCallback = ServerCertificateValidationCallback
            };

            var clone = settings.Clone();
            Assert.Equal(settings, clone);
        }

        [Fact]
        public void TestDefaults()
        {
            var settings = new SslSettings();
            settings.CheckCertificateRevocation.Should().BeFalse();
            Assert.Equal(null, settings.ClientCertificates);
            Assert.Equal(null, settings.ClientCertificateSelectionCallback);
            Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, settings.EnabledSslProtocols);
            Assert.Equal(null, settings.ServerCertificateValidationCallback);
        }

        [SkippableFact]
        public void TestEquals()
        {
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard15);

            var settings = new SslSettings();
            var clone = settings.Clone();
            Assert.Equal(settings, clone);

            clone = settings.Clone();
            clone.CheckCertificateRevocation = !settings.CheckCertificateRevocation;
            Assert.NotEqual(settings, clone);

            clone = settings.Clone();
            var certificateFileName = GetTestCertificateFileName();
            clone.ClientCertificates = new[] { new X509Certificate2(certificateFileName, "password") };
            Assert.NotEqual(settings, clone);

            clone = settings.Clone();
            clone.ClientCertificateSelectionCallback = ClientCertificateSelectionCallback;
            Assert.NotEqual(settings, clone);

            clone = settings.Clone();
            clone.EnabledSslProtocols = SslProtocols.Tls12;
            Assert.NotEqual(settings, clone);

            clone = settings.Clone();
            clone.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            Assert.NotEqual(settings, clone);
        }

        [Fact]
        public void TestEnabledSslProtocols()
        {
            var settings = new SslSettings();
            Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, settings.EnabledSslProtocols);

            var enabledSslProtocols = SslProtocols.Tls;
            settings.EnabledSslProtocols = enabledSslProtocols;
            Assert.Equal(enabledSslProtocols, settings.EnabledSslProtocols);

            settings.Freeze();
            Assert.Equal(enabledSslProtocols, settings.EnabledSslProtocols);
            Assert.Throws<InvalidOperationException>(() => { settings.EnabledSslProtocols = enabledSslProtocols; });
        }

        [Fact]
        public void TestServerCertificateValidationCallback()
        {
            var settings = new SslSettings();
            Assert.Equal(null, settings.ServerCertificateValidationCallback);

            var callback = (RemoteCertificateValidationCallback)ServerCertificateValidationCallback;
            settings.ServerCertificateValidationCallback = callback;
            Assert.Equal(callback, settings.ServerCertificateValidationCallback);

            settings.Freeze();
            Assert.Equal(callback, settings.ServerCertificateValidationCallback);
            Assert.Throws<InvalidOperationException>(() => { settings.ServerCertificateValidationCallback = callback; });
        }

        private string GetTestCertificateFileName()
        {
            var codeBase = typeof(SslSettingsTests).GetTypeInfo().Assembly.CodeBase;
            var codeBaseUrl = new Uri(codeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var codeBaseDirectory = Path.GetDirectoryName(codeBasePath);
            var certificateDirectory = codeBaseDirectory;
            return Path.Combine(certificateDirectory, "testcert.pfx");
        }
    }
}
