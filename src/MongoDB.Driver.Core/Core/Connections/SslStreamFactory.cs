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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    internal class SslStreamFactory : IStreamFactory
    {
        // fields
        private readonly SslStreamSettings _settings;
        private readonly IStreamFactory _wrapped;

        public SslStreamFactory(SslStreamSettings settings, IStreamFactory wrapped)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _wrapped = Ensure.IsNotNull(wrapped, "wrapped");
        }

        public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var stream = await _wrapped.CreateStreamAsync(endPoint, cancellationToken).ConfigureAwait(false);

            var sslStream = new SslStream(
                stream,
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: _settings.ServerCertificateValidationCallback,
                userCertificateSelectionCallback: _settings.ClientCertificateSelectionCallback);

            string targetHost;
            DnsEndPoint dnsEndPoint;
            IPEndPoint ipEndPoint;
            if ((dnsEndPoint = endPoint as DnsEndPoint) != null)
            {
                targetHost = dnsEndPoint.Host;
            }
            else if ((ipEndPoint = endPoint as IPEndPoint) != null)
            {
                targetHost = ipEndPoint.Address.ToString();
            }
            else
            {
                targetHost = endPoint.ToString();
            }

            var clientCertificates = new X509CertificateCollection(_settings.ClientCertificates.ToArray());

            try
            {
                await sslStream.AuthenticateAsClientAsync(targetHost, clientCertificates, _settings.EnabledSslProtocols, _settings.CheckCertificateRevocation).ConfigureAwait(false);
            }
            catch
            {
                stream.Close();
                stream.Dispose();
                throw;
            }
            return sslStream;
        }
    }
}
