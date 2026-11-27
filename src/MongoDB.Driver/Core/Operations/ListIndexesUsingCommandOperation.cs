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

using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ListIndexesUsingCommandOperation : IReadOperation<IAsyncCursor<BsonDocument>>, IExecutableInRetryableReadContext<IAsyncCursor<BsonDocument>>
    {
        private int? _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private bool _enableOverloadRetargeting;
        private int _maxAdaptiveRetries;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool _retryRequested;

        public ListIndexesUsingCommandOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = messageEncoderSettings;
        }

        public int? BatchSize
        {
            get => _batchSize;
            set => _batchSize = value;
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "listIndexes";

        public bool EnableOverloadRetargeting
        {
            get => _enableOverloadRetargeting;
            set => _enableOverloadRetargeting = value;
        }

        public int MaxAdaptiveRetries
        {
            get => _maxAdaptiveRetries;
            set => _maxAdaptiveRetries = value;
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public bool IsOperationRetryable => true;

        public IAsyncCursor<BsonDocument> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = new RetryableReadContext(binding, _retryRequested, _maxAdaptiveRetries, _enableOverloadRetargeting))
            {
                return Execute(operationContext, context);
            }
        }

        public IAsyncCursor<BsonDocument> Execute(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                var operation = CreateOperation();
                try
                {
                    var result = operation.Execute(operationContext, context);
                    return AsyncCursor<BsonDocument>.FromServerResponse(
                        operationContext,
                        context.ChannelSource,
                        context.Channel,
                        result,
                        _comment,
                        batchSize: _batchSize ?? 0,
                        limit: 0,
                        BsonDocumentSerializer.Instance,
                        _messageEncoderSettings,
                        maxTime: null,
                        _retryRequested,
                        _maxAdaptiveRetries,
                        _enableOverloadRetargeting);
                }
                catch (MongoCommandException ex) when (IsCollectionNotFoundException(ex))
                {
                    return new SingleBatchAsyncCursor<BsonDocument>(new List<BsonDocument>());
                }
            }
        }

        public async Task<IAsyncCursor<BsonDocument>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = new RetryableReadContext(binding, _retryRequested, _maxAdaptiveRetries, _enableOverloadRetargeting))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        public async Task<IAsyncCursor<BsonDocument>> ExecuteAsync(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                var operation = CreateOperation();
                try
                {
                    var result = await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);
                    return await AsyncCursor<BsonDocument>.FromServerResponseAsync(
                        operationContext,
                        context.ChannelSource,
                        context.Channel,
                        result,
                        _comment,
                        batchSize: _batchSize ?? 0,
                        limit: 0,
                        BsonDocumentSerializer.Instance,
                        _messageEncoderSettings,
                        maxTime: null,
                        _retryRequested,
                        _maxAdaptiveRetries,
                        _enableOverloadRetargeting).ConfigureAwait(false);
                }
                catch (MongoCommandException ex) when (IsCollectionNotFoundException(ex))
                {
                    return new SingleBatchAsyncCursor<BsonDocument>(new List<BsonDocument>());
                }
            }
        }

        private ReadCommandOperation<BsonDocument> CreateOperation()
        {
            var databaseNamespace = _collectionNamespace.DatabaseNamespace;
            var command = new BsonDocument
            {
                { "listIndexes", _collectionNamespace.CollectionName },
                { "cursor", () => new BsonDocument("batchSize", _batchSize.Value), _batchSize.HasValue },
                { "comment", _comment, _comment != null },
            };
            return new ReadCommandOperation<BsonDocument>(databaseNamespace, command, AsyncCursor<BsonDocument>.CommandResultSerializer, _messageEncoderSettings, OperationName)
            {
                EnableOverloadRetargeting = _enableOverloadRetargeting,
                MaxAdaptiveRetries = _maxAdaptiveRetries,
                RetryRequested = _retryRequested, // might be overridden by retryable read context
            };
        }

        private bool IsCollectionNotFoundException(MongoCommandException ex)
        {
            return ex.Code == 26;
        }
    }
}
