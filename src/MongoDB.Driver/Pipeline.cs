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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered pipeline.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class RenderedPipeline<TDocument>
    {
        private List<BsonDocument> _documents;
        private IBsonSerializer<TDocument> _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedPipeline{TDocument}"/> class.
        /// </summary>
        /// <param name="documents">The pipeline.</param>
        /// <param name="serializer">The serializer.</param>
        public RenderedPipeline(IEnumerable<BsonDocument> documents, IBsonSerializer<TDocument> serializer)
        {
            _documents = Ensure.IsNotNull(documents, "pipeline").ToList();
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        /// <summary>
        /// Gets the documents.
        /// </summary>
        public IList<BsonDocument> Documents
        {
            get { return _documents; }
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
    /// Base class for a pipeline.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class Pipeline<TDocument>
    {
        /// <summary>
        /// Renders the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns></returns>
        public abstract RenderedPipeline<TDocument> Render(IBsonSerializer serializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:AggregateStage[]"/> to <see cref="Pipeline{TDocument}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument>(AggregateStage[] stages)
        {
            return new AggregateStagePipeline<TDocument>(stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="List{AggregateStage}"/> to <see cref="Pipeline{TDocument}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument>(List<AggregateStage> stages)
        {
            return new AggregateStagePipeline<TDocument>(stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:BsonDocument[]"/> to <see cref="Pipeline{TDocument}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument>(BsonDocument[] stages)
        {
            return new AggregateStagePipeline<TDocument>(stages.Select(x => (AggregateStage)x));
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="List{BsonDocument}"/> to <see cref="Pipeline{TDocument}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument>(List<BsonDocument> stages)
        {
            return new AggregateStagePipeline<TDocument>(stages.Select(x => (AggregateStage)x));
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:String[]"/> to <see cref="Pipeline{TDocument}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument>(string[] stages)
        {
            return new AggregateStagePipeline<TDocument>(stages.Select(x => (AggregateStage)x));
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="List{String}"/> to <see cref="Pipeline{TDocument}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument>(List<string> stages)
        {
            return new AggregateStagePipeline<TDocument>(stages.Select(x => (AggregateStage)x));
        }
    }

    /// <summary>
    /// A pipeline composed of <see cref="AggregateStage"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class AggregateStagePipeline<TDocument> : Pipeline<TDocument>
    {
        private readonly IList<AggregateStage> _stages;
        private readonly IBsonSerializer<TDocument> _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateStagePipeline{TDocument}"/> class.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="serializer">The serializer.</param>
        public AggregateStagePipeline(IEnumerable<AggregateStage> stages, IBsonSerializer<TDocument> serializer = null)
        {
            _stages = Ensure.IsNotNull(stages, "stages").ToList();
            _serializer = serializer;
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public IList<AggregateStage> Stages
        {
            get { return _stages; }
        }

        /// <inheritdoc />
        public override RenderedPipeline<TDocument> Render(IBsonSerializer serializer, IBsonSerializerRegistry serializerRegistry)
        {
            var pipeline = new List<BsonDocument>();
            foreach (var stage in _stages)
            {
                var renderedStage = stage.Render(serializer, serializerRegistry);
                serializer = renderedStage.Serializer;
                pipeline.Add(renderedStage.Document);
            }

            return new RenderedPipeline<TDocument>(
                pipeline,
                _serializer ?? (serializer as IBsonSerializer<TDocument>) ?? serializerRegistry.GetSerializer<TDocument>());
        }
    }

    /// <summary>
    /// A rendered aggregate stage.
    /// </summary>
    public class RenderedAggregateStage
    {
        private string _stageName;
        private BsonDocument _document;
        private IBsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedAggregateStage" /> class.
        /// </summary>
        /// <param name="stageName">Name of the stage.</param>
        /// <param name="document">The document.</param>
        /// <param name="serializer">The serializer.</param>
        public RenderedAggregateStage(string stageName, BsonDocument document, IBsonSerializer serializer)
        {
            _stageName = Ensure.IsNotNull(stageName, "stageName");
            _document = Ensure.IsNotNull(document, "document");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        public BsonDocument Document
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// Gets the name of the stage.
        /// </summary>
        /// <value>
        /// The name of the stage.
        /// </value>
        public string StageName
        {
            get { return _stageName; }
        }
    }

    /// <summary>
    /// Base class for aggregate stages.
    /// </summary>
    public abstract class AggregateStage
    {
        /// <summary>
        /// Gets the name of the stage.
        /// </summary>
        /// <value>
        /// The name of the stage.
        /// </value>
        public abstract string StageName { get; }

        /// <summary>
        /// Renders the stage to <see cref="RenderedAggregateStage"/>.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns></returns>
        public abstract RenderedAggregateStage Render(IBsonSerializer serializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="AggregateStage"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator AggregateStage(BsonDocument document)
        {
            return new BsonDocumentStage(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="AggregateStage"/>.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator AggregateStage(string json)
        {
            return new JsonStage(json);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument"/> based stage.
    /// </summary>
    public sealed class BsonDocumentStage : AggregateStage
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentStage"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="serializer">The serializer.</param>
        public BsonDocumentStage(BsonDocument document, IBsonSerializer serializer = null)
        {
            _document = Ensure.IsNotNull(document, "document");
            _serializer = serializer;
        }

        /// <inheritdoc />
        public override string StageName
        {
            get { return _document.GetElement(0).Name; }
        }

        /// <inheritdoc />
        public override RenderedAggregateStage Render(IBsonSerializer serializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedAggregateStage(StageName, _document, _serializer ?? serializer);
        }
    }

    /// <summary>
    /// A <see cref="String"/> based stage.
    /// </summary>
    public sealed class JsonStage : AggregateStage
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStage"/> class.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="serializer">The serializer.</param>
        public JsonStage(string json, IBsonSerializer serializer = null)
        {
            _document = BsonDocument.Parse(Ensure.IsNotNull(json, "json"));
            _serializer = serializer;
        }

        /// <inheritdoc />
        public override string StageName
        {
            get { return _document.GetElement(0).Name; }
        }

        /// <inheritdoc />
        public override RenderedAggregateStage Render(IBsonSerializer serializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedAggregateStage(StageName, _document, _serializer ?? serializer);
        }
    }

    /// <summary>
    /// A delegated aggregate stage.
    /// </summary>
    public sealed class DelegatedAggregateStage : AggregateStage
    {
        private readonly string _stageName;
        private readonly Func<IBsonSerializer, IBsonSerializerRegistry, RenderedAggregateStage> _renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatedAggregateStage"/> class.
        /// </summary>
        /// <param name="stageName">Name of the stage.</param>
        /// <param name="renderer">The renderer.</param>
        public DelegatedAggregateStage(string stageName, Func<IBsonSerializer, IBsonSerializerRegistry, RenderedAggregateStage> renderer)
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
        public override RenderedAggregateStage Render(IBsonSerializer serializer, IBsonSerializerRegistry serializerRegistry)
        {
            return _renderer(serializer, serializerRegistry);
        }
    }
}