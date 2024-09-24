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
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;

namespace MongoDB.Benchmarks
{
    public class LibmongocryptBindingBenchmark
    {
        private const int RepeatCount = 10;
        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        private byte[] _encryptedValuesDocumentBytes;
        private IMongoClient _disposableKeyVaultClient;
        private IAutoEncryptionLibMongoCryptController _libMongoCryptController;

        [Params(1)]
        public int ThreadsCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            MongoClientSettings.Extensions.AddAutoEncryption();

            var localMasterKey = Convert.FromBase64String(LocalMasterKey);

            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localKey = new Dictionary<string, object> { { "key", localMasterKey } };
            kmsProviders.Add("local", localKey);

            var keyVaultNamespace = CollectionNamespace.FromFullName("encryption.__keyVault");
            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace: keyVaultNamespace,
                kmsProviders: kmsProviders,
                bypassAutoEncryption: true);

            var clientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost");
            clientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            clientSettings.ClusterSource = DisposingClusterSource.Instance;

            _disposableKeyVaultClient = new MongoClient(clientSettings);

            var keyVaultDatabase = _disposableKeyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);
            _disposableKeyVaultClient.DropDatabase("crypt-test");

            var clientEncryptionSettings = new ClientEncryptionOptions(
                _disposableKeyVaultClient,
                keyVaultNamespace,
                kmsProviders);

            var encryptedValuesDocument = new BsonDocument();
            using (var clientEncryption = new ClientEncryption(clientEncryptionSettings))
            {
                var dataKeyId = clientEncryption.CreateDataKey(
                    "local",
                    new DataKeyOptions(),
                    CancellationToken.None);

                var encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKeyId);

                for (int i = 0; i < 1500; i++)
                {
                    var toEncryptString = $"value {(i + 1):D4}";
                    var encryptedString =
                        clientEncryption.Encrypt(toEncryptString, encryptOptions, CancellationToken.None);
                    encryptedValuesDocument.Add(new BsonElement($"key{(i + 1):D4}", encryptedString));
                }
            }
            _encryptedValuesDocumentBytes = encryptedValuesDocument.ToBson();

            // Create libmongocrypt binding that will be used for decryption
            _libMongoCryptController =
                MongoClientSettings.Extensions.AutoEncryptionProvider.CreateAutoCryptClientController(_disposableKeyVaultClient, autoEncryptionOptions);
        }

        [Benchmark]
        public void BulkDecryptionUsingBinding()
        {
            ThreadingUtilities.ExecuteOnNewThreads(ThreadsCount, _ =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _libMongoCryptController.DecryptFields(_encryptedValuesDocumentBytes, CancellationToken.None);
                }
            }, 20000);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _libMongoCryptController.Dispose();
            _disposableKeyVaultClient.Dispose();
        }
    }
}
