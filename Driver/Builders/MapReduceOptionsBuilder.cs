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
    /// <summary>
    /// Represents the output options of a map/reduce operation.
    /// </summary>
    public class MapReduceOutput {
        #region private fields
        private BsonValue output;
        #endregion

        #region constructors
        private MapReduceOutput(
            BsonValue output
        ) {
            this.output = output;
        }
        #endregion

        #region implicit operators
        /// <summary>
        /// Allows strings to be implicitly used as the name of the output collection.
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static implicit operator MapReduceOutput(
            string collectionName
        ) {
            return MapReduceOutput.Replace(collectionName);
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should returned inline.
        /// </summary>
        public static MapReduceOutput Inline {
            get {
                var output = new BsonDocument("inline", 1);
                return new MapReduceOutput(output);
            }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (replaces the entire collection).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Replace(
            string collectionName
        ) {
            var output = collectionName;
            return new MapReduceOutput(output);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (replaces the entire collection).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Replace(
            string databaseName,
            string collectionName
        ) {
            var output = new BsonDocument {
                { "replace", collectionName },
                { "db", databaseName }
            };
            return new MapReduceOutput(output);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (adding new values and overwriting existing ones).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Merge(
            string collectionName
        ) {
            var output = new BsonDocument("merge", collectionName);
            return new MapReduceOutput(output);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (adding new values and overwriting existing ones).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Merge(
            string databaseName,
            string collectionName
        ) {
            var output = new BsonDocument {
                { "merge", collectionName },
                { "db", databaseName }
            };
            return new MapReduceOutput(output);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (using the reduce function to combine new values with existing values).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Reduce(
            string collectionName
        ) {
            var output = new BsonDocument("reduce", collectionName);
            return new MapReduceOutput(output);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (using the reduce function to combine new values with existing values).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Reduce(
            string databaseName,
            string collectionName
        ) {
            var output = new BsonDocument {
                { "reduce", collectionName },
                { "db", databaseName }
            };
            return new MapReduceOutput(output);
        }
        #endregion

        #region internal methods
        internal BsonValue ToBsonValue() {
            return output;
        }
        #endregion
    }

    /// <summary>
    /// A builder for the options of a Map/Reduce operation.
    /// </summary>
    public static class MapReduceOptions {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoMapReduceOptions.
        /// </summary>
        public static IMongoMapReduceOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Sets the finalize function.
        /// </summary>
        /// <param name="finalize">The finalize function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetFinalize(
            BsonJavaScript finalize
        ) {
            return new MapReduceOptionsBuilder().SetFinalize(finalize);
        }

        /// <summary>
        /// Sets whether to keep the temp collection (obsolete in 1.8.0+).
        /// </summary>
        /// <param name="value">Whether to keep the temp collection.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetKeepTemp(
            bool value
        ) {
            return new MapReduceOptionsBuilder().SetKeepTemp(value);
        }

        /// <summary>
        /// Sets the number of documents to send to the map function (useful in combination with SetSortOrder).
        /// </summary>
        /// <param name="value">The number of documents to send to the map function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetLimit(
            int value
        ) {
            return new MapReduceOptionsBuilder().SetLimit(value);
        }

        /// <summary>
        /// Sets the output option (see MapReduceOutput).
        /// </summary>
        /// <param name="output">The output option.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetOutput(
            MapReduceOutput output
        ) {
            return new MapReduceOptionsBuilder().SetOutput(output);
        }

        /// <summary>
        /// Sets the optional query that filters which documents are sent to the map function (also useful in combination with SetSortOrder and SetLimit).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetQuery(
            IMongoQuery query
        ) {
            return new MapReduceOptionsBuilder().SetQuery(query);
        }

        /// <summary>
        /// Sets a scope that contains variables that can be accessed by the map, reduce and finalize functions.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetScope(
            IMongoScope scope
        ) {
            return new MapReduceOptionsBuilder().SetScope(scope);
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetSortOrder(
            IMongoSortBy sortBy
        ) {
            return new MapReduceOptionsBuilder().SetSortOrder(sortBy);
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="keys">The names of the keys to sort by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetSortOrder(
            params string[] keys
        ) {
            return new MapReduceOptionsBuilder().SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Sets whether the server should be more verbose when logging map/reduce operations.
        /// </summary>
        /// <param name="value">Whether the server should be more verbose.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetVerbose(
            bool value
        ) {
            return new MapReduceOptionsBuilder().SetVerbose(value);
        }
        #endregion
    }

    /// <summary>
    /// A builder for the options of a Map/Reduce operation.
    /// </summary>
    [Serializable]
    public class MapReduceOptionsBuilder : BuilderBase, IMongoMapReduceOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the MapReduceOptionsBuilder class.
        /// </summary>
        public MapReduceOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Sets the finalize function.
        /// </summary>
        /// <param name="finalize">The finalize function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetFinalize(
            BsonJavaScript finalize
        ) {
            document["finalize"] = finalize;
            return this;
        }

        /// <summary>
        /// Sets whether to keep the temp collection (obsolete in 1.8.0+).
        /// </summary>
        /// <param name="value">Whether to keep the temp collection.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetKeepTemp(
            bool value
        ) {
            document["keeptemp"] = value;
            return this;
        }

        /// <summary>
        /// Sets the number of documents to send to the map function (useful in combination with SetSortOrder).
        /// </summary>
        /// <param name="value">The number of documents to send to the map function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetLimit(
            int value
        ) {
            document["limit"] = value;
            return this;
        }

        /// <summary>
        /// Sets the output option (see MapReduceOutput).
        /// </summary>
        /// <param name="output">The output option.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetOutput(
            MapReduceOutput output
        ) {
            document["out"] = output.ToBsonValue();
            return this;
        }

        /// <summary>
        /// Sets the optional query that filters which documents are sent to the map function (also useful in combination with SetSortOrder and SetLimit).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetQuery(
            IMongoQuery query
        ) {
            document["query"] = BsonDocumentWrapper.Create(query);
            return this;
        }

        /// <summary>
        /// Sets a scope that contains variables that can be accessed by the map, reduce and finalize functions.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetScope(
            IMongoScope scope
        ) {
            document["scope"] = BsonDocumentWrapper.Create(scope);
            return this;
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetSortOrder(
            IMongoSortBy sortBy
        ) {
            document["sort"] = BsonDocumentWrapper.Create(sortBy);
            return this;
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="keys">The names of the keys to sort by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetSortOrder(
            params string[] keys
        ) {
            return SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Sets whether the server should be more verbose when logging map/reduce operations.
        /// </summary>
        /// <param name="value">Whether the server should be more verbose.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetVerbose(
            bool value
        ) {
            document["verbose"] = value;
            return this;
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
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
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
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
