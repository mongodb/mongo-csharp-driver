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
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class Pipeline<TDocument, TResult>
    {
        /// <summary>
        /// Renders the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedPipeline{TDocument}"/></returns>
        public abstract RenderedPipeline<TResult> Render(IBsonSerializer<TDocument> serializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:IPipelineStage[]"/> to <see cref="Pipeline{TDocument, TResult}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument, TResult>(IPipelineStage[] stages)
        {
            if (stages == null)
            {
                return null;
            }

            return new PipelineStagePipeline<TDocument, TResult>(stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="List{IPipelineStage}"/> to <see cref="Pipeline{TDocument, TResult}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Pipeline<TDocument, TResult>(List<IPipelineStage> stages)
        {
            if (stages == null)
            {
                return null;
            }

            return new PipelineStagePipeline<TDocument, TResult>(stages);
        }
    }

    /// <summary>
    /// A pipeline composed of <see cref="PipelineStage{TDocument, TResult}" />.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class PipelineStagePipeline<TDocument, TResult> : Pipeline<TDocument, TResult>
    {
        private readonly IList<IPipelineStage> _stages;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineStagePipeline{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public PipelineStagePipeline(IEnumerable<IPipelineStage> stages, IBsonSerializer<TResult> resultSerializer = null)
        {
            _stages = Ensure.IsNotNull(stages, "stages").ToList();
            _resultSerializer = resultSerializer;
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer<TResult> Serializer
        {
            get { return _resultSerializer; }
        }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public IList<IPipelineStage> Stages
        {
            get { return _stages; }
        }

        /// <inheritdoc />
        public override RenderedPipeline<TResult> Render(IBsonSerializer<TDocument> serializer, IBsonSerializerRegistry serializerRegistry)
        {
            var pipeline = new List<BsonDocument>();
            IBsonSerializer currentSerializer = serializer;
            foreach (var stage in _stages)
            {
                var renderedStage = stage.Render(currentSerializer, serializerRegistry);
                currentSerializer = renderedStage.Serializer;
                pipeline.Add(renderedStage.Document);
            }

            return new RenderedPipeline<TResult>(
                pipeline,
                _resultSerializer ?? (currentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }
}