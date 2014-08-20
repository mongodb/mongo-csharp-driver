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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using NSubstitute;

namespace MongoDB.Driver.Core.Helpers
{
    public class MessageHelper
    {
        public static QueryMessage BuildQueryMessage(
            BsonDocument query = null, 
            int requestId = 0, 
            string databaseName = "foo", 
            string collectionName = "bar")
        {
            return new QueryMessage(
                requestId: requestId,
                databaseName: databaseName,
                collectionName: collectionName,
                query: query ?? new BsonDocument(),
                fields: null,
                skip: 0,
                batchSize: 0,
                slaveOk: false,
                partialOk: false,
                noCursorTimeout: false,
                tailableCursor: false,
                awaitData: false);
        }

        public static ReplyMessage<T> BuildSuccessReply<T>(
            T document,
            IBsonSerializer<T> serializer = null,
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
                awaitCapable: true,
                cursorId: cursorId,
                cursorNotFound: false,
                documents: new[] { document }.ToList(),
                numberReturned: 1,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: serializer ?? Substitute.For<IBsonSerializer<T>>(),
                startingFrom: startingFrom);
        }

        public static ReplyMessage<T> BuildNoDocumentsReturnedReply<T>(
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
                awaitCapable: true,
                cursorId: cursorId,
                cursorNotFound: false,
                documents: new List<T>(),
                numberReturned: 0,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: Substitute.For<IBsonSerializer<T>>(),
                startingFrom: startingFrom);
        }

        public static List<BsonDocument> TranslateMessagesToBsonDocuments(IEnumerable<MongoDBMessage> requests)
        {
            var docs = new List<BsonDocument>();
            foreach (var request in requests)
            {
                using (var stringWriter = new StringWriter())
                using (var jsonWriter = new JsonWriter(stringWriter))
                {
                    var encoderFactory = new JsonMessageEncoderFactory(jsonWriter);

                    request.GetEncoder(encoderFactory).WriteMessage(request);
                    docs.Add(BsonDocument.Parse(stringWriter.GetStringBuilder().ToString()));
                }
            }
            return docs;
        }

        public static List<BsonDocument> TranslateMessagesToBsonDocuments(byte[] bytes)
        {
            return TranslateMessagesToBsonDocuments(TranslateBytesToRequests(bytes));
        }

        public static void WriteRepliesToStream(Stream stream, IEnumerable<ReplyMessage> replies)
        {
            var startPosition = stream.Position;
            foreach (var reply in replies)
            {
                using (var writer = new BsonBinaryWriter(stream))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(writer);
                    var encoder = reply.GetEncoder(encoderFactory);
                    encoder.WriteMessage(reply);
                }
            }
            stream.Position = startPosition;
        }

        private static List<RequestMessage> TranslateBytesToRequests(byte[] bytes)
        {
            var requests = new List<RequestMessage>();

            using (var stream = new MemoryStream(bytes))
            {
                int bytesRead = 0;
                while (stream.Length > bytesRead)
                {
                    int startPosition = bytesRead;
                    using (var reader = new BsonBinaryReader(stream))
                    {
                        var length = reader.StreamReader.ReadInt32();
                        var requestId = reader.StreamReader.ReadInt32();
                        var responseTo = reader.StreamReader.ReadInt32();
                        var opCode = (Opcode)reader.StreamReader.ReadInt32();
                        bytesRead += length;
                        stream.Position = startPosition;

                        var encoderFactory = new BinaryMessageEncoderFactory(reader);
                        switch (opCode)
                        {
                            case Opcode.Query:
                                requests.Add(encoderFactory.GetQueryMessageEncoder().ReadMessage());
                                break;
                            default:
                                throw new InvalidOperationException("Unsupported request type.");
                        }
                    }
                }
            }

            return requests;
        }
    }
}