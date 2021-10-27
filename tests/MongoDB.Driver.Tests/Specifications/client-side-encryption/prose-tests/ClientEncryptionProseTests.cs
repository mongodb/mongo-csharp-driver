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
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Libmongocrypt;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests
{
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

                exception.Should().BeOfType<MongoEncryptionException>();
                exception.Message.Should().Contain("A timeout occurred after 10000ms selecting a server");
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

            var corpusSchema = JsonFileReader.Instance.Documents["corpus.corpus-schema.json"];
            var schemaMap = useLocalSchema ? new BsonDocument("db.coll", corpusSchema) : null;
            using (var client = ConfigureClient())
            using (var clientEncrypted = ConfigureClientEncrypted(schemaMap))
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted.Wrapped as MongoClient))
            {
                CreateCollection(client, __collCollectionNamespace, new BsonDocument("$jsonSchema", corpusSchema));

                var keyVaultCollection = GetCollection(client, __keyVaultCollectionNamespace);
                Insert(
                    keyVaultCollection,
                    async,
                    JsonFileReader.Instance.Documents["corpus.corpus-key-local.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-aws.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-azure.json"],
                    JsonFileReader.Instance.Documents["corpus.corpus-key-gcp.json"]);

                var corpus = JsonFileReader.Instance.Documents["corpus.corpus.json"];
                var corpusCopied = new BsonDocument
                {
                    corpus.GetElement("_id"),
                    corpus.GetElement("altname_aws"),
                    corpus.GetElement("altname_local"),
                    corpus.GetElement("altname_azure"),
                    corpus.GetElement("altname_gcp")
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
                        case "azure":
                            keyId = GuidConverter.FromBytes(Convert.FromBase64String("AZUREAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard);
                            break;
                        case "gcp":
                            keyId = GuidConverter.FromBytes(Convert.FromBase64String("GCPAAAAAAAAAAAAAAAAAAA=="), GuidRepresentation.Standard);
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
            [Values("local", "aws", "azure", "gcp")] string kmsProvider,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequirePlatform
                .Check()
                .SkipWhen(() => kmsProvider == "gcp", SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20)
                .SkipWhen(() => kmsProvider == "gcp", SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20); // gcp is supported starting from netstandard2.1

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
        // aws
        [InlineData("aws", null, null, null)]
        [InlineData("aws", "kms.us-east-1.amazonaws.com", null, null)]
        [InlineData("aws", "kms.us-east-1.amazonaws.com:443", null, null)]
        [InlineData("aws", "kms.us-east-1.amazonaws.com:12345", "$ConnectionRefusedSocketException$", null)]
        [InlineData("aws", "kms.us-east-2.amazonaws.com", "us-east-1", null)]
        [InlineData("aws", "example.com", "parse error", null)]
        // additional not spec tests
        [InlineData("aws", "$test$", "Invalid endpoint, expected dot separator in host, but got: $test$", null)]
        // azure
        [InlineData("azure", "key-vault-csfle.vault.azure.net", null, "parse error")]
        // gcp
        [InlineData("gcp", "cloudkms.googleapis.com:443", null, "parse error")]
        [InlineData("gcp", "example.com:443", "Invalid KMS response", null)]
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

            using (var client = ConfigureClient())
            using (var clientEncryption = ConfigureClientEncryption(client.Wrapped as MongoClient, ValidKmsEndpointConfigurator))
            using (var clientEncryptionInvalid = ConfigureClientEncryption(client.Wrapped as MongoClient, InvalidKmsEndpointConfigurator))
            {
                BsonDocument testCaseMasterKey = null;
                switch (kmsType)
                {
                    case "aws":
                        {
                            testCaseMasterKey = new BsonDocument
                            {
                                { "region", "us-east-1" },
                                { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                                { "endpoint", customEndpoint, customEndpoint != null }
                            };
                        }
                        break;
                    case "azure":
                        {
                            testCaseMasterKey = new BsonDocument
                            {
                                { "keyVaultEndpoint", customEndpoint },
                                { "keyName", "key-name-csfle" }
                            };
                        }
                        break;
                    case "gcp":
                        {
                            testCaseMasterKey = new BsonDocument
                            {
                                { "projectId", "devprod-drivers" },
                                { "location", "global" },
                                { "keyRing", "key-ring-csfle" },
                                { "keyName", "key-name-csfle" },
                                { "endpoint", customEndpoint }
                            };
                        }
                        break;
                    default:
                        throw new Exception($"Unexpected kms type {kmsType}.");
                }

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

                    if (expectedExceptionInfo.StartsWith("$") && expectedExceptionInfo.EndsWith("Exception$"))
                    {
                        var expectedException = CoreExceptionHelper.CreateException(expectedExceptionInfo.Trim('$'));
                        var excectedExceptionType = expectedException.GetType().GetTypeInfo();
                        excectedExceptionType.IsAssignableFrom(innerException.GetType()).Should().BeTrue();
                        innerException.Message.Should().StartWith(expectedException.Message);
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

                using (var client_encryption = ConfigureClientEncryption(client_test.Wrapped as MongoClient, kmsProviderFilter: "local"))
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

        [Trait("Category", "CsfleKmsTls")]
        [SkippableTheory]
        [ParameterAttributeData]
        public void KmsTlsTest([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            RequireEnvironment.Check().EnvironmentVariable("KMS_TLS_ERROR_TYPE", isDefined: true);

            using (var clientEncrypted = ConfigureClientEncrypted())
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted.Wrapped as MongoClient))
            {
                var dataKeyOptions = CreateDataKeyOptions(
                    kmsProvider: "aws",
                    customMasterKey: new BsonDocument
                    {
                        { "region", "us-east-1" },
                        { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" },
                        { "endpoint", "127.0.0.1:8000" }
                    });

                var exception = Record.Exception(() => CreateDataKey(clientEncryption, "aws", dataKeyOptions, async));

                // .Net doesn't make difference between different certificate issues and throws the same exception for all cases.
                // To ensure that we assert the expected case you need to configure a RemoteCertificateValidationCallback
                // to the SslStream ctor in LibMongoCryptControllerBase.SendKmsRequest/SendKmsRequestAsync and assert
                // sslPolicyErrors (for invalidHostname) and expiration dates (for expiredCertificate).
                exception.Message.Should().Be("Encryption related exception: The remote certificate is invalid according to the validation procedure.", $"because {Environment.GetEnvironmentVariable("KMS_TLS_ERROR_TYPE")} EG configuration");
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
            [Values("local", "aws", "azure", "gcp")] string kmsProvider,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var clientEncrypted = ConfigureClientEncrypted())
            using (var clientEncryption = ConfigureClientEncryption(clientEncrypted.Wrapped as MongoClient))
            {
                var dataKeyOptions = CreateDataKeyOptions(kmsProvider);
                var exception = Record.Exception(() => _ = CreateDataKey(clientEncryption, kmsProvider, dataKeyOptions, async));
                AssertResult(exception);
            }

            void AssertResult(Exception ex)
            {
                var currentOperatingSystem = RequirePlatform.GetCurrentOperatingSystem();
                var shouldThrowPlatformNotSupportedException = currentOperatingSystem != SupportedOperatingSystem.Windows;

                switch (kmsProvider)
                {
                    case "gcp" when shouldThrowPlatformNotSupportedException && CurrentTargetFrameworkIs(SupportedTargetFramework.NetStandard20):
                        {
                            var errorMessage = AssertExceptionTypesAndReturnErrorMessage<CryptException>(ex);
                            errorMessage.Should().Be("error constructing KMS message: Failed to create GCP oauth request signature: RSACryptoServiceProvider.ImportPkcs8PrivateKey is supported only on frameworks higher or equal to .netstandard2.1.");
                        }
                        break;
                    default:
                        ex.Should().BeNull(); // the rest of supported cases should not throw
                        break;
                }
            }

            string AssertExceptionTypesAndReturnErrorMessage<TInnerException>(Exception ex) where TInnerException : Exception
            {
                var e = ex.Should().BeOfType<MongoEncryptionException>().Subject;
                return e.InnerException.Should().BeOfType<TInnerException>().Subject.Message;
            }

            bool CurrentTargetFrameworkIs(SupportedTargetFramework supportedTargetFramework)
            {
                var currentTargetFramework = RequirePlatform.GetCurrentTargetFramework();
                return supportedTargetFramework == currentTargetFramework;
            }
        }

        // private methods
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
            int? maxPoolSize = null)
        {
            var configuredSettings = ConfigureClientEncryptedSettings(
                schemaMap,
                externalKeyVaultClient,
                kmsProviderFilter,
                eventCapturer,
                extraOptions,
                bypassAutoEncryption,
                maxPoolSize);

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

            return clientEncryptedSettings;
        }

        private ClientEncryption ConfigureClientEncryption(
            MongoClient client,
            Action<string, Dictionary<string, object>> kmsProviderConfigurator = null,
            string kmsProviderFilter = null)
        {
            var kmsProviders = GetKmsProviders(kmsProviderConfigurator, kmsProviderFilter);

            var clientEncryptionOptions = new ClientEncryptionOptions(
                keyVaultClient: client.Settings.AutoEncryptionOptions?.KeyVaultClient ?? client,
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: kmsProviders);

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
            switch (kmsProvider)
            {
                case "local":
                    Ensure.IsNull(customMasterKey, "local masterKey");
                    return new DataKeyOptions(alternateKeyNames: alternateKeyNames);
                case "aws":
                    var awsMasterKey = customMasterKey ??
                        new BsonDocument
                        {
                            { "region", "us-east-1" },
                            { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" }
                        };
                    return new DataKeyOptions(
                        alternateKeyNames: alternateKeyNames,
                        masterKey: awsMasterKey);
                case "azure":
                    var azureMasterKey = customMasterKey ??
                        new BsonDocument
                        {
                            { "keyName", "key-name-csfle" },
                            { "keyVaultEndpoint", "key-vault-csfle.vault.azure.net" }
                        };
                    return new DataKeyOptions(
                        alternateKeyNames: alternateKeyNames,
                        masterKey: azureMasterKey);
                case "gcp":
                    var gcpMasterKey = customMasterKey ??
                        new BsonDocument
                        {
                            { "projectId", "devprod-drivers" },
                            { "location", "global" },
                            { "keyRing", "key-ring-csfle" },
                            { "keyName", "key-name-csfle" }
                        };
                    return new DataKeyOptions(
                        alternateKeyNames: alternateKeyNames,
                        masterKey: gcpMasterKey);
                default:
                    throw new ArgumentException($"Incorrect kms provider {kmsProvider}", nameof(kmsProvider));
            }
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
                    extraOptions = new Dictionary<string, object>()
                    {
                        { "mongocryptdSpawnPath", GetEnvironmentVariableOrDefaultOrThrowIfNothing("MONGODB_BINARIES", string.Empty) }
                    };
                }

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

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders(
            Action<string, Dictionary<string, object>> kmsProviderConfigurator = null,
            string kmsProviderFilter = null)
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
