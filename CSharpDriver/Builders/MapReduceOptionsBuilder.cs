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
    public static class MapReduceOptions {
        #region public static properties
        public static MapReduceOptionsBuilder None {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static MapReduceOptionsBuilder Finalize(
            BsonJavaScript finalize
        ) {
            return new MapReduceOptionsBuilder().Finalize(finalize);
        }

        public static MapReduceOptionsBuilder KeepTemp(
            bool value
        ) {
            return new MapReduceOptionsBuilder().KeepTemp(value);
        }

        public static MapReduceOptionsBuilder Limit(
            int value
        ) {
            return new MapReduceOptionsBuilder().Limit(value);
        }

        public static MapReduceOptionsBuilder Out(
            string collectionName
        ) {
            return new MapReduceOptionsBuilder().Out(collectionName);
        }

        public static MapReduceOptionsBuilder Query<TQuery>(
            TQuery query
        ) {
            return new MapReduceOptionsBuilder().Query(query);
        }

        public static MapReduceOptionsBuilder Scope<TScope>(
            TScope scope
        ) {
            return new MapReduceOptionsBuilder().Scope(scope);
        }

        public static MapReduceOptionsBuilder Sort<TSortBy>(
            TSortBy sortBy
        ) {
            return new MapReduceOptionsBuilder().Sort(sortBy);
        }

        public static MapReduceOptionsBuilder Sort(
            params string[] keys
        ) {
            return new MapReduceOptionsBuilder().Sort(SortBy.Ascending(keys));
        }

        public static MapReduceOptionsBuilder Verbose(
            bool value
        ) {
            return new MapReduceOptionsBuilder().Verbose(value);
        }
        #endregion
    }

    [Serializable]
    public class MapReduceOptionsBuilder : BuilderBase, IConvertibleToBsonDocument, IBsonSerializable {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public MapReduceOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public MapReduceOptionsBuilder Finalize(
            BsonJavaScript finalize
        ) {
            document["finalize"] = finalize;
            return this;
        }

        public MapReduceOptionsBuilder KeepTemp(
            bool value
        ) {
            document["keeptemp"] = value;
            return this;
        }

        public MapReduceOptionsBuilder Limit(
            int value
        ) {
            document["limit"] = value;
            return this;
        }

        public MapReduceOptionsBuilder Out(
            string collectionName
        ) {
            document["out"] = collectionName;
            return this;
        }

        public MapReduceOptionsBuilder Query<TQuery>(
            TQuery query
        ) {
            document["query"] = BsonDocumentWrapper.Create(query);
            return this;
        }

        public MapReduceOptionsBuilder Scope<TScope>(
            TScope scope
        ) {
            document["scope"] = BsonDocumentWrapper.Create(scope);
            return this;
        }

        public MapReduceOptionsBuilder Sort<TSortBy>(
            TSortBy sortBy
        ) {
            document["sort"] = BsonDocumentWrapper.Create(sortBy);
            return this;
        }

        public MapReduceOptionsBuilder Sort(
            params string[] keys
        ) {
            return Sort(SortBy.Ascending(keys));
        }

        public BsonDocument ToBsonDocument() {
            return document;
        }

        public MapReduceOptionsBuilder Verbose(
            bool value
        ) {
            document["verbose"] = value;
            return this;
        }
        #endregion

        #region internal methods
        internal MapReduceOptionsBuilder Append(
            BsonDocument options
        ) {
            document.Add(options);
            return this;
        }
        #endregion

        #region explicit interface implementations
        void IBsonSerializable.Deserialize(
            BsonReader bsonReader
        ) {
            throw new InvalidOperationException("Deserialize is not supported for MapReduceOptionsBuilder");
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
