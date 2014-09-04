/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class InsertOpcodeOperation : InsertOpcodeOperation<BsonDocument>
    {
        // constructors
        public InsertOpcodeOperation(
            CollectionNamespace collectionNamespace,
            BatchableSource<BsonDocument> documentSource,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, documentSource, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    public class InsertOpcodeOperation<TDocument> : IWriteOperation<WriteConcernResult>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private bool _continueOnError;
        private readonly BatchableSource<TDocument> _documentSource;
        private int? _maxBatchCount;
        private int? _maxDocumentSize;
        private int? _maxMessageSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IBsonSerializer<TDocument> _serializer;
        private Func<bool> _shouldSendGetLastError;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        public InsertOpcodeOperation(CollectionNamespace collectionNamespace, BatchableSource<TDocument> documentSource, IBsonSerializer<TDocument> serializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _documentSource = Ensure.IsNotNull(documentSource, "documentSource");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public bool ContinueOnError
        {
            get { return _continueOnError; }
            set { _continueOnError = value; }
        }

        public BatchableSource<TDocument> DocumentSource
        {
            get { return _documentSource; }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public int? MaxMessageSize
        {
            get { return _maxMessageSize; }
            set { _maxMessageSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }

        public Func<bool> ShouldSendGetLastError
        {
            get { return _shouldSendGetLastError; }
            set { _shouldSendGetLastError = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        private InsertWireProtocol<TDocument> CreateProtocol()
        {
            return new InsertWireProtocol<TDocument>(
                _collectionNamespace,
                _writeConcern,
                _serializer,
                _messageEncoderSettings,
                _documentSource,
                _maxBatchCount,
                _maxMessageSize,
                _continueOnError,
                _shouldSendGetLastError);
        }

        public async Task<WriteConcernResult> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(connection, "connection");

            if (connection.Description.BuildInfoResult.ServerVersion >= new SemanticVersion(2, 6, 0) && _writeConcern.IsAcknowledged)
            {
                var emulator = new InsertOpcodeOperationEmulator<TDocument>(_collectionNamespace, _serializer, _documentSource, _messageEncoderSettings)
                {
                    ContinueOnError = _continueOnError,
                    MaxBatchCount = _maxBatchCount,
                    MaxDocumentSize = _maxDocumentSize,
                    MaxMessageSize = _maxMessageSize,
                    WriteConcern = _writeConcern
                };
                return await emulator.ExecuteAsync(connection, timeout, cancellationToken);
            }
            else
            {
                var protocol = CreateProtocol();
                return await protocol.ExecuteAsync(connection, timeout, cancellationToken);
            }
        }

        public async Task<WriteConcernResult> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }
    }
}
