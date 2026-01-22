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

using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ListIndexesOperation : IReadOperation<IAsyncCursor<BsonDocument>>
    {
        private int? _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool _retryRequested;

        public ListIndexesOperation(
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

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                var operation = CreateOperation();
                return operation.Execute(operationContext, context);
            }
        }

        public async Task<IAsyncCursor<BsonDocument>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                var operation = CreateOperation();
                return await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        private EventContext.OperationIdDisposer BeginOperation() => EventContext.BeginOperation(null, "listIndexes");

        private IExecutableInRetryableReadContext<IAsyncCursor<BsonDocument>> CreateOperation()
        {
            return new ListIndexesUsingCommandOperation(_collectionNamespace, _messageEncoderSettings)
            {
                BatchSize = _batchSize,
                Comment = _comment,
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
        }
    }
}
