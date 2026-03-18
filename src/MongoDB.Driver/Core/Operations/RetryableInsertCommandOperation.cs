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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed  class RetryableInsertCommandOperation<TDocument> : RetryableWriteCommandOperationBase where TDocument : class
    {
        private bool? _bypassDocumentValidation;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BatchableSource<TDocument> _documents;
        private readonly IBsonSerializer<TDocument> _documentSerializer;

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

        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public override string OperationName => null;

        public BatchableSource<TDocument> Documents
        {
            get { return _documents; }
        }

        public IBsonSerializer<TDocument> DocumentSerializer
        {
            get { return _documentSerializer; }
        }

        protected override BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, int attempt, long? transactionNumber)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, WriteConcern);
            return new BsonDocument
            {
                { "insert", _collectionNamespace.CollectionName },
                { "ordered", IsOrdered },
                { "bypassDocumentValidation", () => _bypassDocumentValidation, _bypassDocumentValidation.HasValue },
                { "comment", Comment, Comment != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "txnNumber", () => transactionNumber.Value, transactionNumber.HasValue }
            };
        }

        protected override IEnumerable<BatchableCommandMessageSection> CreateCommandPayloads(IChannelHandle channel, int attempt)
        {
            BatchableSource<TDocument> documents;
            if (attempt == 1)
            {
                documents = _documents;
            }
            else
            {
                documents = new BatchableSource<TDocument>(_documents.Items, _documents.Offset, _documents.ProcessedCount, canBeSplit: false);
            }

            var elementNameValidator = NoOpElementNameValidator.Instance;
            var maxBatchCount = Math.Min(MaxBatchCount ?? int.MaxValue, channel.ConnectionDescription.MaxBatchCount);
            var maxDocumentSize = channel.ConnectionDescription.MaxDocumentSize;
            var payload = new Type1CommandMessageSection<TDocument>("documents", documents, _documentSerializer, elementNameValidator, maxBatchCount, maxDocumentSize);
            return new Type1CommandMessageSection[] { payload };
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
            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(obj, null)) { return false; }
                if (object.ReferenceEquals(this, obj)) { return true; }
                return
                    base.Equals(obj) &&
                    obj is InsertSerializer other &&
                    object.Equals(_documentSerializer, other._documentSerializer);
            }

            public override int GetHashCode() => 0;

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
