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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// An interface representing methods used to create, delete and modify indexes.
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
        /// Creates the indexes.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task CreateIndexAsync(object keys, CreateIndexOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Drops the index asynchronous.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task DropIndexAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Drops the index asynchronous.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task DropIndexAsync(object keys, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the indexes asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of index definitions.</returns>
        Task<IAsyncCursor<BsonDocument>> GetIndexesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
