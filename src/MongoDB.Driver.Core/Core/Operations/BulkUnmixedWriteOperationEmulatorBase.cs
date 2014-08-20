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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class BulkUnmixedWriteOperationEmulatorBase
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private bool _isOrdered = true;
        private int _maxBatchCount = 0;
        private int _maxBatchLength = int.MaxValue;
        private BsonBinaryReaderSettings _readerSettings = BsonBinaryReaderSettings.Defaults;
        private IEnumerable<WriteRequest> _requests;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;
        private BsonBinaryWriterSettings _writerSettings = BsonBinaryWriterSettings.Defaults;

        // constructors
        protected BulkUnmixedWriteOperationEmulatorBase(
            string databaseName,
            string collectionName,
            IEnumerable<WriteRequest> requests)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _requests = Ensure.IsNotNull(requests, "requests");
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsGreaterThanOrEqualToZero(value, "value"); }
        }

        public int MaxBatchLength
        {
            get { return _maxBatchLength; }
            set { _maxBatchLength = Ensure.IsGreaterThanOrEqualToZero(value, "value"); }
        }

        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        public BsonBinaryReaderSettings ReaderSettings
        {
            get { return _readerSettings; }
            set { _readerSettings = Ensure.IsNotNull(value, "value"); }
        }

        public IEnumerable<WriteRequest> Requests
        {
            get { return _requests; }
            set { _requests = Ensure.IsNotNull(value, "value"); }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
            set { _writerSettings = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        protected abstract IWireProtocol<BsonDocument> CreateProtocol(IConnectionHandle connection, WriteRequest request);

        protected virtual async Task<BulkWriteBatchResult> EmulateSingleRequestAsync(IConnectionHandle connection, WriteRequest request, int originalIndex, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var protocol = CreateProtocol(connection, request);

            WriteConcernResult writeConcernResult = null;
            WriteConcernException writeConcernException = null;
            try
            {
                var protocolResult = await protocol.ExecuteAsync(connection, timeout, cancellationToken);
                if (protocolResult != null)
                {
                    writeConcernResult = new WriteConcernResult(protocolResult);
                }
            }
            catch (WriteException ex)
            {
                writeConcernResult = new WriteConcernResult(ex.Result);
                writeConcernException = new WriteConcernException(ex.Message, writeConcernResult);
            }

            var indexMap = new IndexMap.RangeBased(0, originalIndex, 1);
            return BulkWriteBatchResult.Create(
                request,
                writeConcernResult,
                writeConcernException,
                indexMap);
        }

        public async Task<BulkWriteResult> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);

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

                var batchResult = await EmulateSingleRequestAsync(connection, request, originalIndex, slidingTimeout, cancellationToken);
                batchResults.Add(batchResult);

                hasWriteErrors |= batchResult.HasWriteErrors;
                originalIndex++;
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, !_writeConcern.Equals(WriteConcern.Unacknowledged));
            return combiner.CreateResultOrThrowIfHasErrors(remainingRequests);
        }
    }
}
