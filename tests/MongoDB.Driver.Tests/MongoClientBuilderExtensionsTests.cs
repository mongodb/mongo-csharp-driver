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
using System.Security;
using FluentAssertions;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests;

public class MongoClientBuilderExtensionsTests
{
    private static readonly CollectionNamespace __keyVaultNamespace = CollectionNamespace.FromFullName("encryption.__keyVault");

    // ---- AuthenticationBuilder ----

    [Fact]
    public void UseUsernamePassword_should_create_a_default_credential_in_the_admin_database()
    {
        var subject = new AuthenticationBuilder();

        var result = subject.UseUsernamePassword("user", "pass");

        result.Should().BeSameAs(subject);
        AssertPasswordCredential(subject.Credential, null, "admin", "user", "pass");
    }

    [Fact]
    public void UseUsernamePassword_with_auth_source_should_use_the_source()
    {
        var subject = new AuthenticationBuilder();

        subject.UseUsernamePassword("user", "pass", "other");

        AssertPasswordCredential(subject.Credential, null, "other", "user", "pass");
    }

    [Fact]
    public void UsePlainCredential_should_create_a_plain_credential_in_the_external_database()
    {
        var subject = new AuthenticationBuilder();

        var result = subject.UsePlainCredential("user", "pass");

        result.Should().BeSameAs(subject);
        AssertPasswordCredential(subject.Credential, "PLAIN", "$external", "user", "pass");
    }

    [Fact]
    public void UsePlainCredential_with_auth_source_should_use_the_source()
    {
        var subject = new AuthenticationBuilder();

        subject.UsePlainCredential("user", "pass", "other");

        AssertPasswordCredential(subject.Credential, "PLAIN", "other", "user", "pass");
    }

    [Fact]
    public void UseGssapiCredential_with_username_should_use_external_evidence()
    {
        var subject = new AuthenticationBuilder();

        var result = subject.UseGssapiCredential("user");

        result.Should().BeSameAs(subject);
        subject.Credential.Mechanism.Should().Be("GSSAPI");
        subject.Credential.Source.Should().Be("$external");
        subject.Credential.Username.Should().Be("user");
        subject.Credential.Evidence.Should().BeOfType<ExternalEvidence>();
    }

    [Fact]
    public void UseGssapiCredential_with_string_password_should_use_password_evidence()
    {
        var subject = new AuthenticationBuilder();

        subject.UseGssapiCredential("user", "pass");

        AssertPasswordCredential(subject.Credential, "GSSAPI", "$external", "user", "pass");
    }

    [Fact]
    public void UseGssapiCredential_with_secure_string_password_should_use_password_evidence()
    {
        var subject = new AuthenticationBuilder();
        using var password = new SecureString();
        foreach (var c in "pass")
        {
            password.AppendChar(c);
        }

        subject.UseGssapiCredential("user", password);

        AssertPasswordCredential(subject.Credential, "GSSAPI", "$external", "user", "pass");
    }

    [Fact]
    public void UseOidcCallback_should_set_the_callback_property()
    {
        var callback = Mock.Of<IOidcCallback>();
        var subject = new AuthenticationBuilder();

        var result = subject.UseOidcCallback(callback);

        result.Should().BeSameAs(subject);
        subject.Credential.Mechanism.Should().Be("MONGODB-OIDC");
        subject.Credential.Username.Should().BeNull();
        subject.Credential.GetMechanismProperty<object>(OidcConfiguration.CallbackMechanismPropertyName, null).Should().BeSameAs(callback);
    }

    [Fact]
    public void UseOidcCallback_with_principal_name_should_set_the_username()
    {
        var callback = Mock.Of<IOidcCallback>();
        var subject = new AuthenticationBuilder();

        subject.UseOidcCallback(callback, "principal");

        subject.Credential.Username.Should().Be("principal");
        subject.Credential.GetMechanismProperty<object>(OidcConfiguration.CallbackMechanismPropertyName, null).Should().BeSameAs(callback);
    }

