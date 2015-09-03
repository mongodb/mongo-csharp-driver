/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered pipeline.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public class RenderedPipelineDefinition<TOutput>
    {
        private List<BsonDocument> _documents;
        private IBsonSerializer<TOutput> _outputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedPipelineDefinition{TOutput}"/> class.
        /// </summary>
        /// <param name="documents">The pipeline.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public RenderedPipelineDefinition(IEnumerable<BsonDocument> documents, IBsonSerializer<TOutput> outputSerializer)
        {
            _documents = Ensure.IsNotNull(documents, nameof(documents)).ToList();
            _outputSerializer = Ensure.IsNotNull(outputSerializer, nameof(outputSerializer));
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
        public IBsonSerializer<TOutput> OutputSerializer
        {
            get { return _outputSerializer; }
        }
    }

    /// <summary>
    /// Base class for a pipeline.
    /// </summary>
    /// <typeparam name="TInput">The type of the input.</typeparam>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public abstract class PipelineDefinition<TInput, TOutput>
    {
        /// <summary>
        /// Renders the pipeline.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedPipelineDefinition{TOutput}"/></returns>
        public abstract RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="IPipelineStageDefinition"/>[] to <see cref="PipelineDefinition{TInput, TOutput}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineDefinition<TInput, TOutput>(IPipelineStageDefinition[] stages)
        {
            if (stages == null)
            {
                return null;
            }

            return new PipelineStagePipelineDefinition<TInput, TOutput>(stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="List{IPipelineStage}"/> to <see cref="PipelineDefinition{TInput, TOutput}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineDefinition<TInput, TOutput>(List<IPipelineStageDefinition> stages)
        {
            if (stages == null)
            {
                return null;
            }

            return new PipelineStagePipelineDefinition<TInput, TOutput>(stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/>[] to <see cref="PipelineDefinition{TInput, TOutput}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineDefinition<TInput, TOutput>(BsonDocument[] stages)
        {
            if (stages == null)
            {
                return null;
            }

            return new BsonDocumentStagePipelineDefinition<TInput, TOutput>(stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="List{BsonDocument}"/> to <see cref="PipelineDefinition{TInput, TOutput}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineDefinition<TInput, TOutput>(List<BsonDocument> stages)
        {
            if (stages == null)
            {
                return null;
            }

            return new BsonDocumentStagePipelineDefinition<TInput, TOutput>(stages);
        }
    }

    /// <summary>
    /// A pipeline defined by combining two pipeline definitions.
    /// </summary>
    /// <typeparam name="TInput">The type of the input.</typeparam>
    /// <typeparam name="TIntermediateOutput">The type of the intermediate output.</typeparam>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public sealed class CombinedPipelineDefinition<TInput, TIntermediateOutput, TOutput> : PipelineDefinition<TInput, TOutput>
    {
        private PipelineDefinition<TInput, TIntermediateOutput> _first;
        private PipelineDefinition<TIntermediateOutput, TOutput> _second;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedPipelineDefinition{TInput, TIntermediateOutput, TOutput}"/> class.
        /// </summary>
        /// <param name="first">The first pipeline.</param>
        /// <param name="second">The second pipeline.</param>
        public CombinedPipelineDefinition(PipelineDefinition<TInput, TIntermediateOutput> first, PipelineDefinition<TIntermediateOutput, TOutput> second)
        {
            _first = Ensure.IsNotNull(first, nameof(first));
            _second = Ensure.IsNotNull(second, nameof(second));
        }

        /// <summary>
        /// Gets the first pipeline.
        /// </summary>
        public PipelineDefinition<TInput, TIntermediateOutput> First
        {
            get { return _first; }
        }

        /// <summary>
        /// Gets the second pipeline.
        /// </summary>
        public PipelineDefinition<TIntermediateOutput, TOutput> Second
        {
            get { return _second; }
        }

        /// <inheritdoc />
        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFirst = _first.Render(inputSerializer, serializerRegistry);
            var renderedSecond = _second.Render(renderedFirst.OutputSerializer, serializerRegistry);

            return new RenderedPipelineDefinition<TOutput>(
                renderedFirst.Documents.Concat(renderedSecond.Documents),
                renderedSecond.OutputSerializer);
        }
    }

    /// <summary>
    /// A pipeline composed of instances of <see cref="BsonDocument"/>.
    /// </summary>
    /// <typeparam name="TInput">The type of the input.</typeparam>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public sealed class BsonDocumentStagePipelineDefinition<TInput, TOutput> : PipelineDefinition<TInput, TOutput>
    {
        private readonly IBsonSerializer<TOutput> _outputSerializer;
        private readonly List<BsonDocument> _stages;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentStagePipelineDefinition{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public BsonDocumentStagePipelineDefinition(IEnumerable<BsonDocument> stages, IBsonSerializer<TOutput> outputSerializer = null)
        {
            _stages = Ensure.IsNotNull(stages, nameof(stages)).ToList();
            _outputSerializer = outputSerializer;
        }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public IList<BsonDocument> Stages
        {
            get { return _stages; }
        }

        /// <inheritdoc />
        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedPipelineDefinition<TOutput>(
                _stages,
                _outputSerializer ?? (inputSerializer as IBsonSerializer<TOutput>) ?? serializerRegistry.GetSerializer<TOutput>());
        }
    }

    /// <summary>
    /// A pipeline composed of instances of <see cref="IPipelineStageDefinition" />.
    /// </summary>
    /// <typeparam name="TInput">The type of the input.</typeparam>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public sealed class PipelineStagePipelineDefinition<TInput, TOutput> : PipelineDefinition<TInput, TOutput>
    {
        private readonly IList<IPipelineStageDefinition> _stages;
        private readonly IBsonSerializer<TOutput> _outputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineStagePipelineDefinition{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public PipelineStagePipelineDefinition(IEnumerable<IPipelineStageDefinition> stages, IBsonSerializer<TOutput> outputSerializer = null)
        {
            _stages = VerifyStages(Ensure.IsNotNull(stages, nameof(stages)).ToList());
            _outputSerializer = outputSerializer;
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer<TOutput> Serializer
        {
            get { return _outputSerializer; }
        }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public IList<IPipelineStageDefinition> Stages
        {
            get { return _stages; }
        }

        /// <inheritdoc />
        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var pipeline = new List<BsonDocument>();

            IBsonSerializer currentSerializer = inputSerializer;
            foreach (var stage in _stages)
            {
                var renderedStage = stage.Render(currentSerializer, serializerRegistry);
                currentSerializer = renderedStage.OutputSerializer;
                if (renderedStage.Document.ElementCount > 0)
                {
                    pipeline.Add(renderedStage.Document);
                }
            }

            return new RenderedPipelineDefinition<TOutput>(
                pipeline,
                _outputSerializer ?? (currentSerializer as IBsonSerializer<TOutput>) ?? serializerRegistry.GetSerializer<TOutput>());
        }

        private static List<IPipelineStageDefinition> VerifyStages(List<IPipelineStageDefinition> stages)
        {
            var nextInputType = typeof(TInput);
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].InputType != nextInputType)
                {
                    var message = string.Format(
                        "The input type to stage[{0}] was expected to be {1}, but was {2}.",
                        i,
                        nextInputType,
                        stages[i].InputType);
                    throw new ArgumentException(message, "stages");
                }

                nextInputType = stages[i].OutputType;
            }

            if (nextInputType != typeof(TOutput))
            {
                var message = string.Format(
                    "The output type to the last stage was expected to be {0}, but was {1}.",
                    nextInputType,
                    stages.Last().OutputType);
                throw new ArgumentException(message, "stages");
            }

            return stages;
        }
    }

    internal class OptimizingPipelineDefinition<TInput, TOutput> : PipelineDefinition<TInput, TOutput>
    {
        private readonly PipelineDefinition<TInput, TOutput> _wrapped;

        public OptimizingPipelineDefinition(PipelineDefinition<TInput, TOutput> wrapped)
        {
            _wrapped = wrapped;
        }

        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var rendered = _wrapped.Render(inputSerializer, serializerRegistry);

            // do some combining of $match documents if possible. This is optimized for the 
            // OfType case where we've added a discriminator as a match at the beginning of the pipeline.
            if (rendered.Documents.Count > 1)
            {
                var firstStage = rendered.Documents[0].GetElement(0);
                var secondStage = rendered.Documents[1].GetElement(0);
                if (firstStage.Name == "$match" && secondStage.Name == "$match")
                {
                    var combinedFilter = Builders<BsonDocument>.Filter.And(
                        (BsonDocument)firstStage.Value,
                        (BsonDocument)secondStage.Value);
                    var combinedStage = new BsonDocument("$match", combinedFilter.Render(BsonDocumentSerializer.Instance, serializerRegistry));

                    rendered.Documents[0] = combinedStage;
                    rendered.Documents.RemoveAt(1);
                }
            }

            return rendered;
        }
    }
}