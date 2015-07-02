/* Copyright 2010-2015 MongoDB Inc.
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
            private void SerializeFilter(BsonBinaryWriter bsonWriter, BsonDocument filter)
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                BsonDocumentSerializer.Instance.Serialize(context, filter);
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
                    SerializeFilter(bsonWriter, updateRequest.Filter);
                    bsonWriter.WriteName("u");
                    SerializeUpdate(bsonWriter, updateRequest.Update, updateRequest.UpdateType);
                    if (updateRequest.IsMulti)
                    {
                        bsonWriter.WriteBoolean("multi", updateRequest.IsMulti);
                    }
                    if (updateRequest.IsUpsert)
                    {
                        bsonWriter.WriteBoolean("upsert", updateRequest.IsUpsert);
                    }
                    bsonWriter.WriteEndDocument();
                }
                finally
                {
                    bsonWriter.PopMaxDocumentSize();
                }
            }

            private void SerializeUpdate(BsonBinaryWriter bsonWriter, BsonDocument update, UpdateType updateType)
            {
                bsonWriter.PushElementNameValidator(ElementNameValidatorFactory.ForUpdateType(updateType));
                try
                {
                    var position = bsonWriter.BaseStream.Position;
                    var context = BsonSerializationContext.CreateRoot(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, update);
                    if (updateType == UpdateType.Update && bsonWriter.BaseStream.Position == position + 8)
                    {
                        throw new BsonSerializationException("Update documents cannot be empty.");
                    }
                }
                finally
                {
                    bsonWriter.PopElementNameValidator();
                }
            }
        }
    }
}
