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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkUpdateOperation : BulkUnmixedWriteOperationBase
    {
        // fields
        private bool _checkElementNames = true;

        // constructors
        public BulkUpdateOperation(
            string databaseName,
            string collectionName,
            IEnumerable<UpdateRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseName, collectionName, requests, messageEncoderSettings)
        {
        }

        // properties
        public bool CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

        protected override string CommandName
        {
            get { return "update"; }
        }

        public new IEnumerable<UpdateRequest> Requests
        {
            get { return base.Requests.Cast<UpdateRequest>(); }
            set { base.Requests = value; }
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
            return new BulkUpdateOperationEmulator(DatabaseName, CollectionName, Requests, MessageEncoderSettings)
            {
                CheckElementNames = _checkElementNames,
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
            protected override void SerializeRequest(BsonSerializationContext context, WriteRequest request)
            {
                var updateRequest = (UpdateRequest)request;
                var bsonWriter = (BsonBinaryWriter)context.Writer;

                bsonWriter.PushMaxDocumentSize(MaxWireDocumentSize);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("q");
                BsonSerializer.Serialize(bsonWriter, updateRequest.Query);
                bsonWriter.WriteName("u");
                BsonSerializer.Serialize(bsonWriter, updateRequest.Update);
                if (updateRequest.IsMultiUpdate.HasValue)
                {
                    bsonWriter.WriteBoolean("multi", updateRequest.IsMultiUpdate.Value);
                }
                if (updateRequest.IsUpsert.HasValue)
                {
                    bsonWriter.WriteBoolean("upsert", updateRequest.IsUpsert.Value);
                }
                bsonWriter.WriteEndDocument();
                bsonWriter.PopMaxDocumentSize();
            }
        }
    }
}
