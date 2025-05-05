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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Base class for search definitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class SearchDefinition<TDocument>
    {
        /// <summary>
        /// Renders the search definition to a <see cref="BsonDocument" />.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>
        /// A <see cref="BsonDocument" />.
        /// </returns>
        public abstract BsonDocument Render(RenderArgs<TDocument> args);

        /// <summary>
        /// Performs an implicit conversion from a BSON document to a <see cref="SearchDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="document">The BSON document specifying the search definition.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchDefinition<TDocument>(BsonDocument document) =>
            document != null ? new BsonDocumentSearchDefinition<TDocument>(document) : null;

        /// <summary>
        /// Performs an implicit conversion from a string to a <see cref="SearchDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="json">The string specifying the search definition in JSON.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchDefinition<TDocument>(string json) =>
            json != null ? new JsonSearchDefinition<TDocument>(json) : null;
    }

    /// <summary>
    /// Extension methods for SearchDefinition.
    /// </summary>
    public static class SearchDefinitionExtensions
    {
        /// <summary>
        /// Determines whether to use the configured serializers for the specified <see cref="SearchDefinition{TDocument}"/>.
        /// When set to true (the default value), the configured serializers will be used to serialize the values of certain Atlas Search operators, such as "Equals", "In" and "Range".
        /// If set to false, then a default conversion will be used.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="searchDefinition">The search definition instance.</param>
        /// <param name="useConfiguredSerializers">Whether to use the configured serializers or not.</param>
        /// <returns>The same <see cref="SearchDefinition{TDocument}"/> instance with default serialization enabled.</returns>
        public static SearchDefinition<TDocument> UseConfiguredSerializers<TDocument>(this SearchDefinition<TDocument> searchDefinition, bool useConfiguredSerializers)
        {
            if (searchDefinition is not OperatorSearchDefinition<TDocument> operatorSearchDefinition)
            {
                throw new NotSupportedException($"{nameof(UseConfiguredSerializers)} cannot be used with SearchDefinition type: {searchDefinition.GetType()}.");
            }

            operatorSearchDefinition.SetUseConfiguredSerializers(useConfiguredSerializers);
            return operatorSearchDefinition;
        }
    }

    /// <summary>
    /// A search definition based on a BSON document.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BsonDocumentSearchDefinition<TDocument> : SearchDefinition<TDocument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentSearchDefinition{TDocument}"/> class.
        /// </summary>
        /// <param name="document">The BSON document specifying the search definition.</param>
        public BsonDocumentSearchDefinition(BsonDocument document)
        {
            Document = Ensure.IsNotNull(document, nameof(document));
        }

        /// <summary>
        /// Gets the BSON document.
        /// </summary>
        public BsonDocument Document { get; private set; }

        /// <inheritdoc />
        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            (BsonDocument)Document.Clone();
    }

    /// <summary>
    /// A search definition based on a JSON string.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class JsonSearchDefinition<TDocument> : SearchDefinition<TDocument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSearchDefinition{TDocument}"/> class.
        /// </summary>
        /// <param name="json">The JSON string specifying the search definition.</param>
        public JsonSearchDefinition(string json)
        {
            Json = Ensure.IsNotNullOrEmpty(json, nameof(json));
        }

        /// <summary>
        /// Gets the JSON string.
        /// </summary>
        public string Json { get; private set; }

        /// <inheritdoc />
        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            BsonDocument.Parse(Json);
    }

    internal abstract class OperatorSearchDefinition<TDocument> : SearchDefinition<TDocument>
    {
        private protected enum OperatorType
        {
            Autocomplete,
            Compound,
            EmbeddedDocument,
            Equals,
            Exists,
            Facet,
            GeoShape,
            GeoWithin,
            In,
            MoreLikeThis,
            Near,
            Phrase,
            QueryString,
            Range,
            Regex,
            Span,
            Text,
            Wildcard
        }

        private readonly OperatorType _operatorType;
        // _path and _score used by many but not all subclasses
        protected readonly SearchPathDefinition<TDocument> _path;
        protected readonly SearchScoreDefinition<TDocument> _score;

        protected bool _useConfiguredSerializers = true;

        private protected OperatorSearchDefinition(OperatorType operatorType)
            : this(operatorType, null)
        {
        }

        private protected OperatorSearchDefinition(OperatorType operatorType, SearchScoreDefinition<TDocument> score)
        {
            _operatorType = operatorType;
            _score = score;
        }

        private protected OperatorSearchDefinition(OperatorType operatorType, SearchPathDefinition<TDocument> path, SearchScoreDefinition<TDocument> score)
        {
            _operatorType = operatorType;
            _path = Ensure.IsNotNull(path, nameof(path));
            _score = score;
        }

        /// <inheritdoc />
        public override BsonDocument Render(RenderArgs<TDocument> args)
        {
            BsonDocument renderedArgs;

            if (_path is null)
            {
                renderedArgs = RenderArguments(args, null);
            }
            else
            {
                var renderedPath = _path.RenderAndGetFieldSerializer(args, out var fieldSerializer);
                renderedArgs = RenderArguments(args, fieldSerializer);
                renderedArgs.Add("path", renderedPath);
            }

            renderedArgs.Add("score", () => _score.Render(args), _score != null);
            return new(_operatorType.ToCamelCase(), renderedArgs);
        }

        private protected virtual BsonDocument RenderArguments(
            RenderArgs<TDocument> args,
            IBsonSerializer fieldSerializer) => new();

        internal void SetUseConfiguredSerializers(bool useConfiguredSerializers)
        {
            _useConfiguredSerializers = useConfiguredSerializers;
        }

        protected static BsonValue ToBsonValue<T>(T value) =>
            value switch
            {
                bool v => (BsonBoolean)v,
                sbyte v => (BsonInt32)v,
                byte v => (BsonInt32)v,
                short v => (BsonInt32)v,
                ushort v => (BsonInt32)v,
                int v => (BsonInt32)v,
                uint v => (BsonInt64)v,
                long v => (BsonInt64)v,
                float v => (BsonDouble)v,
                double v => (BsonDouble)v,
                decimal v => (BsonDecimal128)v,
                DateTime v => (BsonDateTime)v,
                DateTimeOffset v => (BsonDateTime)v.UtcDateTime,
                ObjectId v => (BsonObjectId)v,
                Guid v => new BsonBinaryData(v, GuidRepresentation.Standard),
                string v => (BsonString)v,
                null => BsonNull.Value,
                _ => throw new InvalidCastException()
            };
    }
}
