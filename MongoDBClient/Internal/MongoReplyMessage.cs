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
    internal class MongoReplyMessage<T> : MongoMessage where T : new() {
        #region private fields
        private ResponseFlags responseFlags;
        private long cursorId;
        private int startingFrom;
        private int numberReturned;
        private List<T> documents;
        #endregion

        #region constructors
        public MongoReplyMessage(
            byte[] bytes
        )
            : base(MessageOpcode.Reply) {
            ReadFrom(bytes);
        }
        #endregion

        #region public properties
        public ResponseFlags ResponseFlags {
            get { return responseFlags; }
            set { responseFlags = value; }
        }

        public long CursorId {
            get { return cursorId; }
            set { cursorId = value; }
        }

        public int StartingFrom {
            get { return startingFrom; }
            set { startingFrom = value; }
        }

        public int NumberReturned {
            get { return numberReturned; }
            set { numberReturned = value; }
        }

        public List<T> Documents {
            get { return documents; }
            set { documents = value; }
        }
        #endregion

        #region public methods
        #endregion

        #region private methods
        internal void ReadFrom(
            byte[] bytes
        ) {
            MemoryStream memoryStream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(memoryStream);
            long start = reader.BaseStream.Position;

            ReadMessageHeaderFrom(reader);
            responseFlags = (ResponseFlags) reader.ReadInt32();
            cursorId = reader.ReadInt64();
            startingFrom = reader.ReadInt32();
            numberReturned = reader.ReadInt32();
            documents = new List<T>();

            BsonReader bsonReader = new BsonReader(reader);
            BsonSerializer serializer = new BsonSerializer(typeof(T));
            while (reader.BaseStream.Position - start < MessageLength) {
                T document = (T) serializer.ReadObject(bsonReader);
                documents.Add(document);
            }
        }
        #endregion
    }
}
