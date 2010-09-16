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
    internal class MongoQueryMessage : MongoRequestMessage {
        #region private fields
        private string collectionFullName;
        private QueryFlags flags;
        private int numberToSkip;
        private int numberToReturn;
        private BsonDocument query;
        private BsonDocument fields;
        #endregion

        #region constructors
        internal MongoQueryMessage(
            string collectionFullName,
            QueryFlags flags,
            int numberToSkip,
            int numberToReturn,
            BsonDocument query,
            BsonDocument fields
        ) :
            this(collectionFullName, flags, numberToSkip, numberToReturn, query, fields, null) {
        }

        internal MongoQueryMessage(
            string collectionFullName,
            QueryFlags flags,
            int numberToSkip,
            int numberToReturn,
            BsonDocument query,
            BsonDocument fields,
            BsonBuffer buffer
        ) :
            base(MessageOpcode.Query, buffer) {
            this.collectionFullName = collectionFullName;
            this.flags = flags;
            this.numberToSkip = numberToSkip;
            this.numberToReturn = numberToReturn;
            this.query = query;
            this.fields = fields;
        }
        #endregion

        #region protected methods
        protected override void WriteBody() {
            buffer.Write((int) flags);
            buffer.WriteCString(collectionFullName);
            buffer.Write(numberToSkip);
            buffer.Write(numberToReturn);

            BsonWriter bsonWriter = BsonWriter.Create(buffer);
            if (query == null) {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteEndDocument();
            } else {
                query.WriteTo(bsonWriter);
            }
            if (fields != null) {
                fields.WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
