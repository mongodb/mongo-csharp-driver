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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Libmongocrypt;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests
{
    public class ClientEncryptionProseTests
    {
        #region static
        private static readonly CollectionNamespace __collCollectionNamespace = CollectionNamespace.FromFullName("db.coll");
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("admin.datakeys");
        #endregion

        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";
        private const string SchemaMap =
            @"{
                ""db.coll"": {
                    ""bsonType"": ""object"",
                    ""properties"": {
                        ""encrypted_placeholder"": {
                            ""encrypt"": {
                                ""keyId"": ""/placeholder"",
                                ""bsonType"": ""string"",
                                ""algorithm"": ""AEAD_AES_256_CBC_HMAC_SHA_512-Random""
                              }
                          }
                      }
                  }
            }";

        private readonly ICluster _cluster;

        public ClientEncryptionProseTests()
        {
            _cluster = CoreTestConfiguration.Cluster;
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void BsonSizeLimitAndBatchSizeSplittingTest(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "insert");
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(kmsProviderFilter: "local", eventCapturer: eventCapturer))
            {
                var collLimitSchema = JsonFileReader.Instance.Documents["limits.limits-schema.json"];
                CreateCollection(client, __collCollectionNamespace, new BsonDocument("$jsonSchema", collLimitSchema));
                var datakeysLimitsKey = JsonFileReader.Instance.Documents["limits.limits-key.json"];
                var keyVaultCollection = GetCollection(client, __keyVaultCollectionNamespace);
                Insert(keyVaultCollection, async, datakeysLimitsKey);

                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);

                var exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        new BsonDocument
                        {
                            { "_id", "over_2mib_under_16mib" },
                            { "unencrypted", new string('a', 2097152) }
                        }));
                exception.Should().BeNull();
                eventCapturer.Clear();

                var limitsDoc = JsonFileReader.Instance.Documents["limits.limits-doc.json"];
                limitsDoc.AddRange(
                    new BsonDocument
                    {
                        {"_id", "encryption_exceeds_2mib"},
                        {"unencrypted", new string('a', 2097152 - 2000)}
                    });
                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        limitsDoc));
                exception.Should().BeNull();
                eventCapturer.Clear();

                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        new BsonDocument
                        {
                            { "_id", "over_2mib_1" },
                            { "unencrypted", new string('a', 2097152) }
                        },
                        new BsonDocument
                        {
                            { "_id", "over_2mib_2" },
                            { "unencrypted", new string('a', 2097152) }
                        }));
                exception.Should().BeNull();
                eventCapturer.Count.Should().Be(2);
                eventCapturer.Clear();

                var limitsDoc1 = JsonFileReader.Instance.Documents["limits.limits-doc.json"];
                limitsDoc1.AddRange(
                    new BsonDocument
                    {
                        { "_id", "encryption_exceeds_2mib_1" },
                        { "unencrypted", new string('a', 2097152 - 2000) }
                    });
                var limitsDoc2 = JsonFileReader.Instance.Documents["limits.limits-doc.json"];
                limitsDoc2.AddRange(
                    new BsonDocument
                    {
                        { "_id", "encryption_exceeds_2mib_2" },
                        { "unencrypted", new string('a', 2097152 - 2000) }
                    });

                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        limitsDoc1,
                        limitsDoc2));
                exception.Should().BeNull();
                eventCapturer.Count.Should().Be(2);
                eventCapturer.Clear();

                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        new BsonDocument
                        {
                            { "_id", "under_16mib" },
                            { "unencrypted", new string('a', 16777216 - 2000) }
                        }));
                exception.Should().BeNull();
                eventCapturer.Clear();

                limitsDoc = JsonFileReader.Instance.Documents["limits.limits-doc.json"];
                limitsDoc.AddRange(
                    new BsonDocument
                    {
                        {"_id", "encryption_exceeds_16mib"},
                        {"unencrypted", new string('a', 16777216 - 2000)}
                    });
                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        limitsDoc));
                exception.Should().NotBeNull();
                eventCapturer.Clear();

                // additional not spec tests
                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        new BsonDocument
                        {
                            { "_id", "advanced_over_2mib_1" },
                            { "unencrypted", new string('a', 2097152) }
                        },
                        new BsonDocument
                        {
                            { "_id", "advanced_over_2mib_2" },
                            { "unencrypted", new string('a', 2097152) }
                        },
                        new BsonDocument
                        {
                            { "_id", "advanced_over_2mib_3" },
                            { "unencrypted", new string('a', 2097152) }
                        }));
                exception.Should().BeNull();
                eventCapturer.Count.Should().Be(3);
                eventCapturer.Clear();

                exception = Record.Exception(
                    () => Insert(
                        coll,
                        async,
                        new BsonDocument
                        {
                            { "_id", "small_1" },
                            { "unencrypted", "a" }
                        },
                        new BsonDocument
                        {
                            { "_id", "small_2" },
                            { "unencrypted", "a" }
                        },
                        new BsonDocument
                        {
                            { "_id", "small_3" },
                            { "unencrypted", "a" }
                        }));
                exception.Should().BeNull();
                eventCapturer.Count.Should().Be(1);
                eventCapturer.Clear();
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CorpusTest(
            [Values(false, true)] bool useLocalSchema,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var corpusSchema = JsonFileReader.Instance.Documents["corpus.corpus-schema.json"];
            var schemaMap = useLocalSchema ? new BsonDocument("db.coll", corpusSchema) : null;
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(schemaMap))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted.Wrapped as MongoClient))
            {
                CreateCollection(client, __collCollectionNamespace, new BsonDocument("$jsonSchema", corpusSchema));

                var corpusKeyLocal = JsonFileReader.Instance.Documents["corpus.corpus-key-local.json"];
                var corpusKeyAws = JsonFileReader.Instance.Documents["corpus.corpus-key-aws.json"];
                var keyVaultCollection = GetCollection(client, __keyVaultCollectionNamespace);
                Insert(keyVaultCollection, async, corpusKeyLocal, corpusKeyAws);

                var corpus = JsonFileReader.Instance.Documents["corpus.corpus.json"];
                var corpusCopied = new BsonDocument
                {
                    corpus.GetElement("_id"),
                    corpus.GetElement("altname_aws"),
                    corpus.GetElement("altname_local")
                };

                foreach (var corpusElement in corpus.Elements.Where(c => c.Value.IsBsonDocument))
                {
                    var corpusValue = corpusElement.Value.DeepClone();
                    var kms = corpusValue["kms"].AsString;
                    var abbreviatedAlgorithmName = corpusValue["algo"].AsString;
                    var identifier = corpusValue["identifier"].AsString;
                    var allowed = corpusValue["allowed"].ToBoolean();
                    var value = corpusValue["value"];
                    var method = corpusValue["method"].AsString;
                    switch (method)
                    {
                        case "auto":
                            corpusCopied.Add(corpusElement);
                            continue;
                        case "explicit":
                            {
                                var encryptionOptions = CreateEncryptOptions(abbreviatedAlgorithmName, identifier, kms);
                                BsonBinaryData encrypted = null;
                                var exception = Record.Exception(() =>
                                {
                                    encrypted = ExplicitEncrypt(
                                        clientEncryption,
                                        encryptionOptions,
                                        value,
                                        async);
                                });
                                if (allowed)
                                {
                                    exception.Should().BeNull();
                                    encrypted.Should().NotBeNull();
                                    corpusValue["value"] = encrypted;
                                }
                                else
                                {
                                    exception.Should().NotBeNull();
                                }
                                corpusCopied.Add(new BsonElement(corpusElement.Name, corpusValue));
                            }
                            break;
                        default:
                            throw new ArgumentException($"Unsupported method name {method}.", nameof(method));
                    }
                }

                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                Insert(coll, async, corpusCopied);

                var corpusDecrypted = Find(coll, new BsonDocument(), async).Single();
                corpusDecrypted.Should().Be(corpus);

                var corpusEncryptedExpected = JsonFileReader.Instance.Documents["corpus.corpus-encrypted.json"];
                coll = GetCollection(client, __collCollectionNamespace);
                var corpusEncryptedActual = Find(coll, new BsonDocument(), async).Single();
                foreach (var expectedElement in corpusEncryptedExpected.Elements.Where(c => c.Value.IsBsonDocument))
                {
                    var expectedElementValue = expectedElement.Value;
                    var expectedAlgorithm = ParseAlgorithm(expectedElementValue["algo"].AsString);
                    var expectedAllowed = expectedElementValue["allowed"].ToBoolean();
                    var expectedValue = expectedElementValue["value"];
                    var actualValue = corpusEncryptedActual.GetValue(expectedElement.Name)["value"];

                    switch (expectedAlgorithm)
                    {
                        case EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic:
                            actualValue.Should().Be(expectedValue);
                            break;
                        case EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random:
                            if (expectedAllowed)
                            {
                                actualValue.Should().NotBe(expectedValue);
                            }
                            break;
                        default:
                            throw new ArgumentException($"Unsupported expected algorithm {expectedAllowed}.", nameof(expectedAlgorithm));
                    }

                    if (expectedAllowed)
                    {
                        var actualDecryptedValue = ExplicitDecrypt(clientEncryption, actualValue.AsBsonBinaryData, async);
                        var expectedDecryptedValue = ExplicitDecrypt(clientEncryption, expectedValue.AsBsonBinaryData, async);
                        actualDecryptedValue.Should().Be(expectedDecryptedValue);
                    }
                    else
                    {
                        actualValue.Should().Be(expectedValue);
                    }
                }
            }

            EncryptOptions CreateEncryptOptions(string algorithm, string identifier, string kms)
            {
                Guid? keyId = null;
                string alternateName = null;
                if (identifier == "id")
                {
                    switch (kms)
                    {
                        case "local":
                            keyId = GuidConverter.FromBytes(Convert.FromBase64String("LOCALAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard);
                            break;
                        case "aws":
                            keyId = GuidConverter.FromBytes(Convert.FromBase64String("AWSAAAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard);
                            break;
                        default:
                            throw new ArgumentException($"Unsupported kms type {kms}.");
                    }
                }
                else if (identifier == "altname")
                {
                    alternateName = kms;
                }
                else
                {
                    throw new ArgumentException($"Unsupported identifier {identifier}.", nameof(identifier));
                }

                return new EncryptOptions(ParseAlgorithm(algorithm).ToString(), alternateName, keyId);
            }

            EncryptionAlgorithm ParseAlgorithm(string algorithm)
            {
                switch (algorithm)
                {
                    case "rand":
                        return EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random;
                    case "det":
                        return EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic;
                    default:
                        throw new ArgumentException($"Unsupported algorithm {algorithm}.");
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CreateDataKeyAndDoubleEncryptionTest(
            [Values("local", "aws")] string kmsProvider,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(BsonDocument.Parse(SchemaMap)))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted.Wrapped as MongoClient))
            {
                var dataKeyOptions = CreateDataKeyOptions(kmsProvider);
                var dataKey = CreateDataKey(clientEncryption, kmsProvider, dataKeyOptions, async);

                var keyVaultCollection = GetCollection(client, __keyVaultCollectionNamespace);
                var keyVaultDocument =
                    Find(
                        keyVaultCollection,
                        new BsonDocument("_id", new BsonBinaryData(dataKey, GuidRepresentation.Standard)),
                        async)
                    .Single();
                keyVaultDocument["masterKey"]["provider"].Should().Be(BsonValue.Create(kmsProvider));

                var encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKey);

                var encryptedValue = ExplicitEncrypt(
                    clientEncryption,
                    encryptOptions,
                    $"hello {kmsProvider}",
                    async);
                encryptedValue.SubType.Should().Be(BsonBinarySubType.Encrypted);

                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                Insert(
                    coll,
                    async,
                    new BsonDocument
                    {
                        {"_id", kmsProvider},
                        {"value", encryptedValue}
                    });

                var findResult = Find(coll, new BsonDocument("_id", kmsProvider), async).Single();
                findResult["value"].ToString().Should().Be($"hello {kmsProvider}");

                encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    alternateKeyName: $"{kmsProvider}_altname");
                var encryptedValueWithAlternateKeyName = ExplicitEncrypt(
                    clientEncryption,
                    encryptOptions,
                    $"hello {kmsProvider}",
                    async);
                encryptedValueWithAlternateKeyName.SubType.Should().Be(BsonBinarySubType.Encrypted);
                encryptedValueWithAlternateKeyName.Should().Be(encryptedValue);

                if (kmsProvider == "local") // the test description expects this assert only once for a local kms provider
                {
                    coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                    var exception = Record.Exception(() => Insert(coll, async, new BsonDocument("encrypted_placeholder", encryptedValue)));
                    exception.Should().BeOfType<MongoEncryptionException>();
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CustomEndpointTest([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var client = ConfigureClient())
            using (var clientEncryption = ConfigureClientEncryption(client.Wrapped as MongoClient))
            {
                var testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" }
                };
                TestCase(testCaseMasterKey);

                testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                    { "endpoint", "kms.us-east-1.amazonaws.com" }
                };
                TestCase(testCaseMasterKey);

                testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                    { "endpoint", "kms.us-east-1.amazonaws.com:443" }
                };
                TestCase(testCaseMasterKey);

                testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                    { "endpoint", "kms.us-east-1.amazonaws.com:12345" }
                };
                var exception = Record.Exception(() => TestCase(testCaseMasterKey));
                exception.InnerException.Should().BeAssignableTo<SocketException>();

                testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                    { "endpoint", "kms.us-east-2.amazonaws.com" }
                };
                exception = Record.Exception(() => TestCase(testCaseMasterKey));
                exception.Should().NotBeNull();
                exception.Message.Should().Contain("us-east-1");

                testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                    { "endpoint", "example.com" }
                };
                exception = Record.Exception(() => TestCase(testCaseMasterKey));
                exception.Should().NotBeNull();
                exception.Message.Should().Contain("parse error");

                // additional not spec tests
                testCaseMasterKey = new BsonDocument
                {
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                    { "endpoint", "$test$" }
                };
                exception = Record.Exception(() => TestCase(testCaseMasterKey));
                exception.Should().NotBeNull();
                exception.InnerException.Should().BeAssignableTo<SocketException>();

                void TestCase(BsonDocument masterKey)
                {
                    var dataKeyOptions = new DataKeyOptions(masterKey: masterKey);
                    var dataKey = CreateDataKey(clientEncryption, "aws", dataKeyOptions, async);

                    var encryptOptions = new EncryptOptions(
                        algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                        keyId: dataKey);
                    var value = "test";
                    var encrypted = ExplicitEncrypt(clientEncryption, encryptOptions, value, async);
                    var decrypted = ExplicitDecrypt(clientEncryption, encrypted, async);
                    decrypted.Should().Be(BsonValue.Create(value));
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void ExternalKeyVaultTest(
            [Values(false, true)] bool withExternalKeyVault,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var clientEncryptedSchema = new BsonDocument("db.coll", JsonFileReader.Instance.Documents["external.external-schema.json"]);
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(clientEncryptedSchema, withExternalKeyVault))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted.Wrapped as MongoClient))
            {
                var datakeys = GetCollection(client, __keyVaultCollectionNamespace);
                var externalKey = JsonFileReader.Instance.Documents["external.external-key.json"];
                Insert(datakeys, async, externalKey);

                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                var exception = Record.Exception(() => Insert(coll, async, new BsonDocument("encrypted", "test")));
                if (withExternalKeyVault)
                {
                    exception.InnerException.Should().BeOfType<MongoAuthenticationException>();
                }
                else
                {
                    exception.Should().BeNull();
                }

                var encryptionOptions = new EncryptOptions(
                    algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: GuidConverter.FromBytes(Convert.FromBase64String("LOCALAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard));
                exception = Record.Exception(() => ExplicitEncrypt(clientEncryption, encryptionOptions, "test", async));
                if (withExternalKeyVault)
                {
                    exception.InnerException.Should().BeOfType<MongoAuthenticationException>();
                }
                else
                {
                    exception.Should().BeNull();
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void ViewAreProhibitedTest([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var viewName = CollectionNamespace.FromFullName("db.view");
            using (var client = ConfigureClient(false))
            using (var clientEncrypted = ConfigureClientEncrypted(kmsProviderFilter: "local"))
            {
                DropView(viewName);
                client
                    .GetDatabase(viewName.DatabaseNamespace.DatabaseName)
                    .CreateView(
                        viewName.CollectionName,
                        __collCollectionNamespace.CollectionName,
                        new EmptyPipelineDefinition<BsonDocument>());

                var view = GetCollection(clientEncrypted, viewName);
                var exception = Record.Exception(
                    () => Insert(
                        view,
                        async,
                        documents: new BsonDocument("test", 1)));
                exception.Message.Should().Be("Encryption related exception: cannot auto encrypt a view.");
            }
        }

        // private methods
        private DisposableMongoClient ConfigureClient(bool clearCollections = true)
        {
            var client = CreateMongoClient();
            if (clearCollections)
            {
                var clientAdminDatabase = client.GetDatabase(__keyVaultCollectionNamespace.DatabaseNamespace.DatabaseName);
                clientAdminDatabase.DropCollection(__keyVaultCollectionNamespace.CollectionName);
                var clientDbDatabase = client.GetDatabase(__collCollectionNamespace.DatabaseNamespace.DatabaseName);
                clientDbDatabase.DropCollection(__collCollectionNamespace.CollectionName);
            }
            return client;
        }

        private DisposableMongoClient ConfigureClientEncrypted(
            BsonDocument schemaMap = null,
            bool withExternalKeyVault = false,
            string kmsProviderFilter = null,
            EventCapturer eventCapturer = null)
        {
            var kmsProviders = GetKmsProviders();

            var clientEncrypted =
                CreateMongoClient(
                    keyVaultNamespace: __keyVaultCollectionNamespace,
                    schemaMapDocument: schemaMap,
                    kmsProviders:
                        kmsProviderFilter == null
                            ? kmsProviders
                            : kmsProviders
                                .Where(c => c.Key == kmsProviderFilter)
                                .ToDictionary(key => key.Key, value => value.Value),
                    withExternalKeyVault: withExternalKeyVault,
                    clusterConfigurator:
                        eventCapturer != null
                            ? c => c.Subscribe(eventCapturer)
                            : (Action<ClusterBuilder>)null);
            return clientEncrypted;
        }

        private ClientEncryption ConfigureClientEncryption(MongoClient client)
        {
            var clientEncryptionOptions = new ClientEncryptionOptions(
                keyVaultClient: client.Settings.AutoEncryptionOptions?.KeyVaultClient ?? client,
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: GetKmsProviders());

            return new ClientEncryption(clientEncryptionOptions);
        }

        private void CreateCollection(IMongoClient client, CollectionNamespace collectionNamespace, BsonDocument validatorSchema)
        {
            client
                .GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName)
                .CreateCollection(
                    collectionNamespace.CollectionName,
                    new CreateCollectionOptions<BsonDocument>()
                    {
                        Validator = new BsonDocumentFilterDefinition<BsonDocument>(validatorSchema)
                    });
        }

        private Guid CreateDataKey(
            ClientEncryption clientEncryption,
            string kmsProvider,
            DataKeyOptions dataKeyOptions,
            bool async)
        {
            if (async)
            {
                return clientEncryption
                    .CreateDataKeyAsync(kmsProvider, dataKeyOptions, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return clientEncryption.CreateDataKey(kmsProvider, dataKeyOptions, CancellationToken.None);
            }
        }

        private DataKeyOptions CreateDataKeyOptions(string kmsProvider)
        {
            var alternateKeyNames = new[] { $"{kmsProvider}_altname" };
            switch (kmsProvider)
            {
                case "local":
                    return new DataKeyOptions(alternateKeyNames: alternateKeyNames);
                case "aws":
                    var masterKey = new BsonDocument
                    {
                        { "region", "us-east-1" },
                        { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" }
                    };
                    return new DataKeyOptions(
                        alternateKeyNames: alternateKeyNames,
                        masterKey: masterKey);
                default:
                    throw new ArgumentException($"Incorrect kms provider {kmsProvider}", nameof(kmsProvider));
            }
        }

        private DisposableMongoClient CreateMongoClient(
            CollectionNamespace keyVaultNamespace = null,
            BsonDocument schemaMapDocument = null,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = null,
            bool withExternalKeyVault = false,
            Action<ClusterBuilder> clusterConfigurator = null)
        {
            var mongoClientSettings = DriverTestConfiguration.GetClientSettings().Clone();
            mongoClientSettings.GuidRepresentation = GuidRepresentation.Unspecified;
            mongoClientSettings.ClusterConfigurator = clusterConfigurator;

            if (keyVaultNamespace != null || schemaMapDocument != null || kmsProviders != null || withExternalKeyVault)
            {
                var extraOptions = new Dictionary<string, object>()
                {
                    { "mongocryptdSpawnPath", Environment.GetEnvironmentVariable("MONGODB_BINARIES") ?? string.Empty }
                };

                var schemaMap = GetSchemaMapIfNotNull(schemaMapDocument);

                if (kmsProviders == null)
                {
                    kmsProviders = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>(new Dictionary<string, IReadOnlyDictionary<string, object>>());
                }

                var autoEncryptionOptions = new AutoEncryptionOptions(
                    keyVaultNamespace: keyVaultNamespace,
                    kmsProviders: kmsProviders,
                    schemaMap: schemaMap,
                    extraOptions: extraOptions);

                if (withExternalKeyVault)
                {
                    var externalKeyVaultClientSettings = DriverTestConfiguration.GetClientSettings().Clone();
                    externalKeyVaultClientSettings.Credential = MongoCredential.FromComponents(null, null, "fake-user", "fake-pwd");
                    var externalKeyVaultClient = new MongoClient(externalKeyVaultClientSettings);
                    autoEncryptionOptions = autoEncryptionOptions.With(keyVaultClient: externalKeyVaultClient);
                }
                mongoClientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            }

            return new DisposableMongoClient(new MongoClient(mongoClientSettings));
        }

        private void DropView(CollectionNamespace viewNamespace)
        {
            var operation = new DropCollectionOperation(viewNamespace, CoreTestConfiguration.MessageEncoderSettings);
            using (var session = CoreTestConfiguration.StartSession(_cluster))
            using (var binding = new WritableServerBinding(_cluster, session.Fork()))
            using (var bindingHandle = new ReadWriteBindingHandle(binding))
            {
                operation.Execute(bindingHandle, CancellationToken.None);
            }
        }

        private BsonValue ExplicitDecrypt(
            ClientEncryption clientEncryption,
            BsonBinaryData value,
            bool async)
        {
            BsonValue decryptedValue;
            if (async)
            {
                decryptedValue = clientEncryption
                    .DecryptAsync(
                        value,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                decryptedValue = clientEncryption.Decrypt(
                    value,
                    CancellationToken.None);
            }

            return decryptedValue;
        }

        private BsonBinaryData ExplicitEncrypt(
            ClientEncryption clientEncryption,
            EncryptOptions encryptOptions,
            BsonValue value,
            bool async)
        {
            BsonBinaryData encryptedValue;
            if (async)
            {
                encryptedValue = clientEncryption
                    .EncryptAsync(
                        value,
                        encryptOptions,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                encryptedValue = clientEncryption.Encrypt(
                    value,
                    encryptOptions,
                    CancellationToken.None);
            }

            return encryptedValue;
        }

        private IAsyncCursor<BsonDocument> Find(
            IMongoCollection<BsonDocument> collection,
            BsonDocument filter,
            bool async)
        {
            if (async)
            {
                return collection
                    .FindAsync(new BsonDocumentFilterDefinition<BsonDocument>(filter))
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return collection
                    .FindSync(new BsonDocumentFilterDefinition<BsonDocument>(filter));
            }
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoClient client, CollectionNamespace collectionNamespace)
        {
            var collectionSettings = new MongoCollectionSettings
            {
                ReadConcern = ReadConcern.Majority,
                WriteConcern = WriteConcern.WMajority
            };
            return client
                .GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName)
                .GetCollection<BsonDocument>(collectionNamespace.CollectionName, collectionSettings);
        }

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders()
        {
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();

            var awsRegion = Environment.GetEnvironmentVariable("FLE_AWS_REGION") ?? "us-east-1";
            var awsAccessKey = Environment.GetEnvironmentVariable("FLE_AWS_ACCESS_KEY_ID") ?? throw new Exception("The AWS_ACCESS_KEY_ID system variable should be configured on the machine.");
            var awsSecretAccessKey = Environment.GetEnvironmentVariable("FLE_AWS_SECRET_ACCESS_KEY") ?? throw new Exception("The AWS_SECRET_ACCESS_KEY system variable should be configured on the machine.");
            var kmsOptions = new Dictionary<string, object>
            {
                { "region", awsRegion },
                { "accessKeyId", awsAccessKey },
                { "secretAccessKey", awsSecretAccessKey }
            };
            kmsProviders.Add("aws", kmsOptions);

            var localOptions = new Dictionary<string, object>
            {
                { "key", new BsonBinaryData(Convert.FromBase64String(LocalMasterKey)).Bytes }
            };
            kmsProviders.Add("local", localOptions);

            return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>(kmsProviders);
        }

        private Dictionary<string, BsonDocument> GetSchemaMapIfNotNull(BsonDocument schemaMapDocument)
        {
            Dictionary<string, BsonDocument> schemaMap = null;
            if (schemaMapDocument != null)
            {
                var element = schemaMapDocument.Single();
                schemaMap = new Dictionary<string, BsonDocument>
                    {
                        { element.Name, element.Value.AsBsonDocument }
                    };
            }
            return schemaMap;
        }

        private void Insert(
            IMongoCollection<BsonDocument> collection,
            bool async,
            params BsonDocument[] documents)
        {
            if (async)
            {
                collection
                    .InsertManyAsync(documents)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                collection.InsertMany(documents);
            }
        }

        public class JsonFileReader : EmbeddedResourceJsonFileReader
        {
            #region static
            // private static fields
            private static readonly string[] __ignoreKeyNames =
            {
                "dbPointer" // not supported
            };
            private static readonly Lazy<JsonFileReader> __instance = new Lazy<JsonFileReader>(() => new JsonFileReader(), isThreadSafe: true);

            // public static properties
            public static JsonFileReader Instance => __instance.Value;
            #endregion

            private readonly IReadOnlyDictionary<string, BsonDocument> _documents;

            public JsonFileReader()
            {
                _documents = new ReadOnlyDictionary<string, BsonDocument>(ReadDocuments());
            }

            protected override string[] PathPrefixes => new[]
            {
                "MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests.corpus.",
                "MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests.external.",
                "MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests.limits."
            };

            public IReadOnlyDictionary<string, BsonDocument> Documents
            {
                get
                {
                    return _documents.ToDictionary(k => k.Key, v => v.Value.DeepClone().AsBsonDocument);
                }
            }

            // private methods
            private IDictionary<string, BsonDocument> ReadDocuments()
            {
                var documents = ReadJsonDocuments();
                return new Dictionary<string, BsonDocument>(
                    documents.ToDictionary(
                        key =>
                        {
                            var path = key["_path"].ToString();
                            var testTitle = "MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests";
                            var startIndex = path.IndexOf(testTitle, StringComparison.Ordinal);
                            if (startIndex != -1)
                            {
                                return path.Substring(startIndex + testTitle.Length + 1);
                            }
                            else
                            {
                                throw new ArgumentException($"Unexpected test file: {path}.");
                            }
                        },
                        value =>
                        {
                            RemoveIgnoredElements(value);
                            return value;
                        }));
            }

            private void RemoveIgnoredElements(BsonDocument document)
            {
                document.Remove("_path");
                var ignoredElements = document
                    .Where(c => __ignoreKeyNames.Any(i => c.Name.Contains(i)))
                    .ToList();
                foreach (var ignored in ignoredElements.Where(c => c.Value.IsBsonDocument))
                {
                    document.RemoveElement(ignored);
                }
            }
        }
    }
}
