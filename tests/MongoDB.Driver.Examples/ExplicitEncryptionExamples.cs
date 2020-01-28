/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Libmongocrypt;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Examples
{
    public class ExplicitEncryptionExamples
    {
        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        private readonly ITestOutputHelper _output;

        public ExplicitEncryptionExamples(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ClientSideExplicitEncryptionAndDecryptionTour()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var localMasterKey = Convert.FromBase64String(LocalMasterKey);

            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localKey = new Dictionary<string, object>
            {
                { "key", localMasterKey }
            };
            kmsProviders.Add("local", localKey);

            var keyVaultNamespace = CollectionNamespace.FromFullName("admin.datakeys");
            var keyVaultClient = new MongoClient("mongodb://localhost");
            var keyVaultDatabase = keyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);

            // Create the ClientEncryption instance
            var clientEncryptionSettings = new ClientEncryptionOptions(
                keyVaultClient,
                keyVaultNamespace,
                kmsProviders);
            using (var clientEncryption = new ClientEncryption(clientEncryptionSettings))
            {
                var dataKeyId = clientEncryption.CreateDataKey(
                    "local",
                    new DataKeyOptions(),
                    CancellationToken.None);

                var originalString = "123456789";
                _output.WriteLine($"Original string {originalString}.");

                // Explicitly encrypt a field
                var encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKeyId);
                var encryptedFieldValue = clientEncryption.Encrypt(
                    originalString,
                    encryptOptions,
                    CancellationToken.None);
                _output.WriteLine($"Encrypted value {encryptedFieldValue}.");

                // Explicitly decrypt the field
                var decryptedValue = clientEncryption.Decrypt(encryptedFieldValue, CancellationToken.None);
                _output.WriteLine($"Decrypted value {decryptedValue}.");
            }
        }

        [Fact]
        public void ClientSideExplicitEncryptionAndAutoDecryptionTour()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var localMasterKey = Convert.FromBase64String(LocalMasterKey);

            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localKey = new Dictionary<string, object>
            {
                { "key", localMasterKey }
            };
            kmsProviders.Add("local", localKey);

            var keyVaultNamespace = CollectionNamespace.FromFullName("admin.datakeys");
            var collectionNamespace = CollectionNamespace.FromFullName("test.coll");
            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviders,
                bypassAutoEncryption: true);
            var clientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost");
            clientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            var mongoClient = new MongoClient(clientSettings);
            var database = mongoClient.GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName);
            database.DropCollection(collectionNamespace.CollectionName);
            var collection = database.GetCollection<BsonDocument>(collectionNamespace.CollectionName);

            var keyVaultClient = new MongoClient("mongodb://localhost");
            var keyVaultDatabase = keyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);

            // Create the ClientEncryption instance
            var clientEncryptionSettings = new ClientEncryptionOptions(
                keyVaultClient,
                keyVaultNamespace,
                kmsProviders);
            using (var clientEncryption = new ClientEncryption(clientEncryptionSettings))
            {
                var dataKeyId = clientEncryption.CreateDataKey(
                    "local",
                    new DataKeyOptions(),
                    CancellationToken.None);

                var originalString = "123456789";
                _output.WriteLine($"Original string {originalString}.");

                // Explicitly encrypt a field
                var encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKeyId);
                var encryptedFieldValue = clientEncryption.Encrypt(
                    originalString,
                    encryptOptions,
                    CancellationToken.None);
                _output.WriteLine($"Encrypted value {encryptedFieldValue}.");

                collection.InsertOne(new BsonDocument("encryptedField", encryptedFieldValue));

                // Automatically decrypts the encrypted field.
                var decryptedValue = collection.Find(FilterDefinition<BsonDocument>.Empty).First();
                _output.WriteLine($"Decrypted document {decryptedValue.ToJson()}.");
            }
        }
    }
}
