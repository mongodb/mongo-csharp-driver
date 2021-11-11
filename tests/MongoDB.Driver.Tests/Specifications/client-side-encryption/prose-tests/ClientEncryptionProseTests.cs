﻿/* Copyright 2019-present MongoDB Inc.
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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Libmongocrypt;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests
{
    [Trait("Category", "CSFLE")]
    public class ClientEncryptionProseTests : LoggableTestClass
    {
        #region static
        private static readonly CollectionNamespace __collCollectionNamespace = CollectionNamespace.FromFullName("db.coll");
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("keyvault.datakeys");
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

        // public constructors
        public ClientEncryptionProseTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _cluster = CoreTestConfiguration.Cluster;
        }

        // public methods
        [SkippableTheory]
        [ParameterAttributeData]
        public void BsonSizeLimitAndBatchSizeSplittingTest(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var eventCapturer = CreateEventCapturer(commandNameFilter: "insert");
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
        public void BypassSpawningMongocryptdViaMongocryptdBypassSpawnTest(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var extraOptions = new Dictionary<string, object>
            {
                { "mongocryptdBypassSpawn", true },
                { "mongocryptdURI", "mongodb://localhost:27021/db?serverSelectionTimeoutMS=10000" },
                { "mongocryptdSpawnArgs", new [] { "--pidfilepath=bypass-spawning-mongocryptd.pid", "--port=27021" } },
            };
            var clientEncryptedSchema = new BsonDocument("db.coll", JsonFileReader.Instance.Documents["external.external-schema.json"]);
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(
                schemaMap: clientEncryptedSchema,
                kmsProviderFilter: "local",
                extraOptions: extraOptions))
            {
                var datakeys = GetCollection(client, __keyVaultCollectionNamespace);
                var externalKey = JsonFileReader.Instance.Documents["external.external-key.json"];
                Insert(datakeys, async, externalKey);

                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                var exception = Record.Exception(() => Insert(coll, async, new BsonDocument("encrypted", "test")));

                AssertInnerEncryptionException<TimeoutException>(exception, "A timeout occurred after 10000ms selecting a server");
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void BypassSpawningMongocryptdViaBypassAutoEncryptionTest(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var extraOptions = new Dictionary<string, object>
            {
                { "mongocryptdSpawnArgs", new [] { "--pidfilepath=bypass-spawning-mongocryptd.pid", "--port=27021" } },
            };
            using (var mongocryptdClient = new DisposableMongoClient(new MongoClient("mongodb://localhost:27021/?serverSelectionTimeoutMS=10000"), CreateLogger<DisposableMongoClient>()))
            using (var clientEncrypted = ConfigureClientEncrypted(
                kmsProviderFilter: "local",
                bypassAutoEncryption: true,
                extraOptions: extraOptions))
            {
                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                Insert(coll, async, new BsonDocument("unencrypted", "test"));

                var adminDatabase = mongocryptdClient.GetDatabase(DatabaseNamespace.Admin.DatabaseName);
                var legacyHelloCommand = new BsonDocument(OppressiveLanguageConstants.LegacyHelloCommandName, 1);
                var exception = Record.Exception(() => adminDatabase.RunCommand<BsonDocument>(legacyHelloCommand));

                exception.Should().BeOfType<TimeoutException>();
                exception.Message.Should().Contain("A timeout occurred after 10000ms selecting a server");
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CorpusTest(
            [Values(false, true)] bool useLocalSchema,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequirePlatform
                .Check()
                // it's required only for gcp, but the test design doesn't allow skipping only required steps
                .SkipWhen(SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20)
                .SkipWhen(SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20);

            // this needs only for kmip, but the test design doesn't allow skipping only required steps
            RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED", isDefined: true);

            var corpusSchema = JsonFileReader.Instance.Documents["corpus.corpus-schema.json"];
            var schemaMap = useLocalSchema ? new BsonDocument("db.coll", corpusSchema) : null;
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(schemaMap))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted))
            {
                CreateCollection(client, __collCollectionNamespace, new BsonDocument("$jsonSchema", corpusSchema));

                var keyVaultCollection = GetCollection(client, __keyVaultCollectionNamespace);
                Insert(
                    keyVaultCollection,
                    async,
                    JsonFileReader.Instance.Documents["corpus.corpus-key-local.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-aws.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-azure.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-gcp.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-kmip.json"]);

                var corpus = JsonFileReader.Instance.Documents["corpus.corpus.json"];
                var corpusCopied = new BsonDocument
                {
                    corpus.GetElement("_id"),
                    corpus.GetElement("altname_aws"),
                    corpus.GetElement("altname_local"),
                    corpus.GetElement("altname_azure"),
                    corpus.GetElement("altname_gcp"),
                    corpus.GetElement("altname_kmip")
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
                            throw new ArgumentException($"Unsupported expected algorithm {expectedAlgorithm}.", nameof(expectedAlgorithm));
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
                switch (identifier)
                {
                    case "id":
                        keyId = kms switch
                        {
                            "local" => GuidConverter.FromBytes(Convert.FromBase64String("LOCALAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard),
                            "aws" => GuidConverter.FromBytes(Convert.FromBase64String("AWSAAAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard),
                            "azure" => GuidConverter.FromBytes(Convert.FromBase64String("AZUREAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard),
                            "gcp" => GuidConverter.FromBytes(Convert.FromBase64String("GCPAAAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard),
                            "kmip" => GuidConverter.FromBytes(Convert.FromBase64String("KMIPAAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard),
                            _ => throw new ArgumentException($"Unsupported kms type {kms}."),
                        };
                        break;
                    case "altname":
                        alternateName = kms;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported identifier {identifier}.", nameof(identifier));
                }

                return new EncryptOptions(ParseAlgorithm(algorithm).ToString(), alternateName, keyId);
            }

            EncryptionAlgorithm ParseAlgorithm(string algorithm) => algorithm switch
            {
                "rand" => EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
                "det" => EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
                _ => throw new ArgumentException($"Unsupported algorithm {algorithm}."),
            };
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void CreateDataKeyAndDoubleEncryptionTest(
            [Values("local", "aws", "azure", "gcp", "kmip")] string kmsProvider,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequirePlatform
                .Check()
                .SkipWhen(() => kmsProvider == "gcp", SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20)
                .SkipWhen(() => kmsProvider == "gcp", SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20); // gcp is supported starting from netstandard2.1
            if (kmsProvider == "kmip")
            {
                RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED", isDefined: true);
            }

            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(BsonDocument.Parse(SchemaMap)))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted))
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
        // aws
        [InlineData("aws", null, null, null)]
        [InlineData("aws", "kms.us-east-1.amazonaws.com", null, null)]
        [InlineData("aws", "kms.us-east-1.amazonaws.com:443", null, null)]
        [InlineData("aws", "kms.us-east-1.amazonaws.com:12345", "$ConnectionRefused$", null)]
        [InlineData("aws", "kms.us-east-2.amazonaws.com", "us-east-1", null)]
        [InlineData("aws", "example.com", "parse error", null)]
        // additional not spec tests
        [InlineData("aws", "$test$", "Invalid endpoint, expected dot separator in host, but got: $test$", null)]
        // azure
        [InlineData("azure", "key-vault-csfle.vault.azure.net", null, "parse error")]
        // gcp
        [InlineData("gcp", "cloudkms.googleapis.com:443", null, "parse error")]
        [InlineData("gcp", "example.com:443", "Invalid KMS response", null)]
        // kmip
        [InlineData("kmip", null, null, "$HostNotFound$")]
        [InlineData("kmip", "localhost:5698", null, null)]
        [InlineData("kmip", "doesnotexist.local:5698", "$HostNotFound$", null)]
        public void CustomEndpointTest(
            string kmsType,
            string customEndpoint,
            string expectedExceptionInfoForValidEncryption,
            string expectedExceptionInfoForInvalidEncryption)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequirePlatform
                .Check()
                .SkipWhen(() => kmsType == "gcp", SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20)  // gcp is supported starting from netstandard2.1
                .SkipWhen(() => kmsType == "gcp", SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20);
            if (kmsType == "kmip")
            {
                RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED", isDefined: true);
            }

            using (var client = ConfigureClient())
            using (var clientEncryption = ConfigureClientEncryption(client, ValidKmsEndpointConfigurator))
            using (var clientEncryptionInvalid = ConfigureClientEncryption(client, InvalidKmsEndpointConfigurator))
            {
                var testCaseMasterKey = kmsType switch
                {
                    "aws" => new BsonDocument
                    {
                        { "region", "us-east-1" },
                        { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                        { "endpoint", customEndpoint, customEndpoint != null }
                    },
                    "azure" => new BsonDocument
                    {
                        { "keyVaultEndpoint", customEndpoint },
                        { "keyName", "key-name-csfle" }
                    },
                    "gcp" => new BsonDocument
                    {
                        { "projectId", "devprod-drivers" },
                        { "location", "global" },
                        { "keyRing", "key-ring-csfle" },
                        { "keyName", "key-name-csfle" },
                        { "endpoint", customEndpoint }
                    },
                    "kmip" => new BsonDocument
                    {
                        { "keyId", "1" },
                        { "endpoint", customEndpoint, customEndpoint != null }
                    },
                    _ => throw new Exception($"Unexpected kms type {kmsType}."),
                };
                foreach (var async in new[] { false, true })
                {
                    var exception = Record.Exception(() => TestCase(clientEncryption, testCaseMasterKey, async));
                    AssertResult(exception, expectedExceptionInfoForValidEncryption);
                    if (expectedExceptionInfoForInvalidEncryption != null)
                    {
                        exception = Record.Exception(() => CreateDataKeyTestCaseStep(clientEncryptionInvalid, testCaseMasterKey, async));
                        AssertResult(exception, expectedExceptionInfoForInvalidEncryption);
                    }
                }

            }

            void AssertResult(Exception ex, string expectedExceptionInfo)
            {
                if (expectedExceptionInfo != null)
                {
                    var innerException = ex.Should().BeOfType<MongoEncryptionException>().Subject.InnerException;

                    if (expectedExceptionInfo.StartsWith("$") &&
                        expectedExceptionInfo.EndsWith("$") &&
                        Enum.TryParse<SocketError>(expectedExceptionInfo.Trim(new char[] { '$' }), out var socketError))
                    {
                        var e = innerException.Should().BeAssignableTo<SocketException>().Subject;// kmip triggers driver side exception
                        e.SocketErrorCode.Should().Be(socketError); // the error message is platform dependent
                    }
                    else
                    {
                        var e = innerException.Should().BeOfType<CryptException>().Subject;
                        e.Message.Should().Contain(expectedExceptionInfo.ToString());
                    }
                }
                else
                {
                    ex.Should().BeNull();
                }
            }

            Guid CreateDataKeyTestCaseStep(ClientEncryption testCaseClientEncription, BsonDocument masterKey, bool async)
            {
                var dataKeyOptions = new DataKeyOptions(masterKey: masterKey);
                return CreateDataKey(testCaseClientEncription, kmsType, dataKeyOptions, async);
            }

            void InvalidKmsEndpointConfigurator(string kt, Dictionary<string, object> ko)
            {
                switch (kt)
                {
                    case "azure":
                        ko.Add("identityPlatformEndpoint", "example.com:443");
                        break;
                    case "gcp":
                        ko.Add("endpoint", "example.com:443");
                        break;
                    case "kmip":
                        AddOrReplace(ko, "endpoint", "doesnotexist.local:5698");
                        break;
                }
            }

            void ValidKmsEndpointConfigurator(string kt, Dictionary<string, object> ko)
            {
                switch (kt)
                {
                    // these values are default, so set them just to show the difference with incorrect values
                    // NOTE: "aws" and "local" don't have a way to set endpoints here
                    case "azure":
                        ko.Add("identityPlatformEndpoint", "login.microsoftonline.com:443");
                        break;
                    case "gcp":
                        ko.Add("endpoint", "oauth2.googleapis.com:443");
                        break;
                    case "kmip":
                        // do nothing
                        break;
                }
            }

            void TestCase(ClientEncryption testCaseClientEncription, BsonDocument masterKey, bool async)
            {
                var dataKey = CreateDataKeyTestCaseStep(testCaseClientEncription, masterKey, async);
                var encryptOptions = new EncryptOptions(
                    algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKey);
                var value = "test";
                var encrypted = ExplicitEncrypt(testCaseClientEncription, encryptOptions, value, async);
                var decrypted = ExplicitDecrypt(testCaseClientEncription, encrypted, async);
                decrypted.Should().Be(BsonValue.Create(value));
            }
        }

        [SkippableTheory]
        [MemberData(nameof(DeadlockTest_MemberData))]
        public void DeadlockTest(
            string _,
            int maxPoolSize,
            bool bypassAutoEncryption,
            string keyVaultMongoClientKey,
            int expectedNumberOfClients,
            string[] clientEncryptedEventsExpectation,
            string[] clientKeyVaultEventsExpectation,
            bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var clientKeyVaultEventCapturer = CreateEventCapturer();
            using (var client_keyvault = CreateMongoClient(maxPoolSize: 1, writeConcern: WriteConcern.WMajority, readConcern: ReadConcern.Majority, eventCapturer: clientKeyVaultEventCapturer))
            using (var client_test = ConfigureClient(clearCollections: true, writeConcern: WriteConcern.WMajority, readConcern: ReadConcern.Majority))
            {
                var dataKeysCollection = GetCollection(client_test, __keyVaultCollectionNamespace);
                var externalKey = JsonFileReader.Instance.Documents["external.external-key.json"];
                Insert(dataKeysCollection, async, externalKey);

                var externalSchema = JsonFileReader.Instance.Documents["external.external-schema.json"];
                CreateCollection(client_test, __collCollectionNamespace, new BsonDocument("$jsonSchema", externalSchema));

                using (var client_encryption = ConfigureClientEncryption(client_test, kmsProviderFilter: "local"))
                {
                    var value = "string0";
                    var encryptionOptions = new EncryptOptions(
                        algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                        alternateKeyName: "local");
                    var ciphertext = ExplicitEncrypt(client_encryption, encryptionOptions, value, async);

                    var eventCapturer = CreateEventCapturer().Capture<ClusterOpeningEvent>();

                    using (var client_encrypted = ConfigureClientEncrypted(
                        kmsProviderFilter: "local",
                        maxPoolSize: maxPoolSize,
                        bypassAutoEncryption: bypassAutoEncryption,
                        eventCapturer: eventCapturer,
                        externalKeyVaultClient: GetKeyVaultMongoClientByKey()))
                    {
                        IMongoCollection<BsonDocument> collCollection;
                        if (client_encrypted.Settings.AutoEncryptionOptions.BypassAutoEncryption)
                        {
                            collCollection = GetCollection(client_test, __collCollectionNamespace);
                            Insert(
                                collCollection,
                                async,
                                new BsonDocument
                                {
                                    { "_id", 0 },
                                    { "encrypted", ciphertext }
                                });
                        }
                        else
                        {
                            collCollection = GetCollection(client_encrypted, __collCollectionNamespace);
                            Insert(
                                collCollection,
                                async,
                                new BsonDocument
                                {
                                    { "_id", 0 },
                                    { "encrypted", value }
                                });
                        }

                        collCollection = GetCollection(client_encrypted, __collCollectionNamespace);
                        var findResult = Find(collCollection, BsonDocument.Parse("{ _id : 0 }"), async).Single();
                        findResult.Should().Be($"{{ _id : 0, encrypted : '{value}' }}");
                        var events = eventCapturer.Events.ToList();
                        AssertEvents(events.OfType<CommandStartedEvent>(), clientEncryptedEventsExpectation);
                        if (clientKeyVaultEventsExpectation != null)
                        {
                            AssertEvents(clientKeyVaultEventCapturer.Events.OfType<CommandStartedEvent>(), clientKeyVaultEventsExpectation);
                        }

                        AssertNumberOfClients(events.OfType<ClusterOpeningEvent>());
                    }
                }

                IMongoClient GetKeyVaultMongoClientByKey()
                {
                    switch (keyVaultMongoClientKey)
                    {
                        case "client_keyvault":
                            return client_keyvault;
                        default:
                            return null;
                    }
                }
            }

            void AssertEvents(IEnumerable<CommandStartedEvent> events, string[] expectedEventsDetails)
            {
                for (int i = 0; i < expectedEventsDetails.Length; i++)
                {
                    var arguments = expectedEventsDetails[i].Split(';');
                    (string CommandName, string Database) expectedEventDetails = (arguments[0], arguments[1]);
                    var @event = events.ElementAt(i);
                    @event.DatabaseNamespace.DatabaseName.Should().Be(expectedEventDetails.Database);
                    @event.CommandName.Should().Be(expectedEventDetails.CommandName);
                }
                events.Count().Should().Be(expectedEventsDetails.Count());
            }

            void AssertNumberOfClients(IEnumerable<ClusterOpeningEvent> events)
            {
                events.Count().Should().Be(expectedNumberOfClients);
            }
        }

        public static IEnumerable<object[]> DeadlockTest_MemberData()
        {
            var testCases = new List<object[]>();
            testCases.AddRange(
                CasesWithAsync(
                    name: "case 1",
                    maxPoolSize: 1,
                    bypassAutoEncryption: false,
                    keyVaultMongoClient: null,
                    expectedNumberOfClients: 2,
                    clientEncryptedEventsExpectation:
                    new[]
                    {
                        "listCollections;db",
                        "find;keyvault",
                        "insert;db",
                        "find;db"
                    },
                    clientKeyVaultEventsExpectation: null));
            testCases.AddRange(
                CasesWithAsync(
                    name: "case 2",
                    maxPoolSize: 1,
                    bypassAutoEncryption: false,
                    keyVaultMongoClient: "client_keyvault",
                    expectedNumberOfClients: 2,
                    clientEncryptedEventsExpectation:
                    new[]
                    {
                        "listCollections;db",
                        "insert;db",
                        "find;db"
                    },
                    clientKeyVaultEventsExpectation:
                    new[]
                    {
                        "find;keyvault"
                    }));
            testCases.AddRange(
                CasesWithAsync(
                    name: "case 3",
                    maxPoolSize: 1,
                    bypassAutoEncryption: true,
                    keyVaultMongoClient: null,
                    expectedNumberOfClients: 2,
                    clientEncryptedEventsExpectation:
                    new[]
                    {
                        "find;db",
                        "find;keyvault"
                    },
                    clientKeyVaultEventsExpectation: null));
            testCases.AddRange(
                CasesWithAsync(
                    name: "case 4",
                    maxPoolSize: 1,
                    bypassAutoEncryption: true,
                    keyVaultMongoClient: "client_keyvault",
                    expectedNumberOfClients: 1,
                    clientEncryptedEventsExpectation:
                    new[]
                    {
                        "find;db",
                    },
                    clientKeyVaultEventsExpectation:
                    new[]
                    {
                        "find;keyvault"
                    }));

            // cases 5-8 use "MaxPoolSize: 0" which is not supported by the c# driver

            return testCases;

            IEnumerable<object[]> CasesWithAsync(
                string name,
                int maxPoolSize,
                bool bypassAutoEncryption,
                string keyVaultMongoClient,
                int expectedNumberOfClients,
                string[] clientEncryptedEventsExpectation,
                string[] clientKeyVaultEventsExpectation)
            {
                foreach (var async in new[] { true, false })
                {
                    yield return new object[] { name, maxPoolSize, bypassAutoEncryption, keyVaultMongoClient, expectedNumberOfClients, clientEncryptedEventsExpectation, clientKeyVaultEventsExpectation, async };
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

            IMongoClient externalKeyVaultClient = null;
            if (withExternalKeyVault)
            {
                var externalKeyVaultClientSettings = DriverTestConfiguration.GetClientSettings().Clone();
                externalKeyVaultClientSettings.Credential = MongoCredential.FromComponents(null, null, "fake-user", "fake-pwd");
                externalKeyVaultClient = new MongoClient(externalKeyVaultClientSettings);
            }

            var clientEncryptedSchema = new BsonDocument("db.coll", JsonFileReader.Instance.Documents["external.external-schema.json"]);
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(clientEncryptedSchema, externalKeyVaultClient: externalKeyVaultClient))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted))
            {
                var datakeys = GetCollection(client, __keyVaultCollectionNamespace);
                var externalKey = JsonFileReader.Instance.Documents["external.external-key.json"];
                Insert(datakeys, async, externalKey);

                var coll = GetCollection(clientEncrypted, __collCollectionNamespace);
                var exception = Record.Exception(() => Insert(coll, async, new BsonDocument("encrypted", "test")));
                if (withExternalKeyVault)
                {
                    AssertInnerEncryptionException<MongoAuthenticationException>(exception);
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
                    AssertInnerEncryptionException<MongoAuthenticationException>(exception);
                }
                else
                {
                    exception.Should().BeNull();
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void KmsTlsOptionsTest(
            [Values("aws", "azure", "gcp", "kmip")] string kmsProvider,
            [Values(CertificateType.TlsWithoutClientCert, CertificateType.TlsWithClientCert, CertificateType.Expired, CertificateType.InvalidHostName)] CertificateType certificateType,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequirePlatform
                .Check()
                .SkipWhen(() => kmsProvider == "gcp", SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20)  // gcp is supported starting from netstandard2.1
                .SkipWhen(() => kmsProvider == "gcp", SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20);
            RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED", isDefined: true);

            bool? isCertificateExpired = null, isInvalidHost = null; // will be assigned inside TestRelatedClientEncryptionOptionsConfigurator

            using (var clientEncrypted = ConfigureClientEncrypted())
            using (var clientEncryption = ConfigureClientEncryption(
                clientEncrypted,
                kmsProviderConfigurator: KmsProviderEndpointConfigurator,
                allowClientCertificateFunc: (kmsName) => kmsName == kmsProvider && certificateType == CertificateType.TlsWithClientCert,
                clientEncryptionOptionsConfigurator: TestRelatedClientEncryptionOptionsConfigurator,
                kmsProviderFilter: kmsProvider))
            {
                var dataKeyOptions = CreateDataKeyOptions(
                    kmsProvider: kmsProvider,
                    customMasterKey: kmsProvider switch
                    {
                        "aws" => new BsonDocument
                        {
                            { "region", "us-east-1" },
                            { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                            { "endpoint", GetMockedKmsEndpoint() }
                        },
                        "azure" => new BsonDocument
                        {
                            { "keyVaultEndpoint", "doesnotexist.local" },
                            { "keyName", "foo" }
                        },
                        "gcp" => new BsonDocument
                        {
                            { "projectId", "foo" },
                            { "location", "bar" },
                            { "keyRing", "baz" },
                            { "keyName", "foo" }
                        },
                        "kmip" => new BsonDocument(), // empty doc
                        _ => throw new Exception($"Unexpected kmsProvider {kmsProvider}."),
                    });

                var exception = Record.Exception(() => CreateDataKey(clientEncryption, kmsProvider, dataKeyOptions, async));
                AssertException(exception);
            }

            void AssertException(Exception exception)
            {
                var currentOperatingSystem = OperatingSystemHelper.CurrentOperatingSystem;
                switch (kmsProvider)
                {
                    case "aws":
                        {
                            switch (certificateType)
                            {
                                case CertificateType.TlsWithoutClientCert:
                                    AssertCertificate(isExpired: null, invalidHost: null);
                                    // Expect an error indicating TLS handshake failed.
                                    switch (currentOperatingSystem)
                                    {
                                        case OperatingSystemPlatform.Windows:
                                            AssertTlsWithoutClientCertOnWindows(exception);
                                            break;
                                        case OperatingSystemPlatform.Linux:
                                            AssertInnerEncryptionException(exception, Type.GetType("Interop+Crypto+OpenSslCryptographicException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "SSL Handshake failed with OpenSSL error - SSL_ERROR_SSL.");
                                            break;
                                        case OperatingSystemPlatform.MacOS:
                                            AssertInnerEncryptionException(exception, Type.GetType("Interop+AppleCrypto+SslException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "handshake failure");
                                            break;
                                        default: throw new Exception($"Unsupported OS {currentOperatingSystem}.");
                                    }
                                    break;
                                case CertificateType.TlsWithClientCert:
                                    AssertCertificate(isExpired: null, invalidHost: null);
                                    // Expect an error from libmongocrypt with a message containing the string: "parse
                                    // error". This implies TLS handshake succeeded.
                                    AssertInnerEncryptionException<CryptException>(exception, "Got parse error");
                                    break;
                                case CertificateType.Expired:
                                    AssertCertificate(isExpired: true, invalidHost: false);
                                    // Expect an error indicating TLS handshake failed due to an expired certificate.
                                    AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure");
                                    break;
                                case CertificateType.InvalidHostName:
                                    AssertCertificate(isExpired: false, invalidHost: true);
                                    // Expect an error indicating TLS handshake failed due to an invalid hostname.
                                    AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure");
                                    break;
                                default: throw new Exception($"Unexpected certificate type {certificateType} for {kmsProvider}.");
                            }
                        }
                        break;
                    case "azure":
                        switch (certificateType)
                        {
                            case CertificateType.TlsWithoutClientCert:
                                AssertCertificate(isExpired: null, invalidHost: null);
                                // Expect an error indicating TLS handshake failed.
                                switch (currentOperatingSystem)
                                {
                                    case OperatingSystemPlatform.Windows:
                                        AssertTlsWithoutClientCertOnWindows(exception);
                                        break;
                                    case OperatingSystemPlatform.Linux:
                                        AssertInnerEncryptionException(exception, Type.GetType("Interop+Crypto+OpenSslCryptographicException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "SSL Handshake failed with OpenSSL error - SSL_ERROR_SSL.");
                                        break;
                                    case OperatingSystemPlatform.MacOS:
                                        AssertInnerEncryptionException(exception, Type.GetType("Interop+AppleCrypto+SslException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "handshake failure");
                                        break;
                                    default: throw new Exception($"Unsupported OS {currentOperatingSystem}.");
                                }
                                break;
                            case CertificateType.TlsWithClientCert:
                                AssertCertificate(isExpired: null, invalidHost: null);
                                // Expect an error from libmongocrypt with a message containing the string: "HTTP
                                // status = 404". This implies TLS handshake succeeded.
                                AssertInnerEncryptionException<CryptException>(exception, "HTTP status=404");
                                break;
                            case CertificateType.Expired:
                                AssertCertificate(isExpired: true, invalidHost: false);
                                // Expect an error indicating TLS handshake failed due to an expired certificate.
                                AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure.");
                                break;
                            case CertificateType.InvalidHostName:
                                AssertCertificate(isExpired: false, invalidHost: true);
                                // Expect an error indicating TLS handshake failed due to an invalid hostname.
                                AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure.");
                                break;
                            default: throw new Exception($"Unexpected certificate type {certificateType} for {kmsProvider}.");
                        }
                        break;
                    case "gcp":
                        switch (certificateType)
                        {
                            case CertificateType.TlsWithoutClientCert:
                                AssertCertificate(isExpired: null, invalidHost: null);
                                // Expect an error indicating TLS handshake failed.
                                switch (currentOperatingSystem)
                                {
                                    case OperatingSystemPlatform.Windows:
                                        AssertTlsWithoutClientCertOnWindows(exception);
                                        break;
                                    case OperatingSystemPlatform.Linux:
                                        AssertInnerEncryptionException(exception, Type.GetType("Interop+Crypto+OpenSslCryptographicException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "SSL Handshake failed with OpenSSL error - SSL_ERROR_SSL.");
                                        break;
                                    case OperatingSystemPlatform.MacOS:
                                        AssertInnerEncryptionException(exception, Type.GetType("Interop+AppleCrypto+SslException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "handshake failure");
                                        break;
                                    default: throw new Exception($"Unsupported OS {currentOperatingSystem}.");
                                }
                                break;
                            case CertificateType.TlsWithClientCert:
                                AssertCertificate(isExpired: null, invalidHost: null);
                                // Expect an error from libmongocrypt with a message containing the string: "HTTP
                                // status = 404". This implies TLS handshake succeeded.
                                AssertInnerEncryptionException<CryptException>(exception, "HTTP status=404");
                                break;
                            case CertificateType.Expired:
                                AssertCertificate(isExpired: true, invalidHost: false);
                                // Expect an error indicating TLS handshake failed due to an expired certificate.
                                AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure.");
                                break;
                            case CertificateType.InvalidHostName:
                                AssertCertificate(isExpired: false, invalidHost: true);
                                // Expect an error indicating TLS handshake failed due to an invalid hostname.
                                AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure.");
                                break;
                            default: throw new Exception($"Unexpected certificate type {certificateType} for {kmsProvider}.");
                        }
                        break;
                    case "kmip":
                        switch (certificateType)
                        {
                            case CertificateType.TlsWithoutClientCert:
                                AssertCertificate(isExpired: null, invalidHost: null);
                                // Expect an error indicating TLS handshake failed.
                                switch (currentOperatingSystem)
                                {
                                    case OperatingSystemPlatform.Windows:
                                        AssertTlsWithoutClientCertOnWindows(exception);
                                        break;
                                    case OperatingSystemPlatform.Linux:
                                        AssertInnerEncryptionException(exception, Type.GetType("Interop+Crypto+OpenSslCryptographicException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "SSL Handshake failed with OpenSSL error - SSL_ERROR_SSL.");
                                        break;
                                    case OperatingSystemPlatform.MacOS:
                                        AssertInnerEncryptionException(exception, Type.GetType("Interop+AppleCrypto+SslException, System.Net.Security", throwOnError: true), "Authentication failed, see inner exception.", "handshake failure");
                                        break;
                                    default: throw new Exception($"Unsupported OS {currentOperatingSystem}.");
                                }
                                break;
                            case CertificateType.TlsWithClientCert:
                                AssertCertificate(isExpired: null, invalidHost: null);
                                exception.Should().BeNull();
                                break;
                            case CertificateType.Expired:
                                AssertCertificate(isExpired: true, invalidHost: false);
                                // Expect an error indicating TLS handshake failed due to an expired certificate.
                                AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure.");
                                break;
                            case CertificateType.InvalidHostName:
                                AssertCertificate(isExpired: false, invalidHost: true);
                                // Expect an error indicating TLS handshake failed due to an invalid hostname.
                                AssertInnerEncryptionException<AuthenticationException>(exception, "The remote certificate is invalid according to the validation procedure.");
                                break;
                            default: throw new Exception($"Unexpected certificate type {certificateType} for {kmsProvider}.");
                        }
                        break;
                    default: throw new Exception($"Not supported client certificate type {certificateType}.");
                }
            }

            void AssertCertificate(bool? isExpired, bool? invalidHost)
            {
                isCertificateExpired.Should().Be(isExpired);
                isInvalidHost.Should().Be(invalidHost);
            }

            void AssertTlsWithoutClientCertOnWindows(Exception exception)
            {
                try
                {
                    AssertInnerEncryptionException<System.ComponentModel.Win32Exception>(
                        exception,
#if NET472
                        "A call to SSPI failed, see inner exception.",
#else
                        "Authentication failed, see inner exception.",
#endif
                        "The message received was unexpected or badly formatted");
                }
                catch (Xunit.Sdk.XunitException) // assertation failed
                {
                    // Sometimes the mock server triggers SocketError.ConnectionReset (10054) on windows instead the expected exception.
                    // It looks like a test env issue, a similar behavior presents in other drivers, so we rely on the same check on different OSs
                    AssertInnerEncryptionException<SocketException>(
                        exception,
                        "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.",
                        "An existing connection was forcibly closed by the remote host");
                }
            }

            void KmsProviderEndpointConfigurator(string kmsProviderName, Dictionary<string, object> kmsOptions)
            {
                string endpoint = GetMockedKmsEndpoint();

                switch (kmsProviderName)
                {
                    case "local":
                        // not related to this test, do nothing
                        break;
                    case "aws":
                        // do nothing since aws cannot configure endpoint on kms provider level
                        break;
                    case "azure":
                        kmsOptions.Add("identityPlatformEndpoint", endpoint);
                        break;
                    case "gcp":
                        kmsOptions.Add("endpoint", endpoint);
                        break;
                    case "kmip":
                        AddOrReplace(kmsOptions, "endpoint", endpoint);
                        break;
                    default:
                        throw new Exception($"Unexpected kmsProvider {kmsProvider}.");
                }
            }

            string GetMockedKmsEndpoint() => certificateType switch
            {
                CertificateType.Expired => "127.0.0.1:8000",
                CertificateType.InvalidHostName => "127.0.0.1:8001",
                CertificateType.TlsWithClientCert or CertificateType.TlsWithoutClientCert => kmsProvider != "kmip" ? "127.0.0.1:8002" : "127.0.0.1:5698",
                _ => throw new Exception($"Not supported client certificate type {certificateType}."),
            };

            void TestRelatedClientEncryptionOptionsConfigurator(ClientEncryptionOptions clientEncryptionOptions) // needs only for asserting reasons
            {
                var tlsOptions = new Dictionary<string, SslSettings>((IDictionary<string, SslSettings>)clientEncryptionOptions.TlsOptions);
                if (!tlsOptions.ContainsKey(kmsProvider))
                {
                    tlsOptions.Add(kmsProvider, new SslSettings()); // configure it regardless global tls configuration to be able to validate certificate
                }

                tlsOptions[kmsProvider].ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((subject, certificate, chain, policyErrors) =>
                {
                    if (policyErrors == SslPolicyErrors.None)
                    {
                        // certificate is valid
                        return true;
                    }

                    var x509certificate2 = (X509Certificate2)certificate;
                    isCertificateExpired = x509certificate2.NotAfter < DateTime.UtcNow;
                    isInvalidHost = policyErrors == SslPolicyErrors.RemoteCertificateNameMismatch && certificate.Subject.Contains("wronghost.com");

                    Ensure.That(isCertificateExpired.GetValueOrDefault() || isInvalidHost.GetValueOrDefault(), $"Unexpected certificate issue detected for cert: {x509certificate2} and policyErrors: {policyErrors}.");

                    return false;
                });
                clientEncryptionOptions._tlsOptions(tlsOptions); // avoid validation on serverCertificateValidationCallback
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

        // NOTE: this test is not presented in the prose tests
        [SkippableTheory]
        [ParameterAttributeData]
        public void UnsupportedPlatformsTests(
            [Values("gcp")] string kmsProvider, // the rest kms providers are supported on all supported TFs
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var clientEncrypted = ConfigureClientEncrypted())
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted))
            {
                var dataKeyOptions = CreateDataKeyOptions(kmsProvider);
                var exception = Record.Exception(() => _ = CreateDataKey(clientEncryption, kmsProvider, dataKeyOptions, async));
                if (RequirePlatform.GetCurrentOperatingSystem() != SupportedOperatingSystem.Windows &&
                    RequirePlatform.GetCurrentTargetFramework() == SupportedTargetFramework.NetStandard20)
                {
                    AssertInnerEncryptionException<CryptException>(exception, "error constructing KMS message: Failed to create GCP oauth request signature: RSACryptoServiceProvider.ImportPkcs8PrivateKey is supported only on frameworks higher or equal to .netstandard2.1.");
                }
                else
                {
                    exception.Should().BeNull();
                }
            }
        }

        // private methods
        private void AddOrReplace<TValue>(IDictionary<string, TValue> dict, string key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        private void AssertInnerEncryptionException(Exception ex, Type exType, params string[] innerExceptionErrorMessage)
        {
            Exception e = ex.Should().BeOfType<MongoEncryptionException>().Subject.InnerException;
            foreach (var innerMessage in innerExceptionErrorMessage)
            {
                e.Message.Should().Contain(innerMessage);
                if (e.InnerException != null)
                {
                    e = e.InnerException;
                }
            }

            e.Should().BeOfType(exType);
        }

        private void AssertInnerEncryptionException<TMostInnerException>(Exception ex, params string[] innerExceptionErrorMessage) where TMostInnerException : Exception
        {
            AssertInnerEncryptionException(ex, typeof(TMostInnerException), innerExceptionErrorMessage);
        }

        private DisposableMongoClient ConfigureClient(
            bool clearCollections = true,
            int? maxPoolSize = null,
            WriteConcern writeConcern = null,
            ReadConcern readConcern = null)
        {
            var client = CreateMongoClient(maxPoolSize: maxPoolSize, writeConcern: writeConcern, readConcern: readConcern);
            if (clearCollections)
            {
                var clientKeyVaultDatabase = client.GetDatabase(__keyVaultCollectionNamespace.DatabaseNamespace.DatabaseName);
                clientKeyVaultDatabase.DropCollection(__keyVaultCollectionNamespace.CollectionName);
                var clientDbDatabase = client.GetDatabase(__collCollectionNamespace.DatabaseNamespace.DatabaseName);
                clientDbDatabase.DropCollection(__collCollectionNamespace.CollectionName);
            }
            return client;
        }

        private DisposableMongoClient ConfigureClientEncrypted(
            BsonDocument schemaMap = null,
            IMongoClient externalKeyVaultClient = null,
            string kmsProviderFilter = null,
            EventCapturer eventCapturer = null,
            Dictionary<string, object> extraOptions = null,
            bool bypassAutoEncryption = false,
            int? maxPoolSize = null,
            Action<AutoEncryptionOptions> autoEncryptionOptionsConfigurator = null)
        {
            var configuredSettings = ConfigureClientEncryptedSettings(
                schemaMap,
                externalKeyVaultClient,
                kmsProviderFilter,
                eventCapturer,
                extraOptions,
                bypassAutoEncryption,
                maxPoolSize);
            autoEncryptionOptionsConfigurator?.Invoke(configuredSettings.AutoEncryptionOptions);

            return DriverTestConfiguration.CreateDisposableClient(configuredSettings);
        }

        private MongoClientSettings ConfigureClientEncryptedSettings(
            BsonDocument schemaMap = null,
            IMongoClient externalKeyVaultClient = null,
            string kmsProviderFilter = null,
            EventCapturer eventCapturer = null,
            Dictionary<string, object> extraOptions = null,
            bool bypassAutoEncryption = false,
            int? maxPoolSize = null)
        {
            var kmsProviders = GetKmsProviders(kmsProviderFilter: kmsProviderFilter);
            var tlsOptions = EncryptionTestHelper.CreateTlsOptionsIfAllowed(
                kmsProviders,
                // only kmip currently requires tls configuration for ClientEncrypted
                allowClientCertificateFunc: kmsProviderName => kmsProviderName == "kmip");

            var clientEncryptedSettings =
                CreateMongoClientSettings(
                    keyVaultNamespace: __keyVaultCollectionNamespace,
                    schemaMapDocument: schemaMap,
                    kmsProviders: kmsProviders,
                    externalKeyVaultClient: externalKeyVaultClient,
                    eventCapturer: eventCapturer,
                    extraOptions: extraOptions,
                    bypassAutoEncryption: bypassAutoEncryption,
                    maxPoolSize: maxPoolSize);

            if (tlsOptions != null)
            {
                clientEncryptedSettings.AutoEncryptionOptions = clientEncryptedSettings.AutoEncryptionOptions.With(tlsOptions: tlsOptions);
            }

            return clientEncryptedSettings;
        }

        private ClientEncryption ConfigureClientEncryption(
            DisposableMongoClient client,
            Action<string, Dictionary<string, object>> kmsProviderConfigurator = null,
            Func<string, bool> allowClientCertificateFunc = null,
            Action<ClientEncryptionOptions> clientEncryptionOptionsConfigurator = null,
            string kmsProviderFilter = null)
        {
            var kmsProviders = GetKmsProviders(kmsProviderFilter, kmsProviderConfigurator);
            allowClientCertificateFunc = allowClientCertificateFunc ?? ((kmsProviderName) => kmsProviderName == "kmip"); // configure Tls for kmip by default
            var tlsOptions = EncryptionTestHelper.CreateTlsOptionsIfAllowed(kmsProviders, allowClientCertificateFunc);

            var clientEncryptionOptions = new ClientEncryptionOptions(
                keyVaultClient: client.Settings.AutoEncryptionOptions?.KeyVaultClient ?? client.Wrapped,
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: kmsProviders);

            if (tlsOptions != null)
            {
                clientEncryptionOptions = clientEncryptionOptions.With(tlsOptions: tlsOptions);
            }

            clientEncryptionOptionsConfigurator?.Invoke(clientEncryptionOptions);

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

        private DataKeyOptions CreateDataKeyOptions(string kmsProvider, BsonDocument customMasterKey = null)
        {
            var alternateKeyNames = new[] { $"{kmsProvider}_altname" };
            var masterKey = customMasterKey ??
                kmsProvider switch
                {
                    var kmsName when kmsName == "local" && customMasterKey == null => null,
                    "aws" => new BsonDocument
                    {
                        { "region", "us-east-1" },
                        { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" }
                    },
                    "azure" => new BsonDocument
                    {
                        { "keyName", "key-name-csfle" },
                        { "keyVaultEndpoint", "key-vault-csfle.vault.azure.net" }
                    },
                    "gcp" => new BsonDocument
                    {
                        { "projectId", "devprod-drivers" },
                        { "location", "global" },
                        { "keyRing", "key-ring-csfle" },
                        { "keyName", "key-name-csfle" }
                    },
                    "kmip" => new BsonDocument(),
                    _ => throw new ArgumentException($"Incorrect kms provider {kmsProvider} or provided custom master key {customMasterKey}.", nameof(kmsProvider)),
                };

            return new DataKeyOptions(
                alternateKeyNames: alternateKeyNames,
                masterKey: masterKey);
        }

        private DisposableMongoClient CreateMongoClient(
            CollectionNamespace keyVaultNamespace = null,
            BsonDocument schemaMapDocument = null,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = null,
            IMongoClient externalKeyVaultClient = null,
            EventCapturer eventCapturer = null,
            Dictionary<string, object> extraOptions = null,
            bool bypassAutoEncryption = false,
            int? maxPoolSize = null,
            WriteConcern writeConcern = null,
            ReadConcern readConcern = null)
        {
            var mongoClientSettings = CreateMongoClientSettings(
                keyVaultNamespace,
                schemaMapDocument,
                kmsProviders,
                externalKeyVaultClient,
                eventCapturer,
                extraOptions,
                bypassAutoEncryption,
                maxPoolSize,
                writeConcern,
                readConcern);

            return DriverTestConfiguration.CreateDisposableClient(mongoClientSettings, logger: CreateLogger<DisposableMongoClient>());
        }

        private MongoClientSettings CreateMongoClientSettings(
            CollectionNamespace keyVaultNamespace = null,
            BsonDocument schemaMapDocument = null,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = null,
            IMongoClient externalKeyVaultClient = null,
            EventCapturer eventCapturer = null,
            Dictionary<string, object> extraOptions = null,
            bool bypassAutoEncryption = false,
            int? maxPoolSize = null,
            WriteConcern writeConcern = null,
            ReadConcern readConcern = null)
        {
            var mongoClientSettings = DriverTestConfiguration.GetClientSettings().Clone();
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                mongoClientSettings.GuidRepresentation = GuidRepresentation.Unspecified;
            }
#pragma warning restore 618
            if (eventCapturer != null)
            {
                mongoClientSettings.ClusterConfigurator = builder => builder.Subscribe(eventCapturer);
            }
            else
            {
                var globalClusterConfiguratorAction = mongoClientSettings.ClusterConfigurator;
                mongoClientSettings.ClusterConfigurator = (b) => globalClusterConfiguratorAction(b); // we need to have a new instance of ClusterConfigurator
            }

            if (maxPoolSize.HasValue)
            {
                mongoClientSettings.MaxConnectionPoolSize = maxPoolSize.Value;
            }

            if (writeConcern != null)
            {
                mongoClientSettings.WriteConcern = writeConcern;
            }

            if (readConcern != null)
            {
                mongoClientSettings.ReadConcern = readConcern;
            }

            if (keyVaultNamespace != null || schemaMapDocument != null || kmsProviders != null || externalKeyVaultClient != null)
            {
                if (extraOptions == null)
                {
                    extraOptions = new Dictionary<string, object>();
                }

                EncryptionTestHelper.ConfigureDefaultExtraOptions(extraOptions);

                var schemaMap = GetSchemaMapIfNotNull(schemaMapDocument);

                if (kmsProviders == null)
                {
                    kmsProviders = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>(new Dictionary<string, IReadOnlyDictionary<string, object>>());
                }

                var autoEncryptionOptions = new AutoEncryptionOptions(
                    keyVaultNamespace: keyVaultNamespace,
                    kmsProviders: kmsProviders,
                    schemaMap: schemaMap,
                    extraOptions: extraOptions,
                    bypassAutoEncryption: bypassAutoEncryption);

                if (externalKeyVaultClient != null)
                {
                    autoEncryptionOptions = autoEncryptionOptions.With(keyVaultClient: Optional.Create(externalKeyVaultClient));
                }
                mongoClientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            }

            return mongoClientSettings;
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

        private string GetEnvironmentVariableOrDefaultOrThrowIfNothing(string variableName, string defaultValue = null) =>
            Environment.GetEnvironmentVariable(variableName) ??
            defaultValue ??
            throw new Exception($"{variableName} environment variable must be configured on the machine.");

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders(string kmsProviderFilter = null, Action<string, Dictionary<string, object>> kmsProviderConfigurator = null)
        {
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();

            var awsAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_ACCESS_KEY_ID");
            var awsSecretAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_SECRET_ACCESS_KEY");
            var awsKmsOptions = new Dictionary<string, object>
            {
                { "accessKeyId", awsAccessKey },
                { "secretAccessKey", awsSecretAccessKey }
            };
            kmsProviderConfigurator?.Invoke("aws", awsKmsOptions);
            kmsProviders.Add("aws", awsKmsOptions);

            var localOptions = new Dictionary<string, object>
            {
                { "key", new BsonBinaryData(Convert.FromBase64String(LocalMasterKey)).Bytes }
            };
            kmsProviderConfigurator?.Invoke("local", localOptions);
            kmsProviders.Add("local", localOptions);

            var azureTenantId = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_TENANT_ID");
            var azureClientId = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_CLIENT_ID");
            var azureClientSecret = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_CLIENT_SECRET");
            var azureKmsOptions = new Dictionary<string, object>
            {
                { "tenantId", azureTenantId },
                { "clientId", azureClientId },
                { "clientSecret", azureClientSecret }
            };
            kmsProviderConfigurator?.Invoke("azure", azureKmsOptions);
            kmsProviders.Add("azure", azureKmsOptions);

            var gcpEmail = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_GCP_EMAIL");
            var gcpPrivateKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_GCP_PRIVATE_KEY");
            var gcpKmsOptions = new Dictionary<string, object>
            {
                { "email", gcpEmail },
                { "privateKey", gcpPrivateKey }
            };
            kmsProviderConfigurator?.Invoke("gcp", gcpKmsOptions);
            kmsProviders.Add("gcp", gcpKmsOptions);

            var kmipOptions = new Dictionary<string, object>
            {
                { "endpoint", "localhost:5698" }
            };
            kmsProviderConfigurator?.Invoke("kmip", kmipOptions);
            kmsProviders.Add("kmip", kmipOptions);

            if (kmsProviderFilter != null)
            {
                kmsProviders = kmsProviders.Where(c => c.Key == kmsProviderFilter).ToDictionary(c => c.Key, c => c.Value);
            }

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

        private EventCapturer CreateEventCapturer(string commandNameFilter = null)
        {
            var defaultCommandsToNotCapture = new HashSet<string>
            {
                "hello",
                OppressiveLanguageConstants.LegacyHelloCommandName,
                "buildInfo",
                "getLastError",
                "authenticate",
                "saslStart",
                "saslContinue",
                "getnonce"
            };

            return
                new EventCapturer()
                .Capture<CommandStartedEvent>(
                    e =>
                        !defaultCommandsToNotCapture.Contains(e.CommandName) &&
                        (commandNameFilter == null || e.CommandName == commandNameFilter));
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

        // nested types
        public enum CertificateType
        {
            TlsWithClientCert,
            TlsWithoutClientCert,
            Expired,
            InvalidHostName
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

    public static class ClientEncryptionOptionsReflector
    {
        public static void _tlsOptions(this ClientEncryptionOptions obj, IReadOnlyDictionary<string, SslSettings> tlsOptions) => Reflector.SetFieldValue(obj, nameof(_tlsOptions), tlsOptions);
    }
}
