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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.Specifications.Runner;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    [Trait("Category", "CSFLE")]
    [Trait("Category", "Serverless")]
    public class ClientSideEncryptionTestRunner : MongoClientJsonDrivenTestRunnerBase
    {
        #region static
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("keyvault.datakeys");
        #endregion

        // public methods
        public ClientSideEncryptionTestRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            if (testCase.Name.Contains("awsTemporary"))
            {
                // This test requires setting of some temporary environment variables that can be set by mongo orchestration or manually.
                // Add this environment variable on your local machine only together with FLE_AWS_TEMP_* variables (note: they will be expired in 12 hours)
                RequireEnvironment.Check().EnvironmentVariable("FLE_AWS_TEMPORARY_CREDS_ENABLED");
            }

            if (testCase.Name.Contains("kmip"))
            {
                // kmip requires configuring kms mock server
                RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED");
            }

            RequirePlatform
                .Check()
                .SkipWhen(
                    // spec wording requires skipping only "fle2-Range-<type>-Correctness" tests on macos,
                    // but we see significant performance downgrade with the rest fle2-Range tests too, so skip them as well
                    () => testCase.Name.Contains("fle2-Range"),
                    SupportedOperatingSystem.MacOS); ;

            RequirePlatform
                .Check()
                .SkipWhen(() => testCase.Name.Contains("gcpKMS.json"), SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20) // gcp is supported starting from netstandard2.1
                .SkipWhen(() => testCase.Name.Contains("gcpKMS.json"), SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20); // gcp is supported starting from netstandard2.1

            SetupAndRunTest(testCase);
        }

        protected override string[] ExpectedTestColumns => new[] { "description", "clientOptions", "operations", "expectations", "skipReason", "async", "outcome" };

        protected override string[] ExpectedSharedColumns
        {
            get
            {
                var expectedSharedColumns = new List<string>(base.ExpectedSharedColumns);
                expectedSharedColumns.AddRange(new[] { "json_schema", "key_vault_data", "encrypted_fields" });
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

        protected override void AssertOperation(string name, JsonDrivenTest jsonDrivenTest)
        {
            switch (name)
            {
                case "findOneAndUpdate":
                    {
                        // ignore: "_id" and  "__safeContent__"
                        jsonDrivenTest.Assert(allowExtraFields: true);
                    }
                    break;
                default: base.AssertOperation(name, jsonDrivenTest); break;
            }
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
            var jsonSchema = shared.GetValue("json_schema", defaultValue: null);
            var encrypted_fields = shared.GetValue("encrypted_fields", defaultValue: null);
            if (jsonSchema != null || encrypted_fields != null)
            {
                var database = client.GetDatabase(databaseName).WithWriteConcern(WriteConcern.WMajority);
                BsonDocumentFilterDefinition<BsonDocument> validator = null;
                if (jsonSchema != null)
                {
                    var validatorSchema = new BsonDocument("$jsonSchema", jsonSchema.ToBsonDocument());
                    validator = new BsonDocumentFilterDefinition<BsonDocument>(validatorSchema);
                }
                database.CreateCollection(
                    collectionName,
                    new CreateCollectionOptions<BsonDocument>
                    {
                        EncryptedFields = encrypted_fields?.ToBsonDocument(),
                        Validator = validator
                    });
            }
            else
            {
                base.CreateCollection(client, databaseName, collectionName, test, shared);
            }
        }

        protected override void DropCollection(MongoClient client, string databaseName, string collectionName, BsonDocument test, BsonDocument shared)
        {
            if (shared.TryGetValue("encrypted_fields", out var encrypted_fields))
            {
                var database = client.GetDatabase(databaseName).WithWriteConcern(WriteConcern.WMajority);
                database.DropCollection(collectionName, new DropCollectionOptions { EncryptedFields = encrypted_fields.AsBsonDocument });
            }
            else
            {
                base.DropCollection(client, databaseName, collectionName, test, shared);
            }

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
                if (keyVaultDocuments.Any())
                {
                    keyVaultCollection.InsertMany(keyVaultDocuments);
                }
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
            var extraOptions = new Dictionary<string, object>();

            EncryptionTestHelper.ConfigureDefaultExtraOptions(extraOptions);

            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: kmsProviders,
                extraOptions: extraOptions);

            foreach (var option in autoEncryptOpts.Elements)
            {
                switch (option.Name)
                {
                    case "kmsProviders":
                        kmsProviders = EncryptionTestHelper.ParseKmsProviders(option.Value.AsBsonDocument, legacy: true);
                        autoEncryptionOptions = autoEncryptionOptions.With(kmsProviders: Optional.Create(kmsProviders));
                        var tlsSettings = EncryptionTestHelper.CreateTlsOptionsIfAllowed(kmsProviders, allowClientCertificateFunc: (kms) => kms == "kmip");
                        if (tlsSettings != null)
                        {
                            autoEncryptionOptions = autoEncryptionOptions.With(tlsOptions: tlsSettings);
                        }
                        break;
                    case "schemaMap":
                        var schemaMapsDocument = option.Value.AsBsonDocument;
                        var schemaMaps = schemaMapsDocument.Elements.ToDictionary(e => e.Name, e => e.Value.AsBsonDocument);
                        autoEncryptionOptions = autoEncryptionOptions.With(schemaMap: schemaMaps);
                        break;
                    case "bypassAutoEncryption":
                        autoEncryptionOptions = autoEncryptionOptions.With(bypassAutoEncryption: option.Value.ToBoolean());
                        break;
                    case "keyVaultNamespace":
                        autoEncryptionOptions = autoEncryptionOptions.With(keyVaultNamespace: CollectionNamespace.FromFullName(option.Value.AsString));
                        break;
                    case "encryptedFieldsMap":
                        var encryptedFieldsMapDocument = option.Value.AsBsonDocument;
                        var encryptedFieldsMap = encryptedFieldsMapDocument.Elements.ToDictionary(e => e.Name, e => e.Value.AsBsonDocument);
                        autoEncryptionOptions = autoEncryptionOptions.With(encryptedFieldsMap: encryptedFieldsMap);
                        break;
                    case "bypassQueryAnalysis":
                        autoEncryptionOptions = autoEncryptionOptions.With(bypassQueryAnalysis: option.Value.ToBoolean());
                        break;
                    case "extraOptions":
                        foreach (var extraOption in option.Value.AsBsonDocument.Elements)
                        {
                            switch (extraOption.Name)
                            {
                                case "mongocryptdBypassSpawn":
                                    extraOptions.Add(extraOption.Name, extraOption.Value.ToBoolean());
                                    break;
                                default:
                                    throw new Exception($"Unexpected extra option {extraOption.Name}.");
                            }
                        }
                        autoEncryptionOptions = autoEncryptionOptions.With(extraOptions: extraOptions);
                        break;
                    default:
                        throw new Exception($"Unexpected auto encryption option {option.Name}.");
                }
            }

            return autoEncryptionOptions;
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
        private static readonly string[] __versionedApiIgnoredTestNames =
        {
            // https://jira.mongodb.org/browse/SERVER-58293
            "explain.json:Explain a find with deterministic encryption"
        };
        #endregion

        // protected properties
        protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.client_side_encryption.tests.legacy.";

        // protected methods
        protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
        {
            var testCases = base.CreateTestCases(document);
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
