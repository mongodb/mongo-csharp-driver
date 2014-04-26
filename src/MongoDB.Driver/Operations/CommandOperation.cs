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

namespace MongoDB.Driver.Operations
{
    internal class CommandOperation<TCommandResult> : ReadOperationBase where TCommandResult : CommandResult
    {
        private readonly IMongoCommand _command;
        private readonly QueryFlags _flags;
        private readonly BsonDocument _options;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializer<TCommandResult> _serializer;

        public CommandOperation(
            string databaseName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            IMongoCommand command,
            QueryFlags flags,
            BsonDocument options,
            ReadPreference readPreference,
            IBsonSerializer<TCommandResult> serializer)
            : base(databaseName, "$cmd", readerSettings, writerSettings)
        {
            _command = command;
            _flags = flags;
            _options = options;
            _readPreference = readPreference;
            _serializer = serializer;
        }

        public TCommandResult Execute(MongoConnection connection)
        {
            var maxWireDocumentSize = connection.ServerInstance.MaxWireDocumentSize;
            var forShardRouter = connection.ServerInstance.InstanceType == MongoServerInstanceType.ShardRouter;
            var wrappedQuery = WrapQuery(_command, _options, _readPreference, forShardRouter);

            var queryMessage = new MongoQueryMessage(WriterSettings, CollectionFullName, _flags, maxWireDocumentSize, 0, -1, wrappedQuery, null);
            connection.SendMessage(queryMessage);

            var reply = connection.ReceiveMessage<TCommandResult>(ReaderSettings, _serializer);
            if (reply.NumberReturned == 0)
            {
                var commandDocument = _command.ToBsonDocument();
                var commandName = (commandDocument.ElementCount == 0) ? "(no name)" : commandDocument.GetElement(0).Name;
                var message = string.Format("Command '{0}' failed. No response returned.", commandName);
                throw new MongoCommandException(message);
            }
            var commandResult = reply.Documents[0];
            commandResult.ServerInstance = connection.ServerInstance;
            commandResult.Command = _command;

            if (!commandResult.Ok)
            {
                throw ExceptionMapper.Map(commandResult.Response) ?? new MongoCommandException(commandResult);
            }

            return commandResult;
        }
    }
}
