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
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    public static class GeoHaystackSearchOptions {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoGeoHaystackSearchOptions.
        /// </summary>
        public static IMongoGeoHaystackSearchOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Sets the maximum number of results to return.
        /// </summary>
        /// <param name="value">The maximum number of results to return.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder SetLimit(
            int value
        ) {
            return new GeoHaystackSearchOptionsBuilder().SetLimit(value);
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder SetMaxDistance(
            double value
        ) {
            return new GeoHaystackSearchOptionsBuilder().SetMaxDistance(value);
        }

        /// <summary>
        /// Sets the query on the optional additional field.
        /// </summary>
        /// <param name="additionalFieldName">The name of the additional field.</param>
        /// <param name="value">The value fo the additional field.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder SetQuery(
            string additionalFieldName,
            BsonValue value
        ) {
            return new GeoHaystackSearchOptionsBuilder().SetQuery(additionalFieldName, value);
        }
        #endregion
    }

    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    [Serializable]
    public class GeoHaystackSearchOptionsBuilder : BuilderBase, IMongoGeoHaystackSearchOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the GeoHaystackSearchOptionsBuilder class.
        /// </summary>
        public GeoHaystackSearchOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Sets the maximum number of results to return.
        /// </summary>
        /// <param name="value">The maximum number of results to return.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder SetLimit(
            int value
        ) {
            document["limit"] = value;
            return this;
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder SetMaxDistance(
            double value
        ) {
            document["maxDistance"] = value;
            return this;
        }

        /// <summary>
        /// Sets the query on the optional additional field.
        /// </summary>
        /// <param name="additionalFieldName">The name of the additional field.</param>
        /// <param name="value">The value fo the additional field.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder SetQuery(
            string additionalFieldName,
            BsonValue value
        ) {
            document["search"] = new BsonDocument(additionalFieldName, value);
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
