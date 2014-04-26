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
using System.IO;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class UpdateOpcodeOperation : WriteOpcodeOperationBase
    {
        // private fields
        private readonly BulkUpdateOperationArgs _args;

        // constructors
        public UpdateOpcodeOperation(BulkUpdateOperationArgs args)
            : base(args.DatabaseName, args.CollectionName, args.ReaderSettings, args.WriterSettings, args.WriteConcern)
        {
            _args = args;
        }

        // public methods
        public WriteConcernResult Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (serverInstance.Supports(FeatureId.WriteCommands) && _args.WriteConcern.Enabled)
            {
                var emulator = new UpdateOpcodeOperationEmulator(_args);
                return emulator.Execute(connection);
            }

            SendMessageWithWriteConcernResult sendMessageResult;
            using (var stream = new MemoryStream())
            {
                var requests = _args.Requests.ToList();
                if (requests.Count != 1)
                {
                    throw new NotSupportedException("Update opcode only supports a single update request.");
                }
                var updateRequest = (UpdateRequest)requests[0];

                var flags = UpdateFlags.None;
                if (updateRequest.IsMultiUpdate ?? false) { flags |= UpdateFlags.Multi; }
                if (updateRequest.IsUpsert ?? false) { flags |= UpdateFlags.Upsert; }

                var maxDocumentSize = connection.ServerInstance.MaxDocumentSize;
                var query = updateRequest.Query ?? new QueryDocument();

                var message = new MongoUpdateMessage(WriterSettings, CollectionFullName, _args.CheckElementNames, flags, maxDocumentSize, query, updateRequest.Update);
                message.WriteTo(stream);

                sendMessageResult = SendMessageWithWriteConcern(connection, stream, message.RequestId, ReaderSettings, WriterSettings, WriteConcern);
            }

            return WriteConcern.Enabled ? ReadWriteConcernResult(connection, sendMessageResult) : null;
        }
    }
}
