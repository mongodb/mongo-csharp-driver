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

using System.Collections.Generic;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;

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

        public static List<BsonDocument> TranslateMessagesToBsonDocuments(IEnumerable<MongoDBMessage> requests)
        {
            var docs = new List<BsonDocument>();
            foreach (var request in requests)
            {
                using (var stringWriter = new StringWriter())
                {
                    var encoderFactory = new JsonMessageEncoderFactory(stringWriter, null);

                    request.GetEncoder(encoderFactory).WriteMessage(request);
                    docs.Add(BsonDocument.Parse(stringWriter.GetStringBuilder().ToString()));
                }
            }
            return docs;
        }

        public static List<BsonDocument> TranslateMessagesToBsonDocuments(byte[] bytes)
        {
            var docs = new List<BsonDocument>();
            using (var stream = new MemoryStream(bytes))
            {
                while (stream.Position < stream.Length)
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                    var encoderSelector = new CommandMessageEncoderSelector();
                    var message = encoderSelector.GetEncoder(encoderFactory).ReadMessage();
                    using (var stringWriter = new StringWriter())
                    {
                        var jsonEncoderFactory = new JsonMessageEncoderFactory(stringWriter, null);
                        message.GetEncoder(jsonEncoderFactory).WriteMessage(message);
                        docs.Add(BsonDocument.Parse(stringWriter.GetStringBuilder().ToString()));
                    }
                }
            }
            return docs;
        }

        public static void WriteResponsesToStream(Stream stream, params CommandMessage[] messages)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.End);
            foreach (var message in messages)
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetCommandMessageEncoder();
                encoder.WriteMessage(message);
            }
            stream.Position = position;
        }
    }
}
