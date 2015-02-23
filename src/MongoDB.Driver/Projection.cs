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
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class RenderedProjection<TDocument>
    {
        private readonly BsonDocument _projection;
        private readonly IBsonSerializer<TDocument> _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedProjection{TDocument}" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="serializer">The serializer.</param>
        public RenderedProjection(BsonDocument document, IBsonSerializer<TDocument> serializer)
        {
            _projection = document;
            _serializer = Ensure.IsNotNull(serializer, "serializer");
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
        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }
    }

    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class Projection<TDocument, TResult>
    {
        /// <summary>
        /// Renders the projection to a <see cref="RenderedProjection{TResult}"/>.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="BsonDocument"/>.</returns>
        public abstract RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="Projection{TDocument, TResult}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TDocument, TResult>(BsonDocument document)
        {
            return new BsonDocumentProjection<TDocument, TResult>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Projection{TDocument, TResult}"/>.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Projection<TDocument, TResult>(string json)
        {
            return new JsonProjection<TDocument, TResult>(json);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument" /> based projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class BsonDocumentProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentProjection{TDocument, TResult}"/> class.
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjection<TResult>(
                _document,
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A <see cref="Expression" /> based projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class ClientSideExpressionProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly Expression<Func<TDocument, TResult>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSideExpressionProjection{TDocument, TResult}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ClientSideExpressionProjection(Expression<Func<TDocument, TResult>> expression)
        {
            _expression = Ensure.IsNotNull(expression, "expression");
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression<Func<TDocument, TResult>> Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return FindProjectionTranslator.Translate<TDocument, TResult>(_expression, documentSerializer);
        }
    }

    /// <summary>
    /// A <see cref="String" /> based projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class JsonProjection<TDocument, TResult> : Projection<TDocument, TResult>
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
            _json = Ensure.IsNotNull(json, "json");
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjection<TResult>(
                BsonDocument.Parse(_json),
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A <see cref="Object"/> based projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class ObjectProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly object _obj;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectProjection{TDocument, TResult}" /> class.
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
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var serializer = serializerRegistry.GetSerializer(_obj.GetType());
            return new RenderedProjection<TResult>(
                new BsonDocumentWrapper(_obj, serializer),
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A projection that simply renders to a different type without projecting anything.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class TypeChangeProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeChangeProjection{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="resultSerializer">The result serializer.</param>
        public TypeChangeProjection(IBsonSerializer<TResult> resultSerializer = null)
        {
            _resultSerializer = resultSerializer;
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <inheritdoc />
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedProjection<TResult>(
                null,
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }
}
