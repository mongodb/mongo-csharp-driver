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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver;

/// <summary>
/// Convenience methods for registering the KMS providers the driver knows about with an
/// <see cref="AutoEncryptionBuilder"/>. Each one is a thin wrapper over
/// <see cref="AutoEncryptionBuilder.RegisterKmsProvider(string, IReadOnlyDictionary{string, object}, SslSettings)"/>,
/// which remains available for providers or options not covered here.
/// </summary>
// NOTE: every overload below takes only required parameters, and each provider's overloads telescope in a
// fixed order. Do not merge them into single methods with optional parameters. Optional parameter defaults
// are compiled into the caller's assembly, so adding a parameter later would break already compiled
// callers, and adding an overload would make existing call sites ambiguous. Keeping every parameter
// required leaves room to add one more overload when a provider gains an option.
//
// These are also extension methods on purpose. Instance methods win overload resolution over extension
// methods, so promoting any of these onto AutoEncryptionBuilder later would silently rebind newly compiled
// callers while previously compiled ones keep calling the extension. Treat that as a one-way door.
public static class AutoEncryptionBuilderExtensions
{
    /// <summary>
    /// Registers the AWS KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="accessKeyId">The access key id.</param>
    /// <param name="secretAccessKey">The secret access key.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(
        this AutoEncryptionBuilder builder,
        string accessKeyId,
        string secretAccessKey)
        => builder.RegisterAwsKmsProvider(accessKeyId, secretAccessKey, sessionToken: null, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the AWS KMS provider using temporary credentials.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="accessKeyId">The access key id.</param>
    /// <param name="secretAccessKey">The secret access key.</param>
    /// <param name="sessionToken">The session token, required only for temporary credentials.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(
        this AutoEncryptionBuilder builder,
        string accessKeyId,
        string secretAccessKey,
        string sessionToken)
        => builder.RegisterAwsKmsProvider(accessKeyId, secretAccessKey, sessionToken, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the AWS KMS provider under a provider name.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="accessKeyId">The access key id.</param>
    /// <param name="secretAccessKey">The secret access key.</param>
    /// <param name="sessionToken">The session token, or <c>null</c> for long lived credentials.</param>
    /// <param name="name">The provider name, for a named provider such as <c>"aws:name1"</c>.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(
        this AutoEncryptionBuilder builder,
        string accessKeyId,
        string secretAccessKey,
        string sessionToken,
        string name)
        => builder.RegisterAwsKmsProvider(accessKeyId, secretAccessKey, sessionToken, name, tlsSettings: null);

    /// <summary>
    /// Registers the AWS KMS provider under a provider name, with the TLS settings used to reach it.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="accessKeyId">The access key id.</param>
    /// <param name="secretAccessKey">The secret access key.</param>
    /// <param name="sessionToken">The session token, or <c>null</c> for long lived credentials.</param>
    /// <param name="name">The provider name, or <c>null</c> for the bare <c>"aws"</c> provider.</param>
    /// <param name="tlsSettings">The TLS settings used to reach the provider, or <c>null</c> for the defaults.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(
        this AutoEncryptionBuilder builder,
        string accessKeyId,
        string secretAccessKey,
        string sessionToken,
        string name,
        SslSettings tlsSettings)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(accessKeyId, nameof(accessKeyId));
        Ensure.IsNotNullOrEmpty(secretAccessKey, nameof(secretAccessKey));

        var options = new Dictionary<string, object>
        {
            { "accessKeyId", accessKeyId },
            { "secretAccessKey", secretAccessKey }
        };
        if (sessionToken != null)
        {
            options.Add("sessionToken", sessionToken);
        }

        return builder.RegisterKmsProvider(QualifyName("aws", name), options, tlsSettings);
    }

    /// <summary>
    /// Registers the Azure Key Vault KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(
        this AutoEncryptionBuilder builder,
        string tenantId,
        string clientId,
        string clientSecret)
        => builder.RegisterAzureKmsProvider(tenantId, clientId, clientSecret, identityPlatformEndpoint: null, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the Azure Key Vault KMS provider against a specific identity platform endpoint.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="identityPlatformEndpoint">The identity platform endpoint, or <c>null</c> for the default.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(
        this AutoEncryptionBuilder builder,
        string tenantId,
        string clientId,
        string clientSecret,
        string identityPlatformEndpoint)
        => builder.RegisterAzureKmsProvider(tenantId, clientId, clientSecret, identityPlatformEndpoint, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the Azure Key Vault KMS provider under a provider name.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="identityPlatformEndpoint">The identity platform endpoint, or <c>null</c> for the default.</param>
    /// <param name="name">The provider name, for a named provider such as <c>"azure:name1"</c>.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(
        this AutoEncryptionBuilder builder,
        string tenantId,
        string clientId,
        string clientSecret,
        string identityPlatformEndpoint,
        string name)
        => builder.RegisterAzureKmsProvider(tenantId, clientId, clientSecret, identityPlatformEndpoint, name, tlsSettings: null);

    /// <summary>
    /// Registers the Azure Key Vault KMS provider under a provider name, with the TLS settings used to reach it.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="identityPlatformEndpoint">The identity platform endpoint, or <c>null</c> for the default.</param>
    /// <param name="name">The provider name, or <c>null</c> for the bare <c>"azure"</c> provider.</param>
    /// <param name="tlsSettings">The TLS settings used to reach the provider, or <c>null</c> for the defaults.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(
        this AutoEncryptionBuilder builder,
        string tenantId,
        string clientId,
        string clientSecret,
        string identityPlatformEndpoint,
        string name,
        SslSettings tlsSettings)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(tenantId, nameof(tenantId));
        Ensure.IsNotNullOrEmpty(clientId, nameof(clientId));
        Ensure.IsNotNullOrEmpty(clientSecret, nameof(clientSecret));

        var options = new Dictionary<string, object>
        {
            { "tenantId", tenantId },
            { "clientId", clientId },
            { "clientSecret", clientSecret }
        };
        if (identityPlatformEndpoint != null)
        {
            options.Add("identityPlatformEndpoint", identityPlatformEndpoint);
        }

        return builder.RegisterKmsProvider(QualifyName("azure", name), options, tlsSettings);
    }

    /// <summary>
    /// Registers the GCP KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="email">The service account email.</param>
    /// <param name="privateKey">The private key, as a base64 string.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(
        this AutoEncryptionBuilder builder,
        string email,
        string privateKey)
        => builder.RegisterGcpKmsProvider(email, privateKey, endpoint: null, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the GCP KMS provider against a specific endpoint.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="email">The service account email.</param>
    /// <param name="privateKey">The private key, as a base64 string.</param>
    /// <param name="endpoint">The endpoint, or <c>null</c> for the default.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(
        this AutoEncryptionBuilder builder,
        string email,
        string privateKey,
        string endpoint)
        => builder.RegisterGcpKmsProvider(email, privateKey, endpoint, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the GCP KMS provider under a provider name.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="email">The service account email.</param>
    /// <param name="privateKey">The private key, as a base64 string.</param>
    /// <param name="endpoint">The endpoint, or <c>null</c> for the default.</param>
    /// <param name="name">The provider name, for a named provider such as <c>"gcp:name1"</c>.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(
        this AutoEncryptionBuilder builder,
        string email,
        string privateKey,
        string endpoint,
        string name)
        => builder.RegisterGcpKmsProvider(email, privateKey, endpoint, name, tlsSettings: null);

    /// <summary>
    /// Registers the GCP KMS provider under a provider name, with the TLS settings used to reach it.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="email">The service account email.</param>
    /// <param name="privateKey">The private key, as a base64 string.</param>
    /// <param name="endpoint">The endpoint, or <c>null</c> for the default.</param>
    /// <param name="name">The provider name, or <c>null</c> for the bare <c>"gcp"</c> provider.</param>
    /// <param name="tlsSettings">The TLS settings used to reach the provider, or <c>null</c> for the defaults.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(
        this AutoEncryptionBuilder builder,
        string email,
        string privateKey,
        string endpoint,
        string name,
        SslSettings tlsSettings)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(email, nameof(email));
        Ensure.IsNotNullOrEmpty(privateKey, nameof(privateKey));

        var options = new Dictionary<string, object>
        {
            { "email", email },
            { "privateKey", privateKey }
        };
        if (endpoint != null)
        {
            options.Add("endpoint", endpoint);
        }

        return builder.RegisterKmsProvider(QualifyName("gcp", name), options, tlsSettings);
    }

    /// <summary>
    /// Registers the KMIP KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="endpoint">The KMIP endpoint.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterKmipKmsProvider(
        this AutoEncryptionBuilder builder,
        string endpoint)
        => builder.RegisterKmipKmsProvider(endpoint, name: null, tlsSettings: null);

    /// <summary>
    /// Registers the KMIP KMS provider under a provider name.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="endpoint">The KMIP endpoint.</param>
    /// <param name="name">The provider name, for a named provider such as <c>"kmip:name1"</c>.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterKmipKmsProvider(
        this AutoEncryptionBuilder builder,
        string endpoint,
        string name)
        => builder.RegisterKmipKmsProvider(endpoint, name, tlsSettings: null);

    /// <summary>
    /// Registers the KMIP KMS provider under a provider name, with the TLS settings used to reach it.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="endpoint">The KMIP endpoint.</param>
    /// <param name="name">The provider name, or <c>null</c> for the bare <c>"kmip"</c> provider.</param>
    /// <param name="tlsSettings">The TLS settings used to reach the provider, or <c>null</c> for the defaults.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterKmipKmsProvider(
        this AutoEncryptionBuilder builder,
        string endpoint,
        string name,
        SslSettings tlsSettings)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(endpoint, nameof(endpoint));

        var options = new Dictionary<string, object> { { "endpoint", endpoint } };

        return builder.RegisterKmsProvider(QualifyName("kmip", name), options, tlsSettings);
    }

    /// <summary>
    /// Registers the local KMS provider, which uses a master key held by the application rather than a
    /// remote key management service.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="key">The 96 byte master key.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterLocalKmsProvider(
        this AutoEncryptionBuilder builder,
        byte[] key)
        => builder.RegisterLocalKmsProvider(key, name: null);

    /// <summary>
    /// Registers the local KMS provider under a provider name, which uses a master key held by the
    /// application rather than a remote key management service.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="key">The 96 byte master key.</param>
    /// <param name="name">The provider name, or <c>null</c> for the bare <c>"local"</c> provider.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public static AutoEncryptionBuilder RegisterLocalKmsProvider(
        this AutoEncryptionBuilder builder,
        byte[] key,
        string name)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(key, nameof(key));

        var options = new Dictionary<string, object> { { "key", key } };

        return builder.RegisterKmsProvider(QualifyName("local", name), options);
    }

    private static string QualifyName(string provider, string name)
        => name == null ? provider : $"{provider}:{name}";
}
