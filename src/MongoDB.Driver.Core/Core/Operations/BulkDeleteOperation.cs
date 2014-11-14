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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkDeleteOperation : BulkUnmixedWriteOperationBase
    {
        // constructors
        public BulkDeleteOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<DeleteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        // properties
        protected override string CommandName
        {
            get { return "delete"; }
        }

        public new IEnumerable<DeleteRequest> Requests
        {
            get { return base.Requests.Cast<DeleteRequest>(); }
        }

        protected override string RequestsElementName
        {
            get { return "deletes"; }
        }

        // methods
        protected override BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
        {
            return new DeleteBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize);
        }

        protected override BulkUnmixedWriteOperationEmulatorBase CreateEmulator()
        {
            return new BulkDeleteOperationEmulator(CollectionNamespace, Requests, MessageEncoderSettings)
            {
                MaxBatchCount = MaxBatchCount,
                MaxBatchLength = MaxBatchLength,
                IsOrdered = IsOrdered,
                WriteConcern = WriteConcern
            };
        }

        // nested types
        private class DeleteBatchSerializer : BatchSerializer
        {
            // constructors
            public DeleteBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
                : base(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize)
            {
            }

            // methods
            protected override void SerializeRequest(BsonSerializationContext context, WriteRequest request)
            {
                var deleteRequest = (DeleteRequest)request;
                var bsonWriter = (BsonBinaryWriter)context.Writer;
                bsonWriter.PushMaxDocumentSize(MaxDocumentSize);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("q");
                BsonSerializer.Serialize(bsonWriter, deleteRequest.Filter);
                bsonWriter.WriteInt32("limit", deleteRequest.Limit);
                bsonWriter.WriteEndDocument();
                bsonWriter.PopMaxDocumentSize();
            }
        }
    }
}
