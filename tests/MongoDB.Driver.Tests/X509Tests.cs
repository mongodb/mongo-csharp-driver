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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests;

[Trait("Category", "Integration")]
[Trait("Category", "X509")]
public class X509Tests
{
        [Theory]
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

            var settings = DriverTestConfiguration.GetClientSettings().Clone();
            settings.Credential = MongoCredential.CreateMongoX509Credential();
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
            using (var client = DriverTestConfiguration.CreateMongoClient(settings))
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
                if (Feature.SpeculativeAuthentication.IsSupported(CoreTestConfiguration.MaxWireVersion) &&
                    speculativeAuthenticatationShouldSucceedIfPossible)
                {
                    var serverSelector = new ReadPreferenceServerSelector(settings.ReadPreference);
                    var server = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, serverSelector);
                    var channel = server.GetChannel(OperationContext.NoTimeout);
                    var helloResult = channel.ConnectionDescription.HelloResult;
                    helloResult.SpeculativeAuthenticate.Should().NotBeNull();
                }
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