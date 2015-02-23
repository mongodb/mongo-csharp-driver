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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered pipeline stage.
    /// </summary>
    public interface IRenderedPipelineStage
    {
        /// <summary>
        /// Gets the name of the stage.
        /// </summary>
        /// <value>
        /// The name of the stage.
        /// </value>
        string StageName { get; }

        /// <summary>
        /// Gets the document.
        /// </summary>
        BsonDocument Document { get; }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        IBsonSerializer Serializer { get; }
    }

    /// <summary>
    /// A rendered pipeline stage.
    /// </summary>
    public class RenderedPipelineStage<TResult> : IRenderedPipelineStage
    {
        private string _stageName;
        private BsonDocument _document;
        private IBsonSerializer<TResult> _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedPipelineStage{TResult}"/> class.
        /// </summary>
        /// <param name="stageName">Name of the stage.</param>
        /// <param name="document">The document.</param>
        /// <param name="serializer">The serializer.</param>
        public RenderedPipelineStage(string stageName, BsonDocument document, IBsonSerializer<TResult> serializer)
        {
            _stageName = Ensure.IsNotNull(stageName, "stageName");
            _document = Ensure.IsNotNull(document, "document");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        /// <inheritdoc />
        public BsonDocument Document
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer<TResult> Serializer
        {
            get { return _serializer; }
        }

        /// <inheritdoc />
        public string StageName
        {
            get { return _stageName; }
        }

        /// <inheritdoc />
        IBsonSerializer IRenderedPipelineStage.Serializer
        {
            get { return _serializer; }
        }
    }

    /// <summary>
    /// A pipeline stage.
    /// </summary>
    public interface IPipelineStage
    {
        /// <summary>
        /// Gets the name of the stage.
        /// </summary>
        string StageName { get; }

        /// <summary>
        /// Renders the specified document serializer.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>An <see cref="IRenderedPipelineStage" /></returns>
        IRenderedPipelineStage Render(IBsonSerializer documentSerializer, IBsonSerializerRegistry serializerRegistry);
    }

    /// <summary>
    /// Base class for pipeline stages.
    /// </summary>
    public abstract class PipelineStage<TDocument, TResult> : IPipelineStage
    {
        /// <inheritdoc />
        public abstract string StageName { get; }

        /// <summary>
        /// Renders the specified document serializer.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedPipelineStage{TResult}" /></returns>
        public abstract RenderedPipelineStage<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="PipelineStage{TDocument, TResult}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineStage<TDocument, TResult>(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }

            return new BsonDocumentPipelineStage<TDocument, TResult>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="PipelineStage{TDocument, TResult}"/>.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineStage<TDocument, TResult>(string json)
        {
            if (json == null)
            {
                return null;
            }

            return new JsonStage<TDocument, TResult>(json);
        }

        /// <inheritdoc />
        IRenderedPipelineStage IPipelineStage.Render(IBsonSerializer documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return Render((IBsonSerializer<TDocument>)documentSerializer, serializerRegistry);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument"/> based stage.
    /// </summary>
    public sealed class BsonDocumentPipelineStage<TDocument, TResult> : PipelineStage<TDocument, TResult>
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentPipelineStage{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public BsonDocumentPipelineStage(BsonDocument document, IBsonSerializer<TResult> resultSerializer = null)
        {
            _document = Ensure.IsNotNull(document, "document");
            _resultSerializer = resultSerializer;
        }

        /// <inheritdoc />
        public override string StageName
        {
            get { return _document.GetElement(0).Name; }
        }

        /// <inheritdoc />
        public override RenderedPipelineStage<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedPipelineStage<TResult>(
                StageName,
                _document,
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A <see cref="String"/> based stage.
    /// </summary>
    public sealed class JsonStage<TDocument, TResult> : PipelineStage<TDocument, TResult>
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStage{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public JsonStage(string json, IBsonSerializer<TResult> resultSerializer = null)
        {
            _document = BsonDocument.Parse(Ensure.IsNotNull(json, "json"));
            _resultSerializer = resultSerializer;
        }

        /// <inheritdoc />
        public override string StageName
        {
            get { return _document.GetElement(0).Name; }
        }

        /// <inheritdoc />
        public override RenderedPipelineStage<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedPipelineStage<TResult>(
                StageName,
                _document,
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A delegated aggregate stage.
    /// </summary>
    public sealed class DelegatedAggregateStage<TDocument, TResult> : PipelineStage<TDocument, TResult>
    {
        private readonly string _stageName;
        private readonly Func<IBsonSerializer<TDocument>, IBsonSerializerRegistry, RenderedPipelineStage<TResult>> _renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatedAggregateStage{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="stageName">Name of the stage.</param>
        /// <param name="renderer">The renderer.</param>
        public DelegatedAggregateStage(string stageName, Func<IBsonSerializer<TDocument>, IBsonSerializerRegistry, RenderedPipelineStage<TResult>> renderer)
        {
            _stageName = stageName;
            _renderer = renderer;
        }

        /// <inheritdoc />
        public override string StageName
        {
            get { return _stageName; }
        }

        /// <inheritdoc />
        public override RenderedPipelineStage<TResult> Render(IBsonSerializer<TDocument> serializer, IBsonSerializerRegistry serializerRegistry)
        {
            return _renderer(serializer, serializerRegistry);
        }
    }
}