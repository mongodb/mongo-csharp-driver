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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Encryption
{
    internal sealed class AutoEncryptionLibMongoCryptController : LibMongoCryptControllerBase, IAutoEncryptionLibMongoCryptController
    {
        #region static
        public static IAutoEncryptionLibMongoCryptController Create(IMongoClient client, AutoEncryptionOptions autoEncryptionOptions)
        {
            var cryptClient = CryptClientFactory.Create(client.Cluster.Settings.CryptClientSettings);

            var lazyInternalClient = new Lazy<IMongoClient>(() => CreateInternalClient());
            var keyVaultClient = autoEncryptionOptions.KeyVaultClient ?? lazyInternalClient.Value;
            var metadataClient = autoEncryptionOptions.BypassAutoEncryption ? null : lazyInternalClient.Value;
            var internalClient = lazyInternalClient.IsValueCreated ? lazyInternalClient.Value : null;

            return new AutoEncryptionLibMongoCryptController(
                internalClient,
                keyVaultClient,
                metadataClient,
                cryptClient,
                autoEncryptionOptions);

            IMongoClient CreateInternalClient()
            {
                var internalClientSettings = client.Settings.Clone();
                internalClientSettings.AutoEncryptionOptions = null;
                internalClientSettings.MinConnectionPoolSize = 0;
                return new MongoClient(internalClientSettings);
            }
        }
        #endregion

        // private fields
        private readonly IMongoClient _internalClient;
        private readonly IMongoClient _metadataClient;
        private readonly Lazy<IMongoClient> _mongocryptdClient;
        private readonly MongocryptdFactory _mongocryptdFactory;

        // constructors
        private AutoEncryptionLibMongoCryptController(
            IMongoClient internalClient,
            IMongoClient keyVaultClient,
            IMongoClient metadataClient,
            CryptClient cryptClient,
            AutoEncryptionOptions autoEncryptionOptions)
            : base(cryptClient, keyVaultClient, autoEncryptionOptions.KeyVaultNamespace, autoEncryptionOptions.KmsProviders, autoEncryptionOptions.TlsOptions)
        {
            _internalClient = internalClient; // can be null
            _metadataClient = metadataClient; // can be null
            _mongocryptdFactory = new MongocryptdFactory(autoEncryptionOptions.ExtraOptions, autoEncryptionOptions.BypassQueryAnalysis);
            _mongocryptdClient = new Lazy<IMongoClient>(() => _mongocryptdFactory.CreateMongocryptdClient(), isThreadSafe: true);
        }

        // public methods
        public string CryptSharedLibraryVersion() => _cryptClient.CryptSharedLibraryVersion;

        public byte[] DecryptFields(byte[] encryptedDocumentBytes, CancellationToken cancellationToken)
        {
            try
            {
                using (var context = _cryptClient.StartDecryptionContext(encryptedDocumentBytes))
                {
                    return ProcessStates(context, databaseName: null, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<byte[]> DecryptFieldsAsync(byte[] encryptedDocumentBytes, CancellationToken cancellationToken)
        {
            try
            {
                using (var context = _cryptClient.StartDecryptionContext(encryptedDocumentBytes))
                {
                    return await ProcessStatesAsync(context, databaseName: null, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public byte[] EncryptFields(string databaseName, byte[] unencryptedCommandBytes, CancellationToken cancellationToken)
        {
            try
            {
                using (var context = _cryptClient.StartEncryptionContext(databaseName, unencryptedCommandBytes))
                {
                    return ProcessStates(context, databaseName, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<byte[]> EncryptFieldsAsync(string databaseName, byte[] unencryptedCommandBytes, CancellationToken cancellationToken)
        {
            try
            {
                using (var context = _cryptClient.StartEncryptionContext(databaseName, unencryptedCommandBytes))
                {
                    return await ProcessStatesAsync(context, databaseName, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        // protected methods
        protected override void ProcessState(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            switch (context.State)
            {
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_COLLINFO:
                    ProcessNeedCollectionInfoState(context, databaseName, cancellationToken);
                    break;
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS:
                    ProcessNeedMongoMarkingsState(context, databaseName, cancellationToken);
                    break;
                default:
                    base.ProcessState(context, databaseName, cancellationToken);
                    break;
            }
        }

        protected override async Task ProcessStateAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            switch (context.State)
            {
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_COLLINFO:
                    await ProcessNeedCollectionInfoStateAsync(context, databaseName, cancellationToken).ConfigureAwait(false);
                    break;
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS:
                    await ProcessNeedMongoMarkingsStateAsync(context, databaseName, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await base.ProcessStateAsync(context, databaseName, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        // private methods
        private void ProcessNeedCollectionInfoState(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            if (_metadataClient == null)
            {
                // should not be reached
                throw new InvalidOperationException("Metadata client is null.");
            }

            var database = _metadataClient.GetDatabase(databaseName);
            using var binary = context.GetOperation();
            var filterBytes = binary.ToArray();
            using var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var options = new ListCollectionsOptions { Filter = filter };
            var cursor = database.ListCollections(options, cancellationToken);
            var results = cursor.ToList(cancellationToken);
            FeedResults(context, results);
        }

        private async Task ProcessNeedCollectionInfoStateAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            if (_metadataClient == null)
            {
                // should not be reached
                throw new InvalidOperationException("Metadata client is null.");
            }

            var database = _metadataClient.GetDatabase(databaseName);
            using var binary = context.GetOperation();
            var filterBytes = binary.ToArray();
            using var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var options = new ListCollectionsOptions { Filter = filter };
            var cursor = await database.ListCollectionsAsync(options, cancellationToken).ConfigureAwait(false);
            var results = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            FeedResults(context, results);
        }

        private void ProcessNeedMongoMarkingsState(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            var database = _mongocryptdClient.Value.GetDatabase(databaseName);
            using var binary = context.GetOperation();
            var commandBytes = binary.ToArray();
            using var commandDocument = new RawBsonDocument(commandBytes);
            var command = new BsonDocumentCommand<BsonDocument>(commandDocument);

            BsonDocument response = null;
            for (var attempt = 1; response == null; attempt++)
            {
                try
                {
                    response = database.RunCommand(command, cancellationToken: cancellationToken);
                }
                catch (TimeoutException) when (attempt == 1)
                {
                    _mongocryptdFactory.SpawnMongocryptdProcessIfRequired();
                    WaitForMongocryptdReady();
                }
            }

            FeedResult(context, response);
        }

        private async Task ProcessNeedMongoMarkingsStateAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            var database = _mongocryptdClient.Value.GetDatabase(databaseName);
            using var binary = context.GetOperation();
            var commandBytes = binary.ToArray();
            using var commandDocument = new RawBsonDocument(commandBytes);
            var command = new BsonDocumentCommand<BsonDocument>(commandDocument);

            BsonDocument response = null;
            for (var attempt = 1; response == null; attempt++)
            {
                try
                {
                    response = await database.RunCommandAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (TimeoutException) when (attempt == 1)
                {
                    _mongocryptdFactory.SpawnMongocryptdProcessIfRequired();
                    await WaitForMongocryptdReadyAsync().ConfigureAwait(false);
                }
            }

            FeedResult(context, response);
        }

        private void WaitForMongocryptdReady()
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                var clusterDescription = _mongocryptdClient.Value.Cluster?.Description;
#pragma warning disable CA1826
                var mongocryptdServer = clusterDescription?.Servers?.FirstOrDefault();
#pragma warning restore CA1826
                if (mongocryptdServer != null && mongocryptdServer.Type != ServerType.Unknown)
                {
                    return;
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(5));
            }
        }

        private async Task WaitForMongocryptdReadyAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                var clusterDescription = _mongocryptdClient.Value.Cluster?.Description;
#pragma warning disable CA1826
                var mongocryptdServer = clusterDescription?.Servers?.FirstOrDefault();
#pragma warning restore CA1826
                if (mongocryptdServer != null && mongocryptdServer.Type != ServerType.Unknown)
                {
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            // Dispose of the clients as well once rebased on the IDisposable mongoclient work.
            if (_internalClient != null)
            {
                ClusterRegistry.Instance.UnregisterAndDisposeCluster(_internalClient.Cluster);
            }

            if (_mongocryptdClient.IsValueCreated)
            {
                ClusterRegistry.Instance.UnregisterAndDisposeCluster(_mongocryptdClient.Value.Cluster);
            }
        }
    }
}
