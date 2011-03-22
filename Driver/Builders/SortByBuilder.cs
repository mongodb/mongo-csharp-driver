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
    /// A builder for specifying a sort order.
    /// </summary>
    public static class SortBy {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoSortBy.
        /// </summary>
        public static IMongoSortBy Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Adds keys to be sorted by in ascending order.
        /// </summary>
        /// <param name="keys">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static SortByBuilder Ascending(
            params string[] keys
        ) {
            return new SortByBuilder().Ascending(keys);
        }

        /// <summary>
        /// Adds keys to be sorted by in descending order.
        /// </summary>
        /// <param name="keys">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static SortByBuilder Descending(
            params string[] keys
        ) {
            return new SortByBuilder().Descending(keys);
        }
        #endregion
    }

    /// <summary>
    /// A builder for specifying a sort order.
    /// </summary>
    [Serializable]
    public class SortByBuilder : BuilderBase, IMongoSortBy {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the SortByBuider class.
        /// </summary>
        public SortByBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Adds keys to be sorted by in ascending order.
        /// </summary>
        /// <param name="keys">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public SortByBuilder Ascending(
            params string[] keys
        ) {
            foreach (var key in keys) {
                document.Add(key, 1);
            }
            return this;
        }

        /// <summary>
        /// Adds keys to be sorted by in descending order.
        /// </summary>
        /// <param name="keys">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public SortByBuilder Descending(
            params string[] keys
        ) {
            foreach (var key in keys) {
                document.Add(key, -1);
            }
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

        #region protected
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
