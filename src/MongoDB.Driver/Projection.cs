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
    public sealed class RenderedProjection<TResult>
    {
        private readonly BsonDocument _projection;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedProjection{TResult}" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public RenderedProjection(BsonDocument document, IBsonSerializer<TResult> resultSerializer)
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
    public abstract class Projection<TSource>
    {
        /// <summary>
        /// Turns the projection into a projection whose result type is known.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>A typed projection.</returns>
        public virtual Projection<TSource, TResult> As<TResult>(IBsonSerializer<TResult> resultSerializer = null)
        {
            return new KnownResultTypeProjectionAdapter<TSource, TResult>(this, resultSerializer);
        }

        /// <summary>
        /// Renders the projection to a <see cref="RenderedProjection{TResult}"/>.
        /// </summary>
        /// <param name="sourceSerializer">The source serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="BsonDocument"/>.</returns>
        public abstract BsonDocument Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="Projection{TSource}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TSource>(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }

            return new BsonDocumentProjection<TSource>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="Projection{TSource, TResult}" />.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TSource>(string json)
        {
            if (json == null)
            {
                return null;
            }

            return new JsonProjection<TSource>(json);
        }
    }

    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class Projection<TSource, TResult>
    {
        /// <summary>
        /// Renders the projection to a <see cref="RenderedProjection{TResult}"/>.
        /// </summary>
        /// <param name="sourceSerializer">The source serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedProjection{TResult}"/>.</returns>
        public abstract RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="Projection{TSource, TResult}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TSource, TResult>(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }

            return new BsonDocumentProjection<TSource, TResult>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="Projection{TSource, TResult}" />.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TSource, TResult>(string json)
        {
            if (json == null)
            {
                return null;
            }

            return new JsonProjection<TSource, TResult>(json);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Projection{TSource}"/> to <see cref="Projection{TSource, TResult}"/>.
        /// </summary>
        /// <param name="projection">The projection.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TSource, TResult>(Projection<TSource> projection)
        {
            return new KnownResultTypeProjectionAdapter<TSource, TResult>(projection);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument" /> based projection whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public sealed class BsonDocumentProjection<TSource> : Projection<TSource>
    {
        private readonly BsonDocument _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentProjection{TSource}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        public BsonDocumentProjection(BsonDocument document)
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
    public sealed class BsonDocumentProjection<TSource, TResult> : Projection<TSource, TResult>
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentProjection{TSource, TResult}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public BsonDocumentProjection(BsonDocument document, IBsonSerializer<TResult> resultSerializer = null)
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjection<TResult>(
                _document,
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A find <see cref="Expression" /> based projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class FindExpressionProjection<TSource, TResult> : Projection<TSource, TResult>
    {
        private readonly Expression<Func<TSource, TResult>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpressionProjection{TSource, TResult}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public FindExpressionProjection(Expression<Func<TSource, TResult>> expression)
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return FindProjectionTranslator.Translate<TSource, TResult>(_expression, sourceSerializer);
        }
    }

    /// <summary>
    /// A JSON <see cref="String" /> based projection whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public sealed class JsonProjection<TSource> : Projection<TSource>
    {
        private readonly string _json;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjection{TSource}"/> class.
        /// </summary>
        /// <param name="json">The json.</param>
        public JsonProjection(string json)
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
    public sealed class JsonProjection<TSource, TResult> : Projection<TSource, TResult>
    {
        private readonly string _json;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentSort{TDocument}" /> class.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public JsonProjection(string json, IBsonSerializer<TResult> resultSerializer = null)
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjection<TResult>(
                BsonDocument.Parse(_json),
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// An <see cref="Object"/> based projection whose result type is not yet known.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    public sealed class ObjectProjection<TSource> : Projection<TSource>
    {
        private readonly object _obj;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProjection{TSource}"/> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public ObjectProjection(object obj)
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
    public sealed class ObjectProjection<TSource, TResult> : Projection<TSource, TResult>
    {
        private readonly object _obj;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProjection{TSource, TResult}" /> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public ObjectProjection(object obj, IBsonSerializer<TResult> resultSerializer = null)
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var serializer = serializerRegistry.GetSerializer(_obj.GetType());
            return new RenderedProjection<TResult>(
                new BsonDocumentWrapper(_obj, serializer),
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    internal sealed class KnownResultTypeProjectionAdapter<TSource, TResult> : Projection<TSource, TResult>
    {
        private readonly Projection<TSource> _projection;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        public KnownResultTypeProjectionAdapter(Projection<TSource> projection, IBsonSerializer<TResult> resultSerializer = null)
        {
            _projection = Ensure.IsNotNull(projection, "projection");
            _resultSerializer = resultSerializer;
        }

        public Projection<TSource> Projection
        {
            get { return _projection; }
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public override RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = _projection.Render(sourceSerializer, serializerRegistry);
            return new RenderedProjection<TResult>(
                document,
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    internal sealed class EntireDocumentProjection<TSource, TResult> : Projection<TSource, TResult>
    {
        private readonly IBsonSerializer<TResult> _resultSerializer;

        public EntireDocumentProjection(IBsonSerializer<TResult> resultSerializer = null)
        {
            _resultSerializer = resultSerializer;
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public override RenderedProjection<TResult> Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjection<TResult>(
                null,
                _resultSerializer ?? (sourceSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }
}
