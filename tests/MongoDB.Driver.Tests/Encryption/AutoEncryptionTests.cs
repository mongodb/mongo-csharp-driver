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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.Specifications.client_side_encryption;
using MongoDB.Libmongocrypt;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Encryption
{
    [Trait("Category", "CSFLE")]
    public class AutoEncryptionTests : LoggableTestClass
    {
        #region static
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("db.keyvault");
        private static readonly CollectionNamespace __collectionNamespace = CollectionNamespace.FromFullName("db.coll");
        #endregion

        // public constructors
        public AutoEncryptionTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void CryptClient_should_be_initialized([Values(false, true)] bool withAutoEncryption)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var client = GetClient(withAutoEncryption))
            {
                var libMongoCryptController = ((MongoClient)client.Wrapped).LibMongoCryptController;
                if (withAutoEncryption)
                {
                    var cryptClient = libMongoCryptController._cryptClient();
                    cryptClient.Should().NotBeNull();
                }
                else
                {
                    libMongoCryptController.Should().BeNull();
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Mongocryptd_should_be_initialized_when_auto_encryption([Values(false, true)] bool withAutoEncryption, [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var disposableClient = GetClient(
                withAutoEncryption,
                new Dictionary<string, object> { { "cryptSharedLibPath", "non_existing_path_to_use_mongocryptd" } }))
            {
                var client = (MongoClient)disposableClient.Wrapped;
                if (withAutoEncryption)
                {
                    client.LibMongoCryptController.Should().NotBeNull();
                    var clientController = client.LibMongoCryptController;
                    var mongocryptdClient = clientController._mongocryptdClient();
                    mongocryptdClient.Should().NotBeNull();
                    mongocryptdClient.IsValueCreated.Should().BeFalse(); // initialization should be deferred to the first usage

                    var coll = client.GetDatabase(__collectionNamespace.DatabaseNamespace.DatabaseName).GetCollection<BsonDocument>(__collectionNamespace.CollectionName);

                    if (async)
                    {
                        await coll.InsertOneAsync(new BsonDocument());
                    }
                    else
                    {
                        coll.InsertOne(new BsonDocument());
                    }

                    mongocryptdClient.IsValueCreated.Should().BeTrue();
                }
                else
                {
                    client.LibMongoCryptController.Should().BeNull();
                }
            }
        }

        [Fact]
        public void Shared_library_should_be_loaded_when_CRYPT_SHARED_LIB_PATH_is_set()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequireEnvironment.Check().EnvironmentVariable("CRYPT_SHARED_LIB_PATH", isDefined: true, allowEmpty: false);

            Ensure.That(File.Exists(Environment.GetEnvironmentVariable("CRYPT_SHARED_LIB_PATH")), "CRYPT_SHARED_LIB_PATH should exist.");

            using (var client = GetClient(withAutoEncryption: true))
            {
                var libMongoCryptController = ((MongoClient)client.Wrapped).LibMongoCryptController;
                var cryptClient = libMongoCryptController._cryptClient();
                cryptClient.CryptSharedLibraryVersion.Should().NotBeNull();
            }
        }

        private DisposableMongoClient GetClient(bool withAutoEncryption = false, Dictionary<string, object> extraOptions = null)
        {
            var mongoClientSettings = DriverTestConfiguration.GetClientSettings();
            var configurator = mongoClientSettings.ClusterConfigurator;  // ensure client is unique
            mongoClientSettings.ClusterConfigurator = b => { configurator?.Invoke(b); };

            if (withAutoEncryption)
            {
                extraOptions = extraOptions ?? new Dictionary<string, object>();

                EncryptionTestHelper.ConfigureDefaultExtraOptions(extraOptions);

                var kmsProviders = GetKmsProviders();
                var autoEncryptionOptions = new AutoEncryptionOptions(
                    keyVaultNamespace: __keyVaultCollectionNamespace,
                    kmsProviders: kmsProviders,
                    extraOptions: extraOptions,
                    schemaMap: new Dictionary<string, BsonDocument> { { __collectionNamespace.ToString(), new BsonDocument() } });
                mongoClientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            }

            return new DisposableMongoClient(new MongoClient(mongoClientSettings), CreateLogger<DisposableMongoClient>());
        }

        // private methods
        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders() => EncryptionTestHelper.GetKmsProviders(filter: "local");
    }

    internal static class LibMongoCryptControllerBaseReflector
    {
        public static CryptClient _cryptClient(this LibMongoCryptControllerBase libMongoCryptController)
        {
            return (CryptClient)Reflector.GetFieldValue(libMongoCryptController, nameof(_cryptClient));
        }
    }

    internal static class AutoEncryptionLibMongoCryptControllerReflector
    {
        public static Lazy<IMongoClient> _mongocryptdClient(this AutoEncryptionLibMongoCryptController libMongoCryptController)
        {
            return (Lazy<IMongoClient>)Reflector.GetFieldValue(libMongoCryptController, nameof(_mongocryptdClient));
        }
    }
}
