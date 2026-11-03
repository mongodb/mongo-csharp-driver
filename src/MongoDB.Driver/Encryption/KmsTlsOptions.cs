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
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Encryption;

/// <summary>
/// Options for the TLS connection to a KMS host.
/// </summary>
/// <remarks>
/// TLS is always used to reach a KMS host, and certificate validation cannot be disabled or replaced.
/// The values are copied when the provider is registered, so later changes to this instance have no effect.
/// </remarks>
public sealed class KmsTlsOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KmsTlsOptions"/> class.
    /// </summary>
    public KmsTlsOptions()
    {
        AllowedProtocols = SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12;
    }

    internal KmsTlsOptions(KmsTlsOptions other)
    {
        AllowedProtocols = other.AllowedProtocols;
        CheckCertificateRevocation = other.CheckCertificateRevocation;
        ClientCertificates = other.ClientCertificates;
    }

    /// <summary>
    /// Gets or sets the TLS protocol versions the client may negotiate with the KMS host. The handshake uses
    /// the highest version supported by both sides. The default value is TLS 1.2 and TLS 1.3.
    /// </summary>
    public SslProtocols AllowedProtocols
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to check for certificate revocation. The default value is
    /// <c>false</c>.
    /// </summary>
    public bool CheckCertificateRevocation
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the client certificates presented to the KMS host, for example to authenticate to a KMIP
    /// server. The default value is <c>null</c>.
    /// </summary>
    public IEnumerable<X509Certificate> ClientCertificates
    {
        get;
        set => field = value?.ToArray();
    }

    // a new instance on every call, so that clients built from the same options never share a mutable SslSettings
    internal SslSettings ToSslSettings()
        => new()
        {
            CheckCertificateRevocation = CheckCertificateRevocation,
            ClientCertificates = ClientCertificates,
            EnabledSslProtocols = AllowedProtocols
        };
}
