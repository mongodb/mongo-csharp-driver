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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.Tests;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class ClientSideEncryption2Examples
    {
        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";
        private readonly static CollectionNamespace CollectionNamespace = CollectionNamespace.FromFullName("docsExamples.encrypted");
        private readonly static CollectionNamespace KeyVaultNamespace = CollectionNamespace.FromFullName("keyvault.datakeys");

        [Fact]
        public void FLE2AutomaticEncryption()
        {
            RequireServer.Check().Supports(Feature.Csfle2).ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded, ClusterType.LoadBalanced);

            var unencryptedClient = DriverTestConfiguration.Client;

            DropCollections(unencryptedClient);

            var localMasterKey = Convert.FromBase64String(LocalMasterKey);

            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localKey = new Dictionary<string, object>
            {
                { "key", localMasterKey }
            };
            kmsProviders.Add("local", localKey);

            var keyVaultClient = new MongoClient();

            // Create two data keys.
            var clientEncryptionOptions = new ClientEncryptionOptions(keyVaultClient, KeyVaultNamespace, kmsProviders);
            using var clientEncryption = new ClientEncryption(clientEncryptionOptions);
            var dataKeyOptions = new DataKeyOptions();
            var dataKey1 = clientEncryption.CreateDataKey("local", dataKeyOptions, CancellationToken.None);
            var dataKey2 = clientEncryption.CreateDataKey("local", dataKeyOptions, CancellationToken.None);

            // Create an encryptedFieldsMap with an indexed and unindexed field.
            var encryptedFieldsMap = new Dictionary<string, BsonDocument>()
            {
                {
                    CollectionNamespace.ToString(),
                    new BsonDocument
                    {
                        {
                            "fields",
                            new BsonArray
                            {
                                new BsonDocument
                                {
                                    { "path", "encryptedIndexed" },
                                    { "bsonType", "string" },
                                    { "keyId", new BsonBinaryData(dataKey1, GuidRepresentation.Standard) },
                                    { "queries", new BsonDocument("queryType", "equality") }
                                },
                                new BsonDocument
                                {
                                    { "path", "encryptedUnindexed" },
                                    { "bsonType", "string" },
                                    { "keyId", new BsonBinaryData(dataKey2, GuidRepresentation.Standard) }
                                }
                            }
                        }
                    }
                }
            };
            var autoEncryptionOptions = new AutoEncryptionOptions(KeyVaultNamespace, kmsProviders, encryptedFieldsMap: encryptedFieldsMap);
            var encryptedClient = new MongoClient(new MongoClientSettings { AutoEncryptionOptions = autoEncryptionOptions });

            // Create an FLE 2 collection.
            var database = encryptedClient.GetDatabase(CollectionNamespace.DatabaseNamespace.DatabaseName);
            database.CreateCollection(CollectionNamespace.CollectionName);
            var encryptedCollection = database.GetCollection<BsonDocument>(CollectionNamespace.CollectionName);

            // Auto encrypt an insert and find with "Indexed" and "Unindexed" encrypted fields.
            string indexedValue = "indexedValue";
            string unindexedValue = "unindexedValue";
            encryptedCollection.InsertOne(new BsonDocument { { "_id", 1 }, { "encryptedIndexed", indexedValue }, { "encryptedUnindexed", unindexedValue } });

            var findResult = encryptedCollection.Find(new BsonDocument("encryptedIndexed", "indexedValue")).Single().AsBsonDocument;

            findResult["encryptedIndexed"].Should().Be(new BsonString(indexedValue));
            findResult["encryptedUnindexed"].Should().Be(new BsonString(unindexedValue));

            // Find documents without decryption.
            var unencryptedDatabase = unencryptedClient.GetDatabase(CollectionNamespace.DatabaseNamespace.DatabaseName);
            var unencryptedCollection = unencryptedDatabase.GetCollection<BsonDocument>(CollectionNamespace.CollectionName);
            findResult = unencryptedCollection.Find(new BsonDocument("_id", 1)).Single().AsBsonDocument;
            findResult["encryptedIndexed"].Should().BeOfType<BsonBinaryData>();
            findResult["encryptedUnindexed"].Should().BeOfType<BsonBinaryData>();
        }

        private void DropCollections(IMongoClient client)
        {
            var database = client.GetDatabase(CollectionNamespace.DatabaseNamespace.DatabaseName);
            database.DropCollection(
                CollectionNamespace.CollectionName,
                new DropCollectionOptions
                {
                    EncryptedFields = new BsonDocument
                    {
                        {  "escCollection", $"enxcol_.{CollectionNamespace.CollectionName}.esc" },
                        {  "eccCollection", $"enxcol_.{CollectionNamespace.CollectionName}.ecc" },
                        {  "ecocCollection", $"enxcol_.{CollectionNamespace.CollectionName}.ecoc" },
                    }
                });

            database = client.GetDatabase(KeyVaultNamespace.DatabaseNamespace.DatabaseName);
            database.DropCollection(KeyVaultNamespace.CollectionName);
        }
    }
}
