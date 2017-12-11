/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an update command operation.
    /// </summary>
    public class RetryableUpdateCommandOperation : RetryableWriteCommandOperationBase
    {
        // private fields
        private bool? _bypassDocumentValidation;
        private readonly CollectionNamespace _collectionNamespace;
        private bool _isOrdered = true;
        private readonly BatchableSource<UpdateRequest> _updates;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryableUpdateCommandOperation" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="updates">The updates.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public RetryableUpdateCommandOperation(
            CollectionNamespace collectionNamespace,
            BatchableSource<UpdateRequest> updates,
            MessageEncoderSettings messageEncoderSettings)
            : base(Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace)).DatabaseNamespace, messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _updates = Ensure.IsNotNull(updates, nameof(updates));
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>A value indicating whether to bypass document validation.</value>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

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
        /// Gets or sets a value indicating whether the server should process the inserts in order.
        /// </summary>
        /// <value>A value indicating whether the server should process the inserts in order.</value>
        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        /// <summary>
        /// Gets the updates.
        /// </summary>
        /// <value>
        /// The updates.
        /// </value>
        public BatchableSource<UpdateRequest> Updates
        {
            get { return _updates; }
        }

        // protected methods
        /// <inheritdoc />
        protected override BsonDocument CreateCommand(ConnectionDescription connectionDescription, int attempt, long? transactionNumber)
        {
            var serverVersion = connectionDescription.ServerVersion;
            if (!Feature.Collation.IsSupported(serverVersion))
            {
                if (_updates.Items.Skip(_updates.Offset).Take(_updates.Count).Any(u => u.Collation != null))
                {
                    throw new NotSupportedException($"Server version {serverVersion} does not support collations.");
                }
            }
            if (!Feature.ArrayFilters.IsSupported(serverVersion))
            {
                if (_updates.Items.Skip(_updates.Offset).Take(_updates.Count).Any(u => u.ArrayFilters != null))
                {
                    throw new NotSupportedException($"Server version {serverVersion} does not support arrayFilters.");
                }
            }

            var batchSerializer = CreateBatchSerializer(connectionDescription, attempt);
            var batchWrapper = new BsonDocumentWrapper(_updates, batchSerializer);

            BsonDocument writeConcernWrapper = null;
            if (WriteConcernFunc != null)
            {
                var writeConcernSerializer = new DelayedEvaluationWriteConcernSerializer();
                writeConcernWrapper = new BsonDocumentWrapper(WriteConcernFunc, writeConcernSerializer);
            }

            return new BsonDocument
            {
                { "update", _collectionNamespace.CollectionName },
                { "ordered", _isOrdered },
                { "bypassDocumentValidation", () => _bypassDocumentValidation.Value, _bypassDocumentValidation.HasValue },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue },
                { "updates", new BsonArray { batchWrapper } },
                { "writeConcern", writeConcernWrapper, writeConcernWrapper != null }
            };
        }

        // private methods
        private IBsonSerializer<BatchableSource<UpdateRequest>> CreateBatchSerializer(ConnectionDescription connectionDescription, int attempt)
        {
            if (attempt == 1)
            {
                var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, connectionDescription.MaxBatchCount);
                var maxItemSize = connectionDescription.MaxWireDocumentSize;
                var maxBatchSize = connectionDescription.MaxWireDocumentSize;
                return new SizeLimitingBatchableSourceSerializer<UpdateRequest>(UpdateRequestSerializer.Instance, NoOpElementNameValidator.Instance, maxBatchCount, maxItemSize, maxBatchSize);
            }
            else
            {
                var count = _updates.ProcessedCount; // as set by the first attempt
                return new FixedCountBatchableSourceSerializer<UpdateRequest>(UpdateRequestSerializer.Instance, NoOpElementNameValidator.Instance, count);
            }
        }

        // nested types
        private class UpdateRequestSerializer : SealedClassSerializerBase<UpdateRequest>
        {
            public static readonly IBsonSerializer<UpdateRequest> Instance = new UpdateRequestSerializer();

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, UpdateRequest value)
            {
                var writer = context.Writer;
                writer.WriteStartDocument();
                writer.WriteName("q");
                BsonDocumentSerializer.Instance.Serialize(context, value.Filter);
                writer.WriteName("u");
                SerializeUpdate(context, args, value);
                if (value.IsMulti)
                {
                    writer.WriteName("multi");
                    writer.WriteBoolean(value.IsMulti);
                }
                if (value.IsUpsert)
                {
                    writer.WriteName("upsert");
                    writer.WriteBoolean(value.IsUpsert);
                }
                if (value.Collation != null)
                {
                    writer.WriteName("collation");
                    BsonDocumentSerializer.Instance.Serialize(context, value.Collation.ToBsonDocument());
                }
                if (value.ArrayFilters != null)
                {
                    writer.WriteName("arrayFilters");
                    writer.WriteStartArray();
                    foreach (var arrayFilter in value.ArrayFilters)
                    {
                        BsonDocumentSerializer.Instance.Serialize(context, arrayFilter);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndDocument();
            }

            // private methods
            private void SerializeUpdate(BsonSerializationContext context, BsonSerializationArgs args, UpdateRequest request)
            {
                var writer = context.Writer;
                writer.PushElementNameValidator(ElementNameValidatorFactory.ForUpdateType(request.UpdateType));
                try
                {
                    var position = writer.Position;
                    BsonDocumentSerializer.Instance.Serialize(context, request.Update);
                    if (request.UpdateType == UpdateType.Update && writer.Position == position + 8)
                    {
                        throw new BsonSerializationException("Update documents cannot be empty.");
                    }
                }
                finally
                {
                    writer.PopElementNameValidator();
                }
            }
        }
    }
}
