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
using System.Collections.Generic;

namespace MongoDB.Driver.Encryption;

/// <summary>
/// Options for the GCP KMS provider.
/// </summary>
/// <remarks>
/// Set <see cref="Email"/> and <see cref="PrivateKey"/> to authenticate as a service account. Leave both unset
/// to obtain an access token on demand from the GCP metadata server (attached service account), which is only
/// supported for the unnamed <c>"gcp"</c> provider.
/// The values are copied when the provider is registered, so later changes to this instance have no effect.
/// </remarks>
public sealed class GcpKmsProviderOptions
{
    /// <summary>
    /// Gets or sets the service account email. Must be set together with <see cref="PrivateKey"/>.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the service account private key, as a base64 string. Must be set together with
    /// <see cref="Email"/>.
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    /// Gets or sets the endpoint used to obtain access tokens. The default value is <c>null</c>, which uses
    /// <c>oauth2.googleapis.com</c>. Requires the service account credentials to be set.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the TLS settings used to reach the KMS host. The default value is <c>null</c>, which
    /// uses the default TLS settings.
    /// </summary>
    public SslSettings TlsSettings { get; set; }

    internal Dictionary<string, object> ToKmsProviderOptions(bool isNamed)
    {
        var hasEmail = !string.IsNullOrEmpty(Email);
        var hasPrivateKey = !string.IsNullOrEmpty(PrivateKey);
        var hasEndpoint = !string.IsNullOrEmpty(Endpoint);

        if (hasEmail != hasPrivateKey)
        {
            throw new ArgumentException($"{nameof(Email)} and {nameof(PrivateKey)} must be set together.", "options");
        }

        var options = new Dictionary<string, object>();
        if (!hasEmail)
        {
            if (hasEndpoint)
            {
                throw new ArgumentException($"{nameof(Endpoint)} requires {nameof(Email)} and {nameof(PrivateKey)} to be set.", "options");
            }
            if (isNamed)
            {
                throw new ArgumentException("On-demand credentials are not supported for named KMS providers.", "options");
            }

            return options;
        }

        options.Add("email", Email);
        options.Add("privateKey", PrivateKey);
        if (hasEndpoint)
        {
            options.Add("endpoint", Endpoint);
        }

        return options;
    }
}
