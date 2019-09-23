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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.Tests.Specifications.Runner;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    public class ClientSideEncryptionTestRunner : MongoClientJsonDrivenTestRunnerBase
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
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
            clientSettings.GuidRepresentation = GuidRepresentation.Unspecified;
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
                var adminDatabase = client.GetDatabase("admin").WithWriteConcern(WriteConcern.WMajority);
                adminDatabase.DropCollection("datakeys");
            }
        }

        protected override void InsertData(IMongoClient client, string databaseName, string collectionName, BsonDocument shared)
        {
            base.InsertData(client, databaseName, collectionName, shared);

            if (shared.TryGetValue("key_vault_data", out var keyVaultData))
            {
                var adminDatabase = client.GetDatabase("admin");
                var keyVaultCollection = adminDatabase.GetCollection<BsonDocument>(
                    "datakeys",
                    new MongoCollectionSettings
                    {
                        AssignIdOnInsert = false
                    });
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

        protected override void VerifyCollectionData(IEnumerable<BsonDocument> expectedDocuments)
        {
            base.VerifyCollectionData(expectedDocuments, ReplaceTypeAssertionWithActual);
        }

        // private methods
        private AutoEncryptionOptions ConfigureAutoEncryptionOptions(BsonDocument autoEncryptOpts)
        {
            var keyVaultCollectionNamespace = new CollectionNamespace("admin", "datakeys");
            var extraOptions = new Dictionary<string, object>()
            {
                { "mongocryptdSpawnPath", Environment.GetEnvironmentVariable("MONGODB_BINARIES") ?? string.Empty }
            };

            var kmsProviders = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>(new Dictionary<string, IReadOnlyDictionary<string, object>>());
            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace: keyVaultCollectionNamespace,
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

        private ReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> ParseKmsProviders(BsonDocument kmsProviders)
        {
            var providers = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            foreach (var kmsProvider in kmsProviders.Elements)
            {
                var kmsOptions = new Dictionary<string, object>();
                switch (kmsProvider.Name)
                {
                    case "aws":
                        {
                            var awsRegion = Environment.GetEnvironmentVariable("FLE_AWS_REGION") ?? "us-east-1";
                            var awsAccessKey = Environment.GetEnvironmentVariable("FLE_AWS_ACCESS_KEY_ID") ?? throw new Exception("The FLE_AWS_ACCESS_KEY_ID system variable should be configured on the machine.");
                            var awsSecretAccessKey = Environment.GetEnvironmentVariable("FLE_AWS_SECRET_ACCESS_KEY") ?? throw new Exception("The FLE_AWS_SECRET_ACCESS_KEY system variable should be configured on the machine.");
                            kmsOptions.Add("region", awsRegion);
                            kmsOptions.Add("accessKeyId", awsAccessKey);
                            kmsOptions.Add("secretAccessKey", awsSecretAccessKey);
                        }
                        providers.Add(kmsProvider.Name, kmsOptions);
                        break;
                    case "local":
                        if (kmsProvider.Value.AsBsonDocument.TryGetElement("key", out var key))
                        {
                            var binary = key.Value.AsBsonBinaryData;
                            kmsOptions.Add(key.Name, binary.Bytes);
                        }
                        providers.Add(kmsProvider.Name, kmsOptions);
                        break;
                    default:
                        throw new Exception($"Unexpected kms provider type {kmsProvider.Name}.");
                }
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
        #endregion

        // protected properties
        protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.client_side_encryption.tests.";

        // protected methods
        protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
        {
            var testCases = base.CreateTestCases(document).Where(test => !__ignoredTestNames.Any(ignoredName => test.Name.EndsWith(ignoredName)));
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
