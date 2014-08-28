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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class CommandWireProtocol : CommandWireProtocol<BsonDocument>
    {
        // constructors
        public CommandWireProtocol(
            string databaseName,
            BsonDocument command,
            bool slaveOk,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseName, command, slaveOk, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    public class CommandWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // fields
        private readonly BsonDocument _command;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly bool _slaveOk;

        // constructors
        public CommandWireProtocol(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            bool slaveOk,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _command = Ensure.IsNotNull(command, "command");
            _slaveOk = slaveOk;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // methods
        private QueryMessage CreateMessage()
        {
            return new QueryMessage(
                RequestMessage.GetNextRequestId(),
                _databaseNamespace.CommandCollection,
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
            await connection.SendMessageAsync(message, _messageEncoderSettings, slidingTimeout, cancellationToken);
            var reply = await connection.ReceiveMessageAsync<RawBsonDocument>(message.RequestId, RawBsonDocumentSerializer.Instance, _messageEncoderSettings, slidingTimeout, cancellationToken);
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
                var failureDocument = reply.QueryFailureDocument;
                throw ExceptionMapper.Map(failureDocument) ?? new MongoCommandException("Command failed.", _command, failureDocument);
            }

            using (var rawDocument = reply.Documents[0])
            {
                if (!rawDocument.GetValue("ok", false).ToBoolean())
                {
                    var materializedDocument = new BsonDocument(rawDocument);
                    throw ExceptionMapper.Map(materializedDocument) ?? new MongoCommandException("Command failed.", _command, materializedDocument);
                }

                using (var stream = new ByteBufferStream(rawDocument.Slice, ownsByteBuffer: false))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, _messageEncoderSettings);
                    var encoder = (ReplyMessageBinaryEncoder<TCommandResult>)encoderFactory.GetReplyMessageEncoder<TCommandResult>(_resultSerializer);
                    using (var reader = encoder.CreateBinaryReader())
                    {
                        var context = BsonDeserializationContext.CreateRoot<TCommandResult>(reader);
                        return _resultSerializer.Deserialize(context);
                    }
                }
            }
        }
    }
}
