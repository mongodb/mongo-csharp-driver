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
    internal class DeleteOpcodeOperation : WriteOpcodeOperationBase
    {
        // private fields
        private readonly BulkDeleteOperationArgs _args;

        // constructors
        public DeleteOpcodeOperation(BulkDeleteOperationArgs args)
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
                var emulator = new DeleteOpcodeOperationEmulator(_args);
                return emulator.Execute(connection);
            }

            var requests = _args.Requests.ToArray();
            if (requests.Length != 1)
            {
                throw new NotSupportedException("Delete Opcode only supports a single delete request.");
            }
            var deleteRequest = (DeleteRequest)requests[0];

            RemoveFlags flags;
            switch (deleteRequest.Limit)
            {
                case 0: flags = RemoveFlags.None; break;
                case 1: flags = RemoveFlags.Single; break;
                default: throw new NotSupportedException("Delete Opcode only supports limit values of 0 and 1.");
            }

            SendMessageWithWriteConcernResult sendMessageResult;
            using (var stream = new MemoryStream())
            {
                var maxDocumentSize = connection.ServerInstance.MaxDocumentSize;

                var message = new MongoDeleteMessage(WriterSettings, CollectionFullName, flags, maxDocumentSize, deleteRequest.Query);
                message.WriteTo(stream);

                sendMessageResult = SendMessageWithWriteConcern(connection, stream, message.RequestId, ReaderSettings, WriterSettings, WriteConcern);
            }

            return WriteConcern.Enabled ? ReadWriteConcernResult(connection, sendMessageResult) : null;
        }
    }
}
