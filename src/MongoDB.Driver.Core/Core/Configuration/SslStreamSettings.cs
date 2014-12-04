/* Copyright 2013-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    public class SslStreamSettings
    {
        #region static
        private static readonly IEqualityComparer<IEnumerable<X509Certificate>> __certificatesComparer = new CertificatesComparer();
        #endregion

        // fields
        private readonly bool _checkCertificateRevocation;
        private readonly IEnumerable<X509Certificate> _clientCertificates;
        private readonly LocalCertificateSelectionCallback _clientCertificateSelector;
        private readonly SslProtocols _enabledSslProtocols;
        private readonly RemoteCertificateValidationCallback _serverCertificateValidator;

        // constructors
        public SslStreamSettings(
            Optional<IEnumerable<X509Certificate>> clientCertificates = default(Optional<IEnumerable<X509Certificate>>),
            Optional<bool> checkCertificateRevocation = default(Optional<bool>),
            Optional<LocalCertificateSelectionCallback> clientCertificateSelector = default(Optional<LocalCertificateSelectionCallback>),
            Optional<SslProtocols> enabledProtocols = default(Optional<SslProtocols>),
            Optional<RemoteCertificateValidationCallback> serverCertificateValidator = default(Optional<RemoteCertificateValidationCallback>))
        {
            _clientCertificates = clientCertificates.WithDefault(null) ?? Enumerable.Empty<X509Certificate>();
            _checkCertificateRevocation = checkCertificateRevocation.WithDefault(true);
            _clientCertificateSelector = clientCertificateSelector.WithDefault(null);
            _enabledSslProtocols = enabledProtocols.WithDefault(SslProtocols.Default);
            _serverCertificateValidator = serverCertificateValidator.WithDefault(null);
        }

        // properties
        public bool CheckCertificateRevocation
        {
            get { return _checkCertificateRevocation; }
        }

        public IEnumerable<X509Certificate> ClientCertificates
        {
            get { return _clientCertificates; }
        }

        public LocalCertificateSelectionCallback ClientCertificateSelectionCallback
        {
            get { return _clientCertificateSelector; }
        }

        public SslProtocols EnabledSslProtocols
        {
            get { return _enabledSslProtocols; }
        }

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback
        {
            get { return _serverCertificateValidator; }
        }

        // methods
        public SslStreamSettings With(
            Optional<IEnumerable<X509Certificate>> clientCertificates = default(Optional<IEnumerable<X509Certificate>>),
            Optional<bool> checkCertificateRevocation = default(Optional<bool>),
            Optional<LocalCertificateSelectionCallback> clientCertificateSelector = default(Optional<LocalCertificateSelectionCallback>),
            Optional<SslProtocols> enabledProtocols = default(Optional<SslProtocols>),
            Optional<RemoteCertificateValidationCallback> serverCertificateValidator = default(Optional<RemoteCertificateValidationCallback>))
        {
            if (clientCertificates.Replaces(_clientCertificates, __certificatesComparer) ||
                checkCertificateRevocation.Replaces(_checkCertificateRevocation) ||
                clientCertificateSelector.Replaces(_clientCertificateSelector) ||
                enabledProtocols.Replaces(_enabledSslProtocols) ||
                serverCertificateValidator.Replaces(_serverCertificateValidator))
            {
                return new SslStreamSettings(
                    Optional.Arg(clientCertificates.WithDefault(_clientCertificates)),
                    checkCertificateRevocation.WithDefault(_checkCertificateRevocation),
                    clientCertificateSelector.WithDefault(_clientCertificateSelector),
                    enabledProtocols.WithDefault(_enabledSslProtocols),
                    serverCertificateValidator.WithDefault(_serverCertificateValidator));
            }
            else
            {
                return this;
            }
        }

        // nested types
        private class CertificatesComparer : IEqualityComparer<IEnumerable<X509Certificate>>
        {
            public bool Equals(IEnumerable<X509Certificate> x, IEnumerable<X509Certificate> y)
            {
                if (x == null) { return y == null; }
                return x.SequenceEqual(y);
            }

            public int GetHashCode(IEnumerable<X509Certificate> x)
            {
                return 1;
            }
        }
    }
}