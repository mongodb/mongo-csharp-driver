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

namespace MongoDB.CSharpDriver.Internal {
    internal class MongoReplyMessage<R> : MongoMessage where R : new() {
        #region private fields
        private ResponseFlags responseFlags;
        private long cursorId;
        private int startingFrom;
        private int numberReturned;
        private List<R> documents;
        #endregion

        #region constructors
        internal MongoReplyMessage()
            : base(MessageOpcode.Reply) {
        }
        #endregion

        #region internal properties
        internal ResponseFlags ResponseFlags {
            get { return responseFlags; }
        }

        internal long CursorId {
            get { return cursorId; }
        }

        internal int StartingFrom {
            get { return startingFrom; }
        }

        internal int NumberReturned {
            get { return numberReturned; }
        }

        internal List<R> Documents {
            get { return documents; }
        }
        #endregion

        #region internal methods
        internal void ReadFrom(
            byte[] bytes
        ) {
            MemoryStream memoryStream = new MemoryStream(bytes);
            BinaryReader binaryReader = new BinaryReader(memoryStream);
            long messageStartPosition = binaryReader.BaseStream.Position;

            ReadMessageHeaderFrom(binaryReader);
            responseFlags = (ResponseFlags) binaryReader.ReadInt32();
            cursorId = binaryReader.ReadInt64();
            startingFrom = binaryReader.ReadInt32();
            numberReturned = binaryReader.ReadInt32();
            documents = new List<R>();

            BsonReader bsonReader = BsonReader.Create(binaryReader);
            BsonSerializer serializer = new BsonSerializer();
            while (binaryReader.BaseStream.Position - messageStartPosition < messageLength) {
                R document = (R) serializer.Deserialize(bsonReader, typeof(R));
                documents.Add(document);
            }
        }
        #endregion
    }
}
