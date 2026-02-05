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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class FindAndModifyOperationBase<TResult> : IWriteOperation<TResult>, IRetryableWriteOperation<TResult>
    {
        private Collation _collation;
        private BsonValue _comment;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IBsonSerializer<TResult> _resultSerializer;
        private WriteConcern _writeConcern;
        private bool _retryRequested;

        public FindAndModifyOperationBase(CollectionNamespace collectionNamespace, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "findAndModify";

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        public TResult Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, binding, _retryRequested);
            }
        }

        public TResult Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, context);
            }
        }

        public Task<TResult> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, binding, _retryRequested);
            }
        }

        public Task<TResult> ExecuteAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, context);
            }
        }

        public TResult ExecuteAttempt(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var binding = context.Binding;
            var channelSource = context.ChannelSource;
            var channel = context.Channel;

            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription, transactionNumber);
                using (var rawBsonDocument = operation.Execute(operationContext, channelBinding))
                {
                    return ProcessCommandResult(channel.ConnectionDescription.ConnectionId, rawBsonDocument);
                }
            }
        }

        public async Task<TResult> ExecuteAttemptAsync(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var binding = context.Binding;
            var channelSource = context.ChannelSource;
            var channel = context.Channel;

            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription, transactionNumber);
                using (var rawBsonDocument = await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false))
                {
                    return ProcessCommandResult(channel.ConnectionDescription.ConnectionId, rawBsonDocument);
                }
            }
        }

        public abstract BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber);

        protected abstract IElementNameValidator GetCommandValidator();

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation(OperationName);

        private WriteCommandOperation<RawBsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
        {
            var command = CreateCommand(operationContext, session, connectionDescription, transactionNumber);
            return new WriteCommandOperation<RawBsonDocument>(_collectionNamespace.DatabaseNamespace, command, RawBsonDocumentSerializer.Instance, _messageEncoderSettings, OperationName)
            {
                CommandValidator = GetCommandValidator()
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private TResult ProcessCommandResult(ConnectionId connectionId, RawBsonDocument rawBsonDocument)
        {
            var binaryReaderSettings = new BsonBinaryReaderSettings
            {
                Encoding = _messageEncoderSettings.GetOrDefault<UTF8Encoding>(MessageEncoderSettingsName.ReadEncoding, Utf8Encodings.Strict)
            };

            using (var stream = new ByteBufferStream(rawBsonDocument.Slice, ownsBuffer: false))
            using (var reader = new BsonBinaryReader(stream, binaryReaderSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return _resultSerializer.Deserialize(context);
            }
        }
    }
}
