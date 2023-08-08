using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Encapsulates classes needed for rendering Search definitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchDefinitionRenderContext<TDocument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchDefinitionRenderContext{TDocument}"/> class.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="pathPrefix">The path prefix.</param>
        public SearchDefinitionRenderContext(
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            string pathPrefix = null)
        {
            DocumentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));
            PathPrefix = pathPrefix;
            SerializerRegistry = Ensure.IsNotNull(serializerRegistry, nameof(serializerRegistry));
        }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        public IBsonSerializer<TDocument> DocumentSerializer { get; }

        /// <summary>
        /// Gets the path prefix.
        /// </summary>
        public string PathPrefix { get; }

        /// <summary>
        /// Gets the serializer registry.
        /// </summary>
        public IBsonSerializerRegistry SerializerRegistry { get; }
    }
}
