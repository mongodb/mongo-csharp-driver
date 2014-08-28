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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkInsertOperation : BulkUnmixedWriteOperationBase
    {
        // fields
        private Action<object, IBsonSerializer> _assignId;
        private bool _checkElementNames = true;

        // constructors
        public BulkInsertOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<InsertRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        // properties
        public Action<object, IBsonSerializer> AssignId
        {
            get { return _assignId; }
            set { _assignId = value; }
        }

        public bool CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

        protected override string CommandName
        {
            get { return "insert"; }
        }

        public new IEnumerable<InsertRequest> Requests
        {
            get { return base.Requests.Cast<InsertRequest>(); }
            set { base.Requests = value; }
        }

        protected override string RequestsElementName
        {
            get { return "documents"; }
        }

        // methods
        protected override BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
        {
            return new InsertBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize, _checkElementNames);
        }

        protected override BulkUnmixedWriteOperationEmulatorBase CreateEmulator()
        {
            return new BulkInsertOperationEmulator(CollectionNamespace, Requests, MessageEncoderSettings)
            {
                AssignId = _assignId,
                CheckElementNames = _checkElementNames,
                MaxBatchCount = MaxBatchCount,
                MaxBatchLength = MaxBatchLength,
                IsOrdered = IsOrdered,
                WriteConcern = WriteConcern
            };
        }

        protected override IEnumerable<WriteRequest> DecorateRequests(IEnumerable<WriteRequest> requests)
        {
            if (_assignId != null)
            {
                return requests.Select(request =>
                {
                    var insertRequest = (InsertRequest)request;
                    var document = insertRequest.Document;
                    var serializer = insertRequest.Serializer ?? BsonSerializer.LookupSerializer(document.GetType());
                    _assignId(document, serializer);
                    return request;
                });
            }
            else
            {
                return requests;
            }
        }

        // nested types
        private class InsertBatchSerializer : BatchSerializer
        {
            // fields
            private IBsonSerializer _cachedSerializer;
            private Type _cachedSerializerType;
            private readonly bool _checkElementNames;

            // constructors
            public InsertBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize, bool checkElementNames)
                : base(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize)
            {
                _checkElementNames = checkElementNames;
            }

            // methods
            protected override void SerializeRequest(BsonSerializationContext context, WriteRequest request)
            {
                var insertRequest = (InsertRequest)request;
                var document = insertRequest.Document;
                if (document == null)
                {
                    throw new ArgumentException("Batch contains one or more null documents.");
                }

                var serializer = insertRequest.Serializer;
                if (serializer == null)
                {
                    var actualType = document.GetType();
                    if (_cachedSerializerType != actualType)
                    {
                        _cachedSerializer = BsonSerializer.LookupSerializer(actualType);
                        _cachedSerializerType = actualType;
                    }
                    serializer = _cachedSerializer;
                }

                var bsonWriter = (BsonBinaryWriter)context.Writer;
                var savedCheckElementNames = bsonWriter.CheckElementNames;
                try
                {
                    bsonWriter.PushMaxDocumentSize(MaxDocumentSize);
                    bsonWriter.CheckElementNames = _checkElementNames;
                    var documentNominalType = serializer.ValueType;
                    var documentContext = context.CreateChild(documentNominalType);
                    serializer.Serialize(documentContext, document);
                }
                finally
                {
                    bsonWriter.PopMaxDocumentSize();
                    bsonWriter.CheckElementNames = savedCheckElementNames;
                }
            }
        }
    }
}
