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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkUpdateOperation : BulkUnmixedWriteOperationBase
    {
        // constructors
        public BulkUpdateOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<UpdateRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        // properties
        protected override string CommandName
        {
            get { return "update"; }
        }

        public new IEnumerable<UpdateRequest> Requests
        {
            get { return base.Requests.Cast<UpdateRequest>(); }
        }

        protected override string RequestsElementName
        {
            get { return "updates"; }
        }

        // methods
        protected override BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
        {
            return new UpdateBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize);
        }

        protected override BulkUnmixedWriteOperationEmulatorBase CreateEmulator()
        {
            return new BulkUpdateOperationEmulator(CollectionNamespace, Requests, MessageEncoderSettings)
            {
                MaxBatchCount = MaxBatchCount,
                MaxBatchLength = MaxBatchLength,
                IsOrdered = IsOrdered,
                WriteConcern = WriteConcern
            };
        }

        // nested types
        private class UpdateBatchSerializer : BatchSerializer
        {
            // constructors
            public UpdateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
                : base(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize)
            {
            }

            // methods
            private void SerializeCriteria(BsonBinaryWriter bsonWriter, BsonDocument criteria)
            {
                var context = BsonSerializationContext.CreateRoot<BsonDocument>(bsonWriter);
                BsonDocumentSerializer.Instance.Serialize(context, criteria);
            }

            protected override void SerializeRequest(BsonSerializationContext context, WriteRequest request)
            {
                var updateRequest = (UpdateRequest)request;
                var bsonWriter = (BsonBinaryWriter)context.Writer;

                bsonWriter.PushMaxDocumentSize(MaxWireDocumentSize);
                try
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName("q");
                    SerializeCriteria(bsonWriter, updateRequest.Criteria);
                    bsonWriter.WriteName("u");
                    SerializeUpdate(bsonWriter, updateRequest.Update);
                    if (updateRequest.IsMultiUpdate.HasValue)
                    {
                        bsonWriter.WriteBoolean("multi", updateRequest.IsMultiUpdate.Value);
                    }
                    if (updateRequest.IsUpsert.HasValue)
                    {
                        bsonWriter.WriteBoolean("upsert", updateRequest.IsUpsert.Value);
                    }
                    bsonWriter.WriteEndDocument();
                }
                finally
                {
                    bsonWriter.PopMaxDocumentSize();
                }
            }

            private void SerializeUpdate(BsonBinaryWriter bsonWriter, BsonDocument update)
            {
                var updateValidator = new UpdateOrReplacementElementNameValidator();

                bsonWriter.PushElementNameValidator(updateValidator);
                try
                {
                    var context = BsonSerializationContext.CreateRoot<BsonDocument>(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, update);
                }
                finally
                {
                    bsonWriter.PopElementNameValidator();
                }
            }
        }
    }
}
