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
    /// <summary>
    /// Represents a drop index operation.
    /// </summary>
    internal sealed class DropSearchIndexOperation : IWriteOperation<BsonDocument>, IRetryableWriteOperation<BsonDocument>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly string _indexName;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private WriteConcern _writeConcern;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DropIndexOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public DropSearchIndexOperation(
            CollectionNamespace collectionNamespace,
            string indexName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _indexName = Ensure.IsNotNullOrEmpty(indexName, nameof(indexName));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        // properties
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        // methods
        /// <inheritdoc/>
        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, binding, retryRequested: false);
            }
        }

        /// <inheritdoc/>
        public BsonDocument Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, context);
            }
        }

        /// <inheritdoc/>
        public Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            {
                return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, binding, retryRequested: false);
            }
        }

        /// <inheritdoc/>
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
                try
                {
                    return operation.Execute(operationContext, channelBinding);
                }
                catch (MongoCommandException ex) when (ShouldIgnoreException(ex))
                {
                    return ex.Result;
                }
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
                try
                {
                    return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
                }
                catch (MongoCommandException ex) when (ShouldIgnoreException(ex))
                {
                    return ex.Result;
                }
            }
        }

        internal BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
        {
            return new BsonDocument
            {
                { "dropSearchIndex", _collectionNamespace.CollectionName },
                { "name", _indexName }
            };
        }

        private IDisposable BeginOperation() => EventContext.BeginOperation("dropSearchIndex");

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
        {
            var command = CreateCommand(operationContext, session, connectionDescription, transactionNumber);
            return new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }

        private bool ShouldIgnoreException(MongoCommandException ex) =>
            ex?.Code == (int)ServerErrorCode.NamespaceNotFound ||
            ex?.ErrorMessage == "ns not found";
    }
}