    [Fact]
    public void UseOidcEnvironment_should_set_the_environment_property()
    {
        var subject = new AuthenticationBuilder();

        var result = subject.UseOidcEnvironment("azure");

        result.Should().BeSameAs(subject);
        subject.Credential.Mechanism.Should().Be("MONGODB-OIDC");
        subject.Credential.Username.Should().BeNull();
        subject.Credential.GetMechanismProperty<string>(OidcConfiguration.EnvironmentMechanismPropertyName, null).Should().Be("azure");
    }

    [Fact]
    public void UseOidcEnvironment_with_username_should_set_the_username()
    {
        var subject = new AuthenticationBuilder();

        subject.UseOidcEnvironment("azure", "user");

        subject.Credential.Username.Should().Be("user");
        subject.Credential.GetMechanismProperty<string>(OidcConfiguration.EnvironmentMechanismPropertyName, null).Should().Be("azure");
    }

    [Fact]
    public void UseX509Credential_should_not_set_a_username()
    {
        var subject = new AuthenticationBuilder();

        var result = subject.UseX509Credential();

        result.Should().BeSameAs(subject);
        subject.Credential.Mechanism.Should().Be("MONGODB-X509");
        subject.Credential.Source.Should().Be("$external");
        subject.Credential.Username.Should().BeNull();
        subject.Credential.Evidence.Should().BeOfType<ExternalEvidence>();
    }

    [Fact]
    public void UseX509Credential_with_username_should_set_the_username()
    {
        var subject = new AuthenticationBuilder();

        subject.UseX509Credential("CN=client");

        subject.Credential.Username.Should().Be("CN=client");
        subject.Credential.Source.Should().Be("$external");
    }

    [Fact]
    public void Authentication_methods_should_replace_the_credential_configured_earlier()
    {
        var subject = new AuthenticationBuilder();

        subject.UseUsernamePassword("first", "pass")
            .UsePlainCredential("second", "pass")
            .UseX509Credential("third");

        subject.Credential.Mechanism.Should().Be("MONGODB-X509");
        subject.Credential.Username.Should().Be("third");
    }

    public static IEnumerable<object[]> AuthenticationCallsWithNullBuilder =>
    [
        ["UseUsernamePassword", new Action(() => ((AuthenticationBuilder)null).UseUsernamePassword("u", "p"))],
        ["UseUsernamePassword(authSource)", new Action(() => ((AuthenticationBuilder)null).UseUsernamePassword("u", "p", "db"))],
        ["UseGssapiCredential(user)", new Action(() => ((AuthenticationBuilder)null).UseGssapiCredential("u"))],
        ["UseGssapiCredential(user, string)", new Action(() => ((AuthenticationBuilder)null).UseGssapiCredential("u", "p"))],
        ["UseGssapiCredential(user, SecureString)", new Action(() => ((AuthenticationBuilder)null).UseGssapiCredential("u", new SecureString()))],
        ["UseOidcCallback", new Action(() => ((AuthenticationBuilder)null).UseOidcCallback(Mock.Of<IOidcCallback>()))],
        ["UseOidcEnvironment", new Action(() => ((AuthenticationBuilder)null).UseOidcEnvironment("azure"))],
        ["UseX509Credential", new Action(() => ((AuthenticationBuilder)null).UseX509Credential())],
        ["UsePlainCredential", new Action(() => ((AuthenticationBuilder)null).UsePlainCredential("u", "p"))],
        ["UsePlainCredential(authSource)", new Action(() => ((AuthenticationBuilder)null).UsePlainCredential("u", "p", "db"))]
    ];

    [Theory]
    [MemberData(nameof(AuthenticationCallsWithNullBuilder))]
    public void Authentication_methods_should_throw_when_builder_is_null(string method, Action call)
    {
        var exception = Record.Exception(call);

        exception.Should().BeOfType<ArgumentNullException>(because: method)
            .Subject.ParamName.Should().Be("builder");
    }

