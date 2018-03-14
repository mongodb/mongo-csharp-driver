/* Copyright 2017-present MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a delete command operation.
    /// </summary>
    public class RetryableDeleteCommandOperation : RetryableWriteCommandOperationBase
    {
        // private fields
        private readonly CollectionNamespace _collectionNamespace;
        private bool _isOrdered = true;
        private readonly BatchableSource<DeleteRequest> _deletes;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryableDeleteCommandOperation" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="deletes">The deletes.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public RetryableDeleteCommandOperation(
            CollectionNamespace collectionNamespace,
            BatchableSource<DeleteRequest> deletes,
            MessageEncoderSettings messageEncoderSettings)
            : base(Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace)).DatabaseNamespace, messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _deletes = Ensure.IsNotNull(deletes, nameof(deletes));
        }

        // public properties
        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        /// <summary>
        /// Gets the deletes.
        /// </summary>
        /// <value>
        /// The deletes.
        /// </value>
        public BatchableSource<DeleteRequest> Deletes
        {
            get { return _deletes; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server should process the deletes in order.
        /// </summary>
        /// <value>A value indicating whether the server should process the deletes in order.</value>
        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        // protected methods
        /// <inheritdoc />
        protected override BsonDocument CreateCommand(ConnectionDescription connectionDescription, int attempt, long? transactionNumber)
        {
            if (!Feature.Collation.IsSupported(connectionDescription.ServerVersion))
            {
                if (_deletes.Items.Skip(_deletes.Offset).Take(_deletes.Count).Any(d => d.Collation != null))
                {
                    throw new NotSupportedException($"Server version {connectionDescription.ServerVersion} does not support collations.");
                }
            }

            var batchSerializer = CreateBatchSerializer(connectionDescription, attempt);
            var batchWrapper = new BsonDocumentWrapper(_deletes, batchSerializer);

            BsonDocument writeConcernWrapper = null;
            if (WriteConcernFunc != null)
            {
                var writeConcernSerializer = new DelayedEvaluationWriteConcernSerializer();
                writeConcernWrapper = new BsonDocumentWrapper(WriteConcernFunc, writeConcernSerializer);
            }

            return new BsonDocument
            {
                { "delete", _collectionNamespace.CollectionName },
                { "ordered", _isOrdered },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue },
                { "deletes", new BsonArray { batchWrapper } },
                { "writeConcern", writeConcernWrapper, writeConcernWrapper != null }
            };
        }

        // private methods
        private IBsonSerializer<BatchableSource<DeleteRequest>> CreateBatchSerializer(ConnectionDescription connectionDescription, int attempt)
        {
            if (attempt == 1)
            {
                var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, connectionDescription.MaxBatchCount);
                var maxItemSize = connectionDescription.MaxWireDocumentSize;
                var maxBatchSize = connectionDescription.MaxDocumentSize;
                return new SizeLimitingBatchableSourceSerializer<DeleteRequest>(DeleteRequestSerializer.Instance, NoOpElementNameValidator.Instance, maxBatchCount, maxItemSize, maxBatchSize);
            }
            else
            {
                var count = _deletes.ProcessedCount; // as set by the first attempt
                return new FixedCountBatchableSourceSerializer<DeleteRequest>(DeleteRequestSerializer.Instance, NoOpElementNameValidator.Instance, count);
            }
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
                writer.WriteEndDocument();
            }
        }
    }
}
