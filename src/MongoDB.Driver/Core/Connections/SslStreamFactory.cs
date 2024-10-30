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
    /// <summary>
    /// Represents a factory for an ssl stream.
    /// </summary>
    public class SslStreamFactory : IStreamFactory
    {
        // fields
        private readonly SslStreamSettings _settings;
        private readonly IStreamFactory _wrapped;

        /// <summary>
        /// Constructs an Ssl Stream Factory.
        /// </summary>
        /// <param name="settings">The SslStreamSettings.</param>
        /// <param name="wrapped">The underlying stream factory.</param>
        public SslStreamFactory(SslStreamSettings settings, IStreamFactory wrapped)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _wrapped = Ensure.IsNotNull(wrapped, nameof(wrapped));
        }

        // public methods
        /// <inheritdoc />
        public Stream CreateStream(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var stream = _wrapped.CreateStream(endPoint, cancellationToken);
            try
            {
                var sslStream = CreateSslStream(stream);
                var targetHost = GetTargetHost(endPoint);

#if NET6_0_OR_GREATER
                var options = GetAuthenticationOptions(targetHost);
                sslStream.AuthenticateAsClient(options);
#elif NETSTANDARD2_1_OR_GREATER
                var options = GetAuthenticationOptions(targetHost);
                sslStream.AuthenticateAsClientAsync(options, cancellationToken).GetAwaiter().GetResult();
#else
                var clientCertificates = new X509CertificateCollection(_settings.ClientCertificates.ToArray());
                sslStream.AuthenticateAsClient(targetHost, clientCertificates, _settings.EnabledSslProtocols, _settings.CheckCertificateRevocation);
#endif
                return sslStream;
            }
            catch
            {
                DisposeStreamIgnoringExceptions(stream);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var stream = await _wrapped.CreateStreamAsync(endPoint, cancellationToken).ConfigureAwait(false);
            try
            {
                var sslStream = CreateSslStream(stream);
                var targetHost = GetTargetHost(endPoint);

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
                var options = GetAuthenticationOptions(targetHost);
                await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
#else
                var clientCertificates = new X509CertificateCollection(_settings.ClientCertificates.ToArray());
                await sslStream.AuthenticateAsClientAsync(targetHost, clientCertificates, _settings.EnabledSslProtocols, _settings.CheckCertificateRevocation).ConfigureAwait(false);
#endif
                return sslStream;
            }
            catch
            {
                DisposeStreamIgnoringExceptions(stream);
                throw;
            }
        }

        // private methods
        private SslStream CreateSslStream(Stream stream)
        {
            return new SslStream(
                stream,
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: _settings.ServerCertificateValidationCallback,
                userCertificateSelectionCallback: _settings.ClientCertificateSelectionCallback);
        }

        private void DisposeStreamIgnoringExceptions(Stream stream)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                // ignore exception
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        private SslClientAuthenticationOptions GetAuthenticationOptions(string targetHost) => new()
        {
            AllowRenegotiation = false,
            ClientCertificates = new X509CertificateCollection(_settings.ClientCertificates.ToArray()),
            CertificateRevocationCheckMode = _settings.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
            EnabledSslProtocols = _settings.EnabledSslProtocols,
            TargetHost = targetHost
        };
#endif

        private string GetTargetHost(EndPoint endPoint)
        {
            DnsEndPoint dnsEndPoint;
            if ((dnsEndPoint = endPoint as DnsEndPoint) != null)
            {
                return dnsEndPoint.Host;
            }

            IPEndPoint ipEndPoint;
            if ((ipEndPoint = endPoint as IPEndPoint) != null)
            {
                return ipEndPoint.Address.ToString();
            }

            return endPoint.ToString();
        }
    }
}
