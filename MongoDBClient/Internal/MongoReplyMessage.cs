/* Copyright 2010 10gen Inc.
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

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient.Internal {
    internal class MongoReplyMessage : MongoMessage {
        #region private fields
        private ResponseFlags responseFlags;
        private long cursorID;
        private int startingFrom;
        private int numberReturned;
        private List<BsonDocument> documents;
        #endregion

        #region constructors
        public MongoReplyMessage()
            : base(RequestOpCode.Reply) {
        }
        #endregion

        #region public properties
        public ResponseFlags ResponseFlags {
            get { return responseFlags; }
            set { responseFlags = value; }
        }

        public long CursorID {
            get { return cursorID; }
            set { cursorID = value; }
        }

        public int StartingFrom {
            get { return startingFrom; }
            set { startingFrom = value; }
        }

        public int NumberReturned {
            get { return numberReturned; }
            set { numberReturned = value; }
        }

        public List<BsonDocument> Documents {
            get { return documents; }
            set { documents = value; }
        }
        #endregion

        #region public static methods
        internal static MongoReplyMessage ReadFrom(
            Stream stream
        ) {
            BinaryReader reader = new BinaryReader(stream);

            // TODO: can we process the NetworkStream directly?
            int messageLength = reader.ReadInt32();
            int bufferLength = messageLength - 4;
            byte[] bytes = new byte[bufferLength];
            int index = 0;
            int count = bufferLength;
            while (count > 0) {
                int bytesRead = reader.Read(bytes, index, count);
                index += bytesRead;
                count -= bytesRead;
            }

            MemoryStream memoryStream = new MemoryStream(bytes);
            reader = new BinaryReader(memoryStream);

            MongoReplyMessage reply = new MongoReplyMessage();
            reply.RequestID = reader.ReadInt32();
            reply.ResponseTo = reader.ReadInt32();
            reply.OpCode = (RequestOpCode) reader.ReadInt32();
            if (reply.OpCode != RequestOpCode.Reply) {
                throw new MongoException("Invalid reply from server");
            }

            reply.ResponseFlags = (ResponseFlags) reader.ReadInt32();
            reply.CursorID = reader.ReadInt64();
            reply.StartingFrom = reader.ReadInt32();
            reply.NumberReturned = reader.ReadInt32();
            reply.Documents = new List<BsonDocument>();

            BsonReader bsonReader = new BsonReader(reader);
            while (memoryStream.Position < bufferLength) {
                bsonReader.Reset();
                BsonDocument document = BsonDocument.ReadFrom(bsonReader);
                reply.Documents.Add(document);
            }

            return reply;
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter writer
        ) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
