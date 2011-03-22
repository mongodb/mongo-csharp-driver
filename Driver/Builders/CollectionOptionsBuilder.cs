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
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver.Builders {
    /// <summary>
    /// A builder for the options used when creating a collection.
    /// </summary>
    public static class CollectionOptions {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoCollectionOptions.
        /// </summary>
        public static IMongoCollectionOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Sets whether to automatically create an index on the _id element.
        /// </summary>
        /// <param name="value">Whether to automatically create an index on the _id element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetAutoIndexId(
            bool value
        ) {
            return new CollectionOptionsBuilder().SetAutoIndexId(value);
        }

        /// <summary>
        /// Sets whether the collection is capped.
        /// </summary>
        /// <param name="value">Whether the collection is capped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetCapped(
            bool value
        ) {
            return new CollectionOptionsBuilder().SetCapped(value);
        }

        /// <summary>
        /// Sets the max number of documents in a capped collection.
        /// </summary>
        /// <param name="value">The max number of documents.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetMaxDocuments(
            int value
        ) {
            return new CollectionOptionsBuilder().SetMaxDocuments(value);
        }

        /// <summary>
        /// Sets the max size of a capped collection.
        /// </summary>
        /// <param name="value">The max size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetMaxSize(
            long value
        ) {
            return new CollectionOptionsBuilder().SetMaxSize(value);
        }
        #endregion
    }

    /// <summary>
    /// A builder for the options used when creating a collection.
    /// </summary>
    [Serializable]
    public class CollectionOptionsBuilder : BuilderBase, IMongoCollectionOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the CollectionOptionsBuilder class.
        /// </summary>
        public CollectionOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Sets whether to automatically create an index on the _id element.
        /// </summary>
        /// <param name="value">Whether to automatically create an index on the _id element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
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

        /// <summary>
        /// Sets whether the collection is capped.
        /// </summary>
        /// <param name="value">Whether the collection is capped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
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

        /// <summary>
        /// Sets the max number of documents in a capped collection.
        /// </summary>
        /// <param name="value">The max number of documents.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetMaxDocuments(
            int value
        ) {
            document["max"] = value;
            return this;
        }

        /// <summary>
        /// Sets the max size of a capped collection.
        /// </summary>
        /// <param name="value">The max size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetMaxSize(
            long value
        ) {
            document["size"] = value;
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
