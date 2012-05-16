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
using System.Linq.Expressions;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    public static class GeoHaystackSearchOptions
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoGeoHaystackSearchOptions.
        /// </summary>
        public static IMongoGeoHaystackSearchOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets the maximum number of results to return.
        /// </summary>
        /// <param name="value">The maximum number of results to return.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder SetLimit(int value)
        {
            return new GeoHaystackSearchOptionsBuilder().SetLimit(value);
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder SetMaxDistance(double value)
        {
            return new GeoHaystackSearchOptionsBuilder().SetMaxDistance(value);
        }

        /// <summary>
        /// Sets the query on the optional additional field.
        /// </summary>
        /// <param name="additionalFieldName">The name of the additional field.</param>
        /// <param name="value">The value fo the additional field.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder SetQuery(string additionalFieldName, BsonValue value)
        {
            return new GeoHaystackSearchOptionsBuilder().SetQuery(additionalFieldName, value);
        }
    }

    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    [Serializable]
    public class GeoHaystackSearchOptionsBuilder : BuilderBase, IMongoGeoHaystackSearchOptions
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the GeoHaystackSearchOptionsBuilder class.
        /// </summary>
        public GeoHaystackSearchOptionsBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Sets the maximum number of results to return.
        /// </summary>
        /// <param name="value">The maximum number of results to return.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder SetLimit(int value)
        {
            _document["limit"] = value;
            return this;
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder SetMaxDistance(double value)
        {
            _document["maxDistance"] = value;
            return this;
        }

        /// <summary>
        /// Sets the query on the optional additional field.
        /// </summary>
        /// <param name="additionalFieldName">The name of the additional field.</param>
        /// <param name="value">The value fo the additional field.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder SetQuery(string additionalFieldName, BsonValue value)
        {
            _document["search"] = new BsonDocument(additionalFieldName, value);
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

    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class GeoHaystackSearchOptions<TDocument>
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoGeoHaystackSearchOptions.
        /// </summary>
        public static IMongoGeoHaystackSearchOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets the maximum number of results to return.
        /// </summary>
        /// <param name="value">The maximum number of results to return.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder<TDocument> SetLimit(int value)
        {
            return new GeoHaystackSearchOptionsBuilder<TDocument>().SetLimit(value);
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder<TDocument> SetMaxDistance(double value)
        {
            return new GeoHaystackSearchOptionsBuilder<TDocument>().SetMaxDistance(value);
        }

        /// <summary>
        /// Sets the query on the optional additional field.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value fo the additional field.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GeoHaystackSearchOptionsBuilder<TDocument> SetQuery<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new GeoHaystackSearchOptionsBuilder<TDocument>().SetQuery(memberExpression, value);
        }
    }

    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [Serializable]
    public class GeoHaystackSearchOptionsBuilder<TDocument> : BuilderBase, IMongoGeoHaystackSearchOptions
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private GeoHaystackSearchOptionsBuilder _geoHaystackBuilder;

        // constructors
        /// <summary>
        /// Initializes a new instance of the GeoHaystackSearchOptionsBuilder class.
        /// </summary>
        public GeoHaystackSearchOptionsBuilder()
        {
            _serializationInfoHelper = new BsonSerializationInfoHelper();
            _geoHaystackBuilder = new GeoHaystackSearchOptionsBuilder();
        }

        // public methods
        /// <summary>
        /// Sets the maximum number of results to return.
        /// </summary>
        /// <param name="value">The maximum number of results to return.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder<TDocument> SetLimit(int value)
        {
            _geoHaystackBuilder = _geoHaystackBuilder.SetLimit(value);
            return this;
        }

        /// <summary>
        /// Sets the max distance.
        /// </summary>
        /// <param name="value">The max distance.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder<TDocument> SetMaxDistance(double value)
        {
            _geoHaystackBuilder = _geoHaystackBuilder.SetMaxDistance(value);
            return this;
        }

        /// <summary>
        /// Sets the query on the optional additional field.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value fo the additional field.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GeoHaystackSearchOptionsBuilder<TDocument> SetQuery<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            _geoHaystackBuilder = _geoHaystackBuilder.SetQuery(serializationInfo.ElementName, serializedValue);
            return this;
        }

        /// <summary>
        /// Converts this object to a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _geoHaystackBuilder.ToBsonDocument();
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
            ((IBsonSerializable)_geoHaystackBuilder).Serialize(bsonWriter, nominalType, options);
        }
    }
}
