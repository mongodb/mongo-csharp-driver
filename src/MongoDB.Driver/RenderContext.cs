using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Encapsulates classes needed for rendering Builder definitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed record RenderContext<TDocument>
    {
        private IBsonSerializer<TDocument> _documentSerializer;
        private LinqProvider _linqProvider;
        private string _pathPrefix;
        private IBsonSerializerRegistry _serializerRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext{TDocument}"/> record.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="linqProvider">The LINQ provider.</param>
        /// <param name="pathPrefix">The path prefix.</param>
        public RenderContext(
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            LinqProvider linqProvider = LinqProvider.V3,
            string pathPrefix = null)
        {
            DocumentSerializer = documentSerializer;
            LinqProvider = linqProvider;
            PathPrefix = pathPrefix;
            SerializerRegistry = Ensure.IsNotNull(serializerRegistry, nameof(serializerRegistry));
        }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        public IBsonSerializer<TDocument> DocumentSerializer
        {
            get => _documentSerializer;
            init => _documentSerializer = Ensure.IsNotNull(value, nameof(value));
        }

        /// <summary>
        /// Gets the linq provider.
        /// </summary>
        public LinqProvider LinqProvider { get => _linqProvider; init => _linqProvider = value; }

        /// <summary>
        /// Gets the path prefix.
        /// </summary>
        public string PathPrefix { get => _pathPrefix; init => _pathPrefix = value; }

        /// <summary>
        /// Gets the serializer registry.
        /// </summary>
        public IBsonSerializerRegistry SerializerRegistry
        {
            get => _serializerRegistry;
            init => _serializerRegistry = Ensure.IsNotNull(value, nameof(value));
        }
    }
}
