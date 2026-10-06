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

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures whether a <see cref="MongoClient"/> encrypts its connections with TLS, and how server and
/// client certificates are handled.
/// </summary>
public sealed class TlsBuilder
{
    internal TlsBuilder()
    {
        AllowedProtocols = SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12;
    }

    /// <summary>
    /// Gets or sets the TLS protocol versions the client may negotiate with the server. The handshake uses
    /// the highest version supported by both sides. The default value is TLS 1.2 and TLS 1.3.
    /// </summary>
    public SslProtocols AllowedProtocols { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to relax TLS constraints as much as possible. Setting this
    /// to <c>true</c> disables both certificate and host name validation. Cannot be combined with
    /// <see cref="CheckCertificateRevocation"/>; building the client fails if both are <c>true</c>. Use with
    /// care; it is intended for testing against servers with self-signed certificates. The default value
    /// is <c>false</c>.
    /// </summary>
    public bool AllowInsecure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to check for certificate revocation. Cannot be combined with
    /// <see cref="AllowInsecure"/>; building the client fails if both are <c>true</c>. The default value is
    /// <c>false</c>.
    /// </summary>
    public bool CheckCertificateRevocation { get; set; }

    /// <summary>
    /// Gets or sets the client certificates presented to the server. The default value is <c>null</c>.
    /// </summary>
    public IEnumerable<X509Certificate> ClientCertificates
    {
        get;
        set => field = value?.ToArray();
    }

    /// <summary>
    /// Gets or sets the callback used to select the client certificate presented to the server.
    /// The default value is <c>null</c>.
    /// </summary>
    public LocalCertificateSelectionCallback ClientCertificateSelectionCallback { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to connect using TLS. The default value is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the callback used to validate the certificate presented by the server.
    /// The default value is <c>null</c>, which uses the default validation.
    /// </summary>
    public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
}
