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
    /// Represents an insert command operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class RetryableInsertCommandOperation<TDocument> : RetryableWriteCommandOperationBase
    {
        // private fields
        private bool? _bypassDocumentValidation;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BatchableSource<TDocument> _documents;
        private readonly IBsonSerializer<TDocument> _documentSerializer;
        private bool _isOrdered = true;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryableInsertCommandOperation{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="documents">The documents.</param>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public RetryableInsertCommandOperation(
            CollectionNamespace collectionNamespace,
            BatchableSource<TDocument> documents,
            IBsonSerializer<TDocument> documentSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : base(Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace)).DatabaseNamespace, messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _documents = Ensure.IsNotNull(documents, nameof(documents));
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>
        /// A value indicating whether to bypass document validation.
        /// </value>
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
        /// Gets the documents.
        /// </summary>
        /// <value>
        /// The documents.
        /// </value>
        public BatchableSource<TDocument> Documents
        {
            get { return _documents; }
        }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        /// <value>
        /// The document serializer.
        /// </value>
        public IBsonSerializer<TDocument> DocumentSerializer
        {
            get { return _documentSerializer; }
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

        // protected methods
        /// <inheritdoc />
        protected override BsonDocument CreateCommand(ConnectionDescription connectionDescription, int attempt, long? transactionNumber)
        {
            var batchSerializer = CreateBatchSerializer(connectionDescription, attempt);
            var batchWrapper = new BsonDocumentWrapper(_documents, batchSerializer);

            BsonDocument writeConcernWrapper = null;
            if (WriteConcernFunc != null)
            {
                var writeConcernSerializer = new DelayedEvaluationWriteConcernSerializer();
                writeConcernWrapper = new BsonDocumentWrapper(WriteConcernFunc, writeConcernSerializer);
            }

            return new BsonDocument
            {
                { "insert", _collectionNamespace.CollectionName },
                { "ordered", _isOrdered },
                { "bypassDocumentValidation", () => _bypassDocumentValidation, _bypassDocumentValidation.HasValue },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue },
                { "documents", new BsonArray { batchWrapper } },
                { "writeConcern", writeConcernWrapper, writeConcernWrapper != null }
            };
        }

        // private methods
        private IBsonSerializer<BatchableSource<TDocument>> CreateBatchSerializer(ConnectionDescription connectionDescription, int attempt)
        {
            var itemSerializer = new InsertSerializer(_documentSerializer);
            var isSystemIndexesCollection = _collectionNamespace.Equals(CollectionNamespace.DatabaseNamespace.SystemIndexesCollection);
            var elementNameValidator = isSystemIndexesCollection ? (IElementNameValidator)NoOpElementNameValidator.Instance : CollectionElementNameValidator.Instance;

            if (attempt == 1)
            {
                var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, connectionDescription.MaxBatchCount);
                var maxItemSize = connectionDescription.MaxDocumentSize;
                var maxBatchSize = connectionDescription.MaxDocumentSize;
                return new SizeLimitingBatchableSourceSerializer<TDocument>(itemSerializer, elementNameValidator, maxBatchCount, maxItemSize, maxBatchSize);
            }
            else
            {
                var count = _documents.ProcessedCount; // as set by the first attempt
                return new FixedCountBatchableSourceSerializer<TDocument>(itemSerializer, elementNameValidator, count);
            }
        }

        // nested types
        private class InsertSerializer : SerializerBase<TDocument>
        {
            // private fields
            private IBsonSerializer _cachedSerializer;
            private readonly IBsonSerializer<TDocument> _documentSerializer;

            // constructors
            public InsertSerializer(IBsonSerializer<TDocument> documentSerializer)
            {
                _documentSerializer = documentSerializer;
                _cachedSerializer = documentSerializer;
            }

            // public methods
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TDocument value)
            {
                IBsonSerializer serializer;

                var actualType = value.GetType();
                if (actualType == typeof(TDocument))
                {
                    serializer = _documentSerializer;
                }
                else
                {
                    if (_cachedSerializer.ValueType != actualType)
                    {
                        _cachedSerializer = BsonSerializer.LookupSerializer(actualType);
                    }
                    serializer = _cachedSerializer;
                }

                serializer.Serialize(context, value);
            }
        }
    }
}
