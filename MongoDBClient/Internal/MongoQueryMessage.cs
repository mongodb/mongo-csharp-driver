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
    internal class MongoQueryMessage : MongoMessage {
        #region private fields
        private QueryFlags flags;
        private string fullCollectionName;
        private int numberToSkip;
        private int numberToReturn;
        private BsonDocument query;
        private BsonDocument fieldSelector;
        #endregion

        #region constructors
        internal MongoQueryMessage(
            MongoDatabase database,
            MongoCollection collection
        ) : base(RequestOpCode.Query) {
            fullCollectionName = database.Name + "." + collection.Name;
        }
        #endregion

        #region internal properties
        public QueryFlags Flags {
            get { return flags; }
            set { flags = value; }
        }

        public string FullCollectionName {
            get { return fullCollectionName; }
            set { fullCollectionName = value; }
        }

        public int NumberToSkip {
            get { return numberToSkip; }
            set { numberToSkip = value; }
        }

        public int NumberToReturn {
            get { return numberToReturn; }
            set { numberToReturn = value; }
        }

        public BsonDocument Query {
            get { return query ?? (query = new BsonDocument()); }
            set { query = value; }
        }

        public BsonDocument FieldSelector {
            get { return fieldSelector; }
            set { fieldSelector = value; }
        }
       #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter writer
        ) {
            writer.Write((int) flags);
            WriteCString(writer, fullCollectionName);
            writer.Write(numberToSkip);
            writer.Write(numberToReturn);

            BsonWriter bsonWriter = BsonBinaryWriter.Create(writer);
            Query.WriteTo(bsonWriter);
            if (fieldSelector != null) {
                fieldSelector.WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
