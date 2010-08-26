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
    internal class MongoInsertMessage : MongoRequestMessage {
        #region private fields
        private long firstDocumentStart;
        private long lastDocumentStart;
        #endregion

        #region constructors
        internal MongoInsertMessage(
            MongoCollection collection
        )
            : base(MessageOpcode.Insert, collection) {
        }
        #endregion

        #region public methods
        public void AddDocument<T>(
            T document
        ) {
            var memoryStream = AsMemoryStream();
            lastDocumentStart = memoryStream.Position;
            if (firstDocumentStart == 0) {
                firstDocumentStart = lastDocumentStart;
            }

            var serializer = new BsonSerializer(typeof(T));
            var bsonWriter = BsonWriter.Create(binaryWriter);
            serializer.WriteObject(bsonWriter, document);

            BackpatchMessageLength(binaryWriter);
        }

        // assumes AddDocument has been called at least once
        public byte[] RemoveLastDocument() {
            var lastDocumentLength = (int) (memoryStream.Position - lastDocumentStart);
            var lastDocument = new byte[lastDocumentLength];
            memoryStream.Position = lastDocumentStart;
            memoryStream.Read(lastDocument, 0, lastDocumentLength);
            memoryStream.SetLength(lastDocumentStart);
            memoryStream.Position = lastDocumentStart;

            BackpatchMessageLength(binaryWriter);

            return lastDocument;
        }

        // assumes RemoveLastDocument was called first
        public void Reset(
            byte[] lastDocument
        ) {
            memoryStream.SetLength(firstDocumentStart);
            memoryStream.Position = firstDocumentStart;
            lastDocumentStart = firstDocumentStart;

            memoryStream.Write(lastDocument, 0, lastDocument.Length);

            BackpatchMessageLength(binaryWriter);
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter binaryWriter
        ) {
            binaryWriter.Write((int) 0); // reserved
            WriteCStringTo(binaryWriter, collection.FullName);
            // documents to be added later by calling AddDocument
        }
        #endregion
    }
}
