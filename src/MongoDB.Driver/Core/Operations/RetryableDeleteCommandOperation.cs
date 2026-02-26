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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class RetryableDeleteCommandOperation : RetryableWriteCommandOperationBase
    {
        // private fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BatchableSource<DeleteRequest> _deletes;
        private BsonDocument _let;

        public RetryableDeleteCommandOperation(
            CollectionNamespace collectionNamespace,
            BatchableSource<DeleteRequest> deletes,
            MessageEncoderSettings messageEncoderSettings)
            : base(Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace)).DatabaseNamespace, messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _deletes = Ensure.IsNotNull(deletes, nameof(deletes));
        }

        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public override string OperationName => null;

        public BatchableSource<DeleteRequest> Deletes
        {
            get { return _deletes; }
        }

        protected override BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, int attempt, long? transactionNumber)
        {
            if (WriteConcern != null && !WriteConcern.IsAcknowledged)
            {
                if (_deletes.Items.Skip(_deletes.Offset).Take(_deletes.Count).Any(u => u.Hint != null))
                {
                    throw new NotSupportedException("Hint is not supported for unacknowledged writes.");
                }
            }

            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, WriteConcern);
            return new BsonDocument
            {
                { "delete", _collectionNamespace.CollectionName },
                { "ordered", IsOrdered },
                { "comment", Comment, Comment != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue },
                { "let", _let, _let != null }
            };
        }

        /// <inheritdoc />
        protected override IEnumerable<BatchableCommandMessageSection> CreateCommandPayloads(IChannelHandle channel, int attempt)
        {
            BatchableSource<DeleteRequest> deletes;
            if (attempt == 1)
            {
                deletes = _deletes;
            }
            else
            {
                deletes = new BatchableSource<DeleteRequest>(_deletes.Items, _deletes.Offset, _deletes.ProcessedCount, canBeSplit: false);
            }
            var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, channel.ConnectionDescription.MaxBatchCount);
            var maxDocumentSize = channel.ConnectionDescription.MaxWireDocumentSize;
            var payload = new Type1CommandMessageSection<DeleteRequest>("deletes", deletes, DeleteRequestSerializer.Instance, NoOpElementNameValidator.Instance, maxBatchCount, maxDocumentSize);
            return new Type1CommandMessageSection[] { payload };
        }

        // nested types
        private class DeleteRequestSerializer : SealedClassSerializerBase<DeleteRequest>
        {
            public static readonly IBsonSerializer<DeleteRequest> Instance = new DeleteRequestSerializer();

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, DeleteRequest value)
            {
                var writer = context.Writer;
                writer.WriteStartDocument();
                writer.WriteName("q");
                BsonDocumentSerializer.Instance.Serialize(context, value.Filter);
                writer.WriteName("limit");
                writer.WriteInt32(value.Limit);
                if (value.Collation != null)
                {
                    writer.WriteName("collation");
                    BsonDocumentSerializer.Instance.Serialize(context, value.Collation.ToBsonDocument());
                }
                if (value.Hint != null)
                {
                    writer.WriteName("hint");
                    BsonValueSerializer.Instance.Serialize(context, value.Hint);
                }
                writer.WriteEndDocument();
            }
        }
    }
}
