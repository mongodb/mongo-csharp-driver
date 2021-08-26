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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.Specifications.Runner;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    public class ClientSideEncryptionTestRunner : MongoClientJsonDrivenTestRunnerBase
    {
        #region static
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("keyvault.datakeys");
        #endregion

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            if (testCase.Name.Contains("awsTemporary"))
            {
                // This test requires setting of some temporary environment variables that can be set by mongo orchestration or manually.
                // Add this environment variable on your local machine only together with FLE_AWS_TEMP_* variables (note: they will be expired in 12 hours)
                RequireEnvironment.Check().EnvironmentVariable("FLE_AWS_TEMPORARY_CREDS_ENABLED");
            }

            RequirePlatform
                .Check()
                .SkipWhen(SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard15)
                .SkipWhen(() => testCase.Name.Contains("gcpKMS.json"), SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20) // gcp is supported starting from netstandard2.1
                .SkipWhen(SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard15)
                .SkipWhen(() => testCase.Name.Contains("gcpKMS.json"), SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20); // gcp is supported starting from netstandard2.1

            SetupAndRunTest(testCase);
        }

        protected override string[] ExpectedTestColumns => new[] { "description", "clientOptions", "operations", "expectations", "skipReason", "async", "outcome" };

        protected override string[] ExpectedSharedColumns
        {
            get
            {
                var expectedSharedColumns = new List<string>(base.ExpectedSharedColumns);
                expectedSharedColumns.AddRange(new[] { "json_schema", "key_vault_data" });
                return expectedSharedColumns.ToArray();
            }
        }

        protected override void AssertEvent(object actualEvent, BsonDocument expectedEvent)
        {
            base.AssertEvent(
                actualEvent,
                expectedEvent,
                (actual, expected) =>
                {
                    if (actualEvent is CommandStartedEvent commandStartedEvent)
                    {
                        var expectedCommand = expected
                            .GetValue("command_started_event")
                            .AsBsonDocument
                            .GetValue("command")
                            .AsBsonDocument;
                        ReplaceTypeAssertionWithActual(commandStartedEvent.Command, expectedCommand);
                    }
                },
                getPlaceholders: () => new KeyValuePair<string, BsonValue>[0]); // do not use placeholders
        }

        protected override MongoClient CreateClientForTestSetup()
        {
            var clientSettings = DriverTestConfiguration.GetClientSettings().Clone();
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                clientSettings.GuidRepresentation = GuidRepresentation.Unspecified;
            }
