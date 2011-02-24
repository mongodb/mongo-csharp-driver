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
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Builders {
    public static class CollectionOptions {
        #region public static properties
        public static IMongoCollectionOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static CollectionOptionsBuilder SetAutoIndexId(
            bool value
        ) {
            return new CollectionOptionsBuilder().SetAutoIndexId(value);
        }

        public static CollectionOptionsBuilder SetCapped(
            bool value
        ) {
            return new CollectionOptionsBuilder().SetCapped(value);
        }

        public static CollectionOptionsBuilder SetMaxDocuments(
            int value
        ) {
            return new CollectionOptionsBuilder().SetMaxDocuments(value);
        }

        public static CollectionOptionsBuilder SetMaxSize(
            int value
        ) {
            return new CollectionOptionsBuilder().SetMaxSize(value);
        }

        public static IMongoCollectionOptions Wrap(
            object options
        ) {
            return CollectionOptionsWrapper.Create(options);
        }
        #endregion
    }

    [Serializable]
    public class CollectionOptionsBuilder : BuilderBase, IMongoCollectionOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public CollectionOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public CollectionOptionsBuilder SetAutoIndexId(
            bool value
        ) {
            if (value) {
                document["autoIndexId"] = value;
            } else {
                document.Remove("autoIndexId");
            }
            return this;
        }

        public CollectionOptionsBuilder SetCapped(
            bool value
        ) {
            if (value) {
                document["capped"] = value;
            } else {
                document.Remove("capped");
            }
            return this;
        }

        public CollectionOptionsBuilder SetMaxDocuments(
            int value
        ) {
            document["max"] = value;
            return this;
        }

        public CollectionOptionsBuilder SetMaxSize(
            int value
        ) {
            document["size"] = value;
            return this;
        }

        public override BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region protected methods
        protected override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            document.Serialize(bsonWriter, nominalType, options);
        }
        #endregion
    }
}
