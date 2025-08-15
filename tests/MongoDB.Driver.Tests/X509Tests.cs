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
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests;

[Trait("Category", "Integration")]
[Trait("Category", "X509")]
public class X509Tests
{
    const string MONGODB_X509_CLIENT_CERTIFICATE_PATH = "MONGO_X509_CLIENT_CERTIFICATE_PATH";
    const string MONGODB_X509_CLIENT_CERTIFICATE_PASSWORD = "MONGO_X509_CLIENT_CERTIFICATE_PASSWORD";

    const string MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH = "MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH";
    const string MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD = "MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD";

    [Fact]
    public void Authentication_succeeds_with_MONGODB_X509_mechanism()
    {
        RequireEnvironment.Check().EnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PATH, isDefined: true);
        RequireEnvironment.Check().EnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PASSWORD, isDefined: true);
        RequireServer.Check().Tls(required: true);

        var pathToClientCertificate = Environment.GetEnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PATH);
        var password = Environment.GetEnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PASSWORD);
        var clientCertificate = new X509Certificate2(pathToClientCertificate, password);

        var settings = DriverTestConfiguration.GetClientSettings().Clone();
        //settings.Credential = MongoCredential.CreateMongoX509Credential();
        settings.SslSettings = settings.SslSettings.Clone();
        settings.SslSettings.ClientCertificates = [clientCertificate];

        AssertAuthenticationSucceeds(settings);
    }

    [Fact]
    public void Authentication_fails_with_MONGODB_X509_mechanism_when_username_is_wrong()
    {
        RequireEnvironment.Check().EnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PATH, isDefined: true);
        RequireEnvironment.Check().EnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PASSWORD, isDefined: true);
        RequireServer.Check().Tls(required: true);

        var pathToClientCertificate = Environment.GetEnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PATH);
        var password = Environment.GetEnvironmentVariable(MONGODB_X509_CLIENT_CERTIFICATE_PASSWORD);
        var clientCertificate = new X509Certificate2(pathToClientCertificate, password);

        var settings = DriverTestConfiguration.GetClientSettings().Clone();
        settings.Credential = MongoCredential.CreateMongoX509Credential("wrong_username");
        settings.SslSettings = settings.SslSettings.Clone();
        settings.SslSettings.ClientCertificates = [clientCertificate];

        AssertAuthenticationFails(settings);
    }

    [Fact]
    public void Authentication_fails_with_MONGODB_X509_mechanism_when_user_is_not_in_database()
    {
        RequireEnvironment.Check().EnvironmentVariable(MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH, isDefined: true);
        RequireEnvironment.Check().EnvironmentVariable(MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD, isDefined: true);
        RequireServer.Check().Tls(required: true);

        var pathToClientCertificate = Environment.GetEnvironmentVariable(MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH);
        var password = Environment.GetEnvironmentVariable(MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD);
        var clientCertificate = new X509Certificate2(pathToClientCertificate, password);

        var settings = DriverTestConfiguration.GetClientSettings().Clone();
        //settings.Credential = MongoCredential.CreateMongoX509Credential();
        settings.SslSettings = settings.SslSettings.Clone();
        settings.SslSettings.ClientCertificates = [clientCertificate];

        AssertAuthenticationFails(settings);
    }

    private void AssertAuthenticationSucceeds(MongoClientSettings settings)
    {
        using var client = DriverTestConfiguration.CreateMongoClient(settings);
        _ =  client.ListDatabaseNames().ToList();
    }

    private void AssertAuthenticationFails(MongoClientSettings settings)
    {
        using var client = DriverTestConfiguration.CreateMongoClient(settings);
        var exception = Record.Exception(() => client.ListDatabaseNames().ToList());
        exception.Should().BeOfType<MongoAuthenticationException>();

        // var innerException = exception.InnerException;
        // innerException.Should().BeOfType<MongoCommandException>();
        // innerException.Message.Should().Contain("Could not find user");
    }
}