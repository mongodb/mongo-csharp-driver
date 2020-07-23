/* Copyright 2018–present MongoDB Inc.
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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    /// <summary>
    /// Authentication integration tests.
    /// </summary>
    public class AuthenticationTests
    {
        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_fails_when_user_has_Scram_Sha_1_mechanism_and_mechanism_is_Scram_Sha_256(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            // mechanisms field in createUser command requires server >=4.0
            var client = DriverTestConfiguration.Client;
            var userName = $"sha1{Guid.NewGuid()}";
            var password = "sha1";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-256", source: null, username: userName, password: password);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            AssertAuthenticationFails(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_fails_when_user_has_Scram_Sha_256_mechanism_and_mechanism_is_Scram_Sha_1(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"sha256{Guid.NewGuid()}";
            var password = "sha256";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-1", source: null, username: userName, password: password);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            AssertAuthenticationFails(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_fails_when_user_is_non_extant_and_mechanism_is_not_specified(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"cipher{Guid.NewGuid()}";
            var password = "bluepill";
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            AssertAuthenticationFails(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_both_Scram_Sha_mechanisms_and_mechanism_is_not_specified(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"both{Guid.NewGuid()}";
            var password = "both";

            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256", "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_both_scram_sha_mechanisms_and_mechanism_is_Scram_Sha_256(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);

            var client = DriverTestConfiguration.Client;
            var userName = $"both{Guid.NewGuid()}";
            var password = "both";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256", "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-256", source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_Scram_Sha_1_Mechanism_and_mechanism_is_not_specified(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            // mechanisms field in createUser command requires server >=4.0
            var client = DriverTestConfiguration.Client;

            var userName = $"sha1{Guid.NewGuid()}";
            var password = "sha1";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings, async, speculativeAuthenticatationShouldSucceedIfPossible: false);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_Scram_Sha_256_mechanism_and_mechanism_is_not_specified(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;

            var userName = $"sha256{Guid.NewGuid()}";
            var password = "sha256";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_Scram_Sha_1_mechanism_and_mechanism_is_Scram_Sha_1(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);

            // mechanisms field in createUser command requires server >=4.0
            var client = DriverTestConfiguration.Client;
            var userName = $"sha1{Guid.NewGuid()}";
            var password = "sha1";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-1", source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_Scram_Sha_256_mechanism_and_mechanism_is_Scram_Sha_256(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"sha256{Guid.NewGuid()}";
            var password = "sha256";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-256", source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_when_user_has_multiple_credentials_and_mechanism_is_not_specified(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var source1 = "nyc-matrix";
            var userName1 = $"ThomasAnderson{Guid.NewGuid()}";
            var password1 = "WhatIsTheMatrix";
            var source2 = "admin";
            var userName2 = $"Neo{Guid.NewGuid()}";
            var password2 = "TrinityAndZionForever";
            CreateDatabaseUser(client, source1, userName1, password1, role: "read", mechanisms: "SCRAM-SHA-256");
            CreateDatabaseUser(client, source2, userName2, password2, role: "root", mechanisms: "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
#pragma warning disable 618
            settings.Credentials = new[]
            {
#pragma warning restore 618
                MongoCredential.FromComponents(mechanism: null, source: source1, username: userName1, password: password1),
                MongoCredential.FromComponents(mechanism: null, source: source2, username: userName2, password: password2)
            };

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [InlineData("IX", "IX", "\u2168", "\u2163", false)] // "IX", "IX", Roman numeral nine, Roman numeral four
        [InlineData("IX", "IX", "\u2168", "\u2163", true)] // "IX", "IX", Roman numeral nine, Roman numeral four
        public void Authentication_succeeds_with_Ascii_username_and_Ascii_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername,
            string asciiPassword,
            string unicodeUsername,
            string unicodePassword,
            bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueAsciiUserName, password: asciiPassword);

            AssertAuthenticationSucceeds(settings, async);
        }

        // Currently, we only support SaslPrep in .NET Framework due to a lack of a string normalization function in
        // .NET Standard
#if NET452
        [SkippableTheory]
        [InlineData("IX", "IX", "I\u00ADX", "\u2168", "\u2163", false)] // "IX", "IX", "I-X", Roman numeral nine, Roman numeral four
        [InlineData("IX", "IX", "I\u00ADX", "\u2168", "\u2163", true)] // "IX", "IX", "I-X", Roman numeral nine, Roman numeral four
        public void Authentication_succeeds_with_Ascii_username_and_nonSaslPrepped_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername,
            string asciiPassword,
            string nonSaslPreppedPassword,
            string unicodeUsername,
            string unicodePassword,
            bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueAsciiUserName, password: nonSaslPreppedPassword);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [InlineData("IX", "IX", "\u2168", "\u2163", "I\u00ADV", false)] // "IX", "IX", Roman numeral nine, Roman numeral four, I-V
        [InlineData("IX", "IX", "\u2168", "\u2163", "I\u00ADV", true)] // "IX", "IX", Roman numeral nine, Roman numeral four, I-V
        public void Authentication_succeeds_with_Unicode_username_and_nonSaslPrepped_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername,
            string asciiPassword,
            string unicodeUsername,
            string unicodePassword,
            string nonSaslPreppedPassword,
            bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueUnicodeUserName, password: nonSaslPreppedPassword);

            AssertAuthenticationSucceeds(settings, async);
        }

        [SkippableTheory]
        [InlineData("IX", "IX", "\u2168", "\u2163", false)] // "IX", "IX", Roman numeral nine, Roman numeral four
        [InlineData("IX", "IX", "\u2168", "\u2163", true)] // "IX", "IX", Roman numeral nine, Roman numeral four
        public void Authentication_succeeds_with_Unicode_username_and_Unicode_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername,
            string asciiPassword,
            string unicodeUsername,
            string unicodePassword,
            bool async)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueUnicodeUserName, password: unicodePassword);

            AssertAuthenticationSucceeds(settings, async);
        }
#endif

        [SkippableTheory]
        [ParameterAttributeData]
        public void Authentication_succeeds_with_MONGODB_X509_mechanism(
            [Values(false, true)] bool async)
        {
            RequireEnvironment.Check().EnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PATH", isDefined: true);
            RequireEnvironment.Check().EnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PASSWORD", isDefined: true);
            RequireServer.Check().Tls(required: true);

            var pathToClientCertificate = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PATH");
            var password = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PASSWORD");
            var clientCertificate = new X509Certificate2(pathToClientCertificate, password);

            var userName = GetRfc2253FormattedUsernameFromX509ClientCertificate(clientCertificate);
            DropDatabaseUser(DriverTestConfiguration.Client, database: "$external", userName);
            CreateX509DatabaseUser(DriverTestConfiguration.Client, userName);

            var settings = DriverTestConfiguration.GetClientSettings().Clone();
            var serverVersion = CoreTestConfiguration.ServerVersion;
            if (Feature.ServerExtractsUsernameFromX509Certificate.IsSupported(serverVersion))
            {
                settings.Credential = MongoCredential.CreateMongoX509Credential();
            }
            else
            {
                settings.Credential = MongoCredential.CreateMongoX509Credential(userName);
            }
            settings.SslSettings = settings.SslSettings.Clone();
            settings.SslSettings.ClientCertificates = new[] { clientCertificate };

            AssertAuthenticationSucceeds(settings, async, speculativeAuthenticatationShouldSucceedIfPossible: true);
        }

        private void AssertAuthenticationSucceeds(
            MongoClientSettings settings,
            bool async,
            bool speculativeAuthenticatationShouldSucceedIfPossible = true)
        {
            // If we don't use a DisposableClient, the second run of AuthenticationSucceedsWithMongoDB_X509_mechanism
            // will fail because the backing Cluster's connections will be associated with a dropped user
            using (var client = DriverTestConfiguration.CreateDisposableClient(settings))
            {
                // The first command executed with the MongoClient triggers either the sync or async variation of the
                // MongoClient's IAuthenticator
                if (async)
                {
                    _ = client.ListDatabaseNamesAsync().GetAwaiter().GetResult().ToList();
                }
                else
                {
                    _ = client.ListDatabaseNames().ToList();
                }
                if (Feature.SpeculativeAuthentication.IsSupported(CoreTestConfiguration.ServerVersion) &&
                    speculativeAuthenticatationShouldSucceedIfPossible)
                {
                    var cancellationToken = CancellationToken.None;
                    var serverSelector = new ReadPreferenceServerSelector(settings.ReadPreference);
                    var server = client.Cluster.SelectServer(serverSelector, cancellationToken);
                    var channel = server.GetChannel(cancellationToken);
                    var isMasterResult = channel.ConnectionDescription.IsMasterResult;
                    isMasterResult.SpeculativeAuthenticate.Should().NotBeNull();
                }
            }
        }

        private void AssertAuthenticationFails(MongoClientSettings settings, bool async)
        {
            using (var client = DriverTestConfiguration.CreateDisposableClient(settings))
            {
                Exception exception;
                if (async)
                {
                    exception = Record.Exception(() => client.ListDatabaseNamesAsync().GetAwaiter().GetResult().ToList());

                }
                else
                {
                    exception = Record.Exception(() => client.ListDatabaseNames().ToList());
                }

                exception.Should().BeOfType<MongoAuthenticationException>();
            }
        }

        private BsonDocument CreateAdminDatabaseReadWriteUser(
            MongoClient client,
            string userName,
            string password,
            params string[] mechanisms)
        {
            return CreateAdminDatabaseUser(client, userName, password, "readWriteAnyDatabase", mechanisms);
        }

        private BsonDocument CreateAdminDatabaseUser(
            MongoClient client,
            string userName,
            string password,
            string role,
            params string[] mechanisms)
        {
            return CreateDatabaseUser(client, "admin", userName, password, "readWriteAnyDatabase", mechanisms);
        }

        private BsonDocument CreateDatabaseUser(
            MongoClient client,
            string source,
            string userName,
            string password,
            string role,
            params string[] mechanisms)
        {
            var createUserCommand =
                $"{{createUser: '{userName}', pwd: '{password}',"
                + $"roles: ['{role}'], mechanisms: {mechanisms.ToJson()}}}";
            return client.GetDatabase(source).RunCommand<BsonDocument>(createUserCommand);
        }

        private void CreateX509DatabaseUser(MongoClient client, string userName)
        {
            var createUserCommand =
                $"{{ createUser : '{userName}'," +
                @"   roles : [ { role : 'readWrite', db : 'test' },
                               { role : 'userAdminAnyDatabase', db : 'admin' } ]
                  }";
            // this command throws if the user already exists
            _ = client.GetDatabase("$external").RunCommand<BsonDocument>(createUserCommand);
        }

        private void DropDatabaseUser(MongoClient client, string database, string userName)
        {
            var dropUserCommand = $"{{ dropUser : '{userName}' }}";

            try
            {
                _ = client.GetDatabase(database).RunCommand<BsonDocument>(dropUserCommand);
            }
            catch (MongoCommandException) // command throws if user doesn't exist
            {
            }
        }

        private string GetRfc2253FormattedUsernameFromX509ClientCertificate(X509Certificate2 certificate)
        {
            var distinguishedName = certificate.SubjectName.Name;
            // Authentication will fail if we don't remove the delimiting spaces, even if we add the username WITH the
            // delimiting spaces.
            var nameWithoutDelimitingSpaces = string.Join(",", distinguishedName.Split(',').Select(s => s.Trim()));
            return nameWithoutDelimitingSpaces.Replace("S=", "ST=");
        }
    }
}
