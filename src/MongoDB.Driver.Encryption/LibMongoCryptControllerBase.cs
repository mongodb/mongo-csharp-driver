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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    internal abstract class LibMongoCryptControllerBase
    {
        // protected fields
        protected readonly CryptClient _cryptClient;
        protected readonly IMongoClient _keyVaultClient;
        protected readonly Lazy<IMongoCollection<BsonDocument>> _keyVaultCollection;
        protected readonly CollectionNamespace _keyVaultNamespace;

        // private fields
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders;
        private readonly IStreamFactory _networkStreamFactory;
        private readonly IReadOnlyDictionary<string, SslSettings> _tlsOptions;

        // constructors
        protected LibMongoCryptControllerBase(
             CryptClient cryptClient,
             IMongoClient keyVaultClient,
             CollectionNamespace keyVaultNamespace,
             IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
             IReadOnlyDictionary<string, SslSettings> tlsOptions)
        {
            _cryptClient = Ensure.IsNotNull(cryptClient, nameof(cryptClient));
            _keyVaultClient = Ensure.IsNotNull(keyVaultClient, nameof(keyVaultClient)); // _keyVaultClient might not be fully constructed at this point, don't call any instance methods on it yet
            _keyVaultNamespace = Ensure.IsNotNull(keyVaultNamespace, nameof(keyVaultNamespace));
            _keyVaultCollection = new Lazy<IMongoCollection<BsonDocument>>(GetKeyVaultCollection); // delay use _keyVaultClient
            _kmsProviders = Ensure.IsNotNull(kmsProviders, nameof(kmsProviders));
            _networkStreamFactory = new NetworkStreamFactory();
            _tlsOptions = Ensure.IsNotNull(tlsOptions, nameof(tlsOptions));
        }

        // public properties
        public IMongoClient KeyVaultClient => _keyVaultClient;

        // protected methods
        protected static void FeedResult(CryptContext context, BsonDocument document)
        {
            var writerSettings = new BsonBinaryWriterSettings();
            var documentBytes = document.ToBson(writerSettings: writerSettings);
            context.Feed(documentBytes);
            context.MarkDone();
        }

        protected static void FeedResults(CryptContext context, IEnumerable<BsonDocument> documents)
        {
            var writerSettings = new BsonBinaryWriterSettings();
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
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS_CREDENTIALS:
                    ProcessNeedKmsCredentials(context, cancellationToken);
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
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS_CREDENTIALS:
                    await ProcessNeedKmsCredentialsAsync(context, cancellationToken).ConfigureAwait(false);
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

#pragma warning disable CA1822
        protected byte[] ToBsonIfNotNull(BsonValue value, int estimatedBsonSize = 0)
        {
            if (value != null)
            {
                var writerSettings = BsonBinaryWriterSettings.Defaults.Clone();
                return value.ToBson(writerSettings: writerSettings, estimatedBsonSize: estimatedBsonSize);
            }
            return null;
        }
#pragma warning restore CA1822

        // private methods
        private IMongoCollection<BsonDocument> GetKeyVaultCollection()
        {
            var keyVaultDatabase = _keyVaultClient.GetDatabase(_keyVaultNamespace.DatabaseNamespace.DatabaseName);

            var collectionSettings = new MongoCollectionSettings
            {
                ReadConcern = ReadConcern.Majority,
                WriteConcern = WriteConcern.WMajority
            };
            return keyVaultDatabase.GetCollection<BsonDocument>(_keyVaultNamespace.CollectionName, collectionSettings);
        }

        private static DnsEndPoint CreateKmsEndPoint(string value)
        {
            var match = Regex.Match(value, @"^(?<host>.*):(?<port>\d+)$");
            string host;
            int port;
            if (match.Success)
            {
                host = match.Groups["host"].Value;
#pragma warning disable CA1305
                port = int.Parse(match.Groups["port"].Value);
#pragma warning restore CA1305
            }
            else
            {
                host = value;
                port = 443;
            }

            return new DnsEndPoint(host, port);
        }

        private SslStreamSettings GetTlsStreamSettings(string kmsProvider)
        {
            if (!_tlsOptions.TryGetValue(kmsProvider, out var tlsSettings))
            {
                // default settings
                tlsSettings = new SslSettings();
            }
            return Ensure.IsNotNull(tlsSettings, nameof(tlsSettings)).ToSslStreamSettings();
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
            using var binary = context.GetOperation();
            var filterBytes = binary.ToArray();
            using var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var cursor = _keyVaultCollection.Value.FindSync(filter, cancellationToken: cancellationToken);
            var results = cursor.ToList(cancellationToken);
            FeedResults(context, results);
        }

        private void ProcessNeedKmsCredentials(CryptContext context, CancellationToken cancellationToken) =>
            // inner machinery in the below method doesn't support sync approach
            ProcessNeedKmsCredentialsAsync(context, cancellationToken).GetAwaiter().GetResult();

        private async Task ProcessNeedKmsCredentialsAsync(CryptContext context, CancellationToken cancellationToken)
        {
            var newCredentialsList = new List<BsonElement>();
            foreach (var kmsProvider in _kmsProviders.Where(k => k.Value.Count == 0))
            {
                if (MongoClientSettings.Extensions.KmsProviders.TryCreate(kmsProvider.Key, out var provider))
                {
                    var credentials = await provider.GetKmsCredentialsAsync(cancellationToken).ConfigureAwait(false);
                    newCredentialsList.Add(new BsonElement(kmsProvider.Key, credentials));
                }
            }

            context.SetCredentials(ToBsonIfNotNull(new BsonDocument(newCredentialsList)));
        }

        private async Task ProcessNeedMongoKeysStateAsync(CryptContext context, CancellationToken cancellationToken)
        {
            using var binary = context.GetOperation();
            var filterBytes = binary.ToArray();
            using var filterDocument = new RawBsonDocument(filterBytes);
            var filter = new BsonDocumentFilterDefinition<BsonDocument>(filterDocument);
            var cursor = await _keyVaultCollection.Value.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            var results = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            FeedResults(context, results);
        }

        private static byte[] ProcessReadyState(CryptContext context)
        {
            using var binary = context.FinalizeForEncryption();
            return binary.ToArray();
        }

        private void SendKmsRequest(KmsRequest request, CancellationToken cancellation)
        {
            var endpoint = CreateKmsEndPoint(request.Endpoint);

            var tlsStreamSettings = GetTlsStreamSettings(request.KmsProvider);
            var sslStreamFactory = new SslStreamFactory(tlsStreamSettings, _networkStreamFactory);
            using (var sslStream = sslStreamFactory.CreateStream(endpoint, cancellation))
            using (var binary = request.GetMessage())
            {
                var requestBytes = binary.ToArray();
                sslStream.Write(requestBytes, 0, requestBytes.Length);

                while (request.BytesNeeded > 0)
                {
                    var buffer = new byte[request.BytesNeeded]; // BytesNeeded is the maximum number of bytes that libmongocrypt wants to receive.
                    var count = sslStream.Read(buffer, 0, buffer.Length);
                    var responseBytes = new byte[count];
                    Buffer.BlockCopy(buffer, 0, responseBytes, 0, count);
                    request.Feed(responseBytes);
                }
            }
        }

        private async Task SendKmsRequestAsync(KmsRequest request, CancellationToken cancellation)
        {
            var endpoint = CreateKmsEndPoint(request.Endpoint);

            var tlsStreamSettings = GetTlsStreamSettings(request.KmsProvider);
            var sslStreamFactory = new SslStreamFactory(tlsStreamSettings, _networkStreamFactory);
            using (var sslStream = await sslStreamFactory.CreateStreamAsync(endpoint, cancellation).ConfigureAwait(false))
            using (var binary = request.GetMessage())
            {
                var requestBytes = binary.ToArray();
                await sslStream.WriteAsync(requestBytes, 0, requestBytes.Length).ConfigureAwait(false);

                while (request.BytesNeeded > 0)
                {
                    var buffer = new byte[request.BytesNeeded]; // BytesNeeded is the maximum number of bytes that libmongocrypt wants to receive.
                    var count = await sslStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    var responseBytes = new byte[count];
                    Buffer.BlockCopy(buffer, 0, responseBytes, 0, count);
                    request.Feed(responseBytes);
                }
            }
        }

        // nested type
        private class NetworkStreamFactory : IStreamFactory
        {
            public Stream CreateStream(EndPoint endPoint, CancellationToken cancellationToken)
            {
                var socket = CreateSocket();
                socket.Connect(endPoint);

                return new NetworkStream(socket, ownsSocket: true);
            }

            public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
            {
                var socket = CreateSocket();
                await socket.ConnectAsync(endPoint).ConfigureAwait(false);

                return new NetworkStream(socket, ownsSocket: true);
            }

            // private methods
            private static Socket CreateSocket() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
