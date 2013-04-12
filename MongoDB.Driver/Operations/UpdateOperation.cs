/* Copyright 2010-2013 10gen Inc.
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

using MongoDB.Bson.IO;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class UpdateOperation : WriteOperation
    {
        private readonly bool _checkElementNames;
        private readonly UpdateFlags _flags;
        private readonly IMongoQuery _query;
        private readonly IMongoUpdate _update;

        public UpdateOperation(
            string databaseName,
            string collectionName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern,
            IMongoQuery query,
            IMongoUpdate update,
            UpdateFlags flags,
            bool checkElementNames)
            : base(databaseName, collectionName, readerSettings, writerSettings, writeConcern)
        {
            _query = query;
            _update = update;
            _flags = flags;
            _checkElementNames = checkElementNames;
        }

        public WriteConcernResult Execute(MongoConnection connection)
        {
            using (var buffer = new BsonBuffer(new MultiChunkBuffer(BsonChunkPool.Default), true))
            {
                var readerSettings = GetNodeAdjustedReaderSettings(connection.ServerInstance);
                var writerSettings = GetNodeAdjustedWriterSettings(connection.ServerInstance);
                var message = new MongoUpdateMessage(writerSettings, CollectionFullName, _checkElementNames, _flags, _query, _update);
                message.WriteToBuffer(buffer);
                return SendMessageWithWriteConcern(connection, buffer, message.RequestId, readerSettings, writerSettings, WriteConcern);
            }
        }
    }
}
