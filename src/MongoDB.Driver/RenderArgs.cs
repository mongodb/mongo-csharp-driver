using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

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
        private IBsonSerializer<TDocument> _documentSerializer = default;
        private LinqProvider _linqProvider = default;
        private IBsonSerializerRegistry _serializerRegistry = default;
        private PathRenderArgs _pathRenderArgs = default;
        private bool _renderDollarForm = default;
        private bool _isAggregateMode = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderArgs{TDocument}"/> record.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="linqProvider">The LINQ provider.</param>
        /// <param name="pathRenderArgs">The path render arguments.</param>
        /// <param name="renderDollarForm">Value that specifies whether full dollar for should be rendered.</param>
        /// <param name="isAggregateMode">Value that specifies whether rendering an aggregate pipeline.</param>
        public RenderArgs(
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            LinqProvider linqProvider = LinqProvider.V3,
            PathRenderArgs pathRenderArgs = default,
            bool renderDollarForm = default,
            bool isAggregateMode = true)
        {
            DocumentSerializer = documentSerializer;
            LinqProvider = linqProvider;
            PathRenderArgs = pathRenderArgs;
            SerializerRegistry = Ensure.IsNotNull(serializerRegistry, nameof(serializerRegistry));
            RenderDollarForm = renderDollarForm;
            _isAggregateMode = isAggregateMode;
        }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        public IBsonSerializer<TDocument> DocumentSerializer { get => _documentSerializer; init => _documentSerializer = value; }

        /// <summary>
        /// Gets the value indicating whether aggregate is being rendered.
        /// </summary>
        public bool IsAggregateMode { get => _isAggregateMode; init => _isAggregateMode = value; }

        /// <summary>
        /// Gets the linq provider.
        /// </summary>
        public LinqProvider LinqProvider { get => _linqProvider; init => _linqProvider = value; }

        /// <summary>
        /// Gets the path render arguments.
        /// </summary>
        public PathRenderArgs PathRenderArgs { get => _pathRenderArgs; init => _pathRenderArgs = value; }

        /// <summary>
        /// Gets the value indicating whether full dollar form should be rendered.
        /// </summary>
        public bool RenderDollarForm { get => _renderDollarForm; init => _renderDollarForm = value; }

        /// <summary>
        /// Gets the serializer registry.
        /// </summary>
        public IBsonSerializerRegistry SerializerRegistry
        {
            get => _serializerRegistry;
            init => _serializerRegistry = Ensure.IsNotNull(value, nameof(value));
        }

        /// <summary>
        /// Returns <see cref="DocumentSerializer"/> if it implements <c>IBsonSerializer{T}</c>
        /// or resolves <c>IBsonSerializer{T}</c> from <see cref="SerializerRegistry"/>.
        /// </summary>
        public IBsonSerializer<T> GetSerializer<T>() =>
            DocumentSerializer as IBsonSerializer<T> ?? SerializerRegistry.GetSerializer<T>();

        /// <summary>
        /// Creates a new RenderArgs with provided <c>IBsonSerializer{T}</c>.
        /// </summary>
        /// <param name="serializer">The new serializer.</param>
        /// <returns>
        /// A new RenderArgs{TNewDocument} instance.
        /// </returns>
        public RenderArgs<TNewDocument> WithSerializer<TNewDocument>(IBsonSerializer<TNewDocument> serializer) =>
            new(serializer, SerializerRegistry, LinqProvider, PathRenderArgs);
    }
}
