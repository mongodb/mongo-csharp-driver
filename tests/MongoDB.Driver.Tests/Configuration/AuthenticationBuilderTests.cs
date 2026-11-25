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
using FluentAssertions;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class AuthenticationBuilderTests
{
    [Fact]
    public void Credential_should_default_to_null()
    {
        var subject = new AuthenticationBuilder();

        subject.Credential.Should().BeNull();
    }

    [Fact]
    public void Credential_should_be_settable()
    {
        var credential = MongoCredential.FromComponents(null, "admin", null, "user", new PasswordEvidence("password"));
        var subject = new AuthenticationBuilder();

        subject.Credential = credential;

        subject.Credential.Should().BeSameAs(credential);
    }

    [Theory]
    [InlineData("mongodb://host1")]
    [InlineData("mongodb://host1/?authSource=admin")]
    [InlineData("mongodb://host1/db")]
    public void ApplyConnectionString_should_not_create_a_credential_without_authentication_settings(string connectionString)
    {
        var subject = new AuthenticationBuilder();

        subject.ApplyConnectionString(new ConnectionString(connectionString));

        subject.Credential.Should().BeNull();
    }

    [Theory]
    [InlineData("mongodb://user:pass@host1", "admin")]
    [InlineData("mongodb://user:pass@host1/db", "db")]
    [InlineData("mongodb://user:pass@host1/db?authSource=other", "other")]
    [InlineData("mongodb://user:pass@host1/?authSource=other", "other")]
    public void ApplyConnectionString_should_use_the_expected_source(string connectionString, string expectedSource)
    {
        var subject = new AuthenticationBuilder();

        subject.ApplyConnectionString(new ConnectionString(connectionString));

        subject.Credential.Username.Should().Be("user");
        subject.Credential.Source.Should().Be(expectedSource);
        subject.Credential.Evidence.Should().Be(new PasswordEvidence("pass"));
    }

    [Theory]
    [InlineData("mongodb://user:pass@host1/?authMechanism=SCRAM-SHA-256", "SCRAM-SHA-256", "admin")]
    [InlineData("mongodb://user:pass@host1/?authMechanism=SCRAM-SHA-1", "SCRAM-SHA-1", "admin")]
    [InlineData("mongodb://user:pass@host1/?authMechanism=PLAIN&authSource=%24external", "PLAIN", "$external")]
    public void ApplyConnectionString_should_use_the_mechanism(string connectionString, string expectedMechanism, string expectedSource)
    {
        var subject = new AuthenticationBuilder();

        subject.ApplyConnectionString(new ConnectionString(connectionString));

        subject.Credential.Mechanism.Should().Be(expectedMechanism);
        subject.Credential.Source.Should().Be(expectedSource);
    }

    [Fact]
    public void ApplyConnectionString_should_create_an_x509_credential_without_a_user()
    {
        var subject = new AuthenticationBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1/?authMechanism=MONGODB-X509"));

        subject.Credential.Mechanism.Should().Be("MONGODB-X509");
        subject.Credential.Source.Should().Be("$external");
        subject.Credential.Username.Should().BeNull();
        subject.Credential.Evidence.Should().BeOfType<ExternalEvidence>();
    }

    [Fact]
    public void ApplyConnectionString_should_parse_CANONICALIZE_HOST_NAME_as_a_boolean_and_keep_other_properties_as_strings()
    {
        var subject = new AuthenticationBuilder();

        subject.ApplyConnectionString(new ConnectionString(
            "mongodb://user@host1/?authMechanism=GSSAPI&authMechanismProperties=SERVICE_NAME:other,CANONICALIZE_HOST_NAME:true"));

        subject.Credential.Mechanism.Should().Be("GSSAPI");
        subject.Credential.Source.Should().Be("$external");
        subject.Credential.GetMechanismProperty<string>("SERVICE_NAME", null).Should().Be("other");
        subject.Credential.GetMechanismProperty<bool>("CANONICALIZE_HOST_NAME", false).Should().BeTrue();
    }

    [Fact]
    public void ApplyConnectionString_should_apply_oidc_mechanism_properties()
    {
        var subject = new AuthenticationBuilder();

        subject.ApplyConnectionString(new ConnectionString(
            "mongodb://host1/?authMechanism=MONGODB-OIDC&authMechanismProperties=ENVIRONMENT:azure,TOKEN_RESOURCE:resource"));

        subject.Credential.Mechanism.Should().Be("MONGODB-OIDC");
        subject.Credential.GetMechanismProperty<string>("ENVIRONMENT", null).Should().Be("azure");
        subject.Credential.GetMechanismProperty<string>("TOKEN_RESOURCE", null).Should().Be("resource");
    }

    [Fact]
    public void ApplyConnectionString_should_replace_a_credential_configured_earlier()
    {
        var subject = new AuthenticationBuilder
        {
            Credential = MongoCredential.FromComponents(null, "admin", null, "earlier", new PasswordEvidence("password"))
        };

        subject.ApplyConnectionString(new ConnectionString("mongodb://later:pass@host1"));

        subject.Credential.Username.Should().Be("later");
    }

    [Fact]
    public void ApplyConnectionString_should_throw_when_a_default_credential_has_no_password()
    {
        var subject = new AuthenticationBuilder();

        var exception = Record.Exception(() => subject.ApplyConnectionString(new ConnectionString("mongodb://user@host1")));

        exception.Should().BeOfType<ArgumentException>();
    }
}
