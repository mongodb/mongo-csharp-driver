using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Connections
{
    /// <inheritdoc/>
    [Obsolete("Use HelloResult instead.")]
    public sealed class IsMasterResult : HelloResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsMasterResult"/> class.
        /// </summary>
        /// <param name="wrapped">The wrapped result document.</param>
        public IsMasterResult(BsonDocument wrapped) : base(wrapped)
        {
        }
    }
}
