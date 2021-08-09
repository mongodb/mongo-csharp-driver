/* Copyright 2019-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Libmongocrypt;
using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class EncryptionTests
    {
        #region static
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("db.coll");
        #endregion

        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        [Theory]
        [InlineData("mongocryptdBypassSpawn", typeof(int), typeof(ArgumentException))]
        [InlineData("mongocryptdBypassSpawn", typeof(string), typeof(ArgumentException))]
        [InlineData("mongocryptdBypassSpawn", typeof(bool), null)]

        [InlineData("mongocryptdSpawnArgs", typeof(Dictionary<string, string>), typeof(ArgumentException))]
        [InlineData("mongocryptdSpawnArgs", typeof(int), typeof(ArgumentException))]
        [InlineData("mongocryptdSpawnArgs", typeof(List<string>), null)]
        [InlineData("mongocryptdSpawnArgs", typeof(string[]), null)]
        [InlineData("mongocryptdSpawnArgs", typeof(string), null)]

        [InlineData("mongocryptdURI", typeof(object), typeof(ArgumentException))]
        [InlineData("mongocryptdURI", typeof(int), typeof(ArgumentException))]
        [InlineData("mongocryptdURI", typeof(List<string>), typeof(ArgumentException))]
        [InlineData("mongocryptdURI", typeof(string), null)]

        [InlineData("mongocryptdSpawnPath", typeof(object), typeof(ArgumentException))]
        [InlineData("mongocryptdSpawnPath", typeof(int), typeof(ArgumentException))]
        [InlineData("mongocryptdSpawnPath", typeof(string), null)]

        [InlineData("mongocryptdURI1", typeof(object), typeof(ArgumentException))]
        [InlineData("test", typeof(object), typeof(ArgumentException))]
        public void Constructor_of_auto_encryption_should_validate_extra_options(string extraOptionKey, Type extraOptionValueType, Type expectedException)
        {
            var extraOptions = new Dictionary<string, object>
            {
                { extraOptionKey, CreateInstance(extraOptionValueType) }
            };

            var exception = Record.Exception(() =>
                new AutoEncryptionOptions(
                    __keyVaultCollectionNamespace,
                    new Dictionary<string, IReadOnlyDictionary<string, object>>(),
                    extraOptions: extraOptions));
            if (expectedException != null)
            {
                exception.GetType().Should().Be(expectedException);
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(typeof(int), typeof(ArgumentException))]
        [InlineData(typeof(int[]), typeof(ArgumentException))]
        [InlineData(typeof(string[]), typeof(ArgumentException))]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(byte[]), null)]
        public void Constructor_of_encryption_options_should_validate_kms_providers(Type valueType, Type expectedExceptionType)
        {
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var options = valueType != null ? CreateInstance(valueType) : null;
            kmsProviders.Add(
                "testKey",
                new Dictionary<string, object>
                {
                    { "nestedTestKey", options }
                });

            testCase(() =>
                new AutoEncryptionOptions(
                    keyVaultNamespace: __keyVaultCollectionNamespace,
                    kmsProviders: kmsProviders));

            testCase(() =>
                new ClientEncryptionOptions(
                    Mock.Of<IMongoClient>(),
                    kmsProviders: kmsProviders,
                    keyVaultNamespace: __keyVaultCollectionNamespace));

            void testCase(Action testCode)
            {
                var testCaseException = Record.Exception(testCode);
                if (expectedExceptionType != null)
                {
                    testCaseException.GetType().Should().Be(expectedExceptionType);
                }
                else
                {
                    testCaseException.Should().BeNull();
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CryptClient_should_be_initialized([Values(false, true)] bool withAutoEncryption)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var client1 = GetClient(withAutoEncryption))
            using (var client2 = GetClient(withAutoEncryption))
            {
                var libMongoCryptController1 = ((MongoClient)client1.Wrapped).LibMongoCryptController;
                var libMongoCryptController2 = ((MongoClient)client2.Wrapped).LibMongoCryptController;
                if (withAutoEncryption)
                {
                    var cryptClient1 = libMongoCryptController1._cryptClient();
                    var cryptClient2 = libMongoCryptController2._cryptClient();
                    var areTheSame = object.ReferenceEquals(cryptClient1, cryptClient2);
                    areTheSame.Should().BeTrue();
                }
                else
                {
                    libMongoCryptController1.Should().BeNull();
                    libMongoCryptController2.Should().BeNull();
                }

                var kmsProviders = GetKmsProviders();
                var clientEncryption = GetClientEncryption((MongoClient)client1.Wrapped, kmsProviders);
                clientEncryption._libMongoCryptController()._cryptClient().Should().NotBeNull();
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Mongocryptd_should_be_initialized_only_for_auto_encryption([Values(false, true)] bool withAutoEncryption)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var disposableClient = GetClient(withAutoEncryption))
            {
                var kmsProviders = GetKmsProviders();
                var client = (MongoClient)disposableClient.Wrapped;
                var clientEncryption = GetClientEncryption(client, kmsProviders);
                if (withAutoEncryption)
                {
                    client.LibMongoCryptController.Should().NotBeNull();
                    var clientController = client.LibMongoCryptController;
                    clientController._mongocryptdClient().Should().NotBeNull();

                    var clientEncryptionController = clientEncryption._libMongoCryptController();
                    clientEncryptionController.Should().NotBeNull();
                }
                else
                {
                    client.LibMongoCryptController.Should().BeNull();

                    var clientEncryptionController = clientEncryption._libMongoCryptController();
                    clientEncryptionController.Should().NotBeNull();
                }
            }
        }

        // private methods
        private object CreateInstance(Type type)
        {
            if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), 0);
            }
            else
            {
                return type == typeof(string) ? string.Empty : Activator.CreateInstance(type);
            }
        }

        private DisposableMongoClient GetClient(bool withAutoEncryption = false, Dictionary<string, object> extraOptions = null)
        {
            var mongoClientSettings = new MongoClientSettings();

            if (withAutoEncryption)
            {
                if (extraOptions == null)
                {
                    extraOptions = new Dictionary<string, object>()
                    {
                        { "mongocryptdSpawnPath", Environment.GetEnvironmentVariable("MONGODB_BINARIES") ?? string.Empty }
                    };
                }

                var kmsProviders = GetKmsProviders();
                var autoEncryptionOptions = new AutoEncryptionOptions(
                    keyVaultNamespace: __keyVaultCollectionNamespace,
                    kmsProviders: kmsProviders,
                    extraOptions: extraOptions);
                mongoClientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            }

            return new DisposableMongoClient(new MongoClient(mongoClientSettings));
        }

        private ClientEncryption GetClientEncryption(
            MongoClient mongoClient,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders)
        {
            var clientEncryptionOptions = new ClientEncryptionOptions(
                keyVaultClient: mongoClient,
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: kmsProviders);

            return new ClientEncryption(clientEncryptionOptions);
        }

        private Dictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders()
        {
            var localOptions = new Dictionary<string, object>
            {
                { "key", new BsonBinaryData(Convert.FromBase64String(LocalMasterKey)).Bytes }
            };
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "local", localOptions }
            };
            return kmsProviders;
        }
    }

    internal static class ClientEncryptionReflector
    {
        public static ExplicitEncryptionLibMongoCryptController _libMongoCryptController(this ClientEncryption clientEncryption)
        {
            return (ExplicitEncryptionLibMongoCryptController)Reflector.GetFieldValue(clientEncryption, nameof(_libMongoCryptController));
        }
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
        public static IMongoClient _mongocryptdClient(this AutoEncryptionLibMongoCryptController libMongoCryptController)
        {
            return (IMongoClient)Reflector.GetFieldValue(libMongoCryptController, nameof(_mongocryptdClient));
        }
    }
}
