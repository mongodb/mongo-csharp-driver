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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal class BulkDeleteOperation : BulkUnmixedWriteOperationBase
    {
        // private fields
        private readonly BulkDeleteOperationArgs _args;

        // constructors
        public BulkDeleteOperation(BulkDeleteOperationArgs args)
            : base(args)
        {
            _args = args;
        }

        // protected properties
        protected override string CommandName
        {
            get { return "delete"; }
        }

        protected override string RequestsElementName
        {
            get { return "deletes"; }
        }

        // public methods
        public override BulkWriteResult Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (!serverInstance.Supports(FeatureId.WriteCommands))
            {
                var emulator = new BulkDeleteOperationEmulator(_args);
                return emulator.Execute(connection);
            }

            return base.Execute(connection);
        }

        // protected methods
        protected override BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
        {
            return new DeleteBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize);
        }

        // nested classes
        private class DeleteBatchSerializer : BatchSerializer
        {
            // constructors
            public DeleteBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
                : base(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize)
            {
            }

            // protected methods
            protected override void SerializeRequest(BsonBinaryWriter bsonWriter, WriteRequest request)
            {
                var deleteRequest = (DeleteRequest)request;
                bsonWriter.PushMaxDocumentSize(MaxDocumentSize);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("q");
                BsonSerializer.Serialize(bsonWriter, deleteRequest.Query ?? new QueryDocument());
                bsonWriter.WriteInt32("limit", deleteRequest.Limit);
                bsonWriter.WriteEndDocument();
                bsonWriter.PopMaxDocumentSize();
            }
        }
    }
}
