using System;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using MongoDB.Driver.Encryption;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.LibMongoCryptBindingsBenchmark
{
    [Config(typeof(StyleConfig))]
    public class MongoCryptBenchmark
    {
        private const string LocalMasterKey =
            "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        private byte[] _encryptedValuesDocumentBytes;
        private IMongoCollection<BsonDocument> _encryptedClientCollection;
        private IMongoCollection<BsonDocument> _unencryptedClientCollection;
        private AutoEncryptionLibMongoCryptController _libMongoCryptController;

        private const int RepeatCount = 10;

        [Params(1, 2, 8, 64)]
        public int ThreadCounts;

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
            var keyVaultDatabase = keyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);
            keyVaultClient.DropDatabase("crypt-test");

            var clientEncryptionSettings = new ClientEncryptionOptions(
                keyVaultClient,
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
                string toEncryptString = $"value {(i + 1):D4}";
                var encryptedString =
                    clientEncryption.Encrypt(toEncryptString, encryptOptions, CancellationToken.None);
                encryptedValuesDocument.Add(new BsonElement($"key{(i + 1):D4}", encryptedString));
            }

            _encryptedValuesDocumentBytes = encryptedValuesDocument.ToBson();

            // Create libmongocrypt binding that will be used for decryption
            var cryptClient = CryptClientCreator.CreateCryptClient(autoEncryptionOptions.ToCryptClientSettings());
            _libMongoCryptController =
                AutoEncryptionLibMongoCryptController.Create(keyVaultClient, cryptClient, autoEncryptionOptions);

            _unencryptedClientCollection = new MongoClient().GetDatabase("crypt-test")
                .GetCollection<BsonDocument>("encryptedValues");

            _encryptedClientCollection = keyVaultClient.GetDatabase("crypt-test").GetCollection<BsonDocument>("encryptedValues");
            _encryptedClientCollection.InsertOne(encryptedValuesDocument);
        }

        [Benchmark(Baseline = true)]
        public void FindWithNoEncryption()
        {
            ThreadingUtilities.ExecuteOnNewThreads(ThreadCounts, _ =>
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
            ThreadingUtilities.ExecuteOnNewThreads(ThreadCounts, _ =>
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
            ThreadingUtilities.ExecuteOnNewThreads(ThreadCounts, _ =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _libMongoCryptController.DecryptFields(_encryptedValuesDocumentBytes, CancellationToken.None);
                }
            });
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
