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

using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal abstract class WriteOpcodeOperationBase : DatabaseOperationBase
    {
        private readonly WriteConcern _writeConcern;

        protected WriteOpcodeOperationBase(
            string databaseName,
            string collectionName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern)
            : base(databaseName, collectionName, readerSettings, writerSettings)
        {
            _writeConcern = writeConcern;
        }

        protected WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        protected WriteConcernResult ReadWriteConcernResult(MongoConnection connection, SendMessageWithWriteConcernResult sendMessageResult)
        {
            var writeConcernResultSerializer = BsonSerializer.LookupSerializer<WriteConcernResult>();
            var replyMessage = connection.ReceiveMessage<WriteConcernResult>(ReaderSettings, writeConcernResultSerializer);
            if (replyMessage.NumberReturned == 0)
            {
                throw new MongoCommandException("Command 'getLastError' failed. No response returned");
            }
            var writeConcernResult = replyMessage.Documents[0];
            writeConcernResult.Command = sendMessageResult.GetLastErrorCommand;

            var mappedException = ExceptionMapper.Map(writeConcernResult);
            if (mappedException != null)
            {
                throw mappedException;
            }

            return writeConcernResult;
        }

        protected SendMessageWithWriteConcernResult SendMessageWithWriteConcern(
            MongoConnection connection,
            Stream stream,
            int requestId,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern)
        {
            var result = new SendMessageWithWriteConcernResult();

            if (writeConcern.Enabled)
            {
                var maxDocumentSize = connection.ServerInstance.MaxDocumentSize;

                var fsync = (writeConcern.FSync == null) ? null : (BsonValue)writeConcern.FSync;
                var journal = (writeConcern.Journal == null) ? null : (BsonValue)writeConcern.Journal;
                var w = (writeConcern.W == null) ? null : writeConcern.W.ToGetLastErrorWValue();
                var wTimeout = (writeConcern.WTimeout == null) ? null : (BsonValue)(int)writeConcern.WTimeout.Value.TotalMilliseconds;

                var getLastErrorCommand = new CommandDocument
                {
                    { "getlasterror", 1 }, // use all lowercase for backward compatibility
                    { "fsync", fsync, fsync != null },
                    { "j", journal, journal != null },
                    { "w", w, w != null },
                    { "wtimeout", wTimeout, wTimeout != null }
                };

                // piggy back on network transmission for message
                var getLastErrorMessage = new MongoQueryMessage(writerSettings, DatabaseName + ".$cmd", QueryFlags.None, maxDocumentSize, 0, 1, getLastErrorCommand, null);
                getLastErrorMessage.WriteTo(stream);

                result.GetLastErrorCommand = getLastErrorCommand;
                result.GetLastErrorRequestId = getLastErrorMessage.RequestId;
            }

            connection.SendMessage(stream, requestId);

            return result;
        }

        // nested classes
        protected class SendMessageWithWriteConcernResult
        {
            public IMongoCommand GetLastErrorCommand;
            public int? GetLastErrorRequestId;
        }
    }
}
