/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Linq;

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
        /// Gets the output serializer.
        /// </summary>
        public abstract IBsonSerializer<TOutput> OutputSerializer { get; }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public abstract IEnumerable<IPipelineStageDefinition> Stages { get; }

        /// <summary>
        /// Renders the pipeline.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedPipelineDefinition{TOutput}"/></returns>
        public virtual RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return Render(inputSerializer, serializerRegistry, LinqProvider.V3);
        }

        /// <summary>
        /// Renders the pipeline.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="linqProvider">The LINQ provider.</param>
        /// <returns>A <see cref="RenderedPipelineDefinition{TOutput}"/></returns>
        public abstract RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider);

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(LinqProvider.V3);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="linqProvider">The LINQ provider.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(LinqProvider linqProvider)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<TInput>();
            return ToString(inputSerializer, serializerRegistry, linqProvider);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return ToString(inputSerializer, serializerRegistry, LinqProvider.V3);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="linqProvider">The LINQ provider.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var renderedPipeline = Render(inputSerializer, serializerRegistry, linqProvider);
            return $"[{string.Join(", ", renderedPipeline.Documents.Select(stage => stage.ToJson()))}]";
        }

        /// <summary>
        /// Creates a pipeline.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        /// <returns>A <see cref="PipelineDefinition{TInput, TOutput}"/>.</returns>
        public static PipelineDefinition<TInput, TOutput> Create(
            IEnumerable<IPipelineStageDefinition> stages,
            IBsonSerializer<TOutput> outputSerializer = null)
        {
            if (stages == null)
            {
                return null;
            }

            return new PipelineStagePipelineDefinition<TInput, TOutput>(stages, outputSerializer);
        }

        /// <summary>
        /// Creates a pipeline.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        /// <returns>A <see cref="PipelineDefinition{TInput, TOutput}"/>.</returns>
        public static PipelineDefinition<TInput, TOutput> Create(
            IEnumerable<BsonDocument> stages,
            IBsonSerializer<TOutput> outputSerializer = null)
        {
            if (stages == null)
            {
                return null;
            }

            return new BsonDocumentStagePipelineDefinition<TInput, TOutput>(stages, outputSerializer);
        }

        /// <summary>
        /// Creates a pipeline.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        /// <returns>A <see cref="PipelineDefinition{TInput, TOutput}"/>.</returns>
        public static PipelineDefinition<TInput, TOutput> Create(
            IEnumerable<string> stages,
            IBsonSerializer<TOutput> outputSerializer = null)
        {
            return Create(stages?.Select(s => BsonDocument.Parse(s)), outputSerializer);
        }

        /// <summary>
        /// Creates a pipeline.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>A <see cref="PipelineDefinition{TInput, TOutput}"/>.</returns>
        public static PipelineDefinition<TInput, TOutput> Create(
            params BsonDocument[] stages)
        {
            return Create((IEnumerable<BsonDocument>)stages);
        }

        /// <summary>
        /// Creates a pipeline.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>A <see cref="PipelineDefinition{TInput, TOutput}"/>.</returns>
        public static PipelineDefinition<TInput, TOutput> Create(
            params string[] stages)
        {
            return Create((IEnumerable<string>)stages);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="IPipelineStageDefinition"/>[] to <see cref="PipelineDefinition{TInput, TOutput}"/>.
        /// </summary>
        /// <param name="stages">The stages.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineDefinition<TInput, TOutput>(IPipelineStageDefinition[] stages)
        {
            return Create(stages);
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
            return Create(stages);
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
            return Create(stages);
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
            return Create(stages);
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

        /// <inheritdoc />
        public override IBsonSerializer<TOutput> OutputSerializer => _outputSerializer;

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public IList<BsonDocument> Documents
        {
            get { return _stages; }
        }

        /// <inheritdoc />
        public override IEnumerable<IPipelineStageDefinition> Stages => _stages.Select(s => new BsonDocumentPipelineStageDefinition<TInput, TOutput>(s, _outputSerializer));

        /// <inheritdoc />
        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
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

        /// <inheritdoc />
        public override IBsonSerializer<TOutput> OutputSerializer => _outputSerializer;

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        [Obsolete("Use OutputSerializer instead.")]
        public IBsonSerializer<TOutput> Serializer
        {
            get { return _outputSerializer; }
        }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        public override IEnumerable<IPipelineStageDefinition> Stages => _stages;

        /// <inheritdoc />
        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var pipeline = new List<BsonDocument>();

            IBsonSerializer currentSerializer = inputSerializer;
            foreach (var stage in _stages)
            {
                var renderedStage = stage.Render(currentSerializer, serializerRegistry, linqProvider);
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
            var expectedInputType = typeof(TInput);
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].InputType != expectedInputType)
                {
                    var message =
                        $"The input type to stage[{i}] was expected to be {expectedInputType}, but was {stages[i].InputType}.";
                    throw new ArgumentException(message, nameof(stages));
                }

                expectedInputType = stages[i].OutputType;
            }
            var lastStageOutputType = expectedInputType;

            if (lastStageOutputType != typeof(TOutput))
            {
                var message =
                    $"The output type to the last stage was expected to be {typeof(TOutput)}, but was {lastStageOutputType}.";
                throw new ArgumentException(message, nameof(stages));
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

        /// <inheritdoc />
        public override IBsonSerializer<TOutput> OutputSerializer => _wrapped.OutputSerializer;

        /// <inheritdoc />
        public override IEnumerable<IPipelineStageDefinition> Stages => _wrapped.Stages;

        public override RenderedPipelineDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var rendered = _wrapped.Render(inputSerializer, serializerRegistry, linqProvider);

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
                    var combinedStage = new BsonDocument("$match", combinedFilter.Render(BsonDocumentSerializer.Instance, serializerRegistry, linqProvider));

                    rendered.Documents[0] = combinedStage;
                    rendered.Documents.RemoveAt(1);
                }
            }

            return rendered;
        }
    }
}
