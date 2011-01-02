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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Builders {
    public static class IndexKeys {
        #region public static methods
        public static IndexKeysBuilder Ascending(
            params string[] names
        ) {
            return new IndexKeysBuilder().Ascending(names);
        }

        public static IndexKeysBuilder Descending(
            params string[] names
        ) {
            return new IndexKeysBuilder().Descending(names);
        }

        public static IndexKeysBuilder GeoSpatial(
            string name
        ) {
            return new IndexKeysBuilder().GeoSpatial(name);
        }

        public static IMongoIndexKeys Wrap(
            object keys
        ) {
            return IndexKeysWrapper.Create(keys);
        }
        #endregion
    }

    [Serializable]
    public class IndexKeysBuilder : BuilderBase, IMongoIndexKeys {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public IndexKeysBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public IndexKeysBuilder Ascending(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, 1);
            }
            return this;
        }

        public IndexKeysBuilder Descending(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, -1);
            }
            return this;
        }

        public IndexKeysBuilder GeoSpatial(
            string name
        ) {
            document.Add(name, "2d");
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
