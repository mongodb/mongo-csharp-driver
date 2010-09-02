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
    internal class MongoUpdateMessage<U> : MongoRequestMessage where U : new() {
        #region private fields
        private string collectionFullName;
        private UpdateFlags flags;
        private BsonDocument query;
        private U update;
        #endregion

        #region constructors
        internal MongoUpdateMessage(
            string collectionFullName,
            UpdateFlags flags,
            BsonDocument query,
            U update
        ) :
            base(MessageOpcode.Update) {
            this.collectionFullName = collectionFullName;
            this.flags = flags;
            this.query = query;
            this.update = update;
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter binaryWriter
        ) {
            binaryWriter.Write((int) 0); // reserved
            WriteCStringTo(binaryWriter, collectionFullName);
            binaryWriter.Write((int) flags);

            BsonWriter bsonWriter = BsonBinaryWriter.Create(binaryWriter);
            query.WriteTo(bsonWriter);
            BsonSerializer serializer = new BsonSerializer(typeof(U));
            serializer.WriteObject(bsonWriter, update);
        }
        #endregion
    }
}