#pragma warning restore 618
            return new MongoClient(clientSettings);
        }

        protected override void CreateCollection(IMongoClient client, string databaseName, string collectionName, BsonDocument test, BsonDocument shared)
        {
            if (shared.TryGetElement("json_schema", out var jsonSchema))
            {
                var database = client.GetDatabase(databaseName).WithWriteConcern(WriteConcern.WMajority);
                var validatorSchema = new BsonDocument("$jsonSchema", jsonSchema.Value.ToBsonDocument());
                database.CreateCollection(
                    collectionName,
                    new CreateCollectionOptions<BsonDocument>
                    {
                        Validator = new BsonDocumentFilterDefinition<BsonDocument>(validatorSchema)
                    });
            }
            else
            {
                base.CreateCollection(client, databaseName, collectionName, test, shared);
            }
        }

        protected override void DropCollection(MongoClient client, string databaseName, string collectionName, BsonDocument test, BsonDocument shared)
        {
            base.DropCollection(client, databaseName, collectionName, test, shared);

            if (shared.Contains("key_vault_data"))
            {
                var keyVaultDatabase = client.GetDatabase(__keyVaultCollectionNamespace.DatabaseNamespace.DatabaseName).WithWriteConcern(WriteConcern.WMajority);
                keyVaultDatabase.DropCollection(__keyVaultCollectionNamespace.CollectionName);
            }
        }

        protected override void InsertData(IMongoClient client, string databaseName, string collectionName, BsonDocument shared)
        {
            base.InsertData(client, databaseName, collectionName, shared);

            if (shared.TryGetValue("key_vault_data", out var keyVaultData))
            {
                var keyVaultDatabase = client.GetDatabase(__keyVaultCollectionNamespace.DatabaseNamespace.DatabaseName);
                var collectionSettings = new MongoCollectionSettings
                {
                    AssignIdOnInsert = false,
                    ReadConcern = ReadConcern.Majority,
                    WriteConcern = WriteConcern.WMajority
                };
                var keyVaultCollection = keyVaultDatabase.GetCollection<BsonDocument>(__keyVaultCollectionNamespace.CollectionName, collectionSettings);
                var keyVaultDocuments = keyVaultData.AsBsonArray.Select(c => c.AsBsonDocument);
                keyVaultCollection.InsertMany(keyVaultDocuments);
            }
        }

        protected override void ModifyOperationIfNeeded(BsonDocument operation)
        {
            base.ModifyOperationIfNeeded(operation);
            if (!operation.Contains("object"))
            {
                operation.Add(new BsonElement("object", "collection"));
            }
        }

        protected override bool TryConfigureClientOption(MongoClientSettings settings, BsonElement option)
        {
            switch (option.Name)
            {
                case "autoEncryptOpts":
                    settings.AutoEncryptionOptions = ConfigureAutoEncryptionOptions(option.Value.AsBsonDocument);
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override void VerifyCollectionData(IEnumerable<BsonDocument> expectedDocuments, string databaseName, string collectionName)
        {
            base.VerifyCollectionData(expectedDocuments, databaseName, collectionName, ReplaceTypeAssertionWithActual);
        }

        // private methods
        private AutoEncryptionOptions ConfigureAutoEncryptionOptions(BsonDocument autoEncryptOpts)
        {
            var extraOptions = new Dictionary<string, object>()
            {
                { "mongocryptdSpawnPath", GetEnvironmentVariableOrDefaultOrThrowIfNothing("MONGODB_BINARIES", string.Empty) }
            };

            var kmsProviders = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>(new Dictionary<string, IReadOnlyDictionary<string, object>>());
            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: kmsProviders,
                extraOptions: extraOptions);

            foreach (var option in autoEncryptOpts.Elements)
            {
                switch (option.Name)
                {
                    case "kmsProviders":
                        kmsProviders = ParseKmsProviders(option.Value.AsBsonDocument);
                        autoEncryptionOptions = autoEncryptionOptions
                            .With(kmsProviders: kmsProviders);
                        break;
                    case "schemaMap":
                        var schemaMaps = new Dictionary<string, BsonDocument>();
                        var schemaMapsDocument = option.Value.AsBsonDocument;
                        foreach (var schemaMapElement in schemaMapsDocument.Elements)
                        {
                            schemaMaps.Add(schemaMapElement.Name, schemaMapElement.Value.AsBsonDocument);
                        }
                        autoEncryptionOptions = autoEncryptionOptions.With(schemaMap: schemaMaps);
                        break;
                    case "bypassAutoEncryption":
                        autoEncryptionOptions = autoEncryptionOptions.With(bypassAutoEncryption: option.Value.ToBoolean());
                        break;
                    case "keyVaultNamespace":
                        autoEncryptionOptions = autoEncryptionOptions.With(keyVaultNamespace: CollectionNamespace.FromFullName(option.Value.AsString));
                        break;

                    default:
                        throw new Exception($"Unexpected auto encryption option {option.Name}.");
                }
            }

            return autoEncryptionOptions;
        }

        private string GetEnvironmentVariableOrDefaultOrThrowIfNothing(string variableName, string defaultValue = null) =>
            Environment.GetEnvironmentVariable(variableName) ??
            defaultValue ??
            throw new Exception($"{variableName} environment variable must be configured on the machine.");

        private ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> ParseKmsProviders(BsonDocument kmsProviders)
        {
            var providers = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            foreach (var kmsProvider in kmsProviders.Elements)
            {
                var kmsOptions = new Dictionary<string, object>();
                var kmsProviderName = kmsProvider.Name;
                switch (kmsProviderName)
                {
                    case "awsTemporary":
                        {
                            kmsProviderName = "aws";
                            var awsAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_ACCESS_KEY_ID");
                            var awsSecretAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_SECRET_ACCESS_KEY");
                            var awsSessionToken = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_SESSION_TOKEN");
                            kmsOptions.Add("accessKeyId", awsAccessKey);
                            kmsOptions.Add("secretAccessKey", awsSecretAccessKey);
                            kmsOptions.Add("sessionToken", awsSessionToken);
                        }
                        break;
                    case "awsTemporaryNoSessionToken":
                        {
                            kmsProviderName = "aws";
                            var awsAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_ACCESS_KEY_ID");
                            var awsSecretAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_SECRET_ACCESS_KEY");
                            kmsOptions.Add("accessKeyId", awsAccessKey);
                            kmsOptions.Add("secretAccessKey", awsSecretAccessKey);
                        }
                        break;
                    case "aws":
                        {
                            var awsAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_ACCESS_KEY_ID");
                            var awsSecretAccessKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_SECRET_ACCESS_KEY");
                            kmsOptions.Add("accessKeyId", awsAccessKey);
                            kmsOptions.Add("secretAccessKey", awsSecretAccessKey);
                        }
                        break;
                    case "local":
                        if (kmsProvider.Value.AsBsonDocument.TryGetElement("key", out var key))
                        {
                            var binary = key.Value.AsBsonBinaryData;
                            kmsOptions.Add(key.Name, binary.Bytes);
                        }
                        break;
                    case "azure":
                        {
                            var azureTenantId = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_TENANT_ID");
                            var azureClientId = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_CLIENT_ID");
                            var azureClientSecret = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_CLIENT_SECRET");
                            kmsOptions.Add("tenantId", azureTenantId);
                            kmsOptions.Add("clientId", azureClientId);
                            kmsOptions.Add("clientSecret", azureClientSecret);
                        }
                        break;
                    case "gcp":
                        {
                            var gcpEmail = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_GCP_EMAIL");
                            var gcpPrivateKey = GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_GCP_PRIVATE_KEY");
                            kmsOptions.Add("email", gcpEmail);
                            kmsOptions.Add("privateKey", gcpPrivateKey);
                        }
                        break;
                    default:
                        throw new Exception($"Unexpected kms provider type {kmsProvider.Name}.");
                }
                providers.Add(kmsProviderName, kmsOptions);
            }

            return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>(providers);
        }

        public void ReplaceTypeAssertionWithActual(BsonDocument actual, BsonDocument expected)
        {
            for (int i = 0; i < expected.ElementCount; i++)
            {
                var expectedElement = expected.ElementAt(i);
                var value = expectedElement.Value;
                if (value.IsBsonDocument)
                {
                    var valueDocument = value.AsBsonDocument;
                    var actualValue = actual.GetValue(expectedElement.Name, null);
                    if (valueDocument.ElementCount == 1 && valueDocument.Select(c => c.Name).Single().Equals("$$type"))
                    {
                        var type = valueDocument["$$type"].AsString;
                        if (type.Equals("binData"))
                        {
                            expected[expectedElement.Name] = actualValue;
                        }
                        else if (type.Equals("long"))
                        {
                            expected[expectedElement.Name] = actualValue;
                        }
                    }
                    else if (actualValue != null && actualValue.IsBsonDocument)
                    {
                        ReplaceTypeAssertionWithActual(actualValue.AsBsonDocument, valueDocument);
                    }
                }
                else if (value.IsBsonArray)
                {
                    ReplaceTypeAssertionWithActual(actual[expectedElement.Name].AsBsonArray, value.AsBsonArray);
                }
            }
        }

        private void ReplaceTypeAssertionWithActual(BsonArray actual, BsonArray expected)
        {
            for (int i = 0; i < expected.Count; i++)
            {
                var value = expected[i];
                if (value.IsBsonDocument)
                {
                    ReplaceTypeAssertionWithActual(actual[i].AsBsonDocument, value.AsBsonDocument);
                }
                else if (value.IsBsonArray)
                {
                    ReplaceTypeAssertionWithActual(actual[i].AsBsonArray, value.AsBsonArray);
                }
            }
        }
    }

    // nested types
    public class TestCaseFactory : JsonDrivenTestCaseFactory
    {
        #region static
        private static readonly string[] __ignoredTestNames =
        {
            // https://jira.mongodb.org/browse/SPEC-1403
            "maxWireVersion.json:operation fails with maxWireVersion < 8"
        };
        private static readonly string[] __versionedApiIgnoredTestNames =
        {
            // https://jira.mongodb.org/browse/SERVER-58293
            "explain.json:Explain a find with deterministic encryption"
        };
        #endregion

        // protected properties
        protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.client_side_encryption.tests.";

        // protected methods
        protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
        {
            var testCases = base.CreateTestCases(document).Where(test => !__ignoredTestNames.Any(ignoredName => test.Name.EndsWith(ignoredName)));
            if (CoreTestConfiguration.RequireApiVersion)
            {
                testCases = testCases.Where(test => !__versionedApiIgnoredTestNames.Any(ignoredName => test.Name.EndsWith(ignoredName)));
            }
            foreach (var testCase in testCases)
            {
                foreach (var async in new[] { false, true })
                {
                    var name = $"{testCase.Name}:async={async}";
                    var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                    yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                }
            }
        }
    }
}