    public static IEnumerable<object[]> InvalidAuthenticationArguments =>
    [
        [typeof(ArgumentNullException), "authSource", new Action<AuthenticationBuilder>(b => b.UseUsernamePassword("u", "p", null))],
        [typeof(ArgumentException), "authSource", new Action<AuthenticationBuilder>(b => b.UseUsernamePassword("u", "p", ""))],
        [typeof(ArgumentNullException), "authSource", new Action<AuthenticationBuilder>(b => b.UsePlainCredential("u", "p", null))],
        [typeof(ArgumentException), "authSource", new Action<AuthenticationBuilder>(b => b.UsePlainCredential("u", "p", ""))],
        [typeof(ArgumentNullException), "callback", new Action<AuthenticationBuilder>(b => b.UseOidcCallback(null))],
        [typeof(ArgumentNullException), "callback", new Action<AuthenticationBuilder>(b => b.UseOidcCallback(null, "principal"))],
        [typeof(ArgumentNullException), "environment", new Action<AuthenticationBuilder>(b => b.UseOidcEnvironment(null))],
        [typeof(ArgumentNullException), "environment", new Action<AuthenticationBuilder>(b => b.UseOidcEnvironment(null, "user"))]
    ];

    [Theory]
    [MemberData(nameof(InvalidAuthenticationArguments))]
    public void Authentication_methods_should_throw_for_invalid_arguments(Type expectedException, string expectedParamName, Action<AuthenticationBuilder> call)
    {
        var subject = new AuthenticationBuilder();

        var exception = Record.Exception(() => call(subject));

        exception.Should().BeOfType(expectedException);
        ((ArgumentException)exception).ParamName.Should().Be(expectedParamName);
    }

    // ---- AutoEncryptionBuilder ----

