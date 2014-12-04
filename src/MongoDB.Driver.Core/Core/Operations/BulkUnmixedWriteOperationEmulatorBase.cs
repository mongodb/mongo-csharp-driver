/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class BulkUnmixedWriteOperationEmulatorBase
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private bool _isOrdered = true;
        private int? _maxBatchCount;
        private int? _maxBatchLength;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IEnumerable<WriteRequest> _requests;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        protected BulkUnmixedWriteOperationEmulatorBase(
            CollectionNamespace collectionNamespace,
            IEnumerable<WriteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _requests = Ensure.IsNotNull(requests, "requests");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public int? MaxBatchLength
        {
            get { return _maxBatchLength; }
            set { _maxBatchLength = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        public IEnumerable<WriteRequest> Requests
        {
            get { return _requests; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        protected virtual async Task<BulkWriteBatchResult> EmulateSingleRequestAsync(IChannelHandle channel, WriteRequest request, int originalIndex, CancellationToken cancellationToken)
        {
            WriteConcernResult writeConcernResult = null;
            MongoWriteConcernException writeConcernException = null;
            try
            {
                writeConcernResult = await ExecuteProtocolAsync(channel, request, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWriteConcernException ex)
            {
                writeConcernResult = ex.WriteConcernResult;
                writeConcernException = ex;
            }

            var indexMap = new IndexMap.RangeBased(0, originalIndex, 1);
            return BulkWriteBatchResult.Create(
                request,
                writeConcernResult,
                writeConcernException,
                indexMap);
        }

        public async Task<BulkWriteOperationResult> ExecuteAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            var batchResults = new List<BulkWriteBatchResult>();
            var remainingRequests = new List<WriteRequest>();
            var hasWriteErrors = false;

            var originalIndex = 0;
            foreach (WriteRequest request in _requests)
            {
                if (hasWriteErrors && _isOrdered)
                {
                    remainingRequests.Add(request);
                    continue;
                }

                var batchResult = await EmulateSingleRequestAsync(channel, request, originalIndex, cancellationToken).ConfigureAwait(false);
                batchResults.Add(batchResult);

                hasWriteErrors |= batchResult.HasWriteErrors;
                originalIndex++;
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, _writeConcern.IsAcknowledged);
            return combiner.CreateResultOrThrowIfHasErrors(channel.ConnectionDescription.ConnectionId, remainingRequests);
        }

        protected abstract Task<WriteConcernResult> ExecuteProtocolAsync(IChannelHandle channel, WriteRequest request, CancellationToken cancellationToken);
    }
}
