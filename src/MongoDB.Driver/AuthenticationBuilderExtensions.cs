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

using System.Security;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver;

/// <summary>
/// Extension methods for <see cref="AuthenticationBuilder"/>.
/// </summary>
public static class AuthenticationBuilderExtensions
{
    // NOTE: do not merge these overloads into one method with optional parameters. Optional parameter
    // defaults are compiled into the caller's assembly, so adding a parameter later would break already
    // compiled callers, and adding an overload would make existing call sites ambiguous.

    /// <summary>
    /// Authenticates with the default mechanism negotiated with the server (SCRAM-SHA-256 or SCRAM-SHA-1).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="databaseName">Name of the database the user is defined in.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseCredential(
        this AuthenticationBuilder builder,
        string databaseName,
        string username,
        string password)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: null,
            source: null,
            databaseName,
            username,
            new PasswordEvidence(password));
        return builder;
    }

    /// <summary>
    /// Authenticates with GSSAPI (Kerberos), using the credentials of the calling process.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    /// <remarks>This overload is used primarily on linux.</remarks>
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
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
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
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
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
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseOidcCredential(this AuthenticationBuilder builder, IOidcCallback callback)
        => builder.UseOidcCredential(callback, principalName: null);

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using the specified callback to obtain the access token.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="callback">The OIDC callback.</param>
    /// <param name="principalName">The principal name.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseOidcCredential(this AuthenticationBuilder builder, IOidcCallback callback, string principalName)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.CreateRawOidcCredential(principalName)
            .WithMechanismProperty(OidcConfiguration.CallbackMechanismPropertyName, callback);
        return builder;
    }

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using one of the built-in environments.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="environment">The built-in environment.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseOidcCredential(this AuthenticationBuilder builder, string environment)
        => builder.UseOidcCredential(environment, username: null);

    /// <summary>
    /// Authenticates with MONGODB-OIDC, using one of the built-in environments.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="environment">The built-in environment.</param>
    /// <param name="username">The username.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseOidcCredential(this AuthenticationBuilder builder, string environment, string username)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.CreateRawOidcCredential(username)
            .WithMechanismProperty(OidcConfiguration.EnvironmentMechanismPropertyName, environment);
        return builder;
    }

    /// <summary>
    /// Authenticates with MONGODB-X509, using the username contained in the client certificate.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseMongoX509Credential(this AuthenticationBuilder builder)
        => builder.UseMongoX509Credential(username: null);

    /// <summary>
    /// Authenticates with MONGODB-X509.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="username">The username, or <c>null</c> to use the one contained in the client certificate.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UseMongoX509Credential(this AuthenticationBuilder builder, string username)
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
    /// Authenticates with PLAIN (LDAP).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="databaseName">Name of the database the user is defined in.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The same <see cref="AuthenticationBuilder"/> instance so that calls can be chained.</returns>
    public static AuthenticationBuilder UsePlainCredential(
        this AuthenticationBuilder builder,
        string databaseName,
        string username,
        string password)
    {
        Ensure.IsNotNull(builder, nameof(builder));

        builder.Credential = MongoCredential.FromComponents(
            mechanism: "PLAIN",
            source: null,
            databaseName,
            username,
            new PasswordEvidence(password));
        return builder;
    }
}
