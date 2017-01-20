/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    [Obsolete("Use GeoHaystackSearchArgs instead.")]
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
#if NET45
    [Serializable]
#endif
    [Obsolete("Use GeoHaystackSearchArgs instead.")]
    [BsonSerializer(typeof(GeoHaystackSearchOptionsBuilder.Serializer))]
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

        // nested classes
        new internal class Serializer : SerializerBase<GeoHaystackSearchOptionsBuilder>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GeoHaystackSearchOptionsBuilder value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._document);
            }
        }
    }

    /// <summary>
    /// A builder for the options of the GeoHaystackSearch command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [Obsolete("Use GeoHaystackSearchArgs instead.")]
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
#if NET45
    [Serializable]
#endif
    [Obsolete("Use GeoHaystackSearchArgs instead.")]
    [BsonSerializer(typeof(GeoHaystackSearchOptionsBuilder<>.Serializer))]
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

        // nested classes
        new internal class Serializer : SerializerBase<GeoHaystackSearchOptionsBuilder<TDocument>>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GeoHaystackSearchOptionsBuilder<TDocument> value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._geoHaystackBuilder.ToBsonDocument());
            }
        }
    }
}
