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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Encryption
{
    internal sealed class AutoEncryptionLibMongoCryptController : LibMongoCryptControllerBase, IBinaryDocumentFieldDecryptor, IBinaryCommandFieldEncryptor
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly IMongoClient _mongocryptdClient;
        private readonly MongocryptdFactory _mongocryptdFactory;

        // constructors
        public AutoEncryptionLibMongoCryptController(
            IMongoClient client,
            CryptClient cryptClient,
            AutoEncryptionOptions autoEncryptionOptions)
            : base(
                  Ensure.IsNotNull(cryptClient, nameof(cryptClient)),
                  Ensure.IsNotNull(autoEncryptionOptions, nameof(autoEncryptionOptions)).KeyVaultClient ?? client,
                  Ensure.IsNotNull(Ensure.IsNotNull(autoEncryptionOptions, nameof(autoEncryptionOptions)).KeyVaultNamespace, nameof(autoEncryptionOptions.KeyVaultNamespace)))
        {
            _client = Ensure.IsNotNull(client, nameof(client)); // _client might not be fully constructed at this point, don't call any instance methods on it yet
            _mongocryptdFactory = new MongocryptdFactory(autoEncryptionOptions.ExtraOptions);
            _mongocryptdClient = _mongocryptdFactory.CreateMongocryptdClient();
        }

        // public methods
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
            var database = _client.GetDatabase(databaseName);
            var filterBytes = context.GetOperation().ToArray();
            var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var options = new ListCollectionsOptions { Filter = filter };
            var cursor = database.ListCollections(options, cancellationToken);
            var results = cursor.ToList(cancellationToken);
            FeedResults(context, results);
        }

        private async Task ProcessNeedCollectionInfoStateAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            var database = _client.GetDatabase(databaseName);
            var filterBytes = context.GetOperation().ToArray();
            var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var options = new ListCollectionsOptions { Filter = filter };
            var cursor = await database.ListCollectionsAsync(options, cancellationToken).ConfigureAwait(false);
            var results = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            FeedResults(context, results);
        }

        private void ProcessNeedMongoMarkingsState(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            var database = _mongocryptdClient.GetDatabase(databaseName);
            var commandBytes = context.GetOperation().ToArray();
            var commandDocument = new RawBsonDocument(commandBytes);
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

            RestoreDbNodeInResponse(commandDocument, response);
            FeedResult(context, response);
        }

        private async Task ProcessNeedMongoMarkingsStateAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            var database = _mongocryptdClient.GetDatabase(databaseName);
            var commandBytes = context.GetOperation().ToArray();
            var commandDocument = new RawBsonDocument(commandBytes);
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

            RestoreDbNodeInResponse(commandDocument, response);
            FeedResult(context, response);
        }

        private void RestoreDbNodeInResponse(BsonDocument request, BsonDocument response)
        {
            if (request.TryGetElement("$db", out var db))
            {
                var result = response["result"].AsBsonDocument;
                if (!result.Contains("$db"))
                {
                    result.Add(db);
                }
            }
        }

        private void WaitForMongocryptdReady()
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                var clusterDescription = _mongocryptdClient.Cluster?.Description;
                var mongocryptdServer = clusterDescription?.Servers?.FirstOrDefault();
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
                var clusterDescription = _mongocryptdClient.Cluster?.Description;
                var mongocryptdServer = clusterDescription?.Servers?.FirstOrDefault();
                if (mongocryptdServer != null && mongocryptdServer.Type != ServerType.Unknown)
                {
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
            }
        }
    }
}
