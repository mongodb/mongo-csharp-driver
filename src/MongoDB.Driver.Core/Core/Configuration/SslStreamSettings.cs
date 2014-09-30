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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    public class SslStreamSettings
    {
        // fields
        private bool _checkCertificateRevocation;
        private IEnumerable<X509Certificate> _clientCertificates;
        private LocalCertificateSelectionCallback _clientCertificateSelector;
        private SslProtocols _enabledSslProtocols;
        private RemoteCertificateValidationCallback _serverCertificateValidator;

        // constructors
        public SslStreamSettings(
            Optional<IEnumerable<X509Certificate>> clientCertificates = default(Optional<IEnumerable<X509Certificate>>),
            Optional<bool> checkCertificateRevocation = default(Optional<bool>),
            Optional<LocalCertificateSelectionCallback> clientCertificateSelector = default(Optional<LocalCertificateSelectionCallback>),
            Optional<SslProtocols> enabledProtocols = default(Optional<SslProtocols>),
            Optional<RemoteCertificateValidationCallback> serverCertificateValidator = default(Optional<RemoteCertificateValidationCallback>))
        {
            _clientCertificates = clientCertificates.WithDefault(Enumerable.Empty<X509Certificate>());
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
            return new SslStreamSettings(
                new Optional<IEnumerable<X509Certificate>>(clientCertificates.WithDefault(_clientCertificates)),
                checkCertificateRevocation.WithDefault(_checkCertificateRevocation),
                clientCertificateSelector.WithDefault(_clientCertificateSelector),
                enabledProtocols.WithDefault(_enabledSslProtocols),
                serverCertificateValidator.WithDefault(_serverCertificateValidator));
        }
    }
}