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
            var binaryWriter = new BinaryWriter(memoryStream);
            var bsonWriter = BsonWriter.Create(binaryWriter);

            var serializer = new BsonSerializer(typeof(T));
            serializer.WriteObject(bsonWriter, document);

            BackpatchMessageLength(binaryWriter);
        }

        public byte[] RemoveLastDocument() {
            throw new NotImplementedException();
        }

        public void Reset(
            byte[] document
        ) {
            throw new NotImplementedException();
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
