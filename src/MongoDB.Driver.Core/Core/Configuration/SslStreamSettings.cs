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
        // fields
        private readonly bool _checkCertificateRevocation;
        private readonly IEnumerable<X509Certificate> _clientCertificates;
        private readonly LocalCertificateSelectionCallback _clientCertificateSelectionCallback;
        private readonly SslProtocols _enabledSslProtocols;
        private readonly RemoteCertificateValidationCallback _serverCertificateValidationCallback;

        // constructors
        public SslStreamSettings(
            Optional<bool> checkCertificateRevocation = default(Optional<bool>),
            Optional<IEnumerable<X509Certificate>> clientCertificates = default(Optional<IEnumerable<X509Certificate>>),
            Optional<LocalCertificateSelectionCallback> clientCertificateSelectionCallback = default(Optional<LocalCertificateSelectionCallback>),
            Optional<SslProtocols> enabledProtocols = default(Optional<SslProtocols>),
            Optional<RemoteCertificateValidationCallback> serverCertificateValidationCallback = default(Optional<RemoteCertificateValidationCallback>))
        {
            _checkCertificateRevocation = checkCertificateRevocation.WithDefault(true);
            _clientCertificates = Ensure.IsNotNull(clientCertificates.WithDefault(Enumerable.Empty<X509Certificate>()), "clientCertificates").ToList();
            _clientCertificateSelectionCallback = clientCertificateSelectionCallback.WithDefault(null);
            _enabledSslProtocols = enabledProtocols.WithDefault(SslProtocols.Default);
            _serverCertificateValidationCallback = serverCertificateValidationCallback.WithDefault(null);
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
            get { return _clientCertificateSelectionCallback; }
        }

        public SslProtocols EnabledSslProtocols
        {
            get { return _enabledSslProtocols; }
        }

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback
        {
            get { return _serverCertificateValidationCallback; }
        }

        // methods
        public SslStreamSettings With(
            Optional<bool> checkCertificateRevocation = default(Optional<bool>),
            Optional<IEnumerable<X509Certificate>> clientCertificates = default(Optional<IEnumerable<X509Certificate>>),
            Optional<LocalCertificateSelectionCallback> clientCertificateSelectionCallback = default(Optional<LocalCertificateSelectionCallback>),
            Optional<SslProtocols> enabledProtocols = default(Optional<SslProtocols>),
            Optional<RemoteCertificateValidationCallback> serverCertificateValidationCallback = default(Optional<RemoteCertificateValidationCallback>))
        {
            return new SslStreamSettings(
                checkCertificateRevocation: checkCertificateRevocation.WithDefault(_checkCertificateRevocation),
                clientCertificates: Optional.Create(clientCertificates.WithDefault(_clientCertificates)),
                clientCertificateSelectionCallback: clientCertificateSelectionCallback.WithDefault(_clientCertificateSelectionCallback),
                enabledProtocols: enabledProtocols.WithDefault(_enabledSslProtocols),
                serverCertificateValidationCallback: serverCertificateValidationCallback.WithDefault(_serverCertificateValidationCallback));
        }
    }
}