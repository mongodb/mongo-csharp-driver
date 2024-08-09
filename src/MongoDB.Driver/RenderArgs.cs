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

using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Encapsulates settings needed for path rendering.
    /// </summary>
    public record struct PathRenderArgs(string PathPrefix = null, bool AllowScalarValueForArray = false)
    {
    }

    /// <summary>
    /// Encapsulates settings needed for rendering Builder definitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public record struct RenderArgs<TDocument>
    {
        private readonly IBsonSerializer<TDocument> _documentSerializer = default;
        private readonly bool _renderForFind = false;
        private readonly PathRenderArgs _pathRenderArgs = default;
        private readonly bool _renderDollarForm = default;
        private readonly IBsonSerializerRegistry _serializerRegistry = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderArgs{TDocument}"/> record.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="pathRenderArgs">The path render arguments.</param>
        /// <param name="renderDollarForm">Value that specifies whether full dollar for should be rendered.</param>
        /// <param name="renderForFind">Value that specifies whether rendering a find operation.</param>
        public RenderArgs(
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            PathRenderArgs pathRenderArgs = default,
            bool renderDollarForm = default,
            bool renderForFind = false)
        {
            DocumentSerializer = documentSerializer;
            PathRenderArgs = pathRenderArgs;
            SerializerRegistry = serializerRegistry;
            RenderDollarForm = renderDollarForm;
            _renderForFind = renderForFind;
        }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        public readonly IBsonSerializer<TDocument> DocumentSerializer
        {
            get => _documentSerializer;
            init => _documentSerializer = Ensure.IsNotNull(value, nameof(value));
        }

        /// <summary>
        /// Gets the value indicating whether Render is being called for Find.
        /// </summary>
        public readonly bool RenderForFind { get => _renderForFind; init => _renderForFind = value; }

        /// <summary>
        /// Gets the path render arguments.
        /// </summary>
        public readonly PathRenderArgs PathRenderArgs { get => _pathRenderArgs; init => _pathRenderArgs = value; }

        /// <summary>
        /// Gets the value indicating whether full dollar form should be rendered.
        /// </summary>
        public readonly bool RenderDollarForm { get => _renderDollarForm; init => _renderDollarForm = value; }

        /// <summary>
        /// Gets the serializer registry.
        /// </summary>
        public readonly IBsonSerializerRegistry SerializerRegistry
        {
            get => _serializerRegistry;
            init => _serializerRegistry = Ensure.IsNotNull(value, nameof(value));
        }

        /// <summary>
        /// Returns <see cref="DocumentSerializer"/> if it implements <c>IBsonSerializer{T}</c>
        /// or resolves <c>IBsonSerializer{T}</c> from <see cref="SerializerRegistry"/>.
        /// </summary>
        public readonly IBsonSerializer<T> GetSerializer<T>() =>
            _documentSerializer as IBsonSerializer<T> ?? SerializerRegistry.GetSerializer<T>();

        /// <summary>
        /// Creates a new RenderArgs with new document type {TNewDocument}
        /// </summary>
        /// <param name="serializer">The new serializer.</param>
        /// <returns>
        /// A new RenderArgs{TNewDocument} instance.
        /// </returns>
        public readonly RenderArgs<TNewDocument> WithNewDocumentType<TNewDocument>(IBsonSerializer<TNewDocument> serializer) =>
            new(serializer, SerializerRegistry, PathRenderArgs);
    }
}
