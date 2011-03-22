﻿/* Copyright 2010-2011 10gen Inc.
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
    /// A builder for specifying the keys for an index.
    /// </summary>
    public static class IndexKeys {
        #region public static methods
        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Ascending(
            params string[] names
        ) {
            return new IndexKeysBuilder().Ascending(names);
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Descending(
            params string[] names
        ) {
            return new IndexKeysBuilder().Descending(names);
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatial(
            string name
        ) {
            return new IndexKeysBuilder().GeoSpatial(name);
        }
        #endregion
    }

    /// <summary>
    /// A builder for specifying the keys for an index.
    /// </summary>
    [Serializable]
    public class IndexKeysBuilder : BuilderBase, IMongoIndexKeys {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the IndexKeysBuilder class.
        /// </summary>
        public IndexKeysBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder Ascending(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, 1);
            }
            return this;
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder Descending(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, -1);
            }
            return this;
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder GeoSpatial(
            string name
        ) {
            document.Add(name, "2d");
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
