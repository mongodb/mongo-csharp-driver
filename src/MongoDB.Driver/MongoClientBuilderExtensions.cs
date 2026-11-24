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
using System.Security;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver;

/// <summary>
/// Extension methods for the builders exposed by <see cref="MongoClientBuilder"/>.
/// </summary>
public static class MongoClientBuilderExtensions
{
    // ---- AuthenticationBuilder ----

    /// <summary>
    /// Authenticates with a username and password defined in the <c>admin</c> database, using the mechanism
    /// negotiated with the server (SCRAM-SHA-256 or SCRAM-SHA-1).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public static AuthenticationBuilder UseUsernamePassword(this AuthenticationBuilder builder, string username, string password)
        => builder.UseUsernamePassword(username, password, authSource: "admin");

    /// <summary>
    /// Authenticates with a username and password, using the mechanism negotiated with the server
    /// (SCRAM-SHA-256 or SCRAM-SHA-1).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="authSource">The name of the database the user is defined in.</param>
    public static AuthenticationBuilder UseUsernamePassword(
        this AuthenticationBuilder builder,
        string username,
        string password,
        string authSource)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(authSource, nameof(authSource));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: null,
            source: authSource,
            databaseName: null,
            username,
            new PasswordEvidence(password));
        return builder;
    }

    /// <summary>
    /// Authenticates with GSSAPI (Kerberos), using the credentials of the calling process.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    public static AuthenticationBuilder UseGssapiCredential(this AuthenticationBuilder builder, string username)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: "GSSAPI",
            source: "$external",
            databaseName: null,
            username,
            new ExternalEvidence());
        return builder;
    }

    /// <summary>
    /// Authenticates with GSSAPI (Kerberos).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public static AuthenticationBuilder UseGssapiCredential(this AuthenticationBuilder builder, string username, string password)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: "GSSAPI",
            source: "$external",
            databaseName: null,
            username,
            new PasswordEvidence(password));
        return builder;
    }

    /// <summary>
    /// Authenticates with GSSAPI (Kerberos).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public static AuthenticationBuilder UseGssapiCredential(this AuthenticationBuilder builder, string username, SecureString password)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: "GSSAPI",
            source: "$external",
            databaseName: null,
            username,
            new PasswordEvidence(password));
        return builder;
    }

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using the specified callback to obtain the access token.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="callback">The OIDC callback.</param>
    public static AuthenticationBuilder UseOidcCallback(this AuthenticationBuilder builder, IOidcCallback callback)
        => builder.UseOidcCallback(callback, principalName: null);

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using the specified callback to obtain the access token.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="callback">The OIDC callback.</param>
    /// <param name="principalName">The principal name.</param>
    public static AuthenticationBuilder UseOidcCallback(this AuthenticationBuilder builder, IOidcCallback callback, string principalName)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(callback, nameof(callback));

        builder.Credential = MongoCredential.CreateRawOidcCredential(principalName)
            .WithMechanismProperty(OidcConfiguration.CallbackMechanismPropertyName, callback);
        return builder;
    }

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using one of the built-in environments.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="environment">The built-in environment.</param>
    public static AuthenticationBuilder UseOidcEnvironment(this AuthenticationBuilder builder, string environment)
        => builder.UseOidcEnvironment(environment, username: null);

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using one of the built-in environments.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="environment">The built-in environment.</param>
    /// <param name="username">The username.</param>
    public static AuthenticationBuilder UseOidcEnvironment(this AuthenticationBuilder builder, string environment, string username)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(environment, nameof(environment));

        builder.Credential = MongoCredential.CreateRawOidcCredential(username)
            .WithMechanismProperty(OidcConfiguration.EnvironmentMechanismPropertyName, environment);
        return builder;
    }

    /// <summary>
    /// Authenticates with MONGODB-X509, using the username contained in the client certificate.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static AuthenticationBuilder UseX509Credential(this AuthenticationBuilder builder)
        => builder.UseX509Credential(username: null);

    /// <summary>
    /// Authenticates with MONGODB-X509.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username, or <c>null</c> to use the one contained in the client certificate.</param>
    public static AuthenticationBuilder UseX509Credential(this AuthenticationBuilder builder, string username)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: "MONGODB-X509",
            source: "$external",
            databaseName: null,
            username,
            new ExternalEvidence());
        return builder;
    }

    /// <summary>
    /// Authenticates with PLAIN (LDAP) against the <c>$external</c> source.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public static AuthenticationBuilder UsePlainCredential(this AuthenticationBuilder builder, string username, string password)
        => builder.UsePlainCredential(username, password, authSource: "$external");

    /// <summary>
    /// Authenticates with PLAIN (LDAP).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="authSource">The name of the database the user is defined in, usually <c>$external</c>.</param>
    public static AuthenticationBuilder UsePlainCredential(
        this AuthenticationBuilder builder,
        string username,
        string password,
        string authSource)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(authSource, nameof(authSource));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: "PLAIN",
            source: authSource,
            databaseName: null,
            username,
            new PasswordEvidence(password));
        return builder;
    }

    // ---- AutoEncryptionBuilder ----

    /// <summary>
    /// Registers the AWS KMS provider with credentials obtained on demand from the environment
    /// (environment variables, ECS task role or EC2 instance profile).
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(this AutoEncryptionBuilder builder)
        => builder.RegisterAwsKmsProvider(new AwsKmsProviderOptions());

    /// <summary>
    /// Registers the AWS KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="options">The provider options.</param>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(this AutoEncryptionBuilder builder, AwsKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider("aws", options.ToKmsProviderOptions(isNamed: false), options.Tls);
    }

    /// <summary>
    /// Registers a named AWS KMS provider, such as <c>"aws:name1"</c>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The provider name, without the <c>"aws:"</c> prefix.</param>
    /// <param name="options">The provider options. Named providers do not support on-demand credentials.</param>
    public static AutoEncryptionBuilder RegisterAwsKmsProvider(this AutoEncryptionBuilder builder, string name, AwsKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider($"aws:{name}", options.ToKmsProviderOptions(isNamed: true), options.Tls);
    }

    /// <summary>
    /// Registers the Azure Key Vault KMS provider with an access token obtained on demand from the Azure
    /// Instance Metadata Service (managed identity).
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(this AutoEncryptionBuilder builder)
        => builder.RegisterAzureKmsProvider(new AzureKmsProviderOptions());

    /// <summary>
    /// Registers the Azure Key Vault KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="options">The provider options.</param>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(this AutoEncryptionBuilder builder, AzureKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider("azure", options.ToKmsProviderOptions(isNamed: false), options.Tls);
    }

    /// <summary>
    /// Registers a named Azure Key Vault KMS provider, such as <c>"azure:name1"</c>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The provider name, without the <c>"azure:"</c> prefix.</param>
    /// <param name="options">The provider options. Named providers do not support on-demand credentials.</param>
    public static AutoEncryptionBuilder RegisterAzureKmsProvider(this AutoEncryptionBuilder builder, string name, AzureKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider($"azure:{name}", options.ToKmsProviderOptions(isNamed: true), options.Tls);
    }

    /// <summary>
    /// Registers the GCP KMS provider with an access token obtained on demand from the GCP metadata server
    /// (attached service account).
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(this AutoEncryptionBuilder builder)
        => builder.RegisterGcpKmsProvider(new GcpKmsProviderOptions());

    /// <summary>
    /// Registers the GCP KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="options">The provider options.</param>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(this AutoEncryptionBuilder builder, GcpKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider("gcp", options.ToKmsProviderOptions(isNamed: false), options.Tls);
    }

    /// <summary>
    /// Registers a named GCP KMS provider, such as <c>"gcp:name1"</c>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The provider name, without the <c>"gcp:"</c> prefix.</param>
    /// <param name="options">The provider options. Named providers do not support on-demand credentials.</param>
    public static AutoEncryptionBuilder RegisterGcpKmsProvider(this AutoEncryptionBuilder builder, string name, GcpKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider($"gcp:{name}", options.ToKmsProviderOptions(isNamed: true), options.Tls);
    }

    /// <summary>
    /// Registers the KMIP KMS provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="options">The provider options.</param>
    public static AutoEncryptionBuilder RegisterKmipKmsProvider(this AutoEncryptionBuilder builder, KmipKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider("kmip", options.ToKmsProviderOptions(), options.Tls);
    }

    /// <summary>
    /// Registers a named KMIP KMS provider, such as <c>"kmip:name1"</c>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The provider name, without the <c>"kmip:"</c> prefix.</param>
    /// <param name="options">The provider options.</param>
    public static AutoEncryptionBuilder RegisterKmipKmsProvider(this AutoEncryptionBuilder builder, string name, KmipKmsProviderOptions options)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        return builder.RegisterKmsProvider($"kmip:{name}", options.ToKmsProviderOptions(), options.Tls);
    }

    /// <summary>
    /// Registers the local KMS provider, which uses a master key held by the application rather than a
    /// remote key management service.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="key">The 96 byte master key.</param>
    public static AutoEncryptionBuilder RegisterLocalKmsProvider(this AutoEncryptionBuilder builder, byte[] key)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNull(key, nameof(key));

        return builder.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", key } });
    }

    /// <summary>
    /// Registers a named local KMS provider, such as <c>"local:name1"</c>, which uses a master key held by the
    /// application rather than a remote key management service.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The provider name, without the <c>"local:"</c> prefix.</param>
    /// <param name="key">The 96 byte master key.</param>
    public static AutoEncryptionBuilder RegisterLocalKmsProvider(this AutoEncryptionBuilder builder, string name, byte[] key)
    {
        Ensure.IsNotNull(builder, nameof(builder));
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(key, nameof(key));

        return builder.RegisterKmsProvider($"local:{name}", new Dictionary<string, object> { { "key", key } });
    }
}
