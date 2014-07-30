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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal class BulkMixedWriteOperation
    {
        // private fields
        private readonly Action<InsertRequest> _assignId;
        private readonly bool _checkElementNames;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly bool _isOrdered;
        private readonly int _maxBatchCount;
        private readonly int _maxBatchLength;
        private readonly IEnumerable<WriteRequest> _requests;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly WriteConcern _writeConcern;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        public BulkMixedWriteOperation(
            Action<InsertRequest> assignId,
            bool checkElementNames,
            string collectionName,
            string databaseName,
            int maxBatchCount,
            int maxBatchLength,
            bool isOrdered,
            BsonBinaryReaderSettings readerSettings,
            IEnumerable<WriteRequest> requests,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
        {
            _assignId = assignId;
            _checkElementNames = checkElementNames;
            _collectionName = collectionName;
            _databaseName = databaseName;
            _maxBatchCount = maxBatchCount;
            _maxBatchLength = maxBatchLength;
            _isOrdered = isOrdered;
            _readerSettings = readerSettings;
            _requests = requests;
            _writeConcern = writeConcern;
            _writerSettings = writerSettings;
        }

        // public methods
        public BulkWriteResult Execute(MongoConnection connection)
        {
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

                var batchResult = ExecuteBatch(connection, run);
                batchResults.Add(batchResult);

                hasWriteErrors |= batchResult.HasWriteErrors;
            }

            if (runCount == 0)
            {
                throw new InvalidOperationException("Bulk write operation is empty.");
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, _writeConcern.Enabled);
            return combiner.CreateResultOrThrowIfHasErrors(remainingRequests);
        }

        // private methods
        private BulkWriteBatchResult ExecuteBatch(MongoConnection connection, Run run)
        {
            BulkWriteResult result;
            BulkWriteException exception = null;
            try
            {
                switch (run.RequestType)
                {
                    case WriteRequestType.Delete:
                        result = ExecuteDeletes(connection, run.Requests.Cast<DeleteRequest>());
                        break;
                    case WriteRequestType.Insert:
                        result = ExecuteInserts(connection, run.Requests.Cast<InsertRequest>());
                        break;
                    case WriteRequestType.Update:
                        result = ExecuteUpdates(connection, run.Requests.Cast<UpdateRequest>());
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

        private BulkWriteResult ExecuteDeletes(MongoConnection connection, IEnumerable<DeleteRequest> requests)
        {
            var operation = new BulkDeleteOperation(new BulkDeleteOperationArgs(
                _collectionName,
                _databaseName,
                _maxBatchCount,
                _maxBatchLength,
                _isOrdered,
                _readerSettings,
                requests,
                _writeConcern,
                _writerSettings));
            return operation.Execute(connection);
        }

        private BulkWriteResult ExecuteInserts(MongoConnection connection, IEnumerable<InsertRequest> requests)
        {
            var operation = new BulkInsertOperation(new BulkInsertOperationArgs(
                _assignId,
                _checkElementNames,
                _collectionName,
                _databaseName,
                _maxBatchCount,
                _maxBatchLength,
                _isOrdered,
                _readerSettings,
                requests,
                _writeConcern,
                _writerSettings));
            return operation.Execute(connection);
        }

        private BulkWriteResult ExecuteUpdates(MongoConnection connection, IEnumerable<UpdateRequest> requests)
        {
            var operation = new BulkUpdateOperation(new BulkUpdateOperationArgs(
                _checkElementNames,
                _collectionName,
                _databaseName,
                _maxBatchCount,
                _maxBatchLength,
                _isOrdered,
                _readerSettings,
                requests,
                _writeConcern,
                _writerSettings));
            return operation.Execute(connection);
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

        // nested classes
        private class Run
        {
            // private fields
            private IndexMap _indexMap = new IndexMap.RangeBased();
            private readonly List<WriteRequest> _requests = new List<WriteRequest>();

            // public properties
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

            // public methods
            public void Add(WriteRequest request, int originalIndex)
            {
                var index = _requests.Count;
                _indexMap = _indexMap.Add(index, originalIndex);
                _requests.Add(request);
            }
        }
    }
}
