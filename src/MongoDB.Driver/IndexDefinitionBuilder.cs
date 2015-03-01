/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for an index definition.
    /// </summary>
    public static class IndexDefinitionExtensions
    {
        private static class BuilderCache<TDocument>
        {
            public static IndexDefinitionBuilder<TDocument> Instance = new IndexDefinitionBuilder<TDocument>();
        }

        /// <summary>
        /// Combines an existing index definition with an ascending index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Ascending<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Ascending(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with an ascending index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Ascending<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Ascending(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a descending index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Descending<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Descending(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a descending index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Descending<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Descending(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a 2d index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Geo2D<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Geo2D(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a 2d index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Geo2D<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Geo2D(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a geo haystack index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="additionalFieldName">Name of the additional field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> GeoHaystack<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName, FieldName<TDocument> additionalFieldName = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.GeoHaystack(fieldName, additionalFieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a geo haystack index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="additionalFieldName">Name of the additional field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> GeoHaystack<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName, Expression<Func<TDocument, object>> additionalFieldName = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.GeoHaystack(fieldName, additionalFieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a 2dsphere index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Geo2DSphere<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Geo2DSphere(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a 2dsphere index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Geo2DSphere<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Geo2DSphere(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a hashed index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Hashed<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Hashed(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a hashed index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Hashed<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Hashed(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a text index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Text<TDocument>(this IndexDefinition<TDocument> definition, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Text(fieldName));
        }

        /// <summary>
        /// Combines an existing index definition with a text index definition.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="definition">The definition.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined index definition.
        /// </returns>
        public static IndexDefinition<TDocument> Text<TDocument>(this IndexDefinition<TDocument> definition, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(definition, builder.Text(fieldName));
        }
    }

    /// <summary>
    /// A builder for an <see cref="IndexDefinition{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class IndexDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// Creates an ascending index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An ascending index definition.</returns>
        public IndexDefinition<TDocument> Ascending(FieldName<TDocument> fieldName)
        {
            return new DirectionalIndexDefinition<TDocument>(fieldName, SortDirection.Ascending);
        }

        /// <summary>
        /// Creates an ascending index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An ascending index definition.</returns>
        public IndexDefinition<TDocument> Ascending(Expression<Func<TDocument, object>> fieldName)
        {
            return Ascending(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a combined index definition.
        /// </summary>
        /// <param name="definitions">The definitions.</param>
        /// <returns>A combined index definition.</returns>
        public IndexDefinition<TDocument> Combine(params IndexDefinition<TDocument>[] definitions)
        {
            return Combine((IEnumerable<IndexDefinition<TDocument>>)definitions);
        }

        /// <summary>
        /// Creates a combined index definition.
        /// </summary>
        /// <param name="definitions">The definitions.</param>
        /// <returns>A combined index definition.</returns>
        public IndexDefinition<TDocument> Combine(IEnumerable<IndexDefinition<TDocument>> definitions)
        {
            return new CombinedIndexDefinition<TDocument>(definitions);
        }

        /// <summary>
        /// Creates a descending index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A descending index definition.</returns>
        public IndexDefinition<TDocument> Descending(FieldName<TDocument> fieldName)
        {
            return new DirectionalIndexDefinition<TDocument>(fieldName, SortDirection.Descending);
        }

        /// <summary>
        /// Creates a descending index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A descending index definition.</returns>
        public IndexDefinition<TDocument> Descending(Expression<Func<TDocument, object>> fieldName)
        {
            return Descending(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a 2d index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A 2d index definition.</returns>
        public IndexDefinition<TDocument> Geo2D(FieldName<TDocument> fieldName)
        {
            return new SimpleIndexDefinition<TDocument>(fieldName, "2d");
        }

        /// <summary>
        /// Creates a 2d index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A 2d index definition.</returns>
        public IndexDefinition<TDocument> Geo2D(Expression<Func<TDocument, object>> fieldName)
        {
            return Geo2D(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a geo haystack index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="additionalFieldName">Name of the additional field.</param>
        /// <returns>
        /// A geo haystack index definition.
        /// </returns>
        public IndexDefinition<TDocument> GeoHaystack(FieldName<TDocument> fieldName, FieldName<TDocument> additionalFieldName = null)
        {
            return new GeoHaystackIndexDefinition<TDocument>(fieldName, additionalFieldName);
        }

        /// <summary>
        /// Creates a geo haystack index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="additionalFieldName">Name of the additional field.</param>
        /// <returns>
        /// A geo haystack index definition.
        /// </returns>
        public IndexDefinition<TDocument> GeoHaystack(Expression<Func<TDocument, object>> fieldName, Expression<Func<TDocument, object>> additionalFieldName = null)
        {
            FieldName<TDocument> additional = additionalFieldName == null ? null : new ExpressionFieldName<TDocument>(additionalFieldName);
            return GeoHaystack(new ExpressionFieldName<TDocument>(fieldName), additional);
        }

        /// <summary>
        /// Creates a 2dsphere index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A 2dsphere index definition.</returns>
        public IndexDefinition<TDocument> Geo2DSphere(FieldName<TDocument> fieldName)
        {
            return new SimpleIndexDefinition<TDocument>(fieldName, "2dsphere");
        }

        /// <summary>
        /// Creates a 2dsphere index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A 2dsphere index definition.</returns>
        public IndexDefinition<TDocument> Geo2DSphere(Expression<Func<TDocument, object>> fieldName)
        {
            return Geo2DSphere(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a hashed index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A hashed index definition.</returns>
        public IndexDefinition<TDocument> Hashed(FieldName<TDocument> fieldName)
        {
            return new SimpleIndexDefinition<TDocument>(fieldName, "hashed");
        }

        /// <summary>
        /// Creates a hashed index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A hashed index definition.</returns>
        public IndexDefinition<TDocument> Hashed(Expression<Func<TDocument, object>> fieldName)
        {
            return Hashed(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a text index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A text index definition.</returns>
        public IndexDefinition<TDocument> Text(FieldName<TDocument> fieldName)
        {
            return new SimpleIndexDefinition<TDocument>(fieldName, "text");
        }

        /// <summary>
        /// Creates a text index definition.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A text index definition.</returns>
        public IndexDefinition<TDocument> Text(Expression<Func<TDocument, object>> fieldName)
        {
            return Text(new ExpressionFieldName<TDocument>(fieldName));
        }
    }

    internal sealed class CombinedIndexDefinition<TDocument> : IndexDefinition<TDocument>
    {
        private readonly List<IndexDefinition<TDocument>> _definitions;

        public CombinedIndexDefinition(IEnumerable<IndexDefinition<TDocument>> definitions)
        {
            _definitions = Ensure.IsNotNull(definitions, "definitions").ToList();
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();

            foreach (var definition in _definitions)
            {
                var renderedDefinition = definition.Render(documentSerializer, serializerRegistry);

                foreach (var element in renderedDefinition.Elements)
                {
                    if (document.Contains(element.Name))
                    {
                        var message = string.Format(
                            "The index definition contains multiple values for the field '{0}'.",
                            element.Name);
                        throw new MongoException(message);
                    }
                    document.Add(element);
                }
            }

            return document;
        }
    }

    internal sealed class DirectionalIndexDefinition<TDocument> : IndexDefinition<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly SortDirection _direction;

        public DirectionalIndexDefinition(FieldName<TDocument> fieldName, SortDirection direction)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _direction = direction;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            BsonValue value;
            switch (_direction)
            {
                case SortDirection.Ascending:
                    value = 1;
                    break;
                case SortDirection.Descending:
                    value = -1;
                    break;
                default:
                    throw new InvalidOperationException("Unknown value for " + typeof(SortDirection) + ".");
            }

            return new BsonDocument(renderedFieldName, value);
        }
    }

    internal sealed class GeoHaystackIndexDefinition<TDocument> : IndexDefinition<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly FieldName<TDocument> _additionalFieldName;

        public GeoHaystackIndexDefinition(FieldName<TDocument> fieldName, FieldName<TDocument> additionalFieldName = null)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _additionalFieldName = additionalFieldName;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            var document = new BsonDocument(renderedFieldName, "geoHaystack");
            if (_additionalFieldName != null)
            {
                var additionalFieldName = _additionalFieldName.Render(documentSerializer, serializerRegistry);
                document.Add(additionalFieldName, 1);
            }

            return document;
        }
    }

    internal sealed class SimpleIndexDefinition<TDocument> : IndexDefinition<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly string _type;

        public SimpleIndexDefinition(FieldName<TDocument> fieldName, string type)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _type = type;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);
            return new BsonDocument(renderedFieldName, _type);
        }
    }
}
