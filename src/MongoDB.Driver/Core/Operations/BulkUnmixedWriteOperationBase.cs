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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class BulkUnmixedWriteOperationBase<TWriteRequest> : IWriteOperation<BulkWriteOperationResult>, IExecutableInRetryableWriteContext<BulkWriteOperationResult>
        where TWriteRequest : WriteRequest
    {
        // fields
        private bool? _bypassDocumentValidation;
        private CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private bool _isOrdered = true;
        private int? _maxBatchCount;
        private int? _maxBatchLength;
        private MessageEncoderSettings _messageEncoderSettings;
        private List<TWriteRequest> _requests;
        private bool _retryRequested;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        protected BulkUnmixedWriteOperationBase(
            CollectionNamespace collectionNamespace,
            IEnumerable<TWriteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : this(collectionNamespace, Ensure.IsNotNull(requests, nameof(requests)).ToList(), messageEncoderSettings)
        {
        }

        protected BulkUnmixedWriteOperationBase(
            CollectionNamespace collectionNamespace,
            List<TWriteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _requests = Ensure.IsNotNull(requests, nameof(requests));
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
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

        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        public int? MaxBatchLength
        {
            get { return _maxBatchLength; }
            set { _maxBatchLength = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public string OperationName => "bulkWrite";

        public IEnumerable<TWriteRequest> Requests
        {
            get { return _requests; }
        }

        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        // public methods
        public BulkWriteOperationResult Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            EnsureHintIsSupportedIfAnyRequestHasHint();

            return ExecuteBatches(operationContext, context);
        }

        public BulkWriteOperationResult Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            using (var context = RetryableWriteContext.Create(operationContext, binding, IsOperationRetryable()))
            {
                return Execute(operationContext, context);
            }
        }

        public Task<BulkWriteOperationResult> ExecuteAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            EnsureHintIsSupportedIfAnyRequestHasHint();

            return ExecuteBatchesAsync(operationContext, context);
        }

        public async Task<BulkWriteOperationResult> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using (BeginOperation())
            using (var context = await RetryableWriteContext.CreateAsync(operationContext, binding, IsOperationRetryable()).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        // protected methods
        protected abstract IRetryableWriteOperation<BsonDocument> CreateBatchOperation(Batch batch);

        protected abstract bool RequestHasHint(TWriteRequest request);

        // private methods
        private bool IsOperationRetryable()
            => _retryRequested && _requests.All(r => r.IsRetryable());

        private EventContext.OperationIdDisposer BeginOperation() =>
            EventContext.BeginOperation(null, _requests.FirstOrDefault()?.RequestType.ToString().ToLower());

        private BulkWriteBatchResult CreateBatchResult(
            Batch batch,
            BsonDocument writeCommandResult,
            MongoWriteConcernException writeConcernException)
        {
            var requests = batch.Requests;
            var requestsInBatch = requests.GetProcessedItems();
            var indexMap = new IndexMap.RangeBased(0, requests.Offset, requests.Count);
            return BulkWriteBatchResult.Create(
                _isOrdered,
                requestsInBatch,
                writeCommandResult,
                indexMap,
                writeConcernException);
        }

        private void EnsureHintIsSupportedIfAnyRequestHasHint()
        {
            foreach (var request in _requests)
            {
                if (RequestHasHint(request) && !_writeConcern.IsAcknowledged)
                {
                    throw new NotSupportedException("Hint is not supported for unacknowledged writes.");
                }
            }
        }

        private BulkWriteBatchResult ExecuteBatch(OperationContext operationContext, RetryableWriteContext context, Batch batch)
        {
            var operation = CreateBatchOperation(batch);
            BsonDocument operationResult;
            MongoWriteConcernException writeConcernException = null;
            try
            {
                operationResult = RetryableWriteOperationExecutor.Execute(operationContext, operation, context);
            }
            catch (MongoWriteConcernException exception) when (exception.IsWriteConcernErrorOnly())
            {
                operationResult = exception.Result;
                writeConcernException = exception;
            }

            return CreateBatchResult(batch, operationResult, writeConcernException);
        }

        private async Task<BulkWriteBatchResult> ExecuteBatchAsync(OperationContext operationContext, RetryableWriteContext context, Batch batch)
        {
            var operation = CreateBatchOperation(batch);
            BsonDocument operationResult;
            MongoWriteConcernException writeConcernException = null;
            try
            {
                operationResult = await RetryableWriteOperationExecutor.ExecuteAsync(operationContext, operation, context).ConfigureAwait(false);
            }
            catch (MongoWriteConcernException exception) when (exception.IsWriteConcernErrorOnly())
            {
                operationResult = exception.Result;
                writeConcernException = exception;
            }

            return CreateBatchResult(batch, operationResult, writeConcernException);
        }

        private BulkWriteOperationResult ExecuteBatches(OperationContext operationContext, RetryableWriteContext context)
        {
            var helper = new BatchHelper(_requests, _writeConcern, _isOrdered);
            foreach (var batch in helper.GetBatches())
            {
                batch.Result = ExecuteBatch(operationContext, context, batch);
            }
            return helper.CreateFinalResultOrThrow(context.Channel);
        }

        private async Task<BulkWriteOperationResult> ExecuteBatchesAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            var helper = new BatchHelper(_requests, _writeConcern, _isOrdered);
            foreach (var batch in helper.GetBatches())
            {
                batch.Result = await ExecuteBatchAsync(operationContext, context, batch).ConfigureAwait(false);
            }
            return helper.CreateFinalResultOrThrow(context.Channel);
        }

        // nested types
        private class BatchHelper
        {
            private readonly List<BulkWriteBatchResult> _batchResults = new List<BulkWriteBatchResult>();
            private bool _hasWriteErrors;
            private readonly bool _isOrdered;
            private readonly BatchableSource<TWriteRequest> _requests;
            private readonly WriteConcern _writeConcern;

            public BatchHelper(IReadOnlyList<TWriteRequest> requests, WriteConcern writeConcern, bool isOrdered)
            {
                _requests = new BatchableSource<TWriteRequest>(requests, 0, requests.Count, canBeSplit: true);
                _writeConcern = writeConcern;
                _isOrdered = isOrdered;
            }

            public IEnumerable<Batch> GetBatches()
            {
                while (_requests.Count > 0 && ShouldContinue())
                {
                    var batch = new Batch
                    {
                        Requests = _requests
                    };

                    yield return batch;

                    _batchResults.Add(batch.Result);
                    _hasWriteErrors |= batch.Result.HasWriteErrors;

                    _requests.AdvancePastProcessedItems();
                }
            }

            public BulkWriteOperationResult CreateFinalResultOrThrow(IChannelHandle channel)
            {
                var combiner = new BulkWriteBatchResultCombiner(_batchResults, _writeConcern.IsAcknowledged);
                var remainingRequests = _requests.GetUnprocessedItems();
                return combiner.CreateResultOrThrowIfHasErrors(channel.ConnectionDescription.ConnectionId, remainingRequests);
            }

            // private methods
            private bool ShouldContinue()
            {
                return !_hasWriteErrors || !_isOrdered;
            }
        }

        protected class Batch
        {
            public BatchableSource<TWriteRequest> Requests;
            public BulkWriteBatchResult Result;
        }
    }
}
