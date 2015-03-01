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
        /// Gets the name of the pipeline operator.
        /// </summary>
        /// <value>
        /// The name of the pipeline operator.
        /// </value>
        string OperatorName { get; }

        /// <summary>
        /// Gets the document.
        /// </summary>
        BsonDocument Document { get; }

        /// <summary>
        /// Gets the output serializer.
        /// </summary>
        IBsonSerializer OutputSerializer { get; }
    }

    /// <summary>
    /// A rendered pipeline stage.
    /// </summary>
    public class RenderedPipelineStage<TOutput> : IRenderedPipelineStage
    {
        private string _operatorName;
        private BsonDocument _document;
        private IBsonSerializer<TOutput> _outputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedPipelineStage{TOutput}"/> class.
        /// </summary>
        /// <param name="operatorName">Name of the pipeline operator.</param>
        /// <param name="document">The document.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public RenderedPipelineStage(string operatorName, BsonDocument document, IBsonSerializer<TOutput> outputSerializer)
        {
            _operatorName = Ensure.IsNotNull(operatorName, "operatorName");
            _document = Ensure.IsNotNull(document, "document");
            _outputSerializer = Ensure.IsNotNull(outputSerializer, "outputSerializer");
        }

        /// <inheritdoc />
        public BsonDocument Document
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets the output serializer.
        /// </summary>
        public IBsonSerializer<TOutput> OutputSerializer
        {
            get { return _outputSerializer; }
        }

        /// <inheritdoc />
        public string OperatorName
        {
            get { return _operatorName; }
        }

        /// <inheritdoc />
        IBsonSerializer IRenderedPipelineStage.OutputSerializer
        {
            get { return _outputSerializer; }
        }
    }

    /// <summary>
    /// A pipeline stage.
    /// </summary>
    public interface IPipelineStage
    {
        /// <summary>
        /// Gets the type of the input.
        /// </summary>
        Type InputType { get; }

        /// <summary>
        /// Gets the name of the pipeline operator.
        /// </summary>
        string OperatorName { get; }

        /// <summary>
        /// Gets the type of the output.
        /// </summary>
        Type OutputType { get; }

        /// <summary>
        /// Renders the specified document serializer.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>An <see cref="IRenderedPipelineStage" /></returns>
        IRenderedPipelineStage Render(IBsonSerializer inputSerializer, IBsonSerializerRegistry serializerRegistry);
    }

    /// <summary>
    /// Base class for pipeline stages.
    /// </summary>
    public abstract class PipelineStage<TInput, TOutput> : IPipelineStage
    {
        /// <summary>
        /// Gets the type of the input.
        /// </summary>
        Type IPipelineStage.InputType
        {
            get { return typeof(TInput); }
        }

        /// <inheritdoc />
        public abstract string OperatorName { get; }

        /// <summary>
        /// Gets the type of the output.
        /// </summary>
        Type IPipelineStage.OutputType
        {
            get { return typeof(TOutput); }
        }

        /// <summary>
        /// Renders the specified document serializer.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="RenderedPipelineStage{TOutput}" /></returns>
        public abstract RenderedPipelineStage<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="PipelineStage{TInput, TOutput}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineStage<TInput, TOutput>(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }

            return new BsonDocumentPipelineStage<TInput, TOutput>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="PipelineStage{TInput, TOutput}" />.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PipelineStage<TInput, TOutput>(string json)
        {
            if (json == null)
            {
                return null;
            }

            return new JsonPipelineStage<TInput, TOutput>(json);
        }

        /// <inheritdoc />
        IRenderedPipelineStage IPipelineStage.Render(IBsonSerializer inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return Render((IBsonSerializer<TInput>)inputSerializer, serializerRegistry);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument"/> based stage.
    /// </summary>
    public sealed class BsonDocumentPipelineStage<TInput, TOutput> : PipelineStage<TInput, TOutput>
    {
        private readonly BsonDocument _document;
        private readonly IBsonSerializer<TOutput> _outputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentPipelineStage{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public BsonDocumentPipelineStage(BsonDocument document, IBsonSerializer<TOutput> outputSerializer = null)
        {
            _document = Ensure.IsNotNull(document, "document");
            _outputSerializer = outputSerializer;
        }

        /// <inheritdoc />
        public override string OperatorName
        {
            get { return _document.GetElement(0).Name; }
        }

        /// <inheritdoc />
        public override RenderedPipelineStage<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedPipelineStage<TOutput>(
                OperatorName,
                _document,
                _outputSerializer ?? (inputSerializer as IBsonSerializer<TOutput>) ?? serializerRegistry.GetSerializer<TOutput>());
        }
    }

    /// <summary>
    /// A JSON <see cref="String"/> based pipeline stage.
    /// </summary>
    public sealed class JsonPipelineStage<TInput, TOutput> : PipelineStage<TInput, TOutput>
    {
        private readonly BsonDocument _document;
        private readonly string _json;
        private readonly IBsonSerializer<TOutput> _outputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPipelineStage{TInput, TOutput}" /> class.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public JsonPipelineStage(string json, IBsonSerializer<TOutput> outputSerializer = null)
        {
            _json = Ensure.IsNotNullOrEmpty(json, "json");
            _outputSerializer = outputSerializer;

            _document = BsonDocument.Parse(json);
        }

        /// <summary>
        /// Gets the json.
        /// </summary>
        public string Json
        {
            get { return _json; }
        }

        /// <inheritdoc />
        public override string OperatorName
        {
            get { return _document.GetElement(0).Name; }
        }

        /// <summary>
        /// Gets the output serializer.
        /// </summary>
        public IBsonSerializer<TOutput> OutputSerializer
        {
            get { return _outputSerializer; }
        }

        /// <inheritdoc />
        public override RenderedPipelineStage<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedPipelineStage<TOutput>(
                OperatorName,
                BsonDocument.Parse(_json),
                _outputSerializer ?? (inputSerializer as IBsonSerializer<TOutput>) ?? serializerRegistry.GetSerializer<TOutput>());
        }
    }

    internal sealed class DelegatedPipelineStage<TInput, TOutput> : PipelineStage<TInput, TOutput>
    {
        private readonly string _operatorName;
        private readonly Func<IBsonSerializer<TInput>, IBsonSerializerRegistry, RenderedPipelineStage<TOutput>> _renderer;

        public DelegatedPipelineStage(string operatorName, Func<IBsonSerializer<TInput>, IBsonSerializerRegistry, RenderedPipelineStage<TOutput>> renderer)
        {
            _operatorName = operatorName;
            _renderer = renderer;
        }

        public override string OperatorName
        {
            get { return _operatorName; }
        }

        public override RenderedPipelineStage<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return _renderer(inputSerializer, serializerRegistry);
        }
    }
}