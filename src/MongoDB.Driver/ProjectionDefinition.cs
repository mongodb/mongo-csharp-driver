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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Translators;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered projection.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class RenderedProjectionDefinition<TResult>
    {
        private readonly BsonDocument _projection;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedProjectionDefinition{TResult}" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public RenderedProjectionDefinition(BsonDocument document, IBsonSerializer<TResult> resultSerializer)
        {
            _projection = document;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        public BsonDocument Document
        {
            get { return _projection; }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }
    }

    /// <summary>
    /// Base class for projections whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public abstract class ProjectionDefinition<TSource>
    {
        /// <summary>
        /// Turns the projection into a projection whose result type is known.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>A typed projection.</returns>
        public virtual ProjectionDefinition<TSource, TResult> As<TResult>(IBsonSerializer<TResult> resultSerializer = null)
        {
            return new KnownResultTypeProjectionDefinitionAdapter<TSource, TResult>(this, resultSerializer);
        }

        /// <summary>
        /// Renders the projection to a <see cref="RenderedProjectionDefinition{TResult}"/>.
        /// </summary>
        /// <param name="sourceSerializer">The source serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="BsonDocument"/>.</returns>
        public abstract BsonDocument Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="ProjectionDefinition{TSource}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ProjectionDefinition<TSource>(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }

            return new BsonDocumentProjectionDefinition<TSource>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="ProjectionDefinition{TSource, TResult}" />.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ProjectionDefinition<TSource>(string json)
        {
            if (json == null)
            {
                return null;
            }

            return new JsonProjectionDefinition<TSource>(json);
        }
    }

    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class ProjectionDefinition<TSource, TResult>
    {
        /// <summary>
        /// Renders the projection to a <see cref="RenderedProjectionDefinition{TResult}"/>.
        /// </summary>
        /// <param name="sourceSerializer">The source serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedProjectionDefinition{TResult}"/>.</returns>
        public abstract RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="ProjectionDefinition{TSource, TResult}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ProjectionDefinition<TSource, TResult>(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }

            return new BsonDocumentProjectionDefinition<TSource, TResult>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="ProjectionDefinition{TSource, TResult}" />.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ProjectionDefinition<TSource, TResult>(string json)
        {
            if (json == null)
            {
                return null;
            }

            return new JsonProjectionDefinition<TSource, TResult>(json);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ProjectionDefinition{TSource}"/> to <see cref="ProjectionDefinition{TSource, TResult}"/>.
        /// </summary>
        /// <param name="projection">The projection.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ProjectionDefinition<TSource, TResult>(ProjectionDefinition<TSource> projection)
        {
            return new KnownResultTypeProjectionDefinitionAdapter<TSource, TResult>(projection);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument" /> based projection whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public sealed class BsonDocumentProjectionDefinition<TSource> : ProjectionDefinition<TSource>
    {
        private readonly BsonDocument _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentProjectionDefinition{TSource}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        public BsonDocumentProjectionDefinition(BsonDocument document)
        {
            _document = Ensure.IsNotNull(document, "document");
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        public BsonDocument Document
        {
            get { return _document; }
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return _document;
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument" /> based projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class BsonDocumentProjectionDefinition<TSource, TResult> : ProjectionDefinition<TSource, TResult>
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentProjectionDefinition{TSource, TResult}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public BsonDocumentProjectionDefinition(BsonDocument document, IBsonSerializer<TResult> resultSerializer = null)
        {
            _document = Ensure.IsNotNull(document, "document");
            _resultSerializer = resultSerializer;
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        public BsonDocument Document
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <inheritdoc />
        public override RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjectionDefinition<TResult>(
                _document,
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A find <see cref="Expression" /> based projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class FindExpressionProjectionDefinition<TSource, TResult> : ProjectionDefinition<TSource, TResult>
    {
        private readonly Expression<Func<TSource, TResult>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpressionProjectionDefinition{TSource, TResult}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public FindExpressionProjectionDefinition(Expression<Func<TSource, TResult>> expression)
        {
            _expression = Ensure.IsNotNull(expression, "expression");
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression<Func<TSource, TResult>> Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return FindProjectionTranslator.Translate<TSource, TResult>(_expression, sourceSerializer);
        }
    }

    /// <summary>
    /// A JSON <see cref="String" /> based projection whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public sealed class JsonProjectionDefinition<TSource> : ProjectionDefinition<TSource>
    {
        private readonly string _json;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjectionDefinition{TSource}"/> class.
        /// </summary>
        /// <param name="json">The json.</param>
        public JsonProjectionDefinition(string json)
        {
            _json = Ensure.IsNotNullOrEmpty(json, "json");
        }

        /// <summary>
        /// Gets the json.
        /// </summary>
        public string Json
        {
            get { return _json; }
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return BsonDocument.Parse(_json);
        }
    }

    /// <summary>
    /// A JSON <see cref="String" /> based projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class JsonProjectionDefinition<TSource, TResult> : ProjectionDefinition<TSource, TResult>
    {
        private readonly string _json;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentSortDefinition{TDocument}" /> class.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public JsonProjectionDefinition(string json, IBsonSerializer<TResult> resultSerializer = null)
        {
            _json = Ensure.IsNotNullOrEmpty(json, "json");
            _resultSerializer = resultSerializer;
        }

        /// <summary>
        /// Gets the json.
        /// </summary>
        public string Json
        {
            get { return _json; }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <inheritdoc />
        public override RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjectionDefinition<TResult>(
                BsonDocument.Parse(_json),
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// An <see cref="Object"/> based projection whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public sealed class ObjectProjectionDefinition<TSource> : ProjectionDefinition<TSource>
    {
        private readonly object _obj;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProjectionDefinition{TSource}"/> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public ObjectProjectionDefinition(object obj)
        {
            _obj = Ensure.IsNotNull(obj, "obj");
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        public object Object
        {
            get { return _obj; }
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var serializer = serializerRegistry.GetSerializer(_obj.GetType());
            return new BsonDocumentWrapper(_obj, serializer);
        }
    }

    /// <summary>
    /// An <see cref="Object"/> based projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class ObjectProjectionDefinition<TSource, TResult> : ProjectionDefinition<TSource, TResult>
    {
        private readonly object _obj;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProjectionDefinition{TSource, TResult}" /> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public ObjectProjectionDefinition(object obj, IBsonSerializer<TResult> resultSerializer = null)
        {
            _obj = Ensure.IsNotNull(obj, "obj");
            _resultSerializer = resultSerializer;
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        public object Object
        {
            get { return _obj; }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <inheritdoc />
        public override RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var serializer = serializerRegistry.GetSerializer(_obj.GetType());
            return new RenderedProjectionDefinition<TResult>(
                new BsonDocumentWrapper(_obj, serializer),
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    internal sealed class KnownResultTypeProjectionDefinitionAdapter<TSource, TResult> : ProjectionDefinition<TSource, TResult>
    {
        private readonly ProjectionDefinition<TSource> _projection;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        public KnownResultTypeProjectionDefinitionAdapter(ProjectionDefinition<TSource> projection, IBsonSerializer<TResult> resultSerializer = null)
        {
            _projection = Ensure.IsNotNull(projection, "projection");
            _resultSerializer = resultSerializer;
        }

        public ProjectionDefinition<TSource> Projection
        {
            get { return _projection; }
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public override RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = _projection.Render(sourceSerializer, serializerRegistry);
            return new RenderedProjectionDefinition<TResult>(
                document,
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    internal sealed class EntireDocumentProjectionDefinition<TSource, TResult> : ProjectionDefinition<TSource, TResult>
    {
        private readonly IBsonSerializer<TResult> _resultSerializer;

        public EntireDocumentProjectionDefinition(IBsonSerializer<TResult> resultSerializer = null)
        {
            _resultSerializer = resultSerializer;
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public override RenderedProjectionDefinition<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjectionDefinition<TResult>(
                null,
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }
}
