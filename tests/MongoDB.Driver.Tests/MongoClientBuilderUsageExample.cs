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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Tests
{
    // TODO: remove this example before merging the PR!!!
    // This is demo code only: it exists so that we can discuss the use-cases and how the new builder surface.
    //
    // NOTE: the methods below are deliberately not [Fact]s. MongoClientBuilder.Build() and
    // FromConnectionString() are not implemented yet, so these examples are compile-time only.
    public class MongoClientBuilderUsageExample
    {
        // Creating a client from a connection string, with no further configuration.
        public IMongoClient FromConnectionString()
        {
            return MongoClientBuilder
                .FromConnectionString("mongodb+srv://cluster0.example.mongodb.net/?retryWrites=true")
                .Build();
        }

        // Starting from a connection string and overriding a few knobs in code. Anything set on the
        // builder wins over what the connection string said.
        public IMongoClient FromConnectionStringWithOverrides()
        {
            return MongoClientBuilder
                .FromConnectionString("mongodb://localhost:27017")
                .ClientMetadata(metadata => metadata.ApplicationName = "orders-service")
                .ConnectionPool(pool =>
                {
                    pool.MinSize = 10;
                    pool.MaxSize = 200;
                })
                .Build();
        }

        // Username / password authentication with the mechanism negotiated with the server
        // (SCRAM-SHA-256, falling back to SCRAM-SHA-1).
        public IMongoClient UsernamePasswordAuthentication()
        {
            return new MongoClientBuilder()
                .Connectivity(connectivity => connectivity.Servers = new[] { new MongoServerAddress("localhost", 27017) })
                .Authentication(auth => auth.UseUsernamePassword("user", "pencil"))
                .Build();
        }

        // MONGODB-OIDC with one of the built-in environments (for example, workload identity on Azure).
        public IMongoClient OidcAuthenticationWithBuiltInEnvironment()
        {
            return new MongoClientBuilder()
                .Authentication(auth => auth.UseOidcCredential("azure", "my-client-id"))
                .Build();
        }

        // MONGODB-OIDC with a custom callback that fetches the access token from wherever the
        // application gets it.
        public IMongoClient OidcAuthenticationWithCallback()
        {
            return new MongoClientBuilder()
                .Authentication(auth => auth.UseOidcCredential(new MyOidcCallback()))
                .Build();
        }

        // The same configuration written against the inner builders directly, instead of through the
        // Action<T> overloads. Useful when the configuration is assembled in pieces rather than in one
        // fluent chain.
        public IMongoClient ConfiguringInnerBuildersDirectly()
        {
            var builder = new MongoClientBuilder();

            builder.Authentication().UseUsernamePassword("user", "pencil");
            builder.Connectivity().ReplicaSetName = "rs0";
            builder.Operations().ReadPreference = ReadPreference.SecondaryPreferred;

            return builder.Build();
        }

        // Tuning how operations are executed: server selection, default read preference, concerns, retries
        // and the declared API version.
        public IMongoClient OperationsDefaults()
        {
            return new MongoClientBuilder()
                .Operations(operations =>
                {
                    operations.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                    operations.ReadPreference = ReadPreference.Nearest;
                    operations.ReadConcern = ReadConcern.Majority;
                    operations.WriteConcern = WriteConcern.WMajority;
                    operations.RetryReads = true;
                    operations.RetryWrites = true;
                    operations.ServerApi = new ServerApi(ServerApiVersion.V1, strict: true);
                })
                .Build();
        }

        // Network level settings: timeouts and wire compression.
        public IMongoClient NetworkSettings()
        {
            return new MongoClientBuilder()
                .Network(network =>
                {
                    network.ConnectTimeout = TimeSpan.FromSeconds(5);
                    network.SocketTimeout = TimeSpan.FromSeconds(30);
                    network.Compressors = new[] { new CompressorConfiguration(CompressorType.Snappy) };
                })
                .Build();
        }

        // Diagnostics: structured logging plus an event handler. Subscriptions accumulate, so several
        // handlers can be registered for different event types.
        public IMongoClient Diagnostics(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            return new MongoClientBuilder()
                .Diagnostics(diagnostics =>
                {
                    diagnostics.Logging = new LoggingSettings(loggerFactory);
                    diagnostics.Subscribe<CommandStartedEvent>(e => Console.WriteLine($"{e.CommandName} started"));
                    diagnostics.Subscribe<CommandFailedEvent>(e => Console.WriteLine($"{e.CommandName} failed: {e.Failure.Message}"));
                })
                .Build();
        }

        // Automatic client-side field level encryption with a local master key.
        public IMongoClient AutoEncryption(byte[] localMasterKey)
        {
            return new MongoClientBuilder()
                .AutoEncryption(encryption =>
                {
                    encryption.KeyVaultNamespace = CollectionNamespace.FromFullName("encryption.__keyVault");
                    encryption.RegisterLocalKmsProvider(localMasterKey);
                    encryption.RegisterSchema(
                        CollectionNamespace.FromFullName("medical.patients"),
                        BsonDocument.Parse("{ bsonType : 'object', properties : { ssn : { encrypt : { bsonType : 'string', algorithm : 'AEAD_AES_256_CBC_HMAC_SHA_512-Random' } } } }"));
                })
                .Build();
        }

        // Automatic encryption with cloud KMS providers: AWS with credentials obtained on demand from the
        // environment, plus a named Azure provider configured through its options object.
        public IMongoClient AutoEncryptionWithCloudKms(string tenantId, string clientId, string clientSecret)
        {
            return new MongoClientBuilder()
                .AutoEncryption(encryption =>
                {
                    encryption.KeyVaultNamespace = CollectionNamespace.FromFullName("encryption.__keyVault");
                    encryption.RegisterAwsKmsProvider();
                    encryption.RegisterAzureKmsProvider("eu", new AzureKmsProviderOptions
                    {
                        TenantId = tenantId,
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    });
                })
                .Build();
        }

        // Everything at once, to show how the areas read side by side in a single chain.
        public IMongoClient FullyConfiguredClient()
        {
            return new MongoClientBuilder()
                .ClientMetadata(metadata => metadata.ApplicationName = "orders-service")
                .Connectivity(connectivity =>
                {
                    connectivity.Servers = new[]
                    {
                        new MongoServerAddress("mongo-1.example.com", 27017),
                        new MongoServerAddress("mongo-2.example.com", 27017)
                    };
                    connectivity.ReplicaSetName = "rs0";
                })
                .Authentication(auth => auth.UseUsernamePassword("user", "pencil"))
                .Tls(tls => tls.Enabled = true)
                .ConnectionPool(pool => pool.MaxSize = 200)
                .ServerMonitoring(monitoring => monitoring.HeartbeatInterval = TimeSpan.FromSeconds(5))
                .Operations(operations => operations.WriteConcern = WriteConcern.WMajority)
                .Translation(translation => translation.EnableClientSideProjections = true)
                .Build();
        }

        private sealed class MyOidcCallback : IOidcCallback
        {
            public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
                => new OidcAccessToken(accessToken: "<token obtained from the identity provider>", expiresIn: TimeSpan.FromMinutes(5));

            public Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
                => Task.FromResult(GetOidcAccessToken(parameters, cancellationToken));
        }
    }
}
