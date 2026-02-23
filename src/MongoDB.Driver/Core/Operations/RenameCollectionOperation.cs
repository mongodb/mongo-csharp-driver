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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class RenameCollectionOperation : IWriteOperation<BsonDocument>, IRetryableWriteOperation<BsonDocument>
    {
        private readonly CollectionNamespace _collectionNamespace;
        private bool? _dropTarget;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly CollectionNamespace _newCollectionNamespace;
        private WriteConcern _writeConcern;

        public RenameCollectionOperation(
            CollectionNamespace collectionNamespace,
            CollectionNamespace newCollectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _newCollectionNamespace = Ensure.IsNotNull(newCollectionNamespace, nameof(newCollectionNamespace));
            _messageEncoderSettings = messageEncoderSettings;
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public bool? DropTarget
        {
            get { return _dropTarget; }
            set { _dropTarget = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public CollectionNamespace NewCollectionNamespace
        {
            get { return _newCollectionNamespace; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, _writeConcern);
            return new BsonDocument
            {
                { "renameCollection", _collectionNamespace.FullName },
                { "to", _newCollectionNamespace.FullName },
                { "dropTarget", () => _dropTarget.Value, _dropTarget.HasValue },
                { "writeConcern", writeConcern, writeConcern != null }
            };
        }

        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, binding, retryRequested: false);
            }
        }

        public BsonDocument Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, context);
            }
        }

        public Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, binding, retryRequested: false);
            }
        }

        public Task<BsonDocument> ExecuteAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, context);
            }
        }

        public BsonDocument ExecuteAttempt(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var binding = context.Binding;
            var channelSource = context.ChannelSource;
            var channel = context.Channel;

            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription, transactionNumber);
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public async Task<BsonDocument> ExecuteAttemptAsync(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var binding = context.Binding;
            var channelSource = context.ChannelSource;
            var channel = context.Channel;

            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription, transactionNumber);
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation("renameCollection");

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
        {
            var command = CreateCommand(operationContext, session, connectionDescription, transactionNumber);
            return new WriteCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }
    }
}
