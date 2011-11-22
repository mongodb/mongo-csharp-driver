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
    internal class MongoUpdateMessage : MongoRequestMessage {
        #region private fields
        private string collectionFullName;
        private bool checkUpdateDocument;
        private UpdateFlags flags;
        private IMongoQuery query;
        private IMongoUpdate update;
        #endregion

        #region constructors
        internal MongoUpdateMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkUpdateDocument,
            UpdateFlags flags,
            IMongoQuery query,
            IMongoUpdate update
        ) :
            base(MessageOpcode.Update, null, writerSettings) {
            this.collectionFullName = collectionFullName;
            this.checkUpdateDocument = checkUpdateDocument;
            this.flags = flags;
            this.query = query;
            this.update = update;
        }
        #endregion

        #region protected methods
        protected override void WriteBody() {
            buffer.WriteInt32(0); // reserved
            buffer.WriteCString(collectionFullName);
            buffer.WriteInt32((int) flags);

            using (var bsonWriter = BsonWriter.Create(buffer, writerSettings)) {
                if (query == null) {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteEndDocument();
                } else {
                    BsonSerializer.Serialize(bsonWriter, query.GetType(), query, DocumentSerializationOptions.SerializeIdFirstInstance);
                }
                bsonWriter.CheckUpdateDocument = checkUpdateDocument;
                BsonSerializer.Serialize(bsonWriter, update.GetType(), update, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
        }
        #endregion
    }
}
