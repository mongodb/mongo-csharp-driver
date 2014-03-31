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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class BulkUpdateOperation : BulkUnmixedWriteOperationBase
    {
        // private fields
        private readonly BulkUpdateOperationArgs _args;

        // constructors
        public BulkUpdateOperation(BulkUpdateOperationArgs args)
            : base(args)
        {
            _args = args;
        }

        // protected properties
        protected override string CommandName
        {
            get { return "update"; }
        }

        protected override string RequestsElementName
        {
            get { return "updates"; }
        }

        // public methods
        public override BulkWriteResult Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (!serverInstance.Supports(FeatureId.WriteCommands))
            {
                var emulator = new BulkUpdateOperationEmulator(_args);
                return emulator.Execute(connection);
            }

            return base.Execute(connection);
        }

        // protected methods
        protected override BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
        {
            return new UpdateBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize);
        }

        // nested classes
        private class UpdateBatchSerializer : BatchSerializer
        {
            // constructors
            public UpdateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
                : base(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize)
            {
            }

            // protected methods
            protected override void SerializeRequest(BsonBinaryWriter bsonWriter, WriteRequest request)
            {
                var updateRequest = (UpdateRequest)request;

                bsonWriter.PushMaxDocumentSize(MaxWireDocumentSize);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("q");
                BsonSerializer.Serialize(bsonWriter, updateRequest.Query ?? new QueryDocument());
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
