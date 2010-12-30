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
    public class MapReduceOutput {
        #region private fields
        private string option;
        private BsonValue value;
        #endregion

        #region constructors
        private MapReduceOutput(
            string option,
            BsonValue value
        ) {
            this.option = option;
            this.value = value;
        }
        #endregion

        #region implicit operators
        public static implicit operator MapReduceOutput(
            string collectionName
        ) {
            return MapReduceOutput.Replace(collectionName);
        }
        #endregion

        #region public static properties
        public static MapReduceOutput Inline {
            get { return new MapReduceOutput("inline", 1); }
        }
        #endregion

        #region public static methods
        public static MapReduceOutput Replace(
            string collectionName
        ) {
            return new MapReduceOutput(null, collectionName);
        }

        public static MapReduceOutput Merge(
            string collectionName
        ) {
            return new MapReduceOutput("merge", collectionName);
        }

        public static MapReduceOutput Reduce(
            string collectionName
        ) {
            return new MapReduceOutput("reduce", collectionName);
        }
        #endregion

        #region internal methods
        public BsonValue ToBsonValue() {
            if (option == null) {
                return value;
            } else {
                return new BsonDocument(option, value);
            }
        }
        #endregion
    }

    public static class MapReduceOptions {
        #region public static properties
        public static IMongoMapReduceOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static MapReduceOptionsBuilder SetFinalize(
            BsonJavaScript finalize
        ) {
            return new MapReduceOptionsBuilder().SetFinalize(finalize);
        }

        public static MapReduceOptionsBuilder SetKeepTemp(
            bool value
        ) {
            return new MapReduceOptionsBuilder().SetKeepTemp(value);
        }

        public static MapReduceOptionsBuilder SetLimit(
            int value
        ) {
            return new MapReduceOptionsBuilder().SetLimit(value);
        }

        public static MapReduceOptionsBuilder SetOutput(
            MapReduceOutput output
        ) {
            return new MapReduceOptionsBuilder().SetOutput(output);
        }

        public static MapReduceOptionsBuilder SetQuery(
            IMongoQuery query
        ) {
            return new MapReduceOptionsBuilder().SetQuery(query);
        }

        public static MapReduceOptionsBuilder SetScope(
            IMongoScope scope
        ) {
            return new MapReduceOptionsBuilder().SetScope(scope);
        }

        public static MapReduceOptionsBuilder SetSortOrder(
            IMongoSortBy sortBy
        ) {
            return new MapReduceOptionsBuilder().SetSortOrder(sortBy);
        }

        public static MapReduceOptionsBuilder SetSortOrder(
            params string[] keys
        ) {
            return new MapReduceOptionsBuilder().SetSortOrder(SortBy.Ascending(keys));
        }

        public static MapReduceOptionsBuilder SetVerbose(
            bool value
        ) {
            return new MapReduceOptionsBuilder().SetVerbose(value);
        }

        public static IMongoMapReduceOptions Wrap(
            object options
        ) {
            return MapReduceOptionsWrapper.Create(options);
        }
        #endregion
    }

    [Serializable]
    public class MapReduceOptionsBuilder : BuilderBase, IMongoMapReduceOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public MapReduceOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public MapReduceOptionsBuilder SetFinalize(
            BsonJavaScript finalize
        ) {
            document["finalize"] = finalize;
            return this;
        }

        public MapReduceOptionsBuilder SetKeepTemp(
            bool value
        ) {
            document["keeptemp"] = value;
            return this;
        }

        public MapReduceOptionsBuilder SetLimit(
            int value
        ) {
            document["limit"] = value;
            return this;
        }

        public MapReduceOptionsBuilder SetOutput(
            MapReduceOutput output
        ) {
            document["out"] = output.ToBsonValue();
            return this;
        }

        public MapReduceOptionsBuilder SetQuery(
            IMongoQuery query
        ) {
            document["query"] = BsonDocumentWrapper.Create(query);
            return this;
        }

        public MapReduceOptionsBuilder SetScope(
            IMongoScope scope
        ) {
            document["scope"] = BsonDocumentWrapper.Create(scope);
            return this;
        }

        public MapReduceOptionsBuilder SetSortOrder(
            IMongoSortBy sortBy
        ) {
            document["sort"] = BsonDocumentWrapper.Create(sortBy);
            return this;
        }

        public MapReduceOptionsBuilder SetSortOrder(
            params string[] keys
        ) {
            return SetSortOrder(SortBy.Ascending(keys));
        }

        public MapReduceOptionsBuilder SetVerbose(
            bool value
        ) {
            document["verbose"] = value;
            return this;
        }

        public override BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region internal methods
        internal MapReduceOptionsBuilder AddOptions(
            BsonDocument options
        ) {
            document.Add(options);
            return this;
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
