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
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ClientBulkWriteOperation : RetryableWriteCommandOperationBase
    {
        public ClientBulkWriteOperation(
            IReadOnlyList<BulkWriteModel> writeModels,
            ClientBulkWriteOptions options,
            MessageEncoderSettings messageEncoderSettings)
        : base(new DatabaseNamespace("admin"), messageEncoderSettings)
        {
            WriteModels = new BatchableSource<BulkWriteModel>(writeModels, true);
            BypassDocumentValidation = options?.BypassDocumentValidation;
            Comment = options?.Comment;
            IsOrdered = (options?.IsOrdered).GetValueOrDefault(true);
            ErrorsOnly = !(options?.VerboseResult).GetValueOrDefault(false);
            Let = options?.Let;
            WriteConcern = options?.WriteConcern;
        }

        public bool? BypassDocumentValidation { get; init; }

        public bool ErrorsOnly { get; init; }

        public BsonDocument Let { get; init; }

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
            var payload = new ClientBulkWriteOpsCommandMessageSection(operations, maxBatchCount, maxDocumentSize);
            return new ClientBulkWriteOpsCommandMessageSection[] { payload };
        }
    }
}
