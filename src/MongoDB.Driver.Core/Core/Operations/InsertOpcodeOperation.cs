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
using System.Collections.Generic;
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

    public class InsertOpcodeOperation<TDocument> : IWriteOperation<IEnumerable<WriteConcernResult>>
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
        private WriteConcern _writeConcern;

        // constructors
        public InsertOpcodeOperation(CollectionNamespace collectionNamespace, BatchableSource<TDocument> documentSource, IBsonSerializer<TDocument> serializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _documentSource = Ensure.IsNotNull(documentSource, "documentSource");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
            _writeConcern = WriteConcern.Acknowledged;
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
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public int? MaxMessageSize
        {
            get { return _maxMessageSize; }
            set { _maxMessageSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        private InsertWireProtocol<TDocument> CreateProtocol(WriteConcern batchWriteConcern, Func<bool> shouldSendGetLastError)
        {
            return new InsertWireProtocol<TDocument>(
                _collectionNamespace,
                batchWriteConcern,
                _serializer,
                _messageEncoderSettings,
                _documentSource,
                _maxBatchCount,
                _maxMessageSize,
                _continueOnError,
                shouldSendGetLastError);
        }

        public async Task<IEnumerable<WriteConcernResult>> ExecuteAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(channel, "channel");

            if (channel.ConnectionDescription.BuildInfoResult.ServerVersion >= new SemanticVersion(2, 6, 0) && _writeConcern.IsAcknowledged)
            {
                var emulator = new InsertOpcodeOperationEmulator<TDocument>(_collectionNamespace, _serializer, _documentSource, _messageEncoderSettings)
                {
                    ContinueOnError = _continueOnError,
                    MaxBatchCount = _maxBatchCount,
                    MaxDocumentSize = _maxDocumentSize,
                    MaxMessageSize = _maxMessageSize,
                    WriteConcern = _writeConcern
                };
                var result = await emulator.ExecuteAsync(channel, cancellationToken).ConfigureAwait(false);
                return new[] { result };
            }
            else
            {
                if (_documentSource.Batch == null)
                {
                    return await InsertMultipleBatchesAsync(channel, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var result = await InsertSingleBatchAsync(channel, cancellationToken).ConfigureAwait(false);
                    return new[] { result };
                }
            }
        }

        public async Task<IEnumerable<WriteConcernResult>> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(channel, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<WriteConcernResult>> InsertMultipleBatchesAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            var results = _writeConcern.Enabled ? new List<WriteConcernResult>() : null;
            Exception finalException = null;

            WriteConcern batchWriteConcern = _writeConcern;
            Func<bool> shouldSendGetLastError = null;
            if (!_writeConcern.Enabled && !_continueOnError)
            {
                batchWriteConcern = WriteConcern.Acknowledged;
                shouldSendGetLastError = () => _documentSource.HasMore;
            }

            while (_documentSource.HasMore)
            {
                var protocol = CreateProtocol(batchWriteConcern, shouldSendGetLastError);

                WriteConcernResult result;
                try
                {
                    result = await channel.ExecuteProtocolAsync(protocol, cancellationToken).ConfigureAwait(false);
                }
                catch (WriteConcernException ex)
                {
                    result = ex.WriteConcernResult;
                    if (_continueOnError)
                    {
                        finalException = ex;
                    }
                    else if (_writeConcern.Enabled)
                    {
                        results.Add(result);
                        ex.Data["results"] = results;
                        throw;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (results != null)
                {
                    results.Add(result);
                }

                _documentSource.ClearBatch();
            }

            if (_writeConcern.Enabled && finalException != null)
            {
                finalException.Data["results"] = results;
                throw finalException;
            }

            return results;
        }

        private async Task<WriteConcernResult> InsertSingleBatchAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            var protocol = CreateProtocol(_writeConcern, null);
            return await channel.ExecuteProtocolAsync(protocol, cancellationToken).ConfigureAwait(false);
        }
    }
}
