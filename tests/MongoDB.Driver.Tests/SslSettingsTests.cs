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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.TestHelpers;
using Xunit;

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

        [Fact]
        public void TestClientCertificates()
        {
            var settings = new SslSettings();
            Assert.Equal(null, settings.ClientCertificates);

            var certificateFileName = GetTestCertificateFileName();
            var clientCertificates = new[] {  X509CertificateLoader.LoadPkcs12FromFile(certificateFileName, "password"), X509CertificateLoader.LoadPkcs12FromFile(certificateFileName, "password") };
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

        [Fact]
        public void TestClone()
        {
            var certificateFileName = GetTestCertificateFileName();
            var settings = new SslSettings
            {
                CheckCertificateRevocation = false,
                ClientCertificates = [ X509CertificateLoader.LoadPkcs12FromFile(certificateFileName, "password") ],
                ClientCertificateSelectionCallback = ClientCertificateSelectionCallback,
                EnabledSslProtocols = SslProtocols.Tls12,
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
            Assert.Equal(SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12, settings.EnabledSslProtocols);
            Assert.Equal(null, settings.ServerCertificateValidationCallback);
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new SslSettings();
            var clone = settings.Clone();
            Assert.Equal(settings, clone);

            clone = settings.Clone();
            clone.CheckCertificateRevocation = !settings.CheckCertificateRevocation;
            Assert.NotEqual(settings, clone);

            clone = settings.Clone();
            var certificateFileName = GetTestCertificateFileName();
            clone.ClientCertificates = [ X509CertificateLoader.LoadPkcs12FromFile(certificateFileName, "password") ];
            Assert.NotEqual(settings, clone);
            Assert.NotEqual(clone, settings);

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
            Assert.Equal(SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12, settings.EnabledSslProtocols);

            var enabledSslProtocols = SslProtocols.Tls12;
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

        [Fact]
        public void Equals_should_return_false_when_certificates_differ_but_share_issuer_and_serial_number()
        {
            var serialNumber = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var lhs = CreateSelfSignedCertificate("CN=mongodb-test", serialNumber);
            using var rhs = CreateSelfSignedCertificate("CN=mongodb-test", serialNumber);
            lhs.Issuer.Should().Be(rhs.Issuer);
            lhs.GetSerialNumberString().Should().Be(rhs.GetSerialNumberString());

            var subject = new SslSettings { ClientCertificates = [lhs] };
            var other = new SslSettings { ClientCertificates = [rhs] };

            subject.Equals(other).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_true_when_certificates_have_the_same_raw_data()
        {
            using var certificate = CreateSelfSignedCertificate("CN=mongodb-test", [1, 2, 3, 4, 5, 6, 7, 8]);
            using var copy = X509CertificateLoader.LoadCertificate(certificate.RawData);

            var subject = new SslSettings { ClientCertificates = [certificate] };
            var other = new SslSettings { ClientCertificates = [copy] };

            subject.Equals(other).Should().BeTrue();
            subject.GetHashCode().Should().Be(other.GetHashCode());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Equals_should_return_false_when_only_one_certificate_has_a_private_key(bool lhsHasPrivateKey)
        {
            using var certificateWithPrivateKey = CreateSelfSignedCertificateWithPrivateKey("CN=mongodb-test", useEcdsa: false);
            using var certificateWithoutPrivateKey = X509CertificateLoader.LoadCertificate(certificateWithPrivateKey.RawData);
            certificateWithPrivateKey.HasPrivateKey.Should().BeTrue();
            certificateWithoutPrivateKey.HasPrivateKey.Should().BeFalse();

            var subject = new SslSettings { ClientCertificates = [lhsHasPrivateKey ? certificateWithPrivateKey : certificateWithoutPrivateKey] };
            var other = new SslSettings { ClientCertificates = [lhsHasPrivateKey ? certificateWithoutPrivateKey : certificateWithPrivateKey] };

            subject.Equals(other).Should().BeFalse();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Equals_should_return_true_when_certificates_have_the_same_private_key(bool useEcdsa)
        {
            using var certificate = CreateSelfSignedCertificateWithPrivateKey("CN=mongodb-test", useEcdsa);
            using var copy = X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), null);
            copy.HasPrivateKey.Should().BeTrue();

            var subject = new SslSettings { ClientCertificates = [certificate] };
            var other = new SslSettings { ClientCertificates = [copy] };

            subject.Equals(other).Should().BeTrue();
            subject.GetHashCode().Should().Be(other.GetHashCode());
        }

        [Fact]
        public void Equals_should_return_true_when_the_same_certificate_file_is_loaded_twice()
        {
            var certificateFileName = GetTestCertificateFileName();
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificateFileName, "password");
            using var copy = X509CertificateLoader.LoadPkcs12FromFile(certificateFileName, "password");

            var subject = new SslSettings { ClientCertificates = [certificate] };
            var other = new SslSettings { ClientCertificates = [copy] };

            subject.Equals(other).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_false_when_certificates_are_in_a_different_order()
        {
            using var first = CreateSelfSignedCertificate("CN=mongodb-test-1", [1]);
            using var second = CreateSelfSignedCertificate("CN=mongodb-test-2", [2]);

            var subject = new SslSettings { ClientCertificates = [first, second] };
            var other = new SslSettings { ClientCertificates = [second, first] };

            subject.Equals(other).Should().BeFalse();
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, 0, false)]
        [InlineData(null, 1, false)]
        [InlineData(null, 2, false)]
        [InlineData(0, null, false)]
        [InlineData(1, null, false)]
        [InlineData(2, null, false)]
        [InlineData(0, 0, true)]
        public void Equals_should_handle_null_client_certificates(int? lhsCount, int? rhsCount, bool expectedResult)
        {
            var subject = new SslSettings { ClientCertificates = CreateCertificates(lhsCount) };
            var other = new SslSettings { ClientCertificates = CreateCertificates(rhsCount) };

            try
            {
                subject.Equals(other).Should().Be(expectedResult);
            }
            finally
            {
                if (subject.ClientCertificates != null)
                {
                    foreach (var cert in subject.ClientCertificates)
                    {
                        cert.Dispose();
                    }
                }

                if (other.ClientCertificates != null)
                {
                    foreach (var cert in other.ClientCertificates)
                    {
                        cert.Dispose();
                    }
                }
            }
        }

        private X509Certificate[] CreateCertificates(int? count) =>
            count == null
                ? null
                : Enumerable
                    .Range(0, count.Value)
                    .Select(i => (X509Certificate)CreateSelfSignedCertificate($"CN=mongodb-test-{i}", [(byte)(i + 1)]))
                    .ToArray();

        private X509Certificate2 CreateSelfSignedCertificate(string subjectName, byte[] serialNumber)
        {
            using var key = System.Security.Cryptography.RSA.Create(2048);
            var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var notBefore = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return request.Create(request.SubjectName, X509SignatureGenerator.CreateForRSA(key, RSASignaturePadding.Pkcs1), notBefore, notBefore.AddYears(10), serialNumber);
        }

        private X509Certificate2 CreateSelfSignedCertificateWithPrivateKey(string subjectName, bool useEcdsa)
        {
            var notBefore = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            if (useEcdsa)
            {
                using var ecdsaKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                return new CertificateRequest(subjectName, ecdsaKey, HashAlgorithmName.SHA256).CreateSelfSigned(notBefore, notBefore.AddYears(10));
            }

            using var rsaKey = System.Security.Cryptography.RSA.Create(2048);
            return new CertificateRequest(subjectName, rsaKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSelfSigned(notBefore, notBefore.AddYears(10));
        }

        private string GetTestCertificateFileName()
        {
            var codeBase = typeof(SslSettingsTests).GetTypeInfo().Assembly.Location;
            var codeBaseUrl = new Uri(codeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var codeBaseDirectory = Path.GetDirectoryName(codeBasePath);
            var certificateDirectory = codeBaseDirectory;
            return Path.Combine(certificateDirectory, "testcert.pfx");
        }
    }
}
