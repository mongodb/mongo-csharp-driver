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
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.CSharpDriver.Internal {
    internal class MongoDeleteMessage<TQuery> : MongoRequestMessage {
        #region private fields
        private string collectionFullName;
        private RemoveFlags flags;
        private TQuery query;
        #endregion

        #region constructors
        internal MongoDeleteMessage(
            string collectionFullName,
            RemoveFlags flags,
            TQuery query
        ) :
            base(MessageOpcode.Delete) {
            this.collectionFullName = collectionFullName;
            this.flags = flags;
            this.query = query;
        }
        #endregion

        #region protected methods
        protected override void WriteBody() {
            buffer.WriteInt32(0); // reserved
            buffer.WriteCString(collectionFullName);
            buffer.WriteInt32((int) flags);

            BsonWriter bsonWriter = BsonWriter.Create(buffer);
            if (query == null) {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteEndDocument();
            } else {
                BsonSerializer.Serialize(bsonWriter, query, true); // serializeIdFirst
            }
        }
        #endregion
    }
}
