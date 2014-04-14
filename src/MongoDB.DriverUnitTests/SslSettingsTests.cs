/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
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

        [Test]
        public void TestCheckCertificateRevocation()
        {
            var settings = new SslSettings();
            Assert.AreEqual(true, settings.CheckCertificateRevocation);

            var checkCertificateRevocation = !settings.CheckCertificateRevocation;
            settings.CheckCertificateRevocation = checkCertificateRevocation;
            Assert.AreEqual(checkCertificateRevocation, settings.CheckCertificateRevocation);

            settings.Freeze();
            Assert.AreEqual(checkCertificateRevocation, settings.CheckCertificateRevocation);
            Assert.Throws<InvalidOperationException>(() => { settings.CheckCertificateRevocation = checkCertificateRevocation; });
        }

        [Test]
        public void TestClientCertificates()
        {
            var settings = new SslSettings();
            Assert.AreEqual(null, settings.ClientCertificates);

            var clientCertificates = new[] { new X509Certificate2("testcert.pfx", "password"), new X509Certificate2("testcert.pfx", "password") };
            settings.ClientCertificates = clientCertificates;
            Assert.IsTrue(clientCertificates.SequenceEqual(settings.ClientCertificates));
            Assert.AreNotSame(clientCertificates[0], settings.ClientCertificates.ElementAt(0));
            Assert.AreNotSame(clientCertificates[1], settings.ClientCertificates.ElementAt(1));

            settings.Freeze();
            Assert.IsTrue(clientCertificates.SequenceEqual(settings.ClientCertificates));
            Assert.Throws<InvalidOperationException>(() => { settings.ClientCertificates = clientCertificates; });
        }

        [Test]
        public void TestClientCertificateSelectionCallback()
        {
            var settings = new SslSettings();
            Assert.AreEqual(null, settings.ClientCertificateSelectionCallback);

            var callback = (LocalCertificateSelectionCallback)ClientCertificateSelectionCallback;
            settings.ClientCertificateSelectionCallback = callback;
            Assert.AreEqual(callback, settings.ClientCertificateSelectionCallback);

            settings.Freeze();
            Assert.AreEqual(callback, settings.ClientCertificateSelectionCallback);
            Assert.Throws<InvalidOperationException>(() => { settings.ClientCertificateSelectionCallback = callback; });
        }

        [Test]
        public void TestClone()
        {
            var settings = new SslSettings
            {
                CheckCertificateRevocation = false,
                ClientCertificates = new[] { new X509Certificate2("testcert.pfx", "password") },
                ClientCertificateSelectionCallback = ClientCertificateSelectionCallback,
                EnabledSslProtocols = SslProtocols.Tls,
                ServerCertificateValidationCallback = ServerCertificateValidationCallback
            };

            var clone = settings.Clone();
            Assert.AreEqual(settings, clone);
        }

        [Test]
        public void TestDefaults()
        {
            var settings = new SslSettings();
            Assert.AreEqual(true, settings.CheckCertificateRevocation);
            Assert.AreEqual(null, settings.ClientCertificates);
            Assert.AreEqual(null, settings.ClientCertificateSelectionCallback);
            Assert.AreEqual(SslProtocols.Default, settings.EnabledSslProtocols);
            Assert.AreEqual(null, settings.ServerCertificateValidationCallback);
        }

        [Test]
        public void TestEquals()
        {
            var settings = new SslSettings();
            var clone = settings.Clone();
            Assert.AreEqual(settings, clone);

            clone = settings.Clone();
            clone.CheckCertificateRevocation = !settings.CheckCertificateRevocation;
            Assert.AreNotEqual(settings, clone);

            clone = settings.Clone();
            clone.ClientCertificates = new[] { new X509Certificate2("testcert.pfx", "password") };
            Assert.AreNotEqual(settings, clone);

            clone = settings.Clone();
            clone.ClientCertificateSelectionCallback = ClientCertificateSelectionCallback;
            Assert.AreNotEqual(settings, clone);

            clone = settings.Clone();
            clone.EnabledSslProtocols = SslProtocols.Tls;
            Assert.AreNotEqual(settings, clone);

            clone = settings.Clone();
            clone.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            Assert.AreNotEqual(settings, clone);
        }

        [Test]
        public void TestEnabledSslProtocols()
        {
            var settings = new SslSettings();
            Assert.AreEqual(SslProtocols.Default, settings.EnabledSslProtocols);

            var enabledSslProtocols = SslProtocols.Tls;
            settings.EnabledSslProtocols = enabledSslProtocols;
            Assert.AreEqual(enabledSslProtocols, settings.EnabledSslProtocols);

            settings.Freeze();
            Assert.AreEqual(enabledSslProtocols, settings.EnabledSslProtocols);
            Assert.Throws<InvalidOperationException>(() => { settings.EnabledSslProtocols = enabledSslProtocols; });
        }

        [Test]
        public void TestServerCertificateValidationCallback()
        {
            var settings = new SslSettings();
            Assert.AreEqual(null, settings.ServerCertificateValidationCallback);

            var callback = (RemoteCertificateValidationCallback)ServerCertificateValidationCallback;
            settings.ServerCertificateValidationCallback = callback;
            Assert.AreEqual(callback, settings.ServerCertificateValidationCallback);

            settings.Freeze();
            Assert.AreEqual(callback, settings.ServerCertificateValidationCallback);
            Assert.Throws<InvalidOperationException>(() => { settings.ServerCertificateValidationCallback = callback; });
        }
    }
}
