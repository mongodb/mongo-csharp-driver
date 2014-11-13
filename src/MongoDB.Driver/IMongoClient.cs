/* Copyright 2013-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// The client interface to MongoDB.
    /// </summary>
    public interface IMongoClient
    {
        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoClientSettings Settings { get; }

        /// <summary>
        /// Drops the database with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>An implementation of a database.</returns>
        IMongoDatabase GetDatabase(string name);

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An implementation of a database.</returns>
        IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings);

        /// <summary>
        /// Gets the database names.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of the database on the server.</returns>
        Task<IReadOnlyList<string>> GetDatabaseNamesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
