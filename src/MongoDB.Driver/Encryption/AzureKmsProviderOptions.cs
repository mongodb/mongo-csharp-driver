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
/// Options for the Azure Key Vault KMS provider.
/// </summary>
/// <remarks>
/// Set <see cref="TenantId"/>, <see cref="ClientId"/> and <see cref="ClientSecret"/> to authenticate with a
/// client secret. Leave all three unset to obtain an access token on demand from the Azure Instance Metadata
/// Service (managed identity), which is only supported for the unnamed <c>"azure"</c> provider.
/// The values are copied when the provider is registered, so later changes to this instance have no effect.
/// </remarks>
public sealed class AzureKmsProviderOptions
{
    /// <summary>
    /// Gets or sets the tenant id. Must be set together with <see cref="ClientId"/> and <see cref="ClientSecret"/>.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client id. Must be set together with <see cref="TenantId"/> and <see cref="ClientSecret"/>.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret. Must be set together with <see cref="TenantId"/> and <see cref="ClientId"/>.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the identity platform endpoint. The default value is <c>null</c>, which uses
    /// <c>login.microsoftonline.com</c>. Requires the client secret credentials to be set.
    /// </summary>
    public string IdentityPlatformEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the TLS settings used to reach the KMS host. The default value is <c>null</c>, which
    /// uses the default TLS settings.
    /// </summary>
    public SslSettings TlsSettings { get; set; }

    internal Dictionary<string, object> ToKmsProviderOptions(bool isNamed)
    {
        var hasTenantId = !string.IsNullOrEmpty(TenantId);
        var hasClientId = !string.IsNullOrEmpty(ClientId);
        var hasClientSecret = !string.IsNullOrEmpty(ClientSecret);
        var hasIdentityPlatformEndpoint = !string.IsNullOrEmpty(IdentityPlatformEndpoint);

        if (hasTenantId != hasClientId || hasTenantId != hasClientSecret)
        {
            throw new ArgumentException($"{nameof(TenantId)}, {nameof(ClientId)} and {nameof(ClientSecret)} must be set together.", "options");
        }

        var options = new Dictionary<string, object>();
        if (!hasTenantId)
        {
            if (hasIdentityPlatformEndpoint)
            {
                throw new ArgumentException($"{nameof(IdentityPlatformEndpoint)} requires {nameof(TenantId)}, {nameof(ClientId)} and {nameof(ClientSecret)} to be set.", "options");
            }
            if (isNamed)
            {
                throw new ArgumentException("On-demand credentials are not supported for named KMS providers.", "options");
            }

            return options;
        }

        options.Add("tenantId", TenantId);
        options.Add("clientId", ClientId);
        options.Add("clientSecret", ClientSecret);
        if (hasIdentityPlatformEndpoint)
        {
            options.Add("identityPlatformEndpoint", IdentityPlatformEndpoint);
        }

        return options;
    }
}
