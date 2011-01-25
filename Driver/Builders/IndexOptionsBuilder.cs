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
    public static class IndexOptions {
        #region public static properties
        public static IMongoIndexOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static IndexOptionsBuilder SetBackground(
            bool value
        ) {
            return new IndexOptionsBuilder().SetBackground(value);
        }

        public static IndexOptionsBuilder SetDropDups(
            bool value
        ) {
            return new IndexOptionsBuilder().SetDropDups(value);
        }

        public static IndexOptionsBuilder SetGeoSpatialRange(
            double min,
            double max
        ) {
            return new IndexOptionsBuilder().SetGeoSpatialRange(min, max);
        }

        public static IndexOptionsBuilder SetName(
            string value
        ) {
            return new IndexOptionsBuilder().SetName(value);
        }

        public static IndexOptionsBuilder SetUnique(
            bool value
        ) {
            return new IndexOptionsBuilder().SetUnique(value);
        }

        public static IMongoIndexOptions Wrap(
            object options
        ) {
            return IndexOptionsWrapper.Create(options);
        }
        #endregion
    }

    [Serializable]
    public class IndexOptionsBuilder : BuilderBase, IMongoIndexOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public IndexOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public IndexOptionsBuilder SetBackground(
            bool value
        ) {
            document["background"] = value;
            return this;
        }

        public IndexOptionsBuilder SetDropDups(
            bool value
        ) {
            document["dropDups"] = value;
            return this;
        }

        public IndexOptionsBuilder SetGeoSpatialRange(
            double min,
            double max
        ) {
            document["min"] = min;
            document["max"] = max;
            return this;
        }

        public IndexOptionsBuilder SetName(
            string value
        ) {
            document["name"] = value;
            return this;
        }

        public IndexOptionsBuilder SetUnique(
            bool value
        ) {
            document["unique"] = value;
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
