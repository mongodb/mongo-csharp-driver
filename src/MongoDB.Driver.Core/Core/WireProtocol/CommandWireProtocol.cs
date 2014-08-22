/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class CommandWireProtocol : CommandWireProtocol<BsonDocument>
    {
        // constructors
        public CommandWireProtocol(
            string databaseName,
            BsonDocument command,
            bool slaveOk)
            : base(databaseName, command, BsonDocumentSerializer.Instance, slaveOk)
        {
        }
    }

    public class CommandWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // fields
        private readonly BsonDocument _command;
        private readonly string _databaseName;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly bool _slaveOk;

        // constructors
        public CommandWireProtocol(
            string databaseName,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            bool slaveOk)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _command = Ensure.IsNotNull(command, "command");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _slaveOk = slaveOk;
        }

        // methods
        private QueryMessage CreateMessage()
        {
            return new QueryMessage(
                RequestMessage.GetNextRequestId(),
                _databaseName,
                "$cmd",
                _command,
                null,
                0,
                -1,
                _slaveOk,
                false,
                false,
                false,
                false);
        }

        public async Task<TCommandResult> ExecuteAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var message = CreateMessage();
            await connection.SendMessageAsync(message, slidingTimeout, cancellationToken);
            var reply = await connection.ReceiveMessageAsync<RawBsonDocument>(message.RequestId, RawBsonDocumentSerializer.Instance, slidingTimeout, cancellationToken);
            return ProcessReply(reply);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private TCommandResult ProcessReply(ReplyMessage<RawBsonDocument> reply)
        {
            if (reply.NumberReturned == 0)
            {
                throw new MongoCommandException("Command returned no documents.", _command);
            }
            if (reply.NumberReturned > 1)
            {
                throw new MongoCommandException("Command returned multiple documents.", _command);
            }
            if (reply.QueryFailure)
            {
                throw new MongoCommandException("Command reply had QueryFailure flag set.", _command, reply.QueryFailureDocument);
            }

            using (var rawDocument = reply.Documents[0])
            {
                if (!rawDocument.GetValue("ok", false).ToBoolean())
                {
                    var materializedDocument = new BsonDocument(rawDocument);
                    throw ExceptionMapper.Map(materializedDocument) ?? new MongoCommandException("Command failed.", _command, materializedDocument);
                }

                using (var stream = new ByteBufferStream(rawDocument.Slice, ownsByteBuffer: false))
                using (var reader = new BsonBinaryReader(stream))
                {
                    var context = BsonDeserializationContext.CreateRoot<TCommandResult>(reader);
                    return _resultSerializer.Deserialize(context);
                }
            }
        }
    }
}
