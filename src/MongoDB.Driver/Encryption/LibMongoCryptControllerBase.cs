﻿/* Copyright 2019-present MongoDB Inc.
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
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Encryption
{
    internal abstract class LibMongoCryptControllerBase
    {
        // protected fields
        protected readonly CryptClient _cryptClient;
        protected readonly IMongoClient _keyVaultClient;
        protected readonly Lazy<IMongoCollection<BsonDocument>> _keyVaultCollection;
        protected readonly CollectionNamespace _keyVaultNamespace;

        // constructors
        protected LibMongoCryptControllerBase(
             CryptClient cryptClient,
             IMongoClient keyVaultClient,
             CollectionNamespace keyVaultNamespace)
        {
            _cryptClient = cryptClient;
            _keyVaultClient = keyVaultClient; // _keyVaultClient might not be fully constructed at this point, don't call any instance methods on it yet
            _keyVaultNamespace = keyVaultNamespace;
            _keyVaultCollection = new Lazy<IMongoCollection<BsonDocument>>(GetKeyVaultCollection); // delay use _keyVaultClient
        }

        // protected methods
        protected void FeedResult(CryptContext context, BsonDocument document)
        {
            var writerSettings = new BsonBinaryWriterSettings { GuidRepresentation = GuidRepresentation.Unspecified };
            var documentBytes = document.ToBson(writerSettings: writerSettings);
            context.Feed(documentBytes);
            context.MarkDone();
        }

        protected void FeedResults(CryptContext context, IEnumerable<BsonDocument> documents)
        {
            var writerSettings = new BsonBinaryWriterSettings { GuidRepresentation = GuidRepresentation.Unspecified };
            foreach (var document in documents)
            {
                var documentBytes = document.ToBson(writerSettings: writerSettings);
                context.Feed(documentBytes);
            }
            context.MarkDone();
        }

        protected virtual void ProcessState(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            switch (context.State)
            {
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS:
                    ProcessNeedKmsState(context, cancellationToken);
                    break;
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS:
                    ProcessNeedMongoKeysState(context, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected context state: {context.State}.");
            }
        }

        protected virtual async Task ProcessStateAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            switch (context.State)
            {
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS:
                    await ProcessNeedKmsStateAsync(context, cancellationToken).ConfigureAwait(false);
                    break;
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS:
                    await ProcessNeedMongoKeysStateAsync(context, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected context state: {context.State}.");
            }
        }

        protected byte[] ProcessStates(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            byte[] result = null;
            while (context.State != CryptContext.StateCode.MONGOCRYPT_CTX_DONE)
            {
                if (context.State == CryptContext.StateCode.MONGOCRYPT_CTX_READY)
                {
                    result = ProcessReadyState(context);
                }
                else
                {
                    ProcessState(context, databaseName, cancellationToken);
                }
            }
            return result;
        }

        protected async Task<byte[]> ProcessStatesAsync(CryptContext context, string databaseName, CancellationToken cancellationToken)
        {
            byte[] result = null;
            while (context.State != CryptContext.StateCode.MONGOCRYPT_CTX_DONE)
            {
                if (context.State == CryptContext.StateCode.MONGOCRYPT_CTX_READY)
                {
                    result = ProcessReadyState(context);
                }
                else
                {
                    await ProcessStateAsync(context, databaseName, cancellationToken).ConfigureAwait(false);
                }
            }
            return result;
        }

        // private methods
        private IMongoCollection<BsonDocument> GetKeyVaultCollection()
        {
            var keyVaultDatabase = _keyVaultClient.GetDatabase(_keyVaultNamespace.DatabaseNamespace.DatabaseName);
            return keyVaultDatabase.GetCollection<BsonDocument>(_keyVaultNamespace.CollectionName);
        }

        private void ProcessNeedKmsState(CryptContext context, CancellationToken cancellationToken)
        {
            var requests = context.GetKmsMessageRequests();
            foreach (var request in requests)
            {
                SendKmsRequest(request, cancellationToken);
            }
            requests.MarkDone();
        }

        private async Task ProcessNeedKmsStateAsync(CryptContext context, CancellationToken cancellationToken)
        {
            var requests = context.GetKmsMessageRequests();
            foreach (var request in requests)
            {
                await SendKmsRequestAsync(request, cancellationToken).ConfigureAwait(false);
            }
            requests.MarkDone();
        }

        private void ProcessNeedMongoKeysState(CryptContext context, CancellationToken cancellationToken)
        {
            var filterBytes = context.GetOperation().ToArray();
            var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var cursor = _keyVaultCollection.Value.FindSync(filter, cancellationToken: cancellationToken);
            var results = cursor.ToList(cancellationToken);
            FeedResults(context, results);
        }

        private async Task ProcessNeedMongoKeysStateAsync(CryptContext context, CancellationToken cancellationToken)
        {
            var filterBytes = context.GetOperation().ToArray();
            var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var cursor = await _keyVaultCollection.Value.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            var results = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            FeedResults(context, results);
        }

        private byte[] ProcessReadyState(CryptContext context)
        {
            return context.FinalizeForEncryption().ToArray();
        }

        private void SendKmsRequest(KmsRequest request, CancellationToken cancellation)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(request.Endpoint, 443);

            using (var networkStream = new NetworkStream(socket, ownsSocket: true))
            using (var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false))
            {
#if NETSTANDARD1_5
                sslStream.AuthenticateAsClientAsync(request.Endpoint).ConfigureAwait(false).GetAwaiter().GetResult();
#else
                sslStream.AuthenticateAsClient(request.Endpoint);
#endif

                var requestBytes = request.Message.ToArray();
                sslStream.Write(requestBytes);

                var buffer = new byte[4096];
                while (request.BytesNeeded > 0)
                {
                    var count = sslStream.Read(buffer, 0, buffer.Length);
                    var responseBytes = new byte[count];
                    Buffer.BlockCopy(buffer, 0, responseBytes, 0, count);
                    request.Feed(responseBytes);
                }
            }
        }

        private async Task SendKmsRequestAsync(KmsRequest request, CancellationToken cancellation)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
#if NETSTANDARD1_5
            await socket.ConnectAsync(request.Endpoint, 443).ConfigureAwait(false);
#else
            await Task.Factory.FromAsync(socket.BeginConnect(request.Endpoint, 443, null, null), socket.EndConnect).ConfigureAwait(false);
#endif

            using (var networkStream = new NetworkStream(socket, ownsSocket: true))
            using (var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false))
            {
                await sslStream.AuthenticateAsClientAsync(request.Endpoint).ConfigureAwait(false);

                var requestBytes = request.Message.ToArray();
                await sslStream.WriteAsync(requestBytes, 0, requestBytes.Length).ConfigureAwait(false);

                var buffer = new byte[4096];
                while (request.BytesNeeded > 0)
                {
                    var count = await sslStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    var responseBytes = new byte[count];
                    Buffer.BlockCopy(buffer, 0, responseBytes, 0, count);
                    request.Feed(responseBytes);
                }
            }
        }
    }
}