    public static IEnumerable<object[]> KmsProviderRegistrations =>
    [
        [
            "aws",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterAwsKmsProvider()),
            new Dictionary<string, object>()
        ],
        [
            "aws",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterAwsKmsProvider(
                new AwsKmsProviderOptions { AccessKeyId = "id", SecretAccessKey = "secret", SessionToken = "token" })),
            new Dictionary<string, object> { { "accessKeyId", "id" }, { "secretAccessKey", "secret" }, { "sessionToken", "token" } }
        ],
        [
            "aws:name1",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterAwsKmsProvider(
                "name1", new AwsKmsProviderOptions { AccessKeyId = "id", SecretAccessKey = "secret" })),
            new Dictionary<string, object> { { "accessKeyId", "id" }, { "secretAccessKey", "secret" } }
        ],
        [
            "azure",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterAzureKmsProvider()),
            new Dictionary<string, object>()
        ],
        [
            "azure",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterAzureKmsProvider(
                new AzureKmsProviderOptions { TenantId = "tenant", ClientId = "client", ClientSecret = "secret", IdentityPlatformEndpoint = "login.example.com" })),
            new Dictionary<string, object>
            {
                { "tenantId", "tenant" }, { "clientId", "client" }, { "clientSecret", "secret" }, { "identityPlatformEndpoint", "login.example.com" }
            }
        ],
        [
            "azure:name1",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterAzureKmsProvider(
                "name1", new AzureKmsProviderOptions { TenantId = "tenant", ClientId = "client", ClientSecret = "secret" })),
            new Dictionary<string, object> { { "tenantId", "tenant" }, { "clientId", "client" }, { "clientSecret", "secret" } }
        ],
        [
            "gcp",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterGcpKmsProvider()),
            new Dictionary<string, object>()
        ],
        [
            "gcp",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterGcpKmsProvider(
                new GcpKmsProviderOptions { Email = "user@example.com", PrivateKey = "key", Endpoint = "oauth2.example.com" })),
            new Dictionary<string, object> { { "email", "user@example.com" }, { "privateKey", "key" }, { "endpoint", "oauth2.example.com" } }
        ],
        [
            "gcp:name1",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterGcpKmsProvider(
                "name1", new GcpKmsProviderOptions { Email = "user@example.com", PrivateKey = "key" })),
            new Dictionary<string, object> { { "email", "user@example.com" }, { "privateKey", "key" } }
        ],
        [
            "kmip",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterKmipKmsProvider(
                new KmipKmsProviderOptions { Endpoint = "kmip.example.com:5696" })),
            new Dictionary<string, object> { { "endpoint", "kmip.example.com:5696" } }
        ],
        [
            "kmip:name1",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterKmipKmsProvider(
                "name1", new KmipKmsProviderOptions { Endpoint = "kmip.example.com" })),
            new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }
        ],
        [
            "local",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterLocalKmsProvider(new byte[96])),
            new Dictionary<string, object> { { "key", new byte[96] } }
        ],
        [
            "local:name1",
            new Func<AutoEncryptionBuilder, AutoEncryptionBuilder>(b => b.RegisterLocalKmsProvider("name1", new byte[96])),
            new Dictionary<string, object> { { "key", new byte[96] } }
        ]
    ];

    [Theory]
    [MemberData(nameof(KmsProviderRegistrations))]
    public void Register_kms_provider_methods_should_register_the_provider_under_the_expected_name(
        string expectedName,
        Func<AutoEncryptionBuilder, AutoEncryptionBuilder> register,
        Dictionary<string, object> expectedOptions)
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };

        var result = register(subject);

        result.Should().BeSameAs(subject);
        var registered = subject.ToAutoEncryptionOptions().KmsProviders;
        registered.Keys.Should().Equal(expectedName);
        registered[expectedName].Keys.Should().BeEquivalentTo(expectedOptions.Keys);
        foreach (var expected in expectedOptions)
        {
            if (expected.Value is byte[] expectedBytes)
            {
                registered[expectedName][expected.Key].Should().BeOfType<byte[]>().Subject.Should().Equal(expectedBytes);
            }
            else
            {
                registered[expectedName][expected.Key].Should().Be(expected.Value);
            }
        }
    }

    [Fact]
    public void Register_kms_provider_methods_should_register_the_tls_options()
    {
        var tls = new KmsTlsOptions { CheckCertificateRevocation = true };
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };

        subject
            .RegisterAwsKmsProvider(new AwsKmsProviderOptions { Tls = tls })
            .RegisterAzureKmsProvider("name1", new AzureKmsProviderOptions { TenantId = "t", ClientId = "c", ClientSecret = "s", Tls = tls })
            .RegisterGcpKmsProvider("name1", new GcpKmsProviderOptions { Email = "e", PrivateKey = "k", Tls = tls })
            .RegisterKmipKmsProvider(new KmipKmsProviderOptions { Endpoint = "kmip.example.com", Tls = tls })
            .RegisterLocalKmsProvider(new byte[96]);

        var tlsOptions = subject.ToAutoEncryptionOptions().TlsOptions;
        tlsOptions.Keys.Should().BeEquivalentTo("aws", "azure:name1", "gcp:name1", "kmip");
        tlsOptions.Values.Should().OnlyContain(settings => settings.CheckCertificateRevocation);
    }

    [Fact]
    public void Register_kms_provider_methods_should_throw_when_the_provider_is_registered_twice()
    {
        var subject = new AutoEncryptionBuilder().RegisterAwsKmsProvider();

        var exception = Record.Exception(() => subject.RegisterAwsKmsProvider());

        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Register_kms_provider_methods_should_allow_a_bare_and_a_named_provider_of_the_same_kind()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };

        subject.RegisterLocalKmsProvider(new byte[96]).RegisterLocalKmsProvider("name1", new byte[96]);

        subject.ToAutoEncryptionOptions().KmsProviders.Keys.Should().BeEquivalentTo("local", "local:name1");
    }

    public static IEnumerable<object[]> InvalidKmsProviderArguments =>
    [
        ["RegisterAwsKmsProvider(null builder)", typeof(ArgumentNullException), "builder", new Action(() => ((AutoEncryptionBuilder)null).RegisterAwsKmsProvider())],
        ["RegisterAwsKmsProvider(null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterAwsKmsProvider((AwsKmsProviderOptions)null))],
        ["RegisterAwsKmsProvider(null name)", typeof(ArgumentNullException), "name", new Action(() => new AutoEncryptionBuilder().RegisterAwsKmsProvider(null, new AwsKmsProviderOptions()))],
        ["RegisterAwsKmsProvider(empty name)", typeof(ArgumentException), "name", new Action(() => new AutoEncryptionBuilder().RegisterAwsKmsProvider("", new AwsKmsProviderOptions()))],
        ["RegisterAwsKmsProvider(name, null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterAwsKmsProvider("n", null))],
        ["RegisterAzureKmsProvider(null builder)", typeof(ArgumentNullException), "builder", new Action(() => ((AutoEncryptionBuilder)null).RegisterAzureKmsProvider())],
        ["RegisterAzureKmsProvider(null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterAzureKmsProvider((AzureKmsProviderOptions)null))],
        ["RegisterAzureKmsProvider(empty name)", typeof(ArgumentException), "name", new Action(() => new AutoEncryptionBuilder().RegisterAzureKmsProvider("", new AzureKmsProviderOptions()))],
        ["RegisterAzureKmsProvider(name, null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterAzureKmsProvider("n", null))],
        ["RegisterGcpKmsProvider(null builder)", typeof(ArgumentNullException), "builder", new Action(() => ((AutoEncryptionBuilder)null).RegisterGcpKmsProvider())],
        ["RegisterGcpKmsProvider(null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterGcpKmsProvider((GcpKmsProviderOptions)null))],
        ["RegisterGcpKmsProvider(empty name)", typeof(ArgumentException), "name", new Action(() => new AutoEncryptionBuilder().RegisterGcpKmsProvider("", new GcpKmsProviderOptions()))],
        ["RegisterGcpKmsProvider(name, null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterGcpKmsProvider("n", null))],
        ["RegisterKmipKmsProvider(null builder)", typeof(ArgumentNullException), "builder", new Action(() => ((AutoEncryptionBuilder)null).RegisterKmipKmsProvider(new KmipKmsProviderOptions()))],
        ["RegisterKmipKmsProvider(null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterKmipKmsProvider(null))],
        ["RegisterKmipKmsProvider(empty name)", typeof(ArgumentException), "name", new Action(() => new AutoEncryptionBuilder().RegisterKmipKmsProvider("", new KmipKmsProviderOptions { Endpoint = "e" }))],
        ["RegisterKmipKmsProvider(name, null options)", typeof(ArgumentNullException), "options", new Action(() => new AutoEncryptionBuilder().RegisterKmipKmsProvider("n", null))],
        ["RegisterLocalKmsProvider(null builder)", typeof(ArgumentNullException), "builder", new Action(() => ((AutoEncryptionBuilder)null).RegisterLocalKmsProvider(new byte[96]))],
        ["RegisterLocalKmsProvider(null key)", typeof(ArgumentNullException), "key", new Action(() => new AutoEncryptionBuilder().RegisterLocalKmsProvider(null))],
        ["RegisterLocalKmsProvider(empty name)", typeof(ArgumentException), "name", new Action(() => new AutoEncryptionBuilder().RegisterLocalKmsProvider("", new byte[96]))],
        ["RegisterLocalKmsProvider(name, null key)", typeof(ArgumentNullException), "key", new Action(() => new AutoEncryptionBuilder().RegisterLocalKmsProvider("n", null))]
    ];

    [Theory]
    [MemberData(nameof(InvalidKmsProviderArguments))]
    public void Register_kms_provider_methods_should_throw_for_invalid_arguments(
        string call,
        Type expectedException,
        string expectedParamName,
        Action action)
    {
        var exception = Record.Exception(action);

        exception.Should().BeOfType(expectedException, because: call);
        ((ArgumentException)exception).ParamName.Should().Be(expectedParamName, because: call);
    }

    private static void AssertPasswordCredential(
        MongoCredential credential,
        string expectedMechanism,
        string expectedSource,
        string expectedUsername,
        string expectedPassword)
    {
        credential.Mechanism.Should().Be(expectedMechanism);
        credential.Source.Should().Be(expectedSource);
        credential.Username.Should().Be(expectedUsername);
        credential.Evidence.Should().BeOfType<PasswordEvidence>()
            .Subject.ToInsecureString().Should().Be(expectedPassword);
    }
}
