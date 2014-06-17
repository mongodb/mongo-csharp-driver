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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    /// <summary>
    /// Represents the result of one batch executed using a write command.
    /// </summary>
    internal class BulkWriteBatchResult
    {
        // private static fields
        private static readonly IList<BulkWriteError> __noWriteErrors = new BulkWriteError[0];
        private static readonly IList<WriteRequest> __noWriteRequests = new WriteRequest[0];

        // private fields
        private readonly int _batchCount;
        private readonly long _deletedCount;
        private readonly IndexMap _indexMap;
        private readonly long _insertedCount;
        private readonly long _matchedCount;
        private readonly long? _modifiedCount;
        private readonly Batch<WriteRequest> _nextBatch;
        private readonly ReadOnlyCollection<WriteRequest> _processedRequests;
        private readonly ReadOnlyCollection<WriteRequest> _unprocessedRequests;
        private readonly ReadOnlyCollection<BulkWriteUpsert> _upserts;
        private readonly WriteConcernError _writeConcernError;
        private readonly ReadOnlyCollection<BulkWriteError> _writeErrors;

        // constructors
        public BulkWriteBatchResult(
            int batchCount,
            IEnumerable<WriteRequest> processedRequests,
            IEnumerable<WriteRequest> unprocessedRequests,
            long matchedCount,
            long deletedCount,
            long insertedCount,
            long? modifiedCount,
            IEnumerable<BulkWriteUpsert> upserts,
            IEnumerable<BulkWriteError> writeErrors,
            WriteConcernError writeConcernError,
            IndexMap indexMap,
            Batch<WriteRequest> nextBatch)
        {
            _batchCount = batchCount;
            _matchedCount = matchedCount;
            _deletedCount = deletedCount;
            _insertedCount = insertedCount;
            _modifiedCount = modifiedCount;
            _indexMap = indexMap;
            _nextBatch = nextBatch;
            _processedRequests = ToReadOnlyCollection(processedRequests);
            _writeErrors = ToReadOnlyCollection(writeErrors);
            _unprocessedRequests = ToReadOnlyCollection(unprocessedRequests);
            _upserts = ToReadOnlyCollection(upserts);
            _writeConcernError = writeConcernError;
        }

        // public properties
        public int BatchCount
        {
            get { return _batchCount; }
        }

        public long DeletedCount
        {
            get { return _deletedCount; }
        }

        public bool HasWriteConcernError
        {
            get { return _writeConcernError != null; }
        }

        public bool HasWriteErrors
        {
            get { return _writeErrors.Count > 0; }
        }

        public IndexMap IndexMap
        {
            get { return _indexMap; }
        }

        public long InsertedCount
        {
            get { return _insertedCount; }
        }

        public long MatchedCount
        {
            get { return _matchedCount; }
        }

        public long? ModifiedCount
        {
            get { return _modifiedCount; }
        }

        public Batch<WriteRequest> NextBatch
        {
            get { return _nextBatch; }
        }

        public ReadOnlyCollection<WriteRequest> ProcessedRequests
        {
            get { return _processedRequests; }
        }

        public ReadOnlyCollection<WriteRequest> UnprocessedRequests
        {
            get { return _unprocessedRequests; }
        }

        public ReadOnlyCollection<BulkWriteUpsert> Upserts
        {
            get { return _upserts; }
        }

        public WriteConcernError WriteConcernError
        {
            get { return _writeConcernError; }
        }

        public ReadOnlyCollection<BulkWriteError> WriteErrors
        {
            get { return _writeErrors; }
        }

        // public static methods
        public static BulkWriteBatchResult Create(
            BulkWriteResult result,
            BulkWriteException exception,
            IndexMap indexMap)
        {
            var matchedCount = 0L;
            var deletedCount = 0L;
            var insertedCount = 0L;
            long? modifiedCount = null;
            var upserts = Enumerable.Empty<BulkWriteUpsert>();
            if (result.IsAcknowledged)
            {
                matchedCount = result.MatchedCount;
                deletedCount = result.DeletedCount;
                insertedCount = result.InsertedCount;
                modifiedCount = result.IsModifiedCountAvailable ? (long?)result.ModifiedCount : null;
                upserts = result.Upserts;
            }

            var unprocessedRequests = __noWriteRequests;
            var writeErrors = __noWriteErrors;
            WriteConcernError writeConcernError = null;
            if (exception != null)
            {
                unprocessedRequests = exception.UnprocessedRequests;
                writeErrors = exception.WriteErrors;
                writeConcernError = exception.WriteConcernError;
            }

            return new BulkWriteBatchResult(
                result.RequestCount,
                result.ProcessedRequests,
                unprocessedRequests,
                matchedCount,
                deletedCount,
                insertedCount,
                modifiedCount,
                upserts,
                writeErrors,
                writeConcernError,
                indexMap,
                null);
        }

        public static BulkWriteBatchResult Create(
            bool isOrdered,
            IList<WriteRequest> requests,
            BsonDocument writeCommandResponse,
            IndexMap indexMap,
            Batch<WriteRequest> nextBatch)
        {
            var writeErrors = CreateWriteErrors(writeCommandResponse);
            var writeConcernError = CreateWriteConcernError(writeCommandResponse);
            var processedRequests = CreateProcessedRequests(requests, writeErrors, isOrdered);
            var unprocessedRequests = CreateUnprocessedRequests(requests, writeErrors, isOrdered);
            var upserts = CreateUpserts(writeCommandResponse);

            var n = writeCommandResponse.GetValue("n", 0).ToInt64();
            var matchedCount = 0L;
            var deletedCount = 0L;
            var insertedCount = 0L;
            long? modifiedCount = 0L;
            var firstRequest = requests.FirstOrDefault();
            if (firstRequest != null)
            {
                switch (firstRequest.RequestType)
                {
                    case WriteRequestType.Delete:
                        deletedCount = n;
                        break;
                    case WriteRequestType.Insert:
                        insertedCount = n;
                        break;
                    case WriteRequestType.Update:
                        matchedCount = n - upserts.Count();
                        BsonValue nModified;
                        if (writeCommandResponse.TryGetValue("nModified", out nModified))
                        {
                            modifiedCount = nModified.ToInt64();
                        }
                        else
                        {
                            modifiedCount = null;
                        }
                        break;
                }
            }

            return new BulkWriteBatchResult(
                requests.Count,
                processedRequests,
                unprocessedRequests,
                matchedCount,
                deletedCount,
                insertedCount,
                modifiedCount,
                upserts,
                writeErrors,
                writeConcernError,
                indexMap,
                nextBatch);
        }

        public static BulkWriteBatchResult Create(
            WriteRequest request,
            WriteConcernResult writeConcernResult,
            WriteConcernException writeConcernException,
            IndexMap indexMap)
        {
            var processedRequests = new[] { request };
            var unprocessedRequests = Enumerable.Empty<WriteRequest>();
            BsonValue upsertId = null;
            var documentsAffected = 0L;

            if (writeConcernResult != null)
            {
                upsertId = writeConcernResult.Upserted;
                documentsAffected = writeConcernResult.DocumentsAffected;
                var updateRequest = request as UpdateRequest;

                if (upsertId == null &&
                    documentsAffected == 1 &&
                    updateRequest != null &&
                    updateRequest.IsUpsert.GetValueOrDefault(false) &&
                    !writeConcernResult.UpdatedExisting)
                {
                    // Get the _id field first from the Update document
                    // and then from the Query document.
                    upsertId =
                        updateRequest.Update.ToBsonDocument().GetValue("_id", null) ??
                        updateRequest.Query.ToBsonDocument().GetValue("_id", null);
                }
            }

            var upserts = (upsertId == null) ? Enumerable.Empty<BulkWriteUpsert>() : new[] { new BulkWriteUpsert(0, upsertId) };
            var writeErrors = __noWriteErrors;
            WriteConcernError writeConcernError = null;
            Batch<WriteRequest> nextBatch = null;

            if (writeConcernException != null)
            {
                var getLastErrorResponse = writeConcernResult.Response;
                if (IsGetLasterrorResponseAWriteConcernError(getLastErrorResponse))
                {
                    writeConcernError = CreateWriteConcernErrorFromGetLastErrorResponse(getLastErrorResponse);
                }
                else
                {
                    writeErrors = new[] { CreateWriteErrorFromGetLastErrorResponse(getLastErrorResponse) };
                }
            }

            if (request.RequestType == WriteRequestType.Insert && writeErrors.Count == 0)
            {
                documentsAffected = 1; // note: DocumentsAffected is 0 for inserts
            }

            var matchedCount = 0L;
            var deletedCount = 0L;
            var insertedCount = 0L;
            long? modifiedCount = 0L;
            switch (request.RequestType)
            {
                case WriteRequestType.Delete:
                    deletedCount = documentsAffected;
                    break;
                case WriteRequestType.Insert:
                    insertedCount = documentsAffected;
                    break;
                case WriteRequestType.Update:
                    matchedCount = documentsAffected - upserts.Count();
                    modifiedCount = null; // getLasterror does not report this value
                    break;
            }

            return new BulkWriteBatchResult(
                1, // batchCount
                processedRequests,
                unprocessedRequests,
                matchedCount,
                deletedCount,
                insertedCount,
                modifiedCount,
                upserts,
                writeErrors,
                writeConcernError,
                indexMap,
                nextBatch);
        }

        // private static methods
        private static IList<WriteRequest> CreateProcessedRequests(IList<WriteRequest> requests, IList<BulkWriteError> writeErrors, bool isOrdered)
        {
            if (!isOrdered || writeErrors.Count == 0)
            {
                return requests;
            }
            else
            {
                var processedCount = writeErrors.Max(e => e.Index) + 1;
                return requests.Take(processedCount).ToList();
            }
        }

        private static IList<WriteRequest> CreateUnprocessedRequests(IList<WriteRequest> requests, IList<BulkWriteError> writeErrors, bool isOrdered)
        {
            if (!isOrdered || writeErrors.Count == 0)
            {
                return __noWriteRequests;
            }
            else
            {
                var processedCount = writeErrors.Max(e => e.Index) + 1;
                return requests.Skip(processedCount).ToList();
            }
        }

        private static IList<BulkWriteUpsert> CreateUpserts(BsonDocument writeCommandResponse)
        {
            var upserts = new List<BulkWriteUpsert>();

            if (writeCommandResponse.Contains("upserted"))
            {
                foreach (BsonDocument value in writeCommandResponse["upserted"].AsBsonArray)
                {
                    var index = value["index"].ToInt32();
                    var id = value["_id"];
                    var upsert = new BulkWriteUpsert(index, id);
                    upserts.Add(upsert);
                }
            }

            return upserts;
        }

        private static WriteConcernError CreateWriteConcernError(BsonDocument writeCommandResponse)
        {
            if (writeCommandResponse.Contains("writeConcernError"))
            {
                var value = (BsonDocument)writeCommandResponse["writeConcernError"];
                var code = value["code"].ToInt32();
                var message = value["errmsg"].AsString;
                var details = (BsonDocument)value.GetValue("errInfo", null);
                return new WriteConcernError(code, message, details);
            }

            return null;
        }

        private static WriteConcernError CreateWriteConcernErrorFromGetLastErrorResponse(BsonDocument getLastErrorResponse)
        {
            var code = getLastErrorResponse.GetValue("code", 64).ToInt32(); // default = WriteConcernFailed

            string message = null;
            BsonValue value;
            if (getLastErrorResponse.TryGetValue("err", out value) && value.BsonType == BsonType.String)
            {
                message = value.AsString;
            }
            else if (getLastErrorResponse.TryGetValue("jnote", out value) && value.BsonType == BsonType.String)
            {
                message = value.AsString;
            }
            else if (getLastErrorResponse.TryGetValue("wnote", out value) && value.BsonType == BsonType.String)
            {
                message = value.AsString;
            }

            var details = new BsonDocument(getLastErrorResponse.Where(e => !new[] { "ok", "code", "err" }.Contains(e.Name)));

            return new WriteConcernError(code, message, details);
        }

        private static BulkWriteError CreateWriteErrorFromGetLastErrorResponse(BsonDocument getLastErrorResponse)
        {
            var code = getLastErrorResponse.GetValue("code", 8).ToInt32(); // default = UnknownError
            var message = (string)getLastErrorResponse.GetValue("err", null);
            return new BulkWriteError(0, code, message, null);
        }

        private static IList<BulkWriteError> CreateWriteErrors(BsonDocument writeCommandResponse)
        {
            var writeErrors = new List<BulkWriteError>();

            if (writeCommandResponse.Contains("writeErrors"))
            {
                foreach (BsonDocument value in writeCommandResponse["writeErrors"].AsBsonArray)
                {
                    var index = value["index"].ToInt32();
                    var code = value["code"].ToInt32();
                    var message = value["errmsg"].AsString;
                    var details = (BsonDocument)value.GetValue("errInfo", null);
                    var writeError = new BulkWriteError(index, code, message, details);
                    writeErrors.Add(writeError);
                }
            }

            return writeErrors;
        }

        private static bool IsGetLasterrorResponseAWriteConcernError(BsonDocument getLastErrorResponse)
        {
            return new[] { "wtimeout", "jnote", "wnote" }.Any(n => getLastErrorResponse.Contains(n));
        }

        private static ReadOnlyCollection<T> ToReadOnlyCollection<T>(IEnumerable<T> list)
        {
            return (list as ReadOnlyCollection<T>) ?? new ReadOnlyCollection<T>(list.ToList());
        }
    }
}
