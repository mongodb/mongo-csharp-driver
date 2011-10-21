/* Copyright 2010-2011 10gen Inc.
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
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Internal {
    internal class MongoInsertMessage : MongoRequestMessage {
        #region private fields
        private string collectionFullName;
        private bool checkElementNames;
        private InsertFlags flags;
        private int firstDocumentStartPosition;
        private int lastDocumentStartPosition;
        #endregion

        #region constructors
        internal MongoInsertMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkElementNames,
            InsertFlags flags
        )
            : base(MessageOpcode.Insert, null, writerSettings) {
            this.collectionFullName = collectionFullName;
            this.checkElementNames = checkElementNames;
            this.flags = flags;
        }
        #endregion

        #region internal methods
        internal void AddDocument(
            Type nominalType,
            object document
        ) {
            lastDocumentStartPosition = buffer.Position;
            using (var bsonWriter = BsonWriter.Create(buffer, writerSettings)) {
                bsonWriter.CheckElementNames = checkElementNames;
                BsonSerializer.Serialize(bsonWriter, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            BackpatchMessageLength();
        }

        internal byte[] RemoveLastDocument() {
            var lastDocumentLength = (int) (buffer.Position - lastDocumentStartPosition);
            var lastDocument = new byte[lastDocumentLength];
            buffer.CopyTo(lastDocumentStartPosition, lastDocument, 0, lastDocumentLength);
            buffer.Position = lastDocumentStartPosition;
            BackpatchMessageLength();

            return lastDocument;
        }

        internal void ResetBatch(
            byte[] lastDocument // as returned by RemoveLastDocument
        ) {
            buffer.Position = firstDocumentStartPosition;
            buffer.WriteBytes(lastDocument);
            BackpatchMessageLength();
        }
        #endregion

        #region protected methods
        protected override void WriteBody() {
            buffer.WriteInt32((int) flags);
            buffer.WriteCString(collectionFullName);
            firstDocumentStartPosition = buffer.Position;
            // documents to be added later by calling AddDocument
        }
        #endregion
    }
}
