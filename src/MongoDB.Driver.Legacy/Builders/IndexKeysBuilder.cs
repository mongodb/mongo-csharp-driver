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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
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

        /// <summary>
        /// Sets the key name to create a spherical geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialSpherical(string name)
        {
            return new IndexKeysBuilder().GeoSpatialSpherical(name);
        }

        /// <summary>
        /// Sets the key name to create a hashed index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Hashed(string name)
        {
            return new IndexKeysBuilder().Hashed(name);
        }

        /// <summary>
        /// Sets one or more key names to include in the text index.
        /// </summary>
        /// <param name="names">List of key names to include in the text index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Text(params string[] names)
        {
            return new IndexKeysBuilder().Text(names);
        }

        /// <summary>
        /// Create a text index that indexes all text fields of a document.
        /// </summary>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder TextAll()
        {
            return new IndexKeysBuilder().TextAll();
        }
    }

    /// <summary>
    /// A builder for specifying the keys for an index.
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(IndexKeysBuilder.Serializer))]
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
        /// Sets the key name to create a spherical geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder GeoSpatialSpherical(string name)
        {
            _document.Add(name, "2dsphere");
            return this;
        }

        /// <summary>
        /// Sets the key name to create a hashed index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder Hashed(string name)
        {
            _document.Add(name, "hashed");
            return this;
        }

        /// <summary>
        /// Sets one or more key names to include in the text index.
        /// </summary>
        /// <param name="names">List of key names to include in the text index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder Text(params string[] names)
        {
            foreach (var name in names)
            {
                _document.Add(name, "text");
            }
            return this;
        }

        /// <summary>
        /// Create a text index that indexes all text fields of a document.
        /// </summary>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder TextAll()
        {
            _document.Add("$**", "text");
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

        // nested class
        new internal class Serializer : SerializerBase<IndexKeysBuilder>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IndexKeysBuilder value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._document);
            }
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

        /// <summary>
        /// Sets the key name to create a spherical geospatial index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> GeoSpatialSpherical<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return new IndexKeysBuilder<TDocument>().GeoSpatialSpherical(memberExpression);
        }

        /// <summary>
        /// Sets the key name to create a hashed index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexKeysBuilder<TDocument> Hashed<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return new IndexKeysBuilder<TDocument>().Hashed(memberExpression);
        }

        /// <summary>
        /// Sets one or more key names to include in the text index.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder<TDocument> Text(params Expression<Func<TDocument, string>>[] memberExpressions)
        {
            return new IndexKeysBuilder<TDocument>().Text(memberExpressions);
        }

        /// <summary>
        /// Sets one or more key names to include in the text index.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder<TDocument> Text(params Expression<Func<TDocument, IEnumerable<string>>>[] memberExpressions)
        {
            return new IndexKeysBuilder<TDocument>().Text(memberExpressions);
        }

        /// <summary>
        /// Create a text index that indexes all text fields of a document.
        /// </summary>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder<TDocument> TextAll()
        {
            return new IndexKeysBuilder<TDocument>().TextAll();
        }

    }

    /// <summary>
    /// A builder for specifying the keys for an index.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(IndexKeysBuilder<>.Serializer))]
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
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatial(GetElementName(memberExpression));
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
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatialHaystack(GetElementName(memberExpression));
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
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatialHaystack(GetElementName(memberExpression), GetElementName(additionalMemberExpression));
            return this;
        }

        /// <summary>
        /// Sets the key name to create a spherical geospatial index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> GeoSpatialSpherical<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            _indexKeysBuilder = _indexKeysBuilder.GeoSpatialSpherical(GetElementName(memberExpression));
            return this;
        }

        /// <summary>
        /// Sets the key name to create a hashed index on.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexKeysBuilder<TDocument> Hashed<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            _indexKeysBuilder = _indexKeysBuilder.Hashed(GetElementName(memberExpression));
            return this;
        }

        /// <summary>
        /// Sets one or more key names to include in the text index.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder<TDocument> Text(params Expression<Func<TDocument, string>>[] memberExpressions)
        {
            _indexKeysBuilder = _indexKeysBuilder.Text(GetElementNames(memberExpressions).ToArray());
            return this;
        }

        /// <summary>
        /// Sets one or more key names to include in the text index.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder<TDocument> Text(params Expression<Func<TDocument, IEnumerable<string>>>[] memberExpressions)
        {
            _indexKeysBuilder = _indexKeysBuilder.Text(GetElementNames(memberExpressions).ToArray());
            return this;
        }

        /// <summary>
        /// Create a text index that indexes all text fields of a document.
        /// </summary>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexKeysBuilder<TDocument> TextAll()
        {
            _indexKeysBuilder = _indexKeysBuilder.TextAll();
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

        // private methods
        private string GetElementName<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return _serializationInfoHelper.GetSerializationInfo(memberExpression).ElementName;
        }

        private IEnumerable<string> GetElementNames<TMember>(IEnumerable<Expression<Func<TDocument, TMember>>> memberExpressions)
        {
            return memberExpressions.Select(x => GetElementName(x));
        }

        // nested classes
        new internal class Serializer : SerializerBase<IndexKeysBuilder<TDocument>>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IndexKeysBuilder<TDocument> value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._indexKeysBuilder.ToBsonDocument());
            }
        }
    }
}
