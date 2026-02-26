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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ClientBulkWriteOperation : RetryableWriteCommandOperationBase, IWriteOperation<ClientBulkWriteResult>
    {
        private readonly bool? _bypassDocumentValidation;
        private readonly bool _errorsOnly;
        private readonly Dictionary<int, object> _idsMap = new();
        private readonly BsonDocument _let;
        private readonly RenderArgs<BsonDocument> _renderArgs;
        private readonly IBatchableSource<BulkWriteModel> _writeModels;

        public ClientBulkWriteOperation(
            IReadOnlyList<BulkWriteModel> writeModels,
            ClientBulkWriteOptions options,
            MessageEncoderSettings messageEncoderSettings,
            RenderArgs<BsonDocument> renderArgs)
            : base(DatabaseNamespace.Admin, messageEncoderSettings)
        {
            Ensure.IsNotNullOrEmpty(writeModels, nameof(writeModels));
            _writeModels = new BatchableSource<BulkWriteModel>(writeModels, true);
            _bypassDocumentValidation = options?.BypassDocumentValidation;
            _errorsOnly = !(options?.VerboseResult).GetValueOrDefault(false);
            _let = options?.Let;
            _renderArgs = renderArgs;
            Comment = options?.Comment;
            IsOrdered = (options?.IsOrdered).GetValueOrDefault(true);
            WriteConcern = options?.WriteConcern;
        }

        public override string OperationName => "bulkWrite";

        protected override BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, int attempt, long? transactionNumber)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, WriteConcern);
            return new BsonDocument
            {
                { "bulkWrite", 1 },
                { "errorsOnly", _errorsOnly },
                { "ordered", IsOrdered },
                { "bypassDocumentValidation", () => _bypassDocumentValidation, _bypassDocumentValidation.HasValue },
                { "comment", Comment, Comment != null },
                { "let", _let, _let != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue }
            };
        }

        protected override IEnumerable<BatchableCommandMessageSection> CreateCommandPayloads(IChannelHandle channel, int attempt)
        {
            IBatchableSource<BulkWriteModel> operations;
            if (attempt == 1)
            {
                operations = _writeModels;
            }
            else
            {
                operations = new BatchableSource<BulkWriteModel>(_writeModels.Items, _writeModels.Offset, _writeModels.ProcessedCount, canBeSplit: false);
            }
            var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, channel.ConnectionDescription.MaxBatchCount);
            var maxDocumentSize = channel.ConnectionDescription.MaxDocumentSize;
            var payload = new ClientBulkWriteOpsCommandMessageSection(operations, _idsMap, maxBatchCount, maxDocumentSize, _renderArgs);
            return new[] { payload };
        }

        public new ClientBulkWriteResult Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using var operation = BeginOperation();
            var bulkWriteResults = new BulkWriteRawResult();
            while (true)
            {
                using var context = RetryableWriteContext.Create(operationContext, binding, GetEffectiveRetryRequested());
                BsonDocument serverResponse = null;
                try
                {
                    serverResponse = base.Execute(operationContext, context);
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
                            // TODO: CSOT implement a way to support timeout in cursor methods
                            while (individualResults.MoveNext(operationContext.CancellationToken))
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

                _writeModels.AdvancePastProcessedItems();
                EnsureCanProceedNextBatch(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);

                if (_writeModels.AllItemsWereProcessed)
                {
                    return ToFinalResultsOrThrow(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);
                }
            }
        }

        public new async Task<ClientBulkWriteResult> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using var operation = BeginOperation();
            var bulkWriteResults = new BulkWriteRawResult();
            while (true)
            {
                using var context = RetryableWriteContext.Create(operationContext, binding, GetEffectiveRetryRequested());
                BsonDocument serverResponse = null;
                try
                {
                    serverResponse = await base.ExecuteAsync(operationContext, context).ConfigureAwait(false);
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
                            // TODO: CSOT implement a way to support timeout in cursor methods
                            while (await individualResults.MoveNextAsync(operationContext.CancellationToken).ConfigureAwait(false))
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

                _writeModels.AdvancePastProcessedItems();
                EnsureCanProceedNextBatch(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);

                if (_writeModels.AllItemsWereProcessed)
                {
                    return ToFinalResultsOrThrow(context.Channel.ConnectionDescription.ConnectionId, bulkWriteResults);
                }
            }
        }

        private EventContext.OperationIdDisposer BeginOperation() => EventContext.BeginOperation(null, OperationName);

        private void EnsureCanProceedNextBatch(ConnectionId connectionId, BulkWriteRawResult bulkWriteResult)
        {
            if (bulkWriteResult.TopLevelException != null)
            {
                ClientBulkWriteResult partialResult = null;
                if (_writeModels.Offset != 0)
                {
                    partialResult = ToClientBulkWriteResult(bulkWriteResult);
                }

                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during bulkWrite operation. See InnerException for more details.",
                    bulkWriteResult.Errors,
                    partialResult,
                    bulkWriteResult.ConcernErrors,
                    bulkWriteResult.TopLevelException);
            }

            if (bulkWriteResult.Errors.Count > 0 && IsOrdered)
            {
                var partialResult = ToClientBulkWriteResult(bulkWriteResult);
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during ordered bulkWrite operation. See WriteErrors for more details.",
                    bulkWriteResult.Errors,
                    partialResult,
                    bulkWriteResult.ConcernErrors);
            }
        }

        private bool GetEffectiveRetryRequested()
            => RetryRequested && !_writeModels.Items.Any(m => m.IsMulti);

        private ClientBulkWriteResult ToFinalResultsOrThrow(ConnectionId connectionId, BulkWriteRawResult bulkWriteResult)
        {
            var result = ToClientBulkWriteResult(bulkWriteResult);

            if (bulkWriteResult.Errors.Count > 0)
            {
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during bulkWrite operation. See WriteErrors for more details.",
                    bulkWriteResult.Errors,
                    result,
                    bulkWriteResult.ConcernErrors);
            }

            if (bulkWriteResult.ConcernErrors.Count > 0)
            {
                throw new ClientBulkWriteException(
                    connectionId,
                    "An error occurred during bulkWrite operation. See WriteConcernErrors for more details.",
                    bulkWriteResult.Errors,
                    result,
                    bulkWriteResult.ConcernErrors);
            }

            return result;
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

        private void PopulateBulkWriteResponse(BsonDocument bulkWriteResponse, BulkWriteRawResult bulkWriteResult)
        {
            if (bulkWriteResponse.TryGetValue("nErrors", out var errorCount))
            {
                bulkWriteResult.ErrorCount += errorCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nDeleted", out var deletedCount))
            {
                bulkWriteResult.DeletedCount += deletedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nInserted", out var insertedCount))
            {
                bulkWriteResult.InsertedCount += insertedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nMatched", out var matchedCount))
            {
                bulkWriteResult.MatchedCount += matchedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nModified", out var modifiedCount))
            {
                bulkWriteResult.ModifiedCount += modifiedCount.AsInt32;
            }

            if (bulkWriteResponse.TryGetValue("nUpserted", out var upsertedCount))
            {
                bulkWriteResult.UpsertedCount += upsertedCount.AsInt32;
            }
        }

        private void PopulateIndividualResponses(IEnumerable<BsonDocument> individualResponses, BulkWriteRawResult bulkWriteResult)
        {
            foreach (var operationResponse in individualResponses)
            {
                var isSucceeded = operationResponse["ok"].AsDouble != 0;
                var operationIndex = _writeModels.Offset + operationResponse["idx"].AsInt32;

                if (isSucceeded)
                {
                    var writeModel = _writeModels.Items[operationIndex];
                    var writeModelType = writeModel.GetType().GetGenericTypeDefinition();

                    if (writeModelType == typeof(BulkWriteInsertOneModel<>))
                    {
                        _idsMap.TryGetValue(operationIndex, out var insertedId);
                        bulkWriteResult.InsertResults.Add(operationIndex, new()
                        {
                            DocumentId = insertedId
                        });
                    }
                    else if (writeModelType == typeof(BulkWriteUpdateOneModel<>) || writeModelType == typeof(BulkWriteUpdateManyModel<>) || writeModelType == typeof(BulkWriteReplaceOneModel<>))
                    {
                        operationResponse.TryGetValue("upserted", out var upsertedId);
                        bulkWriteResult.UpdateResults.Add(operationIndex, new()
                        {
                            MatchedCount = operationResponse["n"].AsInt32,
                            ModifiedCount = operationResponse["nModified"].AsInt32,
                            UpsertedId = upsertedId?.AsBsonDocument["_id"],
                        });
                    }
                    else if (writeModelType == typeof(BulkWriteDeleteOneModel<>) || writeModelType == typeof(BulkWriteDeleteManyModel<>))
                    {
                        bulkWriteResult.DeleteResults.Add(operationIndex, new()
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

                    bulkWriteResult.Errors.Add(
                        operationIndex,
                        new WriteError(operationError.Category, operationError.Code, operationError.Message, operationError.Details));
                }
            }
        }

        private ClientBulkWriteResult ToClientBulkWriteResult(BulkWriteRawResult rawResult)
        {
            if (WriteConcern?.Equals(WriteConcern.Unacknowledged) == true)
            {
                return new ClientBulkWriteResult
                {
                    Acknowledged = false
                };
            }

            if (IsOrdered && (rawResult.Errors.Count > 0 && rawResult.Errors.First().Key == 0))
            {
                return null;
            }

            if (!IsOrdered && rawResult.Errors.Count == _writeModels.Items.Count)
            {
                return null;
            }

            return new ClientBulkWriteResult
            {
                Acknowledged = true,
                InsertedCount = rawResult.InsertedCount,
                DeletedCount = rawResult.DeletedCount,
                MatchedCount = rawResult.MatchedCount,
                ModifiedCount = rawResult.ModifiedCount,
                UpsertedCount = rawResult.UpsertedCount,
                DeleteResults = rawResult.DeleteResults,
                InsertResults = rawResult.InsertResults,
                UpdateResults = rawResult.UpdateResults
            };
        }

        private class BulkWriteRawResult
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
