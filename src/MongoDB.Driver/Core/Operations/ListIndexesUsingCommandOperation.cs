/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
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

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public IAsyncCursor<BsonDocument> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
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
                    return CreateCursor(context.ChannelSource, context.Channel, result);
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

            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
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
                    return CreateCursor(context.ChannelSource, context.Channel, result);
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
            return new ReadCommandOperation<BsonDocument>(databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
        }

        private IAsyncCursor<BsonDocument> CreateCursor(IChannelSourceHandle channelSource, IChannelHandle channel, BsonDocument result)
        {
            var cursorDocument = result["cursor"].AsBsonDocument;
            var cursorId = cursorDocument["id"].ToInt64();
            var getMoreChannelSource = ChannelPinningHelper.CreateGetMoreChannelSource(channelSource, channel, cursorId);
            var cursor = new AsyncCursor<BsonDocument>(
                getMoreChannelSource,
                CollectionNamespace.FromFullName(cursorDocument["ns"].AsString),
                _comment,
                cursorDocument["firstBatch"].AsBsonArray.OfType<BsonDocument>().ToList(),
                cursorId,
                batchSize: _batchSize ?? 0,
                0,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);

            return cursor;
        }

        private bool IsCollectionNotFoundException(MongoCommandException ex)
        {
            return ex.Code == 26;
        }
    }
}
