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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ClientBulkWriteOperation : RetryableWriteCommandOperationBase, IWriteOperation<BulkWriteResults>
    {
        private readonly Dictionary<int, BsonValue> _idsMap = new();

        public ClientBulkWriteOperation(
            IReadOnlyList<BulkWriteModel> writeModels,
            ClientBulkWriteOptions options,
            MessageEncoderSettings messageEncoderSettings,
            RenderArgs<BsonDocument> renderArgs)
            : base(DatabaseNamespace.Admin, messageEncoderSettings)
        {
            Ensure.IsNotNullOrEmpty(writeModels, nameof(writeModels));
            WriteModels = new BatchableSource<BulkWriteModel>(writeModels, true);
            BypassDocumentValidation = options?.BypassDocumentValidation;
            Comment = options?.Comment;
            IsOrdered = (options?.IsOrdered).GetValueOrDefault(true);
            ErrorsOnly = !(options?.VerboseResult).GetValueOrDefault(false);
            Let = options?.Let;
            WriteConcern = options?.WriteConcern;
            RenderArgs = renderArgs;
        }

        public bool? BypassDocumentValidation { get; init; }

        public bool ErrorsOnly { get; init; }

        public BsonDocument Let { get; init; }

        public RenderArgs<BsonDocument> RenderArgs { get; init; }

        public IBatchableSource<BulkWriteModel> WriteModels { get; }

        protected override BsonDocument CreateCommand(ICoreSessionHandle session, int attempt, long? transactionNumber)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(session, WriteConcern);
            return new BsonDocument
            {
                { "bulkWrite", 1 },
                { "errorsOnly", ErrorsOnly },
                { "ordered", IsOrdered },
                { "bypassDocumentValidation", () => BypassDocumentValidation, BypassDocumentValidation.HasValue },
                { "comment", Comment, Comment != null },
                { "let", Let, Let != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue }
            };
        }

        protected override IEnumerable<BatchableCommandMessageSection> CreateCommandPayloads(IChannelHandle channel, int attempt)
        {
            IBatchableSource<BulkWriteModel> operations;
            if (attempt == 1)
            {
                operations = WriteModels;
            }
            else
            {
                operations = new BatchableSource<BulkWriteModel>(WriteModels.Items, WriteModels.Offset, WriteModels.ProcessedCount, canBeSplit: false);
            }
            var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, channel.ConnectionDescription.MaxBatchCount);
            var maxDocumentSize = channel.ConnectionDescription.MaxDocumentSize;
            var payload = new ClientBulkWriteOpsCommandMessageSection(operations, _idsMap, maxBatchCount, maxDocumentSize, RenderArgs);
            return new[] { payload };
        }

        public new BulkWriteResults Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            var bulkWriteResults = new BulkWriteRawResults();
            while (true)
            {
                using var context = RetryableWriteContext.Create(binding, false, cancellationToken);
                BsonDocument serverResponse = null;
                try
                {
                    serverResponse = base.Execute(context, cancellationToken);
                }
                catch (MongoWriteConcernException concernException)
                {
                    bulkWriteResults.ConcernErrors.Add(concernException);
                    serverResponse = concernException.WriteConcernResult.Response;
                }
                catch (MongoCommandException commandException) when (commandException.Result != null)
                {
                    bulkWriteResults.TopLevelException = commandException;
                    serverResponse = commandException.Result;
                }
                catch (Exception exception)
                {
                    bulkWriteResults.TopLevelException = exception;
                }

                if (serverResponse != null)
                {
                    PopulateBulkWriteResponse(serverResponse, bulkWriteResults);
                    using var individualResults = GetIndividualResultsCursor(context, serverResponse);
                    if (individualResults != null && bulkWriteResults.TopLevelException == null)
                    {
                        try
                        {
                            while (individualResults.MoveNext(cancellationToken))
                            {
                                PopulateIndividualResponses(individualResults.Current, bulkWriteResults);
                            }
                        }
                        catch (Exception e)
                        {
                            bulkWriteResults.TopLevelException = e;
                        }
                    }
                }

                WriteModels.AdvancePastProcessedItems();
                EnsureCanProceedNextBatch(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);

                if (WriteModels.AllItemsWereProcessed)
                {
                    return ToFinalResultsOrThrow(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);
                }
            }
        }

        public new async Task<BulkWriteResults> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            var bulkWriteResults = new BulkWriteRawResults();
            while (true)
            {
                using var context = RetryableWriteContext.Create(binding, false, cancellationToken);
                BsonDocument serverResponse = null;
                try
                {
                    serverResponse = await base.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                }
                catch (MongoWriteConcernException concernException)
                {
                    bulkWriteResults.ConcernErrors.Add(concernException);
                    serverResponse = concernException.WriteConcernResult.Response;
                }
                catch (MongoCommandException commandException) when (commandException.Result != null)
                {
                    bulkWriteResults.TopLevelException = commandException;
                    serverResponse = commandException.Result;
                }
                catch (Exception exception)
                {
                    bulkWriteResults.TopLevelException = exception;
                }

                if (serverResponse != null)
                {
                    PopulateBulkWriteResponse(serverResponse, bulkWriteResults);
                    using var individualResults = GetIndividualResultsCursor(context, serverResponse);
                    if (individualResults != null && bulkWriteResults.TopLevelException == null)
                    {
                        try
                        {
                            while (await individualResults.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                            {
                                PopulateIndividualResponses(individualResults.Current, bulkWriteResults);
                            }
                        }
                        catch (Exception e)
                        {
                            bulkWriteResults.TopLevelException = e;
                        }
                    }
                }

                WriteModels.AdvancePastProcessedItems();
                EnsureCanProceedNextBatch(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);

                if (WriteModels.AllItemsWereProcessed)
                {
                    return ToFinalResultsOrThrow(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);
                }
            }
        }

        private void EnsureCanProceedNextBatch(ConnectionId connectionId, BulkWriteRawResults bulkWriteResults)
        {
            if (bulkWriteResults.TopLevelException != null)
            {
                var partialResults = ToBulkResults(bulkWriteResults);
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during bulkWrite operation. See InnerException for more details.",
                    bulkWriteResults.Errors,
                    partialResults,
                    bulkWriteResults.ConcernErrors,
                    bulkWriteResults.TopLevelException);
            }

            if (bulkWriteResults.Errors.Count > 0 && IsOrdered)
            {
                var partialResults = ToBulkResults(bulkWriteResults);
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during ordered bulkWrite operation. See WriteErrors for more details.",
                    bulkWriteResults.Errors,
                    partialResults,
                    bulkWriteResults.ConcernErrors);
            }
        }

        private BulkWriteResults ToFinalResultsOrThrow(ConnectionId connectionId, BulkWriteRawResults bulkWriteResults)
        {
            var results = ToBulkResults(bulkWriteResults);

            if (bulkWriteResults.Errors.Count > 0)
            {
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during bulkWrite operation. See WriteErrors for more details.",
                    bulkWriteResults.Errors,
                    results,
                    bulkWriteResults.ConcernErrors);
            }

            if (bulkWriteResults.ConcernErrors.Count > 0)
            {
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during bulkWrite operation. See WriteConcernErrors for more details.",
                    bulkWriteResults.Errors,
                    results,
                    bulkWriteResults.ConcernErrors);
            }

            return results;
        }

        private IAsyncCursor<BsonDocument> GetIndividualResultsCursor(RetryableWriteContext context, BsonDocument bulkWriteResponse)
        {
            if (!bulkWriteResponse.TryGetElement("cursor", out var cursorElement))
            {
                return null;
            }

            var cursorDocument = cursorElement.Value.AsBsonDocument;
            return new AsyncCursor<BsonDocument>(
                context.ChannelSource,
                CollectionNamespace.FromFullName(cursorDocument["ns"].AsString),
                null,
                cursorDocument["firstBatch"].AsBsonArray.Cast<BsonDocument>().ToList(),
                cursorDocument["id"].AsInt64,
                0,
                0,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings);
        }

        private void PopulateBulkWriteResponse(BsonDocument bulkWriteResponse, BulkWriteRawResults bulkWriteResults)
        {
            if (bulkWriteResponse.TryGetValue("nErrors", out var errorCount))
            {
                bulkWriteResults.ErrorCount += errorCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nDeleted", out var deletedCount))
            {
                bulkWriteResults.DeletedCount += deletedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nInserted", out var insertedCount))
            {
                bulkWriteResults.InsertedCount += insertedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nMatched", out var matchedCount))
            {
                bulkWriteResults.MatchedCount += matchedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nModified", out var modifiedCount))
            {
                bulkWriteResults.ModifiedCount += modifiedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nUpserted", out var upsertedCount))
            {
                bulkWriteResults.UpsertedCount += upsertedCount.AsInt32;
            }
        }

        private void PopulateIndividualResponses(IEnumerable<BsonDocument> individualResponses, BulkWriteRawResults bulkWriteResults)
        {
            foreach (var operationResponse in individualResponses)
            {
                var isSucceeded = operationResponse["ok"].AsDouble != 0;
                var operationIndex = WriteModels.Offset + operationResponse["idx"].AsInt32;

                if (isSucceeded)
                {
                    var writeModel = WriteModels.Items[operationIndex];
                    var writeModelType = writeModel.GetType().GetGenericTypeDefinition();

                    if (writeModelType == typeof(BulkWriteInsertOneModel<>))
                    {
                        _idsMap.TryGetValue(operationIndex, out var insertedId);
                        bulkWriteResults.InsertResults.Add(operationIndex, new()
                        {
                            InsertedId = insertedId
                        });
                    }
                    else if (writeModelType == typeof(BulkWriteUpdateOneModel<>) || writeModelType == typeof(BulkWriteUpdateManyModel<>) || writeModelType == typeof(BulkWriteReplaceOneModel<>))
                    {
                        operationResponse.TryGetValue("upserted", out var upsertedId);
                        bulkWriteResults.UpdateResults.Add(operationIndex, new()
                        {
                            MatchedCount = operationResponse["n"].AsInt32,
                            ModifiedCount = operationResponse["nModified"].AsInt32,
                            UpsertedId = upsertedId?.AsBsonDocument["_id"],
                        });
                    }
                    else if (writeModelType == typeof(BulkWriteDeleteOneModel<>) || writeModelType == typeof(BulkWriteDeleteManyModel<>))
                    {
                        bulkWriteResults.DeleteResults.Add(operationIndex, new()
                        {
                            DeletedCount = operationResponse["n"].AsInt32,
                        });
                    }
                }
                else
                {
                    BsonDocument errInfo = null;
                    if (operationResponse.TryGetElement("errInfo", out var errInfoElement))
                    {
                        errInfo = errInfoElement.Value.AsBsonDocument;
                    }

                    var operationError = new BulkWriteOperationError(
                        operationIndex,
                        operationResponse["code"].AsInt32,
                        operationResponse["errmsg"]?.AsString,
                        errInfo);

                    bulkWriteResults.Errors.Add(
                        operationIndex,
                        new WriteError(operationError.Category, operationError.Code, operationError.Message, operationError.Details));
                }
            }
        }

        private BulkWriteResults ToBulkResults(BulkWriteRawResults rawResults)
        {
            if (WriteConcern?.Equals(WriteConcern.Unacknowledged) == true)
            {
                return new BulkWriteResults.Unacknowledged();
            }

            if (IsOrdered && (rawResults.Errors.Count > 0 && rawResults.Errors.First().Key == 0))
            {
                return null;
            }

            if (!IsOrdered && rawResults.Errors.Count == WriteModels.Items.Count)
            {
                return null;
            }

            return new BulkWriteResults.Acknowledged
            {
                InsertedCount = rawResults.InsertedCount,
                DeletedCount = rawResults.DeletedCount,
                MatchedCount = rawResults.MatchedCount,
                ModifiedCount = rawResults.ModifiedCount,
                UpsertedCount = rawResults.UpsertedCount,
                DeleteResults = rawResults.DeleteResults,
                InsertResults = rawResults.InsertResults,
                UpdateResults = rawResults.UpdateResults
            };
        }

        private class BulkWriteRawResults
        {
            public long DeletedCount { get; set; }
            public long ErrorCount { get; set; }
            public long InsertedCount { get; set; }
            public long MatchedCount { get; set; }
            public long ModifiedCount { get; set; }
            public long UpsertedCount { get; set; }
            public Exception TopLevelException { get; set; }
            public List<MongoWriteConcernException> ConcernErrors { get; set; } = new();
            public Dictionary<int, BulkWriteDeleteResult> DeleteResults { get; } = new();
            public Dictionary<int, WriteError> Errors { get; } = new();
            public Dictionary<int, BulkWriteInsertOneResult> InsertResults { get; } = new();
            public Dictionary<int, BulkWriteUpdateResult> UpdateResults { get; } = new();
        }
    }
}
