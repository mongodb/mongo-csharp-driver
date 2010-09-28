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
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;
using MongoDB.CSharpDriver;

namespace MongoDB.CSharpDriver.Builders {
    public static class IndexOptions {
        #region public static properties
        public static IndexOptionsBuilder None {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static IndexOptionsBuilder Background(
            bool value
        ) {
            return new IndexOptionsBuilder().Background(value);
        }

        public static IndexOptionsBuilder DropDups(
            bool value
        ) {
            return new IndexOptionsBuilder().DropDups(value);
        }

        public static IndexOptionsBuilder GeoSpatialRange(
            double min,
            double max
        ) {
            return new IndexOptionsBuilder().GeoSpatialRange(min, max);
        }

        public static IndexOptionsBuilder Name(
            string value
        ) {
            return new IndexOptionsBuilder().Name(value);
        }

        public static IndexOptionsBuilder Unique(
            bool value
        ) {
            return new IndexOptionsBuilder().Unique(value);
        }
        #endregion
    }

    public class IndexOptionsBuilder : BuilderBase, IBsonDocumentBuilder, IBsonSerializable {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public IndexOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public IndexOptionsBuilder Background(
            bool value
        ) {
            document["background"] = value;
            return this;
        }

        public IndexOptionsBuilder DropDups(
            bool value
        ) {
            document["dropDups"] = value;
            return this;
        }

        public IndexOptionsBuilder GeoSpatialRange(
            double min,
            double max
        ) {
            document["min"] = min;
            document["max"] = max;
            return this;
        }

        public IndexOptionsBuilder Name(
            string value
        ) {
            document["name"] = value;
            return this;
        }

        public BsonDocument ToBsonDocument() {
            return document;
        }

        public IndexOptionsBuilder Unique(
            bool value
        ) {
            document["unique"] = value;
            return this;
        }
        #endregion

        #region explicit interface implementations
        void IBsonSerializable.Deserialize(
            BsonReader bsonReader
        ) {
            throw new MongoException("Deserialize is not supported for IndexOptionsBuilder");
        }

        void IBsonSerializable.Serialize(
            BsonWriter bsonWriter,
            bool serializeIdFirst
        ) {
            document.Serialize(bsonWriter, serializeIdFirst);
        }
        #endregion
    }
}
