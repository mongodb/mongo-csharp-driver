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
    public static class Fields {
        #region public static properties
        public static IMongoFields Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static FieldsBuilder Exclude(
            params string[] names
        ) {
            return new FieldsBuilder().Exclude(names);
        }

        public static FieldsBuilder Include(
            params string[] names
        ) {
            return new FieldsBuilder().Include(names);
        }

        public static FieldsBuilder Slice(
            string name,
            int size // negative sizes are from the end
        ) {
            return new FieldsBuilder().Slice(name, size);
        }

        public static FieldsBuilder Slice(
            string name,
            int skip,
            int limit
        ) {
            return new FieldsBuilder().Slice(name, skip, limit);
        }

        public static IMongoFields Wrap(
            object fields
        ) {
            return FieldsWrapper.Create(fields);
        }
        #endregion
    }

    [Serializable]
    public class FieldsBuilder : BuilderBase, IMongoFields {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public FieldsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public FieldsBuilder Exclude(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, 0);
            }
            return this;
        }

        public FieldsBuilder Include(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, 1);
            }
            return this;
        }

        public FieldsBuilder Slice(
            string name,
            int size // negative sizes are from the end
        ) {
            document.Add(name, new BsonDocument("$slice", size));
            return this;
        }

        public FieldsBuilder Slice(
            string name,
            int skip,
            int limit
        ) {
            document.Add(name, new BsonDocument("$slice", new BsonArray { skip, limit }));
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
