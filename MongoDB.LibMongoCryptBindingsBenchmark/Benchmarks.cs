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

namespace MongoDB.LibMongoCryptBindingsBenchmark
{
    [Config(typeof(StyleConfig))]
    public class MongoCryptBenchmark
    {
        private const string LocalMasterKey =
            "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        private object _mongoCryptController;
        private MethodInfo _decryptFieldsMethod;
        private byte[] _encryptedValuesDocumentBytes;
        private IMongoCollection<BsonDocument> _encryptedClientCollection;
        private IMongoCollection<BsonDocument> _unencryptedClientCollection;

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

            _unencryptedClientCollection = new MongoClient().GetDatabase("crypt-test")
                .GetCollection<BsonDocument>("encryptedValues");

            _encryptedClientCollection = keyVaultClient.GetDatabase("crypt-test").GetCollection<BsonDocument>("encryptedValues");
            _encryptedClientCollection.InsertOne(encryptedValuesDocument);

            // since the benchmark is not in the same assembly as MongoClient, we can't directly get the LibMongoCryptController in it
            // so use reflection to get it and get the decrypt method that will be used for decryption tasks
            _mongoCryptController = new Reflector(keyVaultClient).LibMongoCryptController;
            _decryptFieldsMethod = _mongoCryptController.GetType().GetMethod("DecryptFields");
        }

        [Benchmark(Baseline = true)]
        public void FindWithNoEncryption()
        {
            var tasks = new Task[ThreadCounts];
            for (int i = 0; i < ThreadCounts; i++)
            {
                tasks[i] = Task.Factory.StartNew(FindWithNoEncryptionTask());
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void BulkDecryptionUsingFind()
        {
            var tasks = new Task[ThreadCounts];
            for (int i = 0; i < ThreadCounts; i++)
            {
                tasks[i] = Task.Factory.StartNew(BulkDecryptionTaskUsingFind());
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void BulkDecryptionUsingBinding()
        {
            var tasks = new Task[ThreadCounts];
            for (int i = 0; i < ThreadCounts; i++)
            {
                tasks[i] = Task.Factory.StartNew(BulkDecryptionTaskUsingBinding());
            }

            Task.WaitAll(tasks);
        }

        private Action FindWithNoEncryptionTask()
        {
            return () =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _unencryptedClientCollection.Find(Builders<BsonDocument>.Filter.Empty).Single();
                }
            };
        }

        private Action BulkDecryptionTaskUsingFind()
        {
            return () =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _encryptedClientCollection.Find(Builders<BsonDocument>.Filter.Empty).Single();
                }
            };
        }

        private Action BulkDecryptionTaskUsingBinding()
        {
            return () =>
            {
                for (int i = 0; i < RepeatCount; i++)
                {
                    _decryptFieldsMethod.Invoke(_mongoCryptController, new object[] { _encryptedValuesDocumentBytes, CancellationToken.None });
                }
            };
        }

        private class StyleConfig : ManualConfig
        {
            public StyleConfig()
            {
                SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage);
            }
        }

        private class Reflector
        {
            private readonly MongoClient _instance;

            public Reflector(MongoClient instance)
            {
                _instance = instance;
            }

            public object? LibMongoCryptController
            {
                get
                {
                    var field = typeof(MongoClient).GetField("_libMongoCryptController", BindingFlags.NonPublic | BindingFlags.Instance);
                    return field.GetValue(_instance);
                }
            }
        }
    }
}
