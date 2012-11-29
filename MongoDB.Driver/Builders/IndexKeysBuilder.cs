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
    /// A builder for specifying the keys for an index.
    /// </summary>
    public static class IndexKeys
    {
        // public static methods
        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Ascending(params string[] names)
        {
            return new IndexKeysBuilder().Ascending(names);
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Descending(params string[] names)
        {
            return new IndexKeysBuilder().Descending(names);
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatial(string name)
        {
            return new IndexKeysBuilder().GeoSpatial(name);
        }

        /// <summary>
        /// Sets the key name to create a geospatial haystack index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialHaystack(string name)
        {
            return new IndexKeysBuilder().GeoSpatialHaystack(name);
        }

        /// <summary>
        /// Sets the key name and additional field name to create a geospatial haystack index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="additionalName">The name of an additional field to index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialHaystack(string name, string additionalName)
        {
            return new IndexKeysBuilder().GeoSpatialHaystack(name, additionalName);
        }
    }

    /// <summary>
    /// A builder for specifying the keys for an index.
    /// </summary>
    [Serializable]
    public class IndexKeysBuilder : BuilderBase, IMongoIndexKeys
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the IndexKeysBuilder class.
        /// </summary>
        public IndexKeysBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder Ascending(params string[] names)
        {
            foreach (var name in names)
            {
                _document.Add(name, 1);
            }
            return this;
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder Descending(params string[] names)
        {
            foreach (var name in names)
            {
                _document.Add(name, -1);
            }
            return this;
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder GeoSpatial(string name)
        {
            _document.Add(name, "2d");
            return this;
        }

        /// <summary>
        /// Sets the key name to create a geospatial haystack index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder GeoSpatialHaystack(string name)
        {
            return GeoSpatialHaystack(name, null);
        }

        /// <summary>
        /// Sets the key name and additional field name to create a geospatial haystack index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="additionalName">The name of an additional field to index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder GeoSpatialHaystack(string name, string additionalName)
        {
            _document.Add(name, "geoHaystack");
            _document.Add(additionalName, 1, additionalName != null);
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
    /// A builder for specifying the keys for an index.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class IndexKeys<TDocument>
    {
        // public static methods
        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> Ascending(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return new IndexKeysBuilder<TDocument>().Ascending(memberExpressions);
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> Descending(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return new IndexKeysBuilder<TDocument>().Descending(memberExpressions);
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> GeoSpatial<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return new IndexKeysBuilder<TDocument>().GeoSpatial(memberExpression);
        }

        /// <summary>
        /// Sets the key name to create a geospatial haystack index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> GeoSpatialHaystack<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return new IndexKeysBuilder<TDocument>().GeoSpatialHaystack(memberExpression);
        }

        /// <summary>
        /// Sets the key name and additional field name to create a geospatial haystack index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TAdditionalMember">The type of the additional member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="additionalMemberExpression">The additional member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> GeoSpatialHaystack<TMember, TAdditionalMember>(Expression<Func<TDocument, TMember>> memberExpression, Expression<Func<TDocument, TAdditionalMember>> additionalMemberExpression)
        {
            return new IndexKeysBuilder<TDocument>().GeoSpatialHaystack(memberExpression, additionalMemberExpression);
        }
    }

    /// <summary>
    /// A builder for specifying the keys for an index.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [Serializable]
    public class IndexKeysBuilder<TDocument> : BuilderBase, IMongoIndexKeys
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private IndexKeysBuilder _indexKeysBuilder;

        // constructors
        /// <summary>
        /// Initializes a new instance of the IndexKeysBuilder class.
        /// </summary>
        public IndexKeysBuilder()
        {
            _serializationInfoHelper = new BsonSerializationInfoHelper();
            _indexKeysBuilder = new IndexKeysBuilder();
        }

        // public methods
        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="memberExpressions">One or more key names.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> Ascending(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            _indexKeysBuilder = _indexKeysBuilder.Ascending(GetElementNames(memberExpressions).ToArray());
            return this;
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> Descending(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            _indexKeysBuilder = _indexKeysBuilder.Descending(GetElementNames(memberExpressions).ToArray());
            return this;
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> GeoSpatial<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatial(serializationInfo.ElementName);
            return this;
        }

        /// <summary>
        /// Sets the key name to create a geospatial haystack index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> GeoSpatialHaystack<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatialHaystack(serializationInfo.ElementName);
            return this;
        }

        /// <summary>
        /// Sets the key name and additional field name to create a geospatial haystack index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TAdditionalMember">The type of the additional member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="additionalMemberExpression">The additional member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> GeoSpatialHaystack<TMember, TAdditionalMember>(Expression<Func<TDocument, TMember>> memberExpression, Expression<Func<TDocument, TAdditionalMember>> additionalMemberExpression)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var additionalSerializationInfo = _serializationInfoHelper.GetSerializationInfo(additionalMemberExpression);
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatialHaystack(serializationInfo.ElementName, additionalSerializationInfo.ElementName);
            return this;
        }

        /// <summary>
        /// Converts this object to a BsonDocument.
        /// </summary>
        /// <returns>
        /// A BsonDocument.
        /// </returns>
        public override BsonDocument ToBsonDocument()
        {
            return _indexKeysBuilder.ToBsonDocument();
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
            ((IBsonSerializable)_indexKeysBuilder).Serialize(bsonWriter, nominalType, options);
        }

        // private methods
        private IEnumerable<string> GetElementNames(IEnumerable<Expression<Func<TDocument, object>>> memberExpressions)
        {
            return memberExpressions
                .Select(x => _serializationInfoHelper.GetSerializationInfo(x))
                .Select(x => x.ElementName);
        }
    }
}
