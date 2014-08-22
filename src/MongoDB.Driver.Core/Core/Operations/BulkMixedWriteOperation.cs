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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class BulkMixedWriteOperation : IWriteOperation<BulkWriteResult>
    {
        #region static
        // static fields
        private static readonly IReadOnlyList<WriteRequest> __noWriteRequests = new WriteRequest[0];
        #endregion

        // fields
        private Action<InsertRequest> _assignId;
        private bool _checkElementNames = true;
        private string _collectionName;
        private string _databaseName;
        private bool _isOrdered = true;
        private int _maxBatchCount = 0;
        private int _maxBatchLength = int.MaxValue;
        private int _maxDocumentSize = int.MaxValue;
        private int _maxWireDocumentSize = int.MaxValue;
        private IEnumerable<WriteRequest> _requests;
        private BsonBinaryReaderSettings _readerSettings = BsonBinaryReaderSettings.Defaults;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;
        private BsonBinaryWriterSettings _writerSettings = BsonBinaryWriterSettings.Defaults;

        // constructors
        public BulkMixedWriteOperation(
            string databaseName,
            string collectionName,
            IEnumerable<WriteRequest> requests)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _requests = Ensure.IsNotNull(requests, "requests");
        }

        // properties
        public Action<InsertRequest> AssignId
        {
            get { return _assignId; }
            set { _assignId = value; }
        }

        public bool CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

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

        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
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

        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsGreaterThanOrEqualToZero(value, "value"); }
        }

        public int MaxWireDocumentSize
        {
            get { return _maxWireDocumentSize; }
            set { _maxWireDocumentSize = Ensure.IsGreaterThanOrEqualToZero(value, "value"); }
        }

        public IEnumerable<WriteRequest> Requests
        {
            get { return _requests; }
            set {  _requests = Ensure.IsNotNull(value, "value"); }
        }

        public BsonBinaryReaderSettings ReaderSettings
        {
            get { return _readerSettings; }
            set { _readerSettings = Ensure.IsNotNull(value, "value"); }
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
        public async Task<BulkWriteResult> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var batchResults = new List<BulkWriteBatchResult>();
            var remainingRequests = Enumerable.Empty<WriteRequest>();
            var hasWriteErrors = false;

            var runCount = 0;
            foreach (var run in FindRuns())
            {
                runCount++;

                if (hasWriteErrors && _isOrdered)
                {
                    remainingRequests = remainingRequests.Concat(run.Requests);
                    continue;
                }

                var batchResult = await ExecuteBatchAsync(connection, run, slidingTimeout, cancellationToken);
                batchResults.Add(batchResult);

                hasWriteErrors |= batchResult.HasWriteErrors;
            }

            if (runCount == 0)
            {
                throw new InvalidOperationException("Bulk write operation is empty.");
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, !_writeConcern.Equals(WriteConcern.Unacknowledged));
            return combiner.CreateResultOrThrowIfHasErrors(remainingRequests.ToList());
        }

        public async Task<BulkWriteResult> ExecuteAsync(IWriteBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }

        private async Task<BulkWriteBatchResult> ExecuteBatchAsync(IConnectionHandle connection, Run run, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BulkWriteResult result;
            BulkWriteException exception = null;
            try
            {
                switch (run.RequestType)
                {
                    case WriteRequestType.Delete:
                        result = await ExecuteDeletesAsync(connection, run.Requests.Cast<DeleteRequest>(), timeout, cancellationToken);
                        break;
                    case WriteRequestType.Insert:
                        result = await ExecuteInsertsAsync(connection, run.Requests.Cast<InsertRequest>(), timeout, cancellationToken);
                        break;
                    case WriteRequestType.Update:
                        result = await ExecuteUpdatesAsync(connection, run.Requests.Cast<UpdateRequest>(), timeout, cancellationToken);
                        break;
                    default:
                        throw new MongoInternalException("Unrecognized RequestType.");
                }
            }
            catch (BulkWriteException ex)
            {
                result = ex.Result;
                exception = ex;
            }

            return BulkWriteBatchResult.Create(result, exception, run.IndexMap);
        }

        private Task<BulkWriteResult> ExecuteDeletesAsync(IConnectionHandle connection, IEnumerable<DeleteRequest> requests, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var operation = new BulkDeleteOperation(_databaseName, _collectionName, requests)
            {
                MaxBatchCount = _maxBatchCount,
                MaxBatchLength = _maxBatchLength,
                ReaderSettings = _readerSettings,
                WriteConcern = _writeConcern,
                WriterSettings = _writerSettings
            };
            return operation.ExecuteAsync(connection, timeout, cancellationToken);
        }

        private Task<BulkWriteResult> ExecuteInsertsAsync(IConnectionHandle connection, IEnumerable<InsertRequest> requests, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var operation = new BulkInsertOperation(_databaseName, _collectionName, requests)
            {
                AssignId = _assignId,
                CheckElementNames = _checkElementNames,
                MaxBatchCount = _maxBatchCount,
                MaxBatchLength = _maxBatchLength,
                IsOrdered = _isOrdered,
                ReaderSettings = _readerSettings,
                WriteConcern = _writeConcern,
                WriterSettings = _writerSettings
            };
            return operation.ExecuteAsync(connection, timeout, cancellationToken);
        }

        private Task<BulkWriteResult> ExecuteUpdatesAsync(IConnectionHandle connection, IEnumerable<UpdateRequest> requests, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var operation = new BulkUpdateOperation(_databaseName, _collectionName, requests)
            {
                CheckElementNames = _checkElementNames,
                MaxBatchCount = _maxBatchCount,
                MaxBatchLength = _maxBatchLength,
                IsOrdered = _isOrdered,
                ReaderSettings = _readerSettings,
                WriteConcern = _writeConcern,
                WriterSettings = _writerSettings
            };
            return operation.ExecuteAsync(connection, timeout, cancellationToken);
        }

        private IEnumerable<Run> FindOrderedRuns()
        {
            Run run = null;

            var originalIndex = 0;
            foreach (var request in _requests)
            {
                if (run == null)
                {
                    run = new Run();
                    run.Add(request, originalIndex);
                }
                else if (run.RequestType == request.RequestType)
                {
                    if (run.Count == _maxBatchCount)
                    {
                        yield return run;
                        run = new Run();
                    }
                    run.Add(request, originalIndex);
                }
                else
                {
                    yield return run;
                    run = new Run();
                    run.Add(request, originalIndex);
                }

                originalIndex++;
            }

            if (run != null)
            {
                yield return run;
            }
        }

        private IEnumerable<Run> FindRuns()
        {
            if (_isOrdered)
            {
                return FindOrderedRuns();
            }
            else
            {
                return FindUnorderedRuns();
            }
        }

        private IEnumerable<Run> FindUnorderedRuns()
        {
            var runs = new List<Run>();

            var originalIndex = 0;
            foreach (var request in _requests)
            {
                var run = runs.FirstOrDefault(r => r.RequestType == request.RequestType);

                if (run == null)
                {
                    run = new Run();
                    runs.Add(run);
                }
                else if (run.Count == _maxBatchCount)
                {
                    yield return run;
                    runs.Remove(run);
                    run = new Run();
                    runs.Add(run);
                }

                run.Add(request, originalIndex);
                originalIndex++;
            }

            foreach (var run in runs)
            {
                yield return run;
            }
        }

        // nested types
        private class Run
        {
            // fields
            private IndexMap _indexMap = new IndexMap.RangeBased();
            private readonly List<WriteRequest> _requests = new List<WriteRequest>();

            // properties
            public int Count
            {
                get { return _requests.Count; }
            }

            public IndexMap IndexMap
            {
                get { return _indexMap; }
            }

            public List<WriteRequest> Requests
            {
                get { return _requests; }
            }

            public WriteRequestType RequestType
            {
                get { return _requests[0].RequestType; }
            }

            // methods
            public void Add(WriteRequest request, int originalIndex)
            {
                var index = _requests.Count;
                _indexMap = _indexMap.Add(index, originalIndex);
                _requests.Add(request);
            }
        }
    }
}
