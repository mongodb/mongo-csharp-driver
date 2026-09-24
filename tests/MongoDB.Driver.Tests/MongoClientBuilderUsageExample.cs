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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Tests
{
    // TODO: remove this example before merging the PR. This is demo code only: it exists so that we can
    // discuss the use-cases and how the new builder surface reads at the call site.
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
                    pool.MinConnectionPoolSize = 10;
                    pool.MaxConnectionPoolSize = 200;
                })
                .Build();
        }

        // Username / password authentication with the mechanism negotiated with the server
        // (SCRAM-SHA-256, falling back to SCRAM-SHA-1).
        public IMongoClient UsernamePasswordAuthentication()
        {
            return new MongoClientBuilder()
                .Connectivity(connectivity => connectivity.Servers = new[] { new MongoServerAddress("localhost", 27017) })
                .Authentication(auth => auth.UseCredential("admin", "user", "pencil"))
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

        // MONGODB-X509: the username is taken from the client certificate, so only the certificate
        // has to be configured.
        public IMongoClient X509Authentication()
        {
            return new MongoClientBuilder()
                .Tls(tls =>
                {
                    tls.UseTls = true;
                    tls.ClientCertificates = new[] { new System.Security.Cryptography.X509Certificates.X509Certificate2("client.pfx") };
                })
                .Authentication(auth => auth.UseMongoX509Credential())
                .Build();
        }

        // The same configuration written against the inner builders directly, instead of through the
        // Action<T> overloads. Useful when the configuration is assembled in pieces rather than in one
        // fluent chain.
        public IMongoClient ConfiguringInnerBuildersDirectly()
        {
            var builder = new MongoClientBuilder();

            builder.Authentication().UseCredential("admin", "user", "pencil");
            builder.Connectivity().ReplicaSetName = "rs0";
            builder.ServerSelection().ReadPreference = ReadPreference.SecondaryPreferred;

            return builder.Build();
        }

        // Tuning how operations are executed: default concerns, retries and the declared API version.
        public IMongoClient OperationExecutionDefaults()
        {
            return new MongoClientBuilder()
                .OperationExecution(execution =>
                {
                    execution.ReadConcern = ReadConcern.Majority;
                    execution.WriteConcern = WriteConcern.WMajority;
                    execution.RetryReads = true;
                    execution.RetryWrites = true;
                    execution.ServerApi = new ServerApi(ServerApiVersion.V1, strict: true);
                })
                .ServerSelection(selection =>
                {
                    selection.ReadPreference = ReadPreference.Nearest;
                    selection.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
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
                    diagnostics.LoggingSettings = new LoggingSettings(loggerFactory);
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
                    encryption.RegisterKmsProvider(
                        "local",
                        new Dictionary<string, object> { { "key", localMasterKey } });
                    encryption.RegisterSchema(
                        CollectionNamespace.FromFullName("medical.patients"),
                        BsonDocument.Parse("{ bsonType : 'object', properties : { ssn : { encrypt : { bsonType : 'string', algorithm : 'AEAD_AES_256_CBC_HMAC_SHA_512-Random' } } } }"));
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
                .Authentication(auth => auth.UseCredential("admin", "user", "pencil"))
                .Tls(tls => tls.UseTls = true)
                .ConnectionPool(pool => pool.MaxConnectionPoolSize = 200)
                .ServerMonitoring(monitoring => monitoring.HeartbeatInterval = TimeSpan.FromSeconds(5))
                .OperationExecution(execution => execution.WriteConcern = WriteConcern.WMajority)
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
