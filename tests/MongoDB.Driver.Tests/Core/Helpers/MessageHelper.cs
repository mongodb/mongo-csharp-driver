/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Helpers
{
    internal sealed class MessageHelper
    {
        private static readonly DatabaseNamespace __defaultDatabaseNamespace = new("foo");
        private static readonly CollectionNamespace __defaultCollectionNamespace = new(__defaultDatabaseNamespace, "bar");

        public static CollectionNamespace DefaultCollectionNamespace
        {
            get { return __defaultCollectionNamespace; }
        }

        public static DatabaseNamespace DefaultDatabaseNamespace
        {
            get { return __defaultDatabaseNamespace; }
        }

        public static RequestCommandMessage BuildCommand(
            BsonDocument command,
            int requestId = 0,
            DatabaseNamespace databaseNamespace = null)
        {
            if (databaseNamespace == null)
            {
                databaseNamespace = __defaultDatabaseNamespace;
            }

            var section = new Type0CommandMessageSection<BsonDocument>(
                command,
                BsonDocumentSerializer.Instance,
                databaseNamespace,
                sessionId: null,
                transactionNumber: null);
            return new RequestCommandMessage(requestId, new[] { section }, false);
        }

        public static ResponseCommandMessage BuildCommandResponse(
            RawBsonDocument document,
            int requestId = 0,
            int responseTo = 0,
            bool moreToCome = false)
        {
            var section = new Type0CommandMessageSection<RawBsonDocument>(document, RawBsonDocumentSerializer.Instance);
            return new ResponseCommandMessage(requestId, responseTo, new[] { section }, moreToCome);
        }

        public static BsonDocument ToCommandPayload(CommandMessage commandMessage)
        {
            using var stream = new MemoryStream();

            var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
            encoderFactory.GetCommandMessageEncoder().WriteMessage(commandMessage);

            stream.Seek(0, SeekOrigin.Begin);
            var message = (CommandMessage)encoderFactory.GetCommandMessageEncoder().ReadMessage();
            if (message.Sections.Count > 1)
            {
                throw new NotSupportedException("Multiple sections are not supported.");
            }

            var commandSection = (Type0CommandMessageSection<RawBsonDocument>)message.Sections[0];
            return commandSection.Document;
        }

        public static byte[] ToWireBytes(CommandMessage commandMessage)
        {
            using var stream = new MemoryStream();

            var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
            encoderFactory.GetCommandMessageEncoder().WriteMessage(commandMessage);

            return stream.ToArray();
        }

        public static void WriteResponseToStream(Stream stream, CommandMessage message)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.End);

            var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
            var encoder = encoderFactory.GetCommandMessageEncoder();
            encoder.WriteMessage(message);

            stream.Position = position;
        }
    }
}
