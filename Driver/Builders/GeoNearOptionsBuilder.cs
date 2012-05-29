/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for the options of the GeoNear command.
    /// </summary>
    public static class GeoNearOptions
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoGeoNearOptions.
        /// </summary>
        public static IMongoGeoNearOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets the distance multiplier.
        /// </summary>
        /// <param name="value">The distance multiplier.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoNearOptionsBuilder SetDistanceMultiplier(double value)
        {
            return new GeoNearOptionsBuilder().SetDistanceMultiplier(value);
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoNearOptionsBuilder SetMaxDistance(double value)
        {
            return new GeoNearOptionsBuilder().SetMaxDistance(value);
        }

        /// <summary>
        /// Sets whether to use a spherical search.
        /// </summary>
        /// <param name="value">Whether to use a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoNearOptionsBuilder SetSpherical(bool value)
        {
            return new GeoNearOptionsBuilder().SetSpherical(value);
        }
    }

    /// <summary>
    /// A builder for the options of the GeoNear command.
    /// </summary>
    [Serializable]
    public class GeoNearOptionsBuilder : BuilderBase, IMongoGeoNearOptions
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the GeoNearOptionsBuilder class.
        /// </summary>
        public GeoNearOptionsBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Sets the distance multiplier.
        /// </summary>
        /// <param name="value">The distance multiplier.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoNearOptionsBuilder SetDistanceMultiplier(double value)
        {
            _document["distanceMultiplier"] = value;
            return this;
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoNearOptionsBuilder SetMaxDistance(double value)
        {
            _document["maxDistance"] = value;
            return this;
        }

        /// <summary>
        /// Sets whether to use a spherical search.
        /// </summary>
        /// <param name="value">Whether to use a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoNearOptionsBuilder SetSpherical(bool value)
        {
            if (value)
            {
                _document["spherical"] = true;
            }
            else
            {
                _document.Remove("spherical");
            }
            return this;
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // protected methods
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected override void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            ((IBsonSerializable)_document).Serialize(bsonWriter, nominalType, options);
        }
    }
}
