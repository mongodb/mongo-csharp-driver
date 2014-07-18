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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using NSubstitute;

namespace MongoDB.Driver.Core.Tests.Helpers
{
    public class MessageHelper
    {
        public static ReplyMessage<T> BuildSuccessReply<T>(
            T document,
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
                cursorId: cursorId,
                cursorNotFound: false,
                documents: new[] { document }.ToList(),
                numberReturned: 1,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: Substitute.For<IBsonSerializer<T>>(),
                startingFrom: startingFrom);
        }

        public static ReplyMessage<T> BuildNoDocumentsReturnedReply<T>(
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
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

        public static List<BsonDocument> TranslateRequestsToBsonDocuments(IEnumerable<RequestMessage> requests)
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
    }
}