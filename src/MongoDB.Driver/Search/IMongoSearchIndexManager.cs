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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// An interface representing methods used to create, delete and modify search indexes.
    /// </summary>
    public interface IMongoSearchIndexManager
    {
        /// <summary>
        /// Creates multiple indexes.
        /// </summary>
        /// <param name="models">The models defining each of the indexes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// An <see cref="IEnumerable{String}" /> of the names of the indexes that were created.
        /// </returns>
        IEnumerable<string> CreateMany(IEnumerable<CreateSearchIndexModel> models, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates multiple indexes.
        /// </summary>
        /// <param name="models">The models defining each of the indexes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A Task whose result is an <see cref="IEnumerable{String}" /> of the names of the indexes that were created.
        /// </returns>
        Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateSearchIndexModel> models, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a search index.
        /// </summary>
        /// <param name="definition">The index definition.</param>
        /// <param name="name">The index name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The name of the index that was created.
        /// </returns>
        string CreateOne(BsonDocument definition, string name = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a search index.
        /// </summary>
        /// <param name="model">The model defining the index.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The name of the index that was created.
        /// </returns>
        string CreateOne(CreateSearchIndexModel model, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a search index.
        /// </summary>
        /// <param name="definition">The index definition.</param>
        /// <param name="name">The index name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The name of the index that was created.
        /// </returns>
        Task<string> CreateOneAsync(BsonDocument definition, string name = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a search index.
        /// </summary>
        /// <param name="model">The model defining the index.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A Task whose result is the name of the index that was created.
        /// </returns>
        Task<string> CreateOneAsync(CreateSearchIndexModel model, CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops an index by its name.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void DropOne(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops an index by its name.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        Task DropOneAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists the search indexes.
        /// </summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="aggregateOptions">The aggregate options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A cursor.
        /// </returns>
        IAsyncCursor<BsonDocument> List(string name = null, AggregateOptions aggregateOptions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists the search indexes.
        /// </summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="aggregateOptions">The aggregate options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A Task whose result is a cursor.
        /// </returns>
        Task<IAsyncCursor<BsonDocument>> ListAsync(string name = null, AggregateOptions aggregateOptions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the search index.
        /// </summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void Update(string name, BsonDocument definition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the search index.
        /// </summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        Task UpdateAsync(string name, BsonDocument definition, CancellationToken cancellationToken = default);
    }
}
