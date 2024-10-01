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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ClientBulkWriteOperation : RetryableWriteCommandOperationBase, IWriteOperation<IBulkWriteResults>
    {
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
            var maxDocumentSize = channel.ConnectionDescription.MaxWireDocumentSize;
            var payload = new ClientBulkWriteOpsCommandMessageSection(operations, maxBatchCount, maxDocumentSize, RenderArgs);
            return new[] { payload };
        }

        public new IBulkWriteResults Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            var bulkWriteResults = new BulkWriteRawResults();
            while (!WriteModels.AllItemsWereProcessed)
            {
                using (var context = RetryableWriteContext.Create(binding, false, cancellationToken))
                {
                    var response = base.Execute(context, cancellationToken);

                    if (response != null)
                    {
                        PopulateBulkWriteResponse(response, bulkWriteResults);
                        if (TryGetIndividualResults(context, response, out var cursor))
                        {
                            while (cursor.MoveNext(cancellationToken))
                            {
                                PopulateIndividualResponses(cursor.Current, bulkWriteResults, 0);
                            }
                        }
                    }

                    WriteModels.AdvancePastProcessedItems();
                }
            }

            return null;
        }

        public new async Task<IBulkWriteResults> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            while (!WriteModels.AllItemsWereProcessed)
            {
                await base.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
                WriteModels.AdvancePastProcessedItems();
            }

            return null;
        }

        private bool TryGetIndividualResults(RetryableWriteContext context, BsonDocument bulkWriteResponse, out AsyncCursor<BsonDocument> cursor)
        {
            if (!bulkWriteResponse.TryGetElement("cursor", out var cursorElement))
            {
                cursor = null;
                return false;
            }

            var cursorDocument = cursorElement.Value.AsBsonDocument;

            cursor = new AsyncCursor<BsonDocument>(
                context.ChannelSource,
                CollectionNamespace.FromFullName(cursorDocument["ns"].AsString),
                null,
                cursorDocument["firstBatch"].AsBsonArray.OfType<BsonDocument>().ToList(),
                cursorDocument["id"].AsInt64,
                0,
                0,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings);
            return true;
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

        private void PopulateIndividualResponses(IEnumerable<BsonDocument> individualResponses, BulkWriteRawResults bulkWriteResults, int offset)
        {
            foreach (var operationResponse in individualResponses)
            {
                bulkWriteResults.ErrorCount++;
            }
        }

        private class BulkWriteRawResults
        {
            public long DeletedCount { get; set; }
            public long ErrorCount { get; set; }
            public long InsertedCount { get; set; }
            public long MatchedCount { get; set; }
            public long ModifiedCount { get; set; }
            public long UpsertedCount { get; set; }
            public Dictionary<long, BulkWriteDeleteResult> DeleteResults { get; set; }
            public Dictionary<long, BulkWriteOperationError> Errors { get; set; }
            public Dictionary<long, BulkWriteInsertOneResult> InsertResults { get; set; }
            public Dictionary<long, BulkWriteUpdateResult> UpdateResults { get; set; }
        }


    }
}
