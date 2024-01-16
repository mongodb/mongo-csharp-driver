using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;

namespace MongoDB.LibMongoCryptBindingsBenchmark
{
    [Config(typeof(StyleConfig))]
    public class MongoCryptBenchmark
    {
        private const int RepeatCount = 10;
        private const string LocalMasterKey =
            "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        private byte[] _encryptedValuesDocumentBytes;
        private IMongoCollection<BsonDocument> _encryptedClientCollection;
        private IMongoCollection<BsonDocument> _unencryptedClientCollection;
        private AutoEncryptionLibMongoCryptController _libMongoCryptController;
        private DisposableMongoClient _disposableKeyVaultClient;

        [Params(1)]
        public int ThreadsCounts;

        [GlobalSetup]
        public void Setup()
        {
            byte[] localMasterKey = Convert.FromBase64String(LocalMasterKey);

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

            var keyVaultClient = new MongoClient(clientSettings);
            _disposableKeyVaultClient = new DisposableMongoClient(keyVaultClient, null);

            var keyVaultDatabase = _disposableKeyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);
            _disposableKeyVaultClient.DropDatabase("crypt-test");

            var clientEncryptionSettings = new ClientEncryptionOptions(
                _disposableKeyVaultClient,
                keyVaultNamespace,
                kmsProviders);

            var clientEncryption = new ClientEncryption(clientEncryptionSettings);

            var dataKeyId = clientEncryption.CreateDataKey(
                "local",
                new DataKeyOptions(),
                CancellationToken.None);

            var encryptOptions = new EncryptOptions(
                EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                keyId: dataKeyId);

            var encryptedValuesDocument = new BsonDocument();
            for (int i = 0; i < 1500; i++)
            {
                var toEncryptString = $"value {(i + 1):D4}";
                var encryptedString =
                    clientEncryption.Encrypt(toEncryptString, encryptOptions, CancellationToken.None);
                encryptedValuesDocument.Add(new BsonElement($"key{(i + 1):D4}", encryptedString));
            }

            _encryptedValuesDocumentBytes = encryptedValuesDocument.ToBson();

            // Create libmongocrypt binding that will be used for decryption
            var cryptClient = CryptClientCreator.CreateCryptClient(autoEncryptionOptions.ToCryptClientSettings());
            _libMongoCryptController =
                AutoEncryptionLibMongoCryptController.Create(_disposableKeyVaultClient, cryptClient, autoEncryptionOptions);

            _unencryptedClientCollection = new MongoClient().GetDatabase("crypt-test")
                .GetCollection<BsonDocument>("encryptedValues");

            _encryptedClientCollection = _disposableKeyVaultClient.GetDatabase("crypt-test").GetCollection<BsonDocument>("encryptedValues");
            _encryptedClientCollection.InsertOne(encryptedValuesDocument);

            clientEncryption.Dispose();
        }

        [Benchmark(Baseline = true)]
        public void FindWithNoEncryption()
        {
            ThreadingUtilities.ExecuteOnNewThreads(ThreadsCounts, _ =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _unencryptedClientCollection.Find(Builders<BsonDocument>.Filter.Empty).Single();
                }
            });
        }

        [Benchmark]
        public void FindWithEncryption()
        {
            ThreadingUtilities.ExecuteOnNewThreads(ThreadsCounts, _ =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _encryptedClientCollection.Find(Builders<BsonDocument>.Filter.Empty).Single();
                }
            });
        }

        [Benchmark]
        public void BulkDecryptionUsingBinding()
        {
            ThreadingUtilities.ExecuteOnNewThreads(ThreadsCounts, _ =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _libMongoCryptController.DecryptFields(_encryptedValuesDocumentBytes, CancellationToken.None);
                }
            });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _disposableKeyVaultClient.Dispose();
        }

        private class StyleConfig : ManualConfig
        {
            public StyleConfig()
            {
                SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage);
            }
        }
    }
}
