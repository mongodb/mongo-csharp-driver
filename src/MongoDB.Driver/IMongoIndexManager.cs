using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// Logical representation of indexes in MongoDB.
    /// </summary>
    public interface IMongoIndexManager<TDocument>
    {
        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        CollectionNamespace CollectionNamespace { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Drops the index asynchronous.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task DropIndexAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Drops the index asynchronous.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task DropIndexAsync(object keys, CancellationToken cancellationToken = default(CancellationToken));
    }
}
