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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// An interface representing methods used to create, delete and modify indexes.
    /// </summary>
    /// <remarks>
    /// This interface is not guaranteed to remain stable. Implementors should use
    /// <see cref="MongoIndexManagerBase{TDocument}"/>.
    /// </remarks>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public interface IMongoIndexManager<TDocument>
    {
        /// <summary>
        /// Gets the namespace of the collection.
        /// </summary>
        CollectionNamespace CollectionNamespace { get; }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        IBsonSerializer<TDocument> DocumentSerializer { get; }

        /// <summary>
        /// Gets the collection settings.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task CreateAsync(IndexKeysDefinition<TDocument> keys, CreateIndexOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Drops an index.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task DropAsync(IndexKeysDefinition<TDocument> keys, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Drops an index by its name.
        /// </summary>
        /// <param name="name">The name of the index to drop.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task DropByNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists the indexes.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        Task<IAsyncCursor<BsonDocument>> ListAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
